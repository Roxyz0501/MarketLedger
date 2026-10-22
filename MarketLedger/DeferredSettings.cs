using System.Text.Json;

namespace MarketLedger;

public sealed record SettingsState(int Version, string? Language, bool Recording);

// Immutable snapshots, one writer, no per-frame serialization. This file is owned by the plugin,
// not SavePluginConfig (whose backup/flush contract belongs to the host).
public sealed class DeferredSettings : IDisposable
{
    private readonly object gate = new();
    private readonly object ioGate = new();
    private readonly string path;
    private readonly Action<string>? fault;
    private SettingsState current;
    private SettingsState? saved;
    private Task worker = Task.CompletedTask;
    private DateTimeOffset next;
    private bool closed;
    private bool finalized;
    private int failures;
    public SettingsState Loaded { get; }
    public bool Recovered { get; private set; }
    public string? Error { get; private set; }
    public bool Stopped { get { lock(gate) return failures >= 3; } }
    public long Saves { get; private set; }
    public long LogicalBytes { get; private set; }
    public int Attempts { get; private set; }
    public bool Dirty { get { lock(gate) return current != saved; } }
    public bool IsSaving { get { lock(gate) return !worker.IsCompleted; } }
    public DeferredSettings(string path, Func<SettingsState> legacy, DateTimeOffset now, Action<string>? fault = null)
    {
        this.path = Path.GetFullPath(path); this.fault = fault; next = now.AddSeconds(60);
        Directory.CreateDirectory(Path.GetDirectoryName(this.path)!);
        if(File.Exists(path))
        {
            try { saved = Read(path); }
            catch(Exception e) when(e is IOException or JsonException or InvalidDataException)
            { Error = e.Message; }
        }
        if(saved == null)
        {
            foreach(var backup in Backups())
            {
                SettingsState candidate;
                try { candidate = Read(backup); }
                catch(Exception e) when(e is IOException or JsonException or InvalidDataException) { continue; }
                // Preserve the damaged primary before restoring; never rotate it into normal backups.
                if(File.Exists(path)) File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                Write(candidate, false); saved = candidate; Recovered = true; Error = null; break;
            }
        }
        if(saved == null && File.Exists(path))
        {
            // No valid backup: retain the damaged bytes and fall back to the legacy/default values.
            File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            Recovered = true;
        }
        Loaded = current = saved ?? legacy();
    }
    private static SettingsState Read(string file)
    {
        if(new FileInfo(file).Length > 65536) throw new InvalidDataException("Settings exceed 64 KiB.");
        using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
        var root = doc.RootElement;
        if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("Version",out var v) || v.ValueKind!=JsonValueKind.Number || !v.TryGetInt32(out int version) || version != 1 ||
           !root.TryGetProperty("Recording",out var r) || r.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
           !root.TryGetProperty("Language",out var language) || language.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            throw new InvalidDataException("Invalid settings schema.");
        var result = JsonSerializer.Deserialize<SettingsState>(root.GetRawText())!;
        if(result.Language != null && Loc.Normalize(result.Language) == null) throw new InvalidDataException("Invalid language.");
        return result;
    }
    private string[] Backups() => Directory.GetFiles(Path.GetDirectoryName(path)!,Path.GetFileName(path)+".backup-*.json")
        .OrderByDescending(x=>Path.GetFileName(x),StringComparer.Ordinal).ToArray();
    private string NextBackup()
    {
        long sequence=DateTime.UtcNow.Ticks;
        int start=Path.GetFileName(path).Length+".backup-".Length;
        foreach(var backup in Backups())
        {
            string name=Path.GetFileName(backup);
            if(name.Length>=start+19 && long.TryParse(name.AsSpan(start,19),out long previous))
                sequence=Math.Max(sequence,checked(previous+1));
        }
        return path+".backup-"+sequence.ToString("D19")+"-"+Guid.NewGuid().ToString("N")+".json";
    }
    public void Change(SettingsState state) { lock(gate) { if(!closed) current=state; } }
    public void Tick(DateTimeOffset now)
    {
        lock(gate)
        {
            if(closed || now<next) return;
            next=now.AddSeconds(60);
            if(current==saved || failures>=3 || !worker.IsCompleted) return;
            worker=Task.Run(SaveOnce);
        }
    }
    public void Retry() { lock(gate) { if(!closed) { failures=0; Error=null; } } }
    public void Flush()
    {
        Task pending; lock(gate) { if(finalized) return; pending=worker; }
        pending.GetAwaiter().GetResult(); SaveOnce();
    }
    private void SaveOnce()
    {
        lock(ioGate)
        {
            SettingsState snapshot;
            lock(gate) { if(finalized || current==saved || failures>=3) return; snapshot=current; }
            try
            {
                Attempts++; Write(snapshot,true);
                lock(gate) { saved=snapshot; failures=0; Error=null; }
            }
            catch(Exception e) { lock(gate) { failures++; Error=e.Message; } }
        }
    }
    private void Write(SettingsState value,bool backup)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(value);
        string temp=path+".tmp-"+Guid.NewGuid().ToString("N");
        try
        {
            fault?.Invoke("write");
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            { stream.Write(bytes); LogicalBytes+=bytes.Length; stream.Flush(true); }
            if(Read(temp)!=value) throw new InvalidDataException("Settings roundtrip mismatch.");
            fault?.Invoke("replace");
            if(File.Exists(path))
            {
                // Validate the predecessor before allowing it to become a valid backup generation.
                _=Read(path);
                string? previous=backup?NextBackup():null;
                File.Replace(temp,path,previous,true);
            }
            else File.Move(temp,path);
            Saves++;
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
        // Cleanup is best effort after a successful commit; failure must not undo the saved snapshot.
        try
        {
            var valid=new List<string>();
            foreach(var old in Backups())
            {
                try { _=Read(old); valid.Add(old); }
                catch(Exception e) when(e is JsonException or InvalidDataException)
                { File.Move(old,old+".corrupt-"+Guid.NewGuid().ToString("N")); }
            }
            foreach(var old in valid.Skip(3)) File.Delete(old);
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException) { /* Committed primary remains valid; retain backups if cleanup is denied. */ }
    }
    public void Dispose()
    {
        lock(gate) { if(closed) return; closed=true; }
        Flush();
        lock(ioGate) { lock(gate) finalized=true; }
    }
}
