using System.Diagnostics;
using Roxyz0501.Dalamud.CommonUi;
using Dalamud.Bindings.ImGui;

namespace MarketLedger;

public sealed partial class Plugin
{
    private static DateTimeOffset SettingsTime=>DateTimeOffset.UnixEpoch.AddMilliseconds(Environment.TickCount64);
    private sealed record WriteRequest(Purchase? Purchase,TaskCompletionSource? Barrier=null);
    private readonly DeferredSettings settings;
    private readonly ArchivePump archivePump;
    private Task? logoutFlush;
    private Task<FileMeasurement>? fileTask;
    private FileMeasurement? fileMeasurement;
    private bool filesActive,filesError,folderError;
    private long fileRevision=-1;
    private long fileArchiveRevision=-1;
    private int writeFailures;
    private volatile bool pauseRecordingRequested;
    private string? persistenceNotice;
    private void ReportPersistenceError()
    {
        string? error=settings.Error??archivePump.Error;
        if(error!=null && persistenceNotice!=error) { persistenceNotice=error; open=true; log.Error("Gil Ledger persistence: {Error}. Check space/permissions and use Retry.",error); }
        else if(error==null) persistenceNotice=null;
    }
    private void Save()=>settings.Change(new(config.Version,config.Language,config.Recording));
    private async Task FlushWrites()
    {
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await writes.Writer.WriteAsync(new(null,done));
        await done.Task;
    }
    private void RequestFileRefresh()
    {
        if(disposed || fileTask!=null) return;
        filesError=false; folderError=false;
        fileRevision=store.Revision; fileArchiveRevision=store.Archive.FilesWritten;
        fileTask=Task.Run(()=>HistoryFiles.Measure(directory,store.ArchiveDirectory,shutdown.Token));
    }
    private void DrawHistoryFiles()
    {
        if(fileTask?.IsCompleted==true)
        {
            try { fileMeasurement=fileTask.GetAwaiter().GetResult(); filesError=false; }
            catch(Exception e) { fileMeasurement=null; filesError=true; log.Error(e,"Gil Ledger history metadata measurement failed; use Reload to retry."); }
            fileTask=null;
        }
        var locations=fileMeasurement?.Locations.Select((x,i)=>new StandardHistoryFilesTab.Location(i.ToString(),x.Directory,x.Count,x.Bytes)).ToArray()
            ?? [new("root",directory,null,null),new("daily",store.ArchiveDirectory,null,null),new("exports",Path.Combine(directory,"exports"),null,null)];
        var snapshot=new StandardHistoryFilesTab.Snapshot(fileMeasurement?.Count,fileMeasurement?.Bytes,T("FilesScope"),
            T(folderError?"FolderError":filesError?"FilesError":fileTask!=null?"Loading":fileMeasurement==null?"FilesUnmeasured":"FilesReady"),locations);
        float before=ImGui.GetCursorPosY();
        StandardHistoryFilesTab.Draw(new(T("HistoryFiles"),T("FileCount"),T("FileSize"),T("FileDirectory"),T("Folder"),T("Refresh"),T("Scope"),T("LogicalBytes")),
            snapshot,RequestFileRefresh,path=>
            {
                try
                {
                    if(path!=directory && path!=store.ArchiveDirectory && path!=Path.Combine(directory,"exports")) throw new IOException("Untrusted path");
                    if(!Directory.Exists(path)) throw new DirectoryNotFoundException();
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute=true }); folderError=false;
                }
                catch { folderError=true; }
            });
        // The unchanged canonical helper emits content only for the active tab. No file IO in Draw.
        bool active=ImGui.GetCursorPosY()!=before;
        if(active && (!filesActive || (!filesError && (fileRevision!=store.Revision || fileArchiveRevision!=store.Archive.FilesWritten)))) RequestFileRefresh();
        filesActive=active;
    }
}
