using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using Roxyz0501.Dalamud.CommonUi;

namespace MarketLedger;

public sealed partial class Plugin
{
    private Filter filter = new();
    private string selectedCharacter="",search="",world="",from="",until="",min="",max="";
    private int quality,sort,page,metric,source=-1;
    private static readonly (int Value,string Key)[] Sources=[(-1,"Confirmed"),(0,"Market"),(1,"NpcShop"),(2,"Unclassified"),(3,"Transfer"),(-2,"AllOutflows")];
    private bool monthly,cumulative,dirty=true,dateError,readError,exportError,forceRefresh,recoveryError;
    private long requestedRevision=-1;
    private QueryResult? result;
    private Task<QueryResult>? queryTask;
    private Task<string>? exportTask;
    private Filter? requestedFilter;
    private int requestedPage;
    private bool requestedMonthly;
    private string exportPath="";

    private void RefreshResults()
    {
        if(recoveryTask?.IsCompleted==true)
        {
            try { recoveryTask.GetAwaiter().GetResult(); recoveryError=false; }
            catch(Exception e) { recoveryError=true; log.Error(e,"Market Ledger recovery failed."); }
            recoveryTask=null;
            recoveryFiles=Directory.EnumerateFiles(directory,"recovery-*.json").Take(100).ToArray();
        }
        if(exportTask?.IsCompleted==true)
        {
            try { exportPath=exportTask.GetAwaiter().GetResult(); exportError=false; fileRevision=-1; }
            catch(Exception e) { exportError=true; log.Error(e,"Market Ledger CSV export failed."); }
            exportTask=null;
        }
        if(queryTask?.IsCompleted==true)
        {
            try
            {
                var next=queryTask.GetAwaiter().GetResult();
                if(requestedFilter==filter && requestedPage==page && requestedMonthly==monthly)
                {
                    result=next; readError=false;
                    UpdateFonts(string.Concat(next.Rows.Select(x=>x.ItemName+x.Character.Name+x.Character.HomeWorld+x.Character.World+x.RetainerName))
                        +string.Concat(next.Characters.Select(x=>x.Name+x.HomeWorld)));
                }
            }
            catch(Exception e) { readError=true; log.Error(e,"Market Ledger read failed."); }
            queryTask=null;
        }
        if(queryTask==null && (dirty || requestedRevision!=store.Revision))
        {
            requestedFilter=filter; requestedPage=page; requestedMonthly=monthly;
            requestedRevision=store.Revision; dirty=false;
            var captured=filter; var p=page; var m=monthly; bool refresh=forceRefresh; forceRefresh=false;
            queryTask=Task.Run(()=>{ if(refresh) store.Refresh(); return store.Query(captured,p,m); });
        }
    }
    private void Draw()
    {
        if(!open || disposed) return;
        RefreshResults();
        int face=config.Language switch {"zh-Hant"=>1,"zh-Hans"=>2,"ko"=>3,_=>0};
        using var font=fonts[face].Push();
        ImGui.SetNextWindowSize(new Vector2(1320,800),ImGuiCond.FirstUseEver);
        bool visible=open;
        if(ImGui.Begin("Gil Ledger###MarketLedger",ref visible))
        {
            if(!config.Recording) ImGui.TextColored(new Vector4(1,.65f,.2f,1),T("Paused"));
            if(!npcShop.Available) ImGui.TextWrapped(T("NpcUnavailable"));
            if(settings.Recovered) ImGui.TextWrapped(T("SettingsRecovered"));
            if(settings.Error!=null || archivePump.Error!=null)
            {
                ImGui.TextWrapped(T("PersistenceError"));
                if(ImGui.Button(T("Retry")+"###RetryPersistence")) { settings.Retry(); archivePump.Retry(); }
            }
            if(!failed.IsEmpty)
            {
                ImGui.TextColored(new Vector4(1,.4f,.3f,1),T("WriteError"));
                if(ImGui.Button(T("Retry")+"###RetrySave")) RetryWrites();
            }
            if(tracker.Unmatched>0) ImGui.TextDisabled($"{T("Unmatched")}: {tracker.Unmatched}");
            if(spending.Overflow>0) ImGui.TextWrapped(T("Overflow"));
            if(ImGui.BeginTabBar("MainTabs"))
            {
                if(ImGui.BeginTabItem(T("History")+"###History"))
                {
                    DrawFilters(); DrawSummary(); DrawExport(); DrawHistory(); ImGui.EndTabItem();
                }
                if(ImGui.BeginTabItem(T("Charts")+"###Charts"))
                {
                    DrawFilters(); DrawSummary(); DrawChart(); ImGui.EndTabItem();
                }
                DrawHistoryFiles();
                StandardPluginTabs.DrawStandardTrailingTabs(T("Settings"),"Settings",DrawSettings,
                    T("LanguageSettings"),DrawLanguage,T("Support"),()=>StandardPluginTabs.DrawSupportContent(T("SupportHeading"),T("SupportText"),T("SupportRecipient"),T("OpenKoFi")));
                ImGui.EndTabBar();
            }
        }
        ImGui.End(); open=visible;
    }
    private void DrawFilters()
    {
        if(ImGui.BeginTable("Filters",4,ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(T("Character")); ImGui.SetNextItemWidth(-1);
            var current=result?.Characters.FirstOrDefault(x=>x.Id==selectedCharacter);
            string preview=selectedCharacter.Length==0?T("All"):current!=null?$"{current.Name} @ {current.HomeWorld}":selectedCharacter;
            if(ImGui.BeginCombo("###Character",preview))
            {
                if(ImGui.Selectable(T("All"),selectedCharacter.Length==0)) selectedCharacter="";
                if(actor is { } a && ImGui.Selectable(T("Current")+$" ({a.Name})")) selectedCharacter=a.Id;
                foreach(var c in result?.Characters ?? []) if(ImGui.Selectable($"{c.Name} @ {c.HomeWorld}##{c.Id}",c.Id==selectedCharacter)) selectedCharacter=c.Id;
                ImGui.EndCombo();
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(T("Source")); ImGui.SetNextItemWidth(-1);
            if(ImGui.BeginCombo("###Source",T(Sources.First(x=>x.Value==source).Key)))
            { foreach(var option in Sources) if(ImGui.Selectable(T(option.Key)+"###Source"+option.Value,source==option.Value)) source=option.Value; ImGui.EndCombo(); }
            ImGui.TableNextColumn(); Input("Search",ref search,160);
            ImGui.TableNextColumn(); Input("World",ref world,64);
            ImGui.TableNextColumn(); Input("From",ref from,10);
            ImGui.TableNextColumn(); Input("Until",ref until,10);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(T("Quality")); ImGui.SetNextItemWidth(-1);
            string[] options=[T("All"),"HQ","NQ"];
            if(ImGui.BeginCombo("###Quality",options[quality])) { for(int i=0;i<3;i++) if(ImGui.Selectable(options[i],i==quality)) quality=i; ImGui.EndCombo(); }
            ImGui.TableNextColumn(); Input("Min",ref min,16);
            ImGui.TableNextColumn(); Input("Max",ref max,16);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(T("Sort")); ImGui.SetNextItemWidth(-1);
            string[] sorts=[T("Newest"),T("Highest"),T("Lowest"),T("Oldest")];
            if(ImGui.BeginCombo("###Sort",sorts[sort])) { for(int i=0;i<4;i++) if(ImGui.Selectable(sorts[i],i==sort)) sort=i; ImGui.EndCombo(); }
            ImGui.EndTable();
        }
        if(ImGui.Button(T("Apply")+"###Apply")) Apply(); ImGui.SameLine();
        if(ImGui.Button(T("Refresh")+"###Refresh")) { forceRefresh=true; dirty=true; } ImGui.SameLine();
        if(ImGui.Button(T("Reset")+"###Reset")) { selectedCharacter=""; search=world=from=until=min=max=""; quality=sort=0; source=-1; Apply(); }
        ImGui.SameLine(); if(ImGui.Button(T("Today")+"###Today")) { from=until=DateTime.Today.ToString("yyyy-MM-dd"); Apply(); }
        ImGui.SameLine(); if(ImGui.Button(T("Month")+"###Month")) { from=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).ToString("yyyy-MM-dd"); until=DateTime.Today.ToString("yyyy-MM-dd"); Apply(); }
        ImGui.SameLine(); if(ImGui.Button(T("Last30")+"###Last30")) { from=DateTime.Today.AddDays(-29).ToString("yyyy-MM-dd"); until=DateTime.Today.ToString("yyyy-MM-dd"); Apply(); }
        ImGui.SameLine(); if(ImGui.Button(T("AllTime")+"###AllTime")) { from=until=""; Apply(); }
        ImGui.TextWrapped(T("Applied"));
        ImGui.TextWrapped(T("SourceHelp"));
        if(dateError) ImGui.TextColored(new Vector4(1,.4f,.3f,1),T("DateError"));
    }
    private void Input(string key,ref string value,int length)
    {
        ImGui.TextUnformatted(T(key)); ImGui.SetNextItemWidth(-1); ImGui.InputText("###"+key,ref value,length+1);
    }
    private void Apply()
    {
        try
        {
            long Date(string text,bool end)
            {
                if(string.IsNullOrWhiteSpace(text)) return 0;
                var d=DateTime.ParseExact(text.Trim(),"yyyy-MM-dd",CultureInfo.InvariantCulture);
                return new DateTimeOffset(end?d.AddDays(1):d).ToUnixTimeSeconds();
            }
            long Amount(string text)=>string.IsNullOrWhiteSpace(text)?0:long.Parse(text,NumberStyles.None,CultureInfo.InvariantCulture);
            long f=Date(from,false),u=Date(until,true),lo=Amount(min),hi=Amount(max);
            if(f<0 || u<0 || (u!=0 && f>=u) || lo<0 || hi<0 || (hi!=0 && lo>hi)) throw new FormatException();
            var next=new Filter(selectedCharacter,search.Trim(),world.Trim(),quality,f,u,lo,hi,sort,source);
            if(next!=filter || page!=0) { filter=next; page=0; result=null; dirty=true; }
            dateError=false;
        }
        catch { dateError=true; }
    }
    private void DrawSummary()
    {
        ImGui.Separator();
        if(readError) { ImGui.TextWrapped(T("ReadError")); ImGui.SameLine(); if(ImGui.Button(T("Retry")+"###RetryQuery")) dirty=true; }
        if(queryTask!=null) ImGui.TextDisabled(T("Loading"));
        if(result is not { } r) return;
        ImGui.TextUnformatted($"{T("Count")}: {r.Totals.Count:N0}     {T("Quantity")}: {r.Totals.Quantity:N0}");
        ImGui.TextUnformatted($"{T("Subtotal")}: {r.Totals.Subtotal:N0}     {T("Tax")}: {r.Totals.Tax:N0}     {T("Total")}: {r.Totals.Total:N0} gil");
        if(r.Totals.UnknownTaxTotal>0) ImGui.TextWrapped($"{T("UnknownTaxTotal")}: {r.Totals.UnknownTaxTotal:N0} gil");
    }
    private void DrawExport()
    {
        ImGui.BeginDisabled(exportTask!=null);
        if(ImGui.Button(T("Export")+"###Export"))
        {
            var captured=filter; exportPath="";
            exportTask=Task.Run(()=>
            {
                string folder=Path.Combine(directory,"exports"); Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,$"GilLedger-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}.csv");
                store.Export(captured,file,shutdown.Token); return file;
            });
        }
        ImGui.EndDisabled();
        if(exportTask!=null) { ImGui.SameLine(); ImGui.TextDisabled(T("Exporting")); }
        if(exportError) ImGui.TextWrapped(T("ExportError"));
        if(exportPath.Length>0) { ImGui.TextWrapped(T("Exported")+": "+exportPath); if(ImGui.SmallButton(T("Folder")+"###ExportFolder")) Util.OpenLink(Path.GetDirectoryName(exportPath)!); }
    }
    private void DrawHistory()
    {
        if(result is not { } r) return;
        if(r.Rows.Count==0) { ImGui.TextWrapped(T("NoData")); return; }
        ImGui.BeginDisabled(page==0);
        if(ImGui.Button(T("Previous")+"###Prev")) { page--; dirty=true; result=null; }
        ImGui.EndDisabled(); ImGui.SameLine();
        ImGui.TextUnformatted($"{T("Page")}: {r.Page+1:N0} / {Math.Max(1,(r.Totals.Count+99)/100):N0}"); ImGui.SameLine();
        ImGui.BeginDisabled((long)(page+1)*LedgerStore.PageSize>=r.Totals.Count);
        if(ImGui.Button(T("Next")+"###Next")) { page++; dirty=true; result=null; }
        ImGui.EndDisabled();
        if(ImGui.BeginTable("Purchases",11,ImGuiTableFlags.Borders|ImGuiTableFlags.RowBg|ImGuiTableFlags.ScrollY|ImGuiTableFlags.ScrollX|ImGuiTableFlags.Resizable,new Vector2(0,Math.Max(120,ImGui.GetContentRegionAvail().Y))))
        {
            string[] headers=["Time","Source","Character","Item","Seller","Quantity","Unit","Subtotal","Tax","Total","World"];
            float[] widths=[155,100,160,205,145,55,90,85,65,95,90];
            for(int i=0;i<headers.Length;i++) ImGui.TableSetupColumn(T(headers[i])+"###"+headers[i],ImGuiTableColumnFlags.WidthFixed,widths[i]);
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for(int i=0;i<headers.Length;i++) { ImGui.TableSetColumnIndex(i); ImGui.TextWrapped(T(headers[i])); }
            foreach(var p in r.Rows)
            {
                ImGui.TableNextRow();
                Cell(DateTimeOffset.FromUnixTimeSeconds(p.Time).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                Cell(T(p.Source.ToString()));
                Cell(p.Character.Name+" @ "+p.Character.HomeWorld);
                Cell(p.ItemId==0?T(p.Source.ToString()):p.ItemName+(p.Hq?" [HQ]":""));
                if(ImGui.IsItemHovered()) ImGui.SetTooltip($"{p.ItemName}\nID: {p.ItemId}");
                Cell(p.RetainerName.Length>0?p.RetainerName:T("Unknown"));
                if(ImGui.IsItemHovered()) ImGui.SetTooltip($"{p.RetainerName}\nID: {p.RetainerId}");
                Cell(p.Quantity==0?"—":p.Quantity.ToString("N0")); Cell(p.Quantity==0?"—":p.UnitPrice.ToString("N0")); Cell(p.TaxKnown?p.Subtotal.ToString("N0"):"—"); Cell(p.TaxKnown?p.Tax.ToString("N0"):"—"); Cell(p.Total.ToString("N0"));
                Cell(p.Character.World);
            }
            ImGui.EndTable();
        }
    }
    private static void Cell(string text) { ImGui.TableNextColumn(); ImGui.TextUnformatted(text); if(ImGui.IsItemHovered()) ImGui.SetTooltip(text); }
    private void DrawChart()
    {
        bool m=monthly;
        if(ImGui.RadioButton(T("Daily")+"###Daily",!monthly)) m=false; ImGui.SameLine();
        if(ImGui.RadioButton(T("Monthly")+"###Monthly",monthly)) m=true;
        if(m!=monthly) { monthly=m; dirty=true; result=null; }
        ImGui.SameLine(); ImGui.Checkbox(T("Cumulative")+"###Cumulative",ref cumulative);
        string[] names=[T("Total"),T("Subtotal"),T("Tax")];
        for(int i=0;i<3;i++) { if(i>0) ImGui.SameLine(); if(ImGui.RadioButton(names[i]+"###Metric"+i,metric==i)) metric=i; }
        ImGui.TextWrapped(T("GraphNote"));
        if(result is not { } r || r.Buckets.Count==0) { ImGui.TextWrapped(T("NoData")); return; }
        // At most 366 bars/points; no database work or full-history enumeration during rendering.
        long running=0;
        long[] values=r.Buckets.Select(b=> { long v=metric switch {1=>b.Subtotal,2=>b.Tax,_=>b.Total}; running+=v; return cumulative?running:v; }).ToArray();
        var start=ImGui.GetCursorScreenPos();
        var size=new Vector2(Math.Max(200,ImGui.GetContentRegionAvail().X),200);
        ImGui.InvisibleButton("###Graph",size);
        var draw=ImGui.GetWindowDrawList();
        draw.AddRectFilled(start,start+size,ImGui.GetColorU32(new Vector4(.07f,.09f,.12f,1)));
        long maximum=Math.Max(1,values.Max()); float step=size.X/values.Length;
        for(int i=0;i<values.Length;i++)
        {
            float h=(float)((double)values[i]/maximum)*(size.Y-20);
            var low=start+new Vector2(i*step,size.Y); var high=start+new Vector2((i+1)*step-.5f,size.Y-h);
            draw.AddRectFilled(new Vector2(low.X,high.Y),new Vector2(high.X,low.Y),ImGui.GetColorU32(new Vector4(.95f,.64f,.2f,1)));
        }
        if(ImGui.IsItemHovered())
        {
            int index=Math.Clamp((int)((ImGui.GetIO().MousePos.X-start.X)/step),0,values.Length-1);
            var b=r.Buckets[index];
            ImGui.SetTooltip($"{b.Date}\n{names[metric]}: {values[index]:N0} gil\n{T("Subtotal")}: {b.Subtotal:N0}\n{T("Tax")}: {b.Tax:N0}");
        }
        ImGui.TextUnformatted($"{r.Buckets[0].Date} — {r.Buckets[^1].Date}     0 — {maximum:N0} gil");
        if(ImGui.BeginTable("Buckets",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.ScrollY,new Vector2(0,Math.Max(100,ImGui.GetContentRegionAvail().Y))))
        {
            foreach(string key in new[]{"Time","Subtotal","Tax","Total"}) ImGui.TableSetupColumn(T(key));
            ImGui.TableSetupScrollFreeze(0,1); ImGui.TableHeadersRow();
            foreach(var b in r.Buckets.Reverse()) { ImGui.TableNextRow(); Cell(b.Date); Cell(b.Subtotal.ToString("N0")); Cell(b.Tax.ToString("N0")); Cell(b.Total.ToString("N0")); }
            ImGui.EndTable();
        }
    }
    private void DrawSettings()
    {
        ImGui.TextWrapped(T("SavePolicy"));
        bool recording=config.Recording;
        if(ImGui.Checkbox(T("Recording")+"###Recording",ref recording)) { DrainSpending(DateTimeOffset.UtcNow,true); config.Recording=recording; tracker.Clear(); spending.Clear(); inventoryReadyAt=DateTimeOffset.UtcNow.AddSeconds(2); Save(); }
        ImGui.TextWrapped(T("Limitations")); ImGui.Spacing(); ImGui.TextWrapped(T("LocalOnly"));
        if(recoveryFiles.Length>0)
        {
            ImGui.TextUnformatted($"{T("RecoveryFiles")}: {recoveryFiles.Length}");
            ImGui.BeginDisabled(recoveryTask!=null);
            if(ImGui.Button(T("Recover")+"###Recover")) Recover();
            ImGui.EndDisabled();
        }
        if(recoveryError) ImGui.TextWrapped(T("WriteError"));
        if(ImGui.Button(T("Folder")+"###DataFolder")) Util.OpenLink(directory);
    }
    private void DrawLanguage()
    {
        string? changed=StandardPluginTabs.DrawLanguageSelector(T("Language"),T("LanguageHelp"),config.Language!);
        if(changed!=null && changed!=config.Language) { config.Language=changed; Save(); commandInfo.HelpMessage=T("CommandHelp"); }
    }
}
