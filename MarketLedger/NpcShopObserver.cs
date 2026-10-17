using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace MarketLedger;

// Observes the game's own ordinary gil-shop request; never initiates a purchase or changes its arguments.
internal sealed unsafe class NpcShopObserver : IDisposable
{
    private readonly Hook<ShopEventHandler.Delegates.ExecuteBuy>? hook;
    private readonly Action<NpcRequest> observe;
    private readonly Func<Character?> character;
    private readonly IPluginLog log;
    public bool Available => hook?.IsEnabled==true;
    public NpcShopObserver(IGameInteropProvider interop,Action<NpcRequest> observe,Func<Character?> character,IPluginLog log)
    {
        this.observe=observe; this.character=character; this.log=log;
        try
        {
            nint address=(nint)ShopEventHandler.MemberFunctionPointers.ExecuteBuy;
            if(address==0) return;
            hook=interop.HookFromAddress<ShopEventHandler.Delegates.ExecuteBuy>(address,Detour);
            hook.Enable();
        }
        catch(Exception e) { hook?.Dispose(); hook=null; log.Error(e,"Gil Ledger: NPC purchase observer unavailable."); }
    }
    private void Detour(ShopEventHandler* handler,int count)
    {
        try
        {
            var who=character();
            if(who!=null && handler!=null && handler->CurrentMode==1 && !handler->IsTradingWithRetainer
                && count>0 && count<=999999 && handler->ItemsCount>0 && handler->ItemsCount<=60
                && handler->BuyItemIndex>=0 && handler->BuyItemIndex<handler->ItemsCount)
            {
                var item=handler->Items[handler->BuyItemIndex];
                if(item.ItemId!=0 && item.PriceBuy>0 && (long)count*item.PriceBuy<=999_999_999)
                    observe(new(Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,who,item.ItemId,
                        item.ItemName.ToString(),item.IsHQ,(uint)count,(uint)item.PriceBuy,handler->ShopName.ToString()));
            }
        }
        catch(Exception e) { log.Error(e,"Gil Ledger: could not observe NPC purchase request."); }
        // Exactly one original call, even if observation failed.
        hook!.Original(handler,count);
    }
    public void Dispose()=>hook?.Dispose();
}
