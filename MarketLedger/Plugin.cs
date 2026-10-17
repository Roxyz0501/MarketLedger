using System.Collections.Concurrent;
using System.Threading.Channels;
using Dalamud;
using Dalamud.Game.Command;
using Dalamud.Game.Network.Structures;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace MarketLedger;

public sealed partial class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly ICommandManager commands;
    private readonly CommandInfo commandInfo;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IMarketBoard market;
    private readonly IGameInventory inventory;
    private readonly NpcShopObserver npcShop;
    private readonly SpendingTracker spending=new();
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly LedgerStore store;
    private readonly PurchaseTracker tracker = new();
    private readonly Channel<Purchase> writes = Channel.CreateBounded<Purchase>(new BoundedChannelOptions(256) { SingleReader=true, FullMode=BoundedChannelFullMode.Wait });
    private readonly ConcurrentQueue<Purchase> failed = new();
    private readonly Task writer;
    private readonly CancellationTokenSource shutdown = new();
    private readonly string directory;
    private string[] recoveryFiles=[];
    private Task? recoveryTask;
    private volatile Character? actor;
    private volatile bool open;
    private bool disposed;
    private IFontHandle[] fonts = [];
    private string fontCharacters = "";
    private readonly HashSet<char> glyphs = new();
    private readonly ConcurrentDictionary<uint,string> itemNames = new();

    public Plugin(IDalamudPluginInterface pi,ICommandManager commands,IFramework framework,IClientState client,
        IPlayerState player,IMarketBoard market,IDataManager data,IPluginLog log,IGameInventory inventory,IGameInteropProvider interop)
    {
        this.pi=pi; this.commands=commands; this.framework=framework; this.client=client;
        this.player=player; this.market=market; this.data=data; this.log=log;
        this.inventory=inventory;
        config=pi.GetPluginConfig() as Configuration ?? new();
        string? game;
        try { game=client.ClientLanguage.ToString() switch {"Japanese"=>"ja","English"=>"en","German"=>"de","French"=>"fr",_=>null}; }
        catch { game=null; }
        string resolved=Loc.Resolve(config.Language,game,pi.UiLanguage);
        if(config.Language!=resolved) { config.Language=resolved; Save(); }
        Loc.Validate();
        directory=pi.GetPluginConfigDirectory();
        store=new LedgerStore(Path.Combine(directory,"market-ledger.db"));
        recoveryFiles=Directory.EnumerateFiles(directory,"recovery-*.json").Take(100).ToArray();
        writer=Task.Run(WriteLoop);
        RefreshActor();
        filter = new Filter(CharacterId:actor?.Id ?? "",From:new DateTimeOffset(DateTime.Today.AddDays(-29)).ToUnixTimeSeconds());
        selectedCharacter=filter.CharacterId;
        from=DateTime.Today.AddDays(-29).ToString("yyyy-MM-dd");
        UpdateFonts("");
        commandInfo=new CommandInfo((_,_)=>Open()) {HelpMessage=T("CommandHelp")};
        commands.AddHandler("/marketledger",commandInfo);
        commands.AddHandler("/gilledger",commandInfo);
        npcShop=new NpcShopObserver(interop,spending.Request,()=>config.Recording && !disposed?actor:null,log);
        inventory.InventoryChangedRaw+=OnInventory;
        framework.Update+=OnUpdate;
        market.OfferingsReceived+=OnOfferings;
        market.PurchaseRequested+=OnRequested;
        market.ItemPurchased+=OnPurchased;
        pi.UiBuilder.Draw+=Draw;
        pi.UiBuilder.OpenMainUi+=Open;
        pi.UiBuilder.OpenConfigUi+=Open;
    }
    private void Open() => open=true;
    private string T(string key)=>Loc.Get(config.Language,key);
    private void Save()=>pi.SavePluginConfig(config);
    private void RefreshActor()
    {
        ulong id=client.IsLoggedIn && player.IsLoaded?player.ContentId:0;
        uint worldId=id!=0?player.CurrentWorld.RowId:0;
        if(actorId==id && actorWorld==worldId) return;
        Character? next=null;
        if(id!=0)
            next=new(player.ContentId.ToString(),player.CharacterName,player.HomeWorld.Value.Name.ToString(),player.CurrentWorld.Value.Name.ToString());
        if(actor?.Id!=next?.Id || actor?.World!=next?.World)
        {
            tracker.Clear(); DrainSpending(DateTimeOffset.UtcNow,true); spending.Clear();
            inventoryReadyAt=DateTimeOffset.UtcNow.AddSeconds(2);
        }
        actorId=id; actorWorld=worldId;
        if(actor!=next) actor=next;
    }
    private void OnUpdate(IFramework _)
    {
        if(disposed) return;
        RefreshActor();
        var now=DateTimeOffset.UtcNow;
        if(spending.HasPending && now>=nextDrain) { nextDrain=now.AddMilliseconds(250); DrainSpending(now); }
    }
    private void OnOfferings(IMarketBoardCurrentOfferings offers)
    {
        if(!config.Recording || actor==null || disposed) return;
        var now=DateTimeOffset.UtcNow;
        tracker.Offer(offers.ItemListings.Select(x=>new Listing(x.ListingId,x.RetainerId,NormalizeItem(x.ItemId),x.IsHq,
            x.ItemQuantity,x.PricePerUnit,x.RetainerName,now)),now);
    }
    private static uint NormalizeItem(uint id)=>id>=1_000_000?id-1_000_000:id;
    private void OnRequested(IMarketBoardPurchaseHandler request)
    {
        var who=actor;
        if(!config.Recording || who==null || disposed) return;
        uint item=NormalizeItem(request.CatalogId);
        string name;
        try
        {
            if(itemNames.Count>=2048) itemNames.Clear();
            name=itemNames.GetOrAdd(item,id=>data.GetExcelSheet<Item>().GetRowOrDefault(id)?.Name.ToString() ?? id.ToString());
        }
        catch { name=item.ToString(); }
        spending.MarketRequested(request.ListingId.ToString(),who,DateTimeOffset.UtcNow);
        tracker.Request(new(request.ListingId,request.RetainerId,item,request.IsHq,request.ItemQuantity,
            request.PricePerUnit,request.TotalTax,request.RetainerCityId,who,name,DateTimeOffset.UtcNow));
    }
    private void OnPurchased(IMarketBoardPurchase purchase)
    {
        if(!config.Recording || disposed) return;
        var result=tracker.Confirm(purchase.CatalogId,purchase.ItemQuantity,actor,DateTimeOffset.UtcNow);
        if(result==null) return;
        spending.Known(result,DateTimeOffset.UtcNow);
        QueuePurchase(result);
    }
    private async Task WriteLoop()
    {
        await foreach(var purchase in writes.Reader.ReadAllAsync())
        {
            try { store.Insert(purchase); }
            catch(Exception e) { RecordFailure(purchase); log.Error(e,"Market Ledger could not persist a purchase."); }
        }
    }
    private void RecordFailure(Purchase purchase)
    {
        failed.Enqueue(purchase); open=true;
        if(failed.Count>=256) { config.Recording=false; tracker.Clear(); spending.Clear(); }
    }
    private void Recover()
    {
        if(recoveryTask!=null) return;
        recoveryTask=Task.Run(()=>
        {
            foreach(var path in recoveryFiles)
            {
                if(new FileInfo(path).Length>4*1024*1024) throw new InvalidDataException("Recovery file exceeds limit.");
                var purchases=System.Text.Json.JsonSerializer.Deserialize<Purchase[]>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty recovery data");
                if(purchases.Length>1024) throw new InvalidDataException("Recovery record count exceeds limit.");
                foreach(var purchase in purchases) store.Insert(purchase);
                File.Move(path,path+".recovered",false);
            }
        });
    }
    private void RetryWrites()
    {
        int count=failed.Count;
        for(int i=0;i<count && failed.TryDequeue(out var p);i++)
            if(!writes.Writer.TryWrite(p)) { failed.Enqueue(p); break; }
    }
    private void UpdateFonts(string text)
    {
        glyphs.UnionWith(Loc.FontText);
        glyphs.UnionWith(Enumerable.Range(32,224).Select(x=>(char)x));
        glyphs.UnionWith(text);
        // Bound the font cache; keep current visible text and all UI strings when it fills.
        if(glyphs.Count>8192) { glyphs.Clear(); glyphs.UnionWith(Loc.FontText); glyphs.UnionWith(Enumerable.Range(32,224).Select(x=>(char)x)); glyphs.UnionWith(text); }
        string current=new(glyphs.Order().ToArray());
        if(current==fontCharacters) return;
        fontCharacters=current;
        var ranges=current.ToGlyphRange();
        var previous=fonts;
        fonts=Enumerable.Range(0,4).Select(face=>pi.UiBuilder.FontAtlas.NewDelegateFontHandle(step=>step.OnPreBuild(tk=>
            tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular,new SafeFontConfig {SizePx=17,FontNo=face,GlyphRanges=ranges})))).ToArray();
        foreach(var font in previous) font.Dispose();
    }
    public void Dispose()
    {
        disposed=true;
        npcShop.Dispose(); inventory.InventoryChangedRaw-=OnInventory;
        market.OfferingsReceived-=OnOfferings; market.PurchaseRequested-=OnRequested; market.ItemPurchased-=OnPurchased;
        framework.Update-=OnUpdate;
        commands.RemoveHandler("/marketledger");
        commands.RemoveHandler("/gilledger");
        pi.UiBuilder.Draw-=Draw; pi.UiBuilder.OpenMainUi-=Open; pi.UiBuilder.OpenConfigUi-=Open;
        shutdown.Cancel();
        DrainSpending(DateTimeOffset.UtcNow,true);
        writes.Writer.TryComplete();
        writer.GetAwaiter().GetResult();
        try { queryTask?.GetAwaiter().GetResult(); exportTask?.GetAwaiter().GetResult(); } catch { }
        try { recoveryTask?.GetAwaiter().GetResult(); } catch { }
        // Preserve failed records for explicit recovery; never silently discard a failed database write.
        if(!failed.IsEmpty)
        {
            try { File.WriteAllText(Path.Combine(directory,$"recovery-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json"),System.Text.Json.JsonSerializer.Serialize(failed.ToArray())); }
            catch(Exception e) { log.Error(e,"Market Ledger could not save emergency recovery data."); }
        }
        foreach(var font in fonts) font.Dispose();
        store.Dispose(); shutdown.Dispose();
    }
}
