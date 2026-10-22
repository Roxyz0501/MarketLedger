using System.Globalization;
using System.Text;

namespace MarketLedger;

public sealed class LedgerStore : IDisposable
{
    private readonly object gate = new();
    private readonly Sqlite db;
    private readonly object archiveGate = new();
    public DailyArchive Archive { get; }
    public string ArchiveDirectory { get; }
    public long ArchiveReadRows { get; private set; }
    public long ArchiveBatches { get; private set; }
    private readonly Dictionary<(Filter, int, bool), QueryResult> cache = new();
    private long revision;
    public long Revision => Interlocked.Read(ref revision);
    public long ReadStatements { get; private set; }
    public long PurchaseWrites { get; private set; }
    public long RowsReturned { get; private set; }
    public const int PageSize = 100;
    private const string Columns = "id,time,cid,character,home,world,item,name,hq,quantity,unit,tax,listing,retainer,seller,city,source,tax_known,total";
    public LedgerStore(string path, Action<string>? archiveFault = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        ArchiveDirectory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!,Path.GetFileNameWithoutExtension(path)+"-history");
        Archive=new DailyArchive(ArchiveDirectory,"../"+Path.GetFileName(path),archiveFault);
        db = new Sqlite(path);
        try
        {
            long version;
            using (var v = db.Prepare("PRAGMA user_version"))
            { v.Step(); version=v.Int(0); if (version > 2) throw new InvalidDataException("Unsupported future ledger schema; no changes made."); }
            db.Execute("PRAGMA journal_mode=WAL");
            db.Execute("PRAGMA synchronous=FULL");
            db.Execute("PRAGMA cache_size=-4096");
            db.Execute("CREATE TABLE IF NOT EXISTS purchases (id TEXT PRIMARY KEY,time INTEGER NOT NULL,cid TEXT NOT NULL,character TEXT NOT NULL,home TEXT NOT NULL,world TEXT NOT NULL,item INTEGER NOT NULL,name TEXT NOT NULL,hq INTEGER NOT NULL,quantity INTEGER NOT NULL,unit INTEGER NOT NULL,tax INTEGER NOT NULL,listing TEXT NOT NULL,retainer TEXT NOT NULL,seller TEXT NOT NULL,city INTEGER NOT NULL,subtotal INTEGER NOT NULL,total INTEGER NOT NULL,UNIQUE(world,listing))");
            db.Execute("CREATE TABLE IF NOT EXISTS characters (cid TEXT PRIMARY KEY,name TEXT NOT NULL,home TEXT NOT NULL)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_date ON purchases(time DESC,id DESC)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_character_date ON purchases(cid,time DESC,id DESC)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_world_date ON purchases(world,time DESC,id DESC)");
            if(version<2)
            {
                if(version==1) db.Execute("VACUUM INTO ?",path+".v1-backup-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N"));
                db.Execute("BEGIN IMMEDIATE");
                try
                {
                    db.Execute("ALTER TABLE purchases ADD COLUMN source INTEGER NOT NULL DEFAULT 0");
                    db.Execute("ALTER TABLE purchases ADD COLUMN tax_known INTEGER NOT NULL DEFAULT 1");
                    db.Execute("CREATE INDEX purchase_source_date ON purchases(source,time DESC,id DESC)");
                    db.Execute("PRAGMA user_version=2"); db.Execute("COMMIT");
                }
                catch { db.Execute("ROLLBACK"); throw; }
            }
            db.Execute("CREATE TABLE IF NOT EXISTS gil_archive_state (singleton INTEGER PRIMARY KEY CHECK(singleton=1),cursor INTEGER NOT NULL,records INTEGER NOT NULL,total INTEGER NOT NULL)");
            db.Execute("INSERT OR IGNORE INTO gil_archive_state VALUES (1,0,0,0)");
        }
        catch { db.Dispose(); throw; }
    }
    // Each successful file is immutable. A failed batch leaves the cursor untouched and is
    // safe to replay. The original DB remains the indexed all-date compatibility reader.
    public int ArchivePending(int limit=64)
    {
        lock(archiveGate)
        {
            var batch=new List<(long Row,Purchase Purchase)>(); long cursor;
            lock(gate)
            {
                using(var state=db.Prepare("SELECT cursor FROM gil_archive_state WHERE singleton=1"))
                { state.Step(); cursor=state.Int(0); }
                using var rows=db.Prepare($"SELECT {Columns},rowid FROM purchases WHERE rowid>? ORDER BY rowid LIMIT ?",cursor,Math.Clamp(limit,1,64));
                while(rows.Step()) batch.Add((rows.Int(19),Read(rows)));
                ArchiveReadRows+=batch.Count;
            }
            if(batch.Count==0) return 0;
            foreach(var row in batch) Archive.Save(row.Purchase);
            lock(gate)
            {
                // Compare-and-swap also prevents another game process moving the cursor backwards.
                db.Execute("UPDATE gil_archive_state SET cursor=?,records=records+?,total=total+? WHERE singleton=1 AND cursor=?",
                    batch[^1].Row,batch.Count,batch.Sum(x=>x.Purchase.Total),cursor);
                ArchiveBatches++;
            }
            return batch.Count;
        }
    }
    public bool Insert(Purchase p)
    {
        if (string.IsNullOrEmpty(p.Character.Id) || string.IsNullOrEmpty(p.ListingId) || p.ListingId == "0" || !Enum.IsDefined(p.Source)
            || p.Total<=0 || (p.Source is ExpenseSource.Market or ExpenseSource.NpcShop && (p.Quantity==0 || p.ItemId==0 || !p.TaxKnown || p.Total!=p.Subtotal+p.Tax))
            || (!p.TaxKnown && (p.Subtotal!=0 || p.Tax!=0))) throw new InvalidDataException("Invalid purchase");
        lock (gate)
        {
            using (var existing = db.Prepare("SELECT 1 FROM purchases WHERE id=? OR (world=? AND listing=?) LIMIT 1", p.Id,p.Character.World,p.ListingId))
                if (existing.Step()) return false;
            db.Execute("BEGIN IMMEDIATE");
            try
            {
                db.Execute($"INSERT INTO purchases ({Columns},subtotal) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    p.Id,p.Time,p.Character.Id,p.Character.Name,p.Character.HomeWorld,p.Character.World,p.ItemId,p.ItemName,p.Hq?1:0,
                    p.Quantity,p.UnitPrice,p.Tax,p.ListingId,p.RetainerId,p.RetainerName,p.RetainerCity,(int)p.Source,p.TaxKnown?1:0,p.Total,p.Subtotal);
                db.Execute("INSERT INTO characters (cid,name,home) VALUES (?,?,?) ON CONFLICT(cid) DO UPDATE SET name=excluded.name,home=excluded.home WHERE name<>excluded.name OR home<>excluded.home",p.Character.Id,p.Character.Name,p.Character.HomeWorld);
                db.Execute("COMMIT"); PurchaseWrites++; Interlocked.Increment(ref revision); cache.Clear();
                return true;
            }
            catch { try { db.Execute("ROLLBACK"); } catch { /* Preserve the original error. Idempotency keys allow explicit recovery. */ } throw; }
        }
    }
    private static (string Sql, object[] Args) Where(Filter f)
    {
        var sql = new StringBuilder(" WHERE 1=1"); var args = new List<object>();
        void Add(string clause, object value) { sql.Append(clause); args.Add(value); }
        if (f.CharacterId.Length > 0) Add(" AND cid=?",f.CharacterId);
        if (f.Source == -1) sql.Append(" AND source IN (0,1)");
        else if (f.Source >= 0) Add(" AND source=?",f.Source);
        if (f.World.Length > 0) Add(" AND world=? COLLATE NOCASE",f.World);
        if (f.Quality != 0) { sql.Append(" AND item>0"); Add(" AND hq=?",f.Quality==1 ? 1 : 0); }
        if (f.From > 0) Add(" AND time>=?",f.From);
        if (f.Until > 0) Add(" AND time<?",f.Until);
        if (f.Min > 0) Add(" AND total>=?",f.Min);
        if (f.Max > 0) Add(" AND total<=?",f.Max);
        if (f.Search.Length > 0)
        {
            string term = "%" + f.Search.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_") + "%";
            sql.Append(" AND (name LIKE ? ESCAPE '\\' OR seller LIKE ? ESCAPE '\\' OR CAST(item AS TEXT)=? OR retainer=?)");
            args.AddRange([term,term,f.Search,f.Search]);
        }
        return (sql.ToString(),args.ToArray());
    }
    private static string Order(Filter f) => f.Sort switch { 1 => "total DESC,time DESC,id DESC", 2 => "total ASC,time DESC,id DESC", 3 => "time ASC,id ASC", _ => "time DESC,id DESC" };
    public QueryResult Query(Filter filter, int page, bool monthly)
    {
        page = Math.Max(0,page);
        lock (gate)
        {
            var key = (filter,page,monthly);
            if (cache.TryGetValue(key,out var saved)) return saved;
            var (where,args) = Where(filter);
            var rows = new List<Purchase>(); var buckets = new List<Bucket>(); var characters = new List<Character>();
            Totals totals;
            // A page change reuses summary/graph/character data from any cached page of this filter.
            var related = cache.FirstOrDefault(x => x.Key.Item1 == filter && x.Key.Item3 == monthly).Value;
            if (related != null) { totals = related.Totals; buckets.AddRange(related.Buckets); characters.AddRange(related.Characters); }
            else
            {
                using (var s = db.Prepare("SELECT COUNT(*),COALESCE(SUM(quantity),0),COALESCE(SUM(subtotal),0),COALESCE(SUM(tax),0),COALESCE(SUM(CASE WHEN tax_known=0 THEN total ELSE 0 END),0) FROM purchases"+where,args))
                { ReadStatements++; s.Step(); totals = new(s.Int(0),s.Int(1),s.Int(2),s.Int(3),s.Int(4)); RowsReturned++; }
                string bucket = monthly ? "substr(ledger_day(time),1,7)" : "ledger_day(time)";
                using (var s = db.Prepare($"SELECT {bucket} AS bucket,SUM(subtotal),SUM(tax),SUM(CASE WHEN tax_known=0 THEN total ELSE 0 END) FROM purchases{where} GROUP BY bucket ORDER BY bucket DESC LIMIT 366",args))
                { ReadStatements++; while(s.Step()) { buckets.Add(new(s.Text(0),s.Int(1),s.Int(2),s.Int(3))); RowsReturned++; } }
                buckets.Reverse();
                using (var s = db.Prepare("SELECT cid,name,home FROM characters ORDER BY name LIMIT 1000"))
                { ReadStatements++; while(s.Step()) { characters.Add(new(s.Text(0),s.Text(1),s.Text(2),"")); RowsReturned++; } }
            }
            using (var s = db.Prepare($"SELECT {Columns} FROM purchases{where} ORDER BY {Order(filter)} LIMIT ? OFFSET ?",[..args,PageSize,checked((long)page*PageSize)]))
            { ReadStatements++; while(s.Step()) { rows.Add(Read(s)); RowsReturned++; } }
            var result = new QueryResult(rows,totals,buckets,characters,Revision,page);
            if (cache.Count >= 8) cache.Remove(cache.Keys.First());
            cache[key] = result; return result;
        }
    }
    private static Purchase Read(Sqlite.Statement s) => new(s.Text(0),s.Int(1),new(s.Text(2),s.Text(3),s.Text(4),s.Text(5)),
        (uint)s.Int(6),s.Text(7),s.Int(8)!=0,(uint)s.Int(9),(uint)s.Int(10),(uint)s.Int(11),s.Text(12),s.Text(13),s.Text(14),(int)s.Int(15),
        (ExpenseSource)s.Int(16),s.Int(17)==0?s.Int(18):null,s.Int(17)!=0);
    public static string CsvCell(string text)
    {
        // Spreadsheet formula injection protection, including leading whitespace and control characters.
        var check = text.TrimStart();
        if (check.Length > 0 && "=+-@".Contains(check[0]) || text.StartsWith('\t') || text.StartsWith('\r') || text.StartsWith('\n')) text = "'" + text;
        return "\"" + text.Replace("\"","\"\"") + "\"";
    }
    public long Export(Filter filter, string path, CancellationToken cancellation = default)
    {
        // Serialize against inserts for a consistent snapshot. Stream rows: export memory stays bounded.
        lock (gate)
        {
            var (where,args) = Where(filter);
            string partial = path + ".partial";
            try
            {
                long count = 0;
                using (var file = new FileStream(partial,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                using (var writer = new StreamWriter(file,new UTF8Encoding(true)))
                using (var s = db.Prepare($"SELECT {Columns} FROM purchases{where} ORDER BY {Order(filter)}",args))
                {
                    ReadStatements++;
                    writer.WriteLine("purchased_at_utc,purchased_at_local,character_id,character,home_world,purchase_world,item_id,item_name,hq,quantity,unit_price,subtotal,tax,total,listing_id,retainer_id,retainer_name,retainer_city_id,source,tax_known,seller_name");
                    while(s.Step())
                    {
                        cancellation.ThrowIfCancellationRequested();
                        var p = Read(s); var t = DateTimeOffset.FromUnixTimeSeconds(p.Time);
                        string[] cells = [t.ToString("O"),t.ToLocalTime().ToString("O"),p.Character.Id,p.Character.Name,p.Character.HomeWorld,p.Character.World,
                            p.ItemId==0?"":p.ItemId.ToString(),p.ItemName,p.ItemId==0?"":p.Hq?"true":"false",p.Quantity==0?"":p.Quantity.ToString(),p.Quantity==0?"":p.UnitPrice.ToString(),p.TaxKnown?p.Subtotal.ToString(CultureInfo.InvariantCulture):"",
                            p.TaxKnown?p.Tax.ToString():"",p.Total.ToString(CultureInfo.InvariantCulture),p.Source==ExpenseSource.Market?p.ListingId:"",p.RetainerId,p.Source==ExpenseSource.Market?p.RetainerName:"",p.RetainerCity==0?"":p.RetainerCity.ToString(),p.Source.ToString(),p.TaxKnown?"true":"false",p.RetainerName];
                        writer.WriteLine(string.Join(',',cells.Select(CsvCell))); count++; RowsReturned++;
                    }
                }
                File.Move(partial,path,false); return count;
            }
            catch { if (File.Exists(partial)) File.Delete(partial); throw; }
        }
    }
    public void Dispose() { lock (gate) { cache.Clear(); db.Dispose(); } }
    public void Refresh() { lock (gate) { cache.Clear(); Interlocked.Increment(ref revision); } }
}
