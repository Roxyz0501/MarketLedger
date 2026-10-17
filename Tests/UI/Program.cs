using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MarketLedger;

unsafe class Program
{
    static T Stub<T>() where T:class=>DispatchProxy.Create<T,Dummy>();
    static int checks;
    static void Check(bool value,string label) { checks++; if(!value) throw new Exception(label); }
    static void Main(string[] args)
    {
        string output=Path.GetFullPath(args[0]); Directory.CreateDirectory(output); Dummy.Directory=Path.Combine(output,"data");
        ImGui.CreateContext(); var io=ImGui.GetIO(); io.DisplaySize=new(1440,900); io.DeltaTime=1f/60;
        io.ConfigInputTrickleEventQueue=false; io.IniFilename=null;
        using(var plugin=new Plugin(Stub<IDalamudPluginInterface>(),Stub<ICommandManager>(),Stub<IFramework>(),Stub<IClientState>(),Stub<IPlayerState>(),Stub<IMarketBoard>(),Stub<IDataManager>(),Stub<IPluginLog>()))
        {
            var type=typeof(Plugin);
            object? Invoke(string method,params object?[] values)=>type.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(plugin,values);
            void Set(string name,object? value)=>type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(plugin,value);
            object? Get(string name)=>type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(plugin);
            var store=(LedgerStore)Get("store")!;
            var actor=new Character("1001","Sample Adventurer","Chocobo","Chocobo");
            for(int i=0;i<24;i++) store.Insert(new(i.ToString(),DateTimeOffset.Now.AddDays(-i).ToUnixTimeSeconds(),actor,(uint)(100+i),i%2==0?"試験用クリスタル":"サンプル素材",i%3==0,(uint)(i+1),(uint)(100+i*97),(uint)(5+i*4),(100+i).ToString(),"2001",i%4==0?"":"Sample Retainer",1));
            var result=store.Query(new(),0,false);
            Set("filter",new Filter()); Set("from",""); Set("result",result); Set("dirty",false); Set("requestedRevision",store.Revision);
            Invoke("UpdateFonts",string.Concat(result.Rows.Select(x=>x.ItemName+x.Character.Name+x.Character.World+x.RetainerName)));
            io.Fonts.Build();
            foreach(var font in Dummy.Fonts.TakeLast(4)) foreach(var ch in Loc.FontText.Distinct().Where(c=>!char.IsControl(c)))
                Check(font.FindGlyphNoFallback(ch)!=null,$"Glyph U+{(int)ch:X4}");
            var textures=new Dictionary<ulong,(nint,int,int)>();
            for(int i=0;i<io.Fonts.Textures.Size;i++)
            {
                byte* pixels; int w=0,h=0,bpp=0; io.Fonts.GetTexDataAsRGBA32(i,&pixels,&w,&h,&bpp);
                io.Fonts.SetTexID(i,new ImTextureID((ulong)i+1)); textures[(ulong)i+1]=((nint)pixels,w,h);
            }
            void Frame() { ImGui.NewFrame(); Invoke("Draw"); ImGui.Render(); }
            void Click(float x,float y) { io.AddMousePosEvent(x,y); Frame(); io.AddMouseButtonEvent(0,true); Frame(); io.AddMouseButtonEvent(0,false); Frame(); Frame(); }
            Dummy.Commands["/marketledger"].Handler("/marketledger",""); Frame(); Frame();
            long reads=store.ReadStatements,writes=store.PurchaseWrites; int saves=Dummy.Saves;
            foreach(string code in Loc.Codes)
            {
                Dummy.Config.Language=code; Frame(); Frame();
                Check(ImGui.GetDrawData().TotalVtxCount>0,"Draw "+code);
                Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,code+"-history.bmp"));
            }
            Check(store.ReadStatements==reads && store.PurchaseWrites==writes,"Rendering makes zero DB reads/writes");
            Check(Dummy.Saves==saves,"Drawing does not save config");
            Dummy.Config.Language="ja"; Frame(); Frame(); Click(155,100); Frame();
            Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,"ja-charts.bmp"));
            // Exercise chart rendering directly in a native window independently of tab hit coordinates.
            ImGui.NewFrame(); ImGui.SetNextWindowSize(new(1100,760)); ImGui.Begin("Chart QA");
            using(var pushed=Dummy.FontsHandle()) Invoke("DrawChart");
            ImGui.End(); ImGui.Render();
            Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,"chart-qa.bmp"));
            Check(ImGui.GetDrawData().TotalVtxCount>100,"Graph draws");
            Set("search","試験用"); Invoke("Apply");
            Check(((Filter)Get("filter")!).Search=="試験用","Filter applied");
            Set("from","2026-02-30"); Invoke("Apply"); Check((bool)Get("dateError")!,"Invalid calendar date rejected");
            Check(((Filter)Get("filter")!).Search=="試験用","Invalid filter retains previous query");
            Check(Dummy.Saves==saves,"Search and invalid filters have zero config writes");
        }
        Check(!Dummy.Commands.ContainsKey("/marketledger"),"Dispose removes command");
        Check(Dummy.Subscriptions==0,"Dispose unsubscribes all events");
        ImGui.DestroyContext(); foreach(var pin in Dummy.Pins) pin.Free();
        Console.WriteLine($"PASS: {checks} native ImGui/glyph/lifecycle/access checks. Screenshots: {output}");
    }
}
public unsafe class Dummy:DispatchProxy
{
    public static Configuration Config=new(){Language="ja"};
    public static string Directory="";
    public static int Saves,Subscriptions;
    public static readonly Dictionary<string,CommandInfo> Commands=new();
    public static readonly List<ImFontPtr> Fonts=new();
    public static readonly List<GCHandle> Pins=new();
    public ImFontPtr Font;
    public static IDisposable FontsHandle() { ImGui.PushFont(Fonts[^4]); return new PopFont(); }
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        if(method==null) return null;
        if(method.Name.StartsWith("add_")) { Subscriptions++; return null; }
        if(method.Name.StartsWith("remove_")) { Subscriptions--; return null; }
        switch(method.Name)
        {
            case "GetPluginConfig": return Config;
            case "GetPluginConfigDirectory": return Directory;
            case "get_UiLanguage": return "en";
            case "SavePluginConfig": Saves++; return null;
            case "AddHandler": return Commands.TryAdd((string)args![0]!, (CommandInfo)args[1]!);
            case "RemoveHandler": return Commands.Remove((string)args![0]!);
            case "get_UiBuilder": case "get_FontAtlas": return DispatchProxy.Create(method.ReturnType,typeof(Dummy));
            case "get_BuildStep": return FontAtlasBuildStep.PreBuild;
            case "NewDelegateFontHandle":
                var toolkit=DispatchProxy.Create<IFontAtlasBuildToolkitPreBuild,Dummy>();
                ((FontAtlasBuildStepDelegate)args![0]!)(toolkit);
                var handle=DispatchProxy.Create<IFontHandle,Dummy>(); ((Dummy)(object)handle).Font=Fonts.Last(); return handle;
            case "AddDalamudAssetFont":
                var safe=(SafeFontConfig)args![1]!; var pin=GCHandle.Alloc(safe.GlyphRanges,GCHandleType.Pinned); Pins.Add(pin);
                var native=ImGui.ImFontConfig(); native.FontDataOwnedByAtlas=true; native.OversampleH=2; native.OversampleV=1; native.RasterizerMultiply=1; native.GlyphMaxAdvanceX=float.MaxValue; native.FontNo=safe.FontNo;
                string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"XIVLauncher/dalamudAssets/dev/UIRes/NotoSansCJK-Regular.ttc");
                var font=ImGui.GetIO().Fonts.AddFontFromFileTTF(path,safe.SizePx,native,(ushort*)pin.AddrOfPinnedObject()); native.Destroy(); Fonts.Add(font); return font;
            case "Push": ImGui.PushFont(Font); return new PopFont();
        }
        return method.ReturnType==typeof(void)?null:method.ReturnType.IsValueType?Activator.CreateInstance(method.ReturnType):null;
    }
    private sealed class PopFont:IDisposable { public void Dispose()=>ImGui.PopFont(); }
}
