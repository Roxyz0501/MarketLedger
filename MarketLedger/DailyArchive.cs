using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarketLedger;

// The durable SQLite row is also the outbox. A monotonically advancing rowid checkpoint
// makes restart/migration bounded, without reading or rewriting all historical files.
public sealed class DailyArchive
{
    public sealed record Manifest(int Format,string TimeZone,string CompatibilityDatabase,string Layout);
    public sealed record Transaction(int Format,Purchase Purchase);
    private readonly string root;
    private readonly string database;
    private readonly Action<string>? fault;
    private TimeZoneInfo? zone;
    public long FilesWritten { get; private set; }
    public long LogicalBytes { get; private set; }
    public DailyArchive(string root,string database,Action<string>? fault=null)
    { this.root=root; this.database=database; this.fault=fault; }
    private void Initialize()
    {
        if(zone!=null) return;
        Directory.CreateDirectory(root);
        string path=Path.Combine(root,"manifest.json");
        if(!File.Exists(path))
            Create(path,JsonSerializer.SerializeToUtf8Bytes(new Manifest(1,TimeZoneInfo.Local.Id,database,"yyyy-MM-dd/sha256(id).json")));
        if(new FileInfo(path).Length>65536) throw new InvalidDataException("Archive manifest exceeds 64 KiB.");
        var m=JsonSerializer.Deserialize<Manifest>(File.ReadAllBytes(path)) ?? throw new InvalidDataException("Invalid archive manifest.");
        if(m.Format!=1 || m.CompatibilityDatabase!=database || m.Layout!="yyyy-MM-dd/sha256(id).json") throw new InvalidDataException("Unsupported archive manifest.");
        zone=TimeZoneInfo.FindSystemTimeZoneById(m.TimeZone);
    }
    public void Save(Purchase p)
    {
        Initialize();
        string day=TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(p.Time),zone!).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
        string directory=Path.Combine(root,day); Directory.CreateDirectory(directory);
        string hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(p.Id))).ToLowerInvariant();
        string path=Path.Combine(directory,hash+".json");
        var bytes=JsonSerializer.SerializeToUtf8Bytes(new Transaction(1,p));
        if(bytes.Length>1024*1024) throw new InvalidDataException("Transaction exceeds 1 MiB.");
        if(!File.Exists(path)) Create(path,bytes);
        // Compare the complete record, including IDs, amount and timestamp, before checkpointing.
        if(new FileInfo(path).Length!=bytes.Length || !File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            throw new InvalidDataException("Archive transaction mismatch: "+p.Id);
    }
    private void Create(string path,byte[] bytes)
    {
        string temp=path+".pending-"+Guid.NewGuid().ToString("N");
        try
        {
            fault?.Invoke("write");
            using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            { file.Write(bytes); LogicalBytes+=bytes.Length; file.Flush(true); }
            using var validation=JsonDocument.Parse(File.ReadAllBytes(temp));
            fault?.Invoke("rename");
            try { File.Move(temp,path,false); FilesWritten++; }
            catch(IOException) when(File.Exists(path)) { /* Another process may have completed the same immutable record. */ }
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}
