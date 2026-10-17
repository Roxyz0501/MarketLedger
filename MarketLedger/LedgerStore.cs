using System.Globalization;
using System.Text;

namespace MarketLedger;

public sealed class LedgerStore : IDisposable
{
    private readonly object gate = new();
    private readonly Sqlite db;
    private readonly Dictionary<(Filter, int, bool), QueryResult> cache = new();
    private long revision;
    public long Revision => Interlocked.Read(ref revision);
    public long ReadStatements { get; private set; }
    public long PurchaseWrites { get; private set; }
    public long RowsReturned { get; private set; }
    public const int PageSize = 100;
    private const string Columns = "id,time,cid,character,home,world,item,name,hq,quantity,unit,tax,listing,retainer,seller,city";
    public LedgerStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        db = new Sqlite(path);
        try
        {
            using (var v = db.Prepare("PRAGMA user_version"))
            { v.Step(); if (v.Int(0) > 1) throw new InvalidDataException("Unsupported future ledger schema; no changes made."); }
            db.Execute("PRAGMA journal_mode=WAL");
            db.Execute("PRAGMA synchronous=FULL");
            db.Execute("PRAGMA cache_size=-4096");
            db.Execute("CREATE TABLE IF NOT EXISTS purchases (id TEXT PRIMARY KEY,time INTEGER NOT NULL,cid TEXT NOT NULL,character TEXT NOT NULL,home TEXT NOT NULL,world TEXT NOT NULL,item INTEGER NOT NULL,name TEXT NOT NULL,hq INTEGER NOT NULL,quantity INTEGER NOT NULL,unit INTEGER NOT NULL,tax INTEGER NOT NULL,listing TEXT NOT NULL,retainer TEXT NOT NULL,seller TEXT NOT NULL,city INTEGER NOT NULL,subtotal INTEGER NOT NULL,total INTEGER NOT NULL,UNIQUE(world,listing))");
            db.Execute("CREATE TABLE IF NOT EXISTS characters (cid TEXT PRIMARY KEY,name TEXT NOT NULL,home TEXT NOT NULL)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_date ON purchases(time DESC,id DESC)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_character_date ON purchases(cid,time DESC,id DESC)");
            db.Execute("CREATE INDEX IF NOT EXISTS purchase_world_date ON purchases(world,time DESC,id DESC)");
            db.Execute("PRAGMA user_version=1");
        }
        catch { db.Dispose(); throw; }
    }
    public bool Insert(Purchase p)
    {
        if (p.Quantity == 0 || p.ItemId == 0 || string.IsNullOrEmpty(p.Character.Id) || p.ListingId == "0") throw new InvalidDataException("Invalid purchase");
        lock (gate)
        {
            using (var existing = db.Prepare("SELECT 1 FROM purchases WHERE id=? OR (world=? AND listing=?) LIMIT 1", p.Id,p.Character.World,p.ListingId))
                if (existing.Step()) return false;
            db.Execute("BEGIN IMMEDIATE");
            try
            {
                db.Execute($"INSERT INTO purchases ({Columns},subtotal,total) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    p.Id,p.Time,p.Character.Id,p.Character.Name,p.Character.HomeWorld,p.Character.World,p.ItemId,p.ItemName,p.Hq?1:0,
                    p.Quantity,p.UnitPrice,p.Tax,p.ListingId,p.RetainerId,p.RetainerName,p.RetainerCity,p.Subtotal,p.Total);
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
        if (f.World.Length > 0) Add(" AND world=? COLLATE NOCASE",f.World);
        if (f.Quality != 0) Add(" AND hq=?",f.Quality==1 ? 1 : 0);
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
                using (var s = db.Prepare("SELECT COUNT(*),COALESCE(SUM(quantity),0),COALESCE(SUM(subtotal),0),COALESCE(SUM(tax),0) FROM purchases"+where,args))
                { ReadStatements++; s.Step(); totals = new(s.Int(0),s.Int(1),s.Int(2),s.Int(3)); RowsReturned++; }
                string bucket = monthly ? "substr(ledger_day(time),1,7)" : "ledger_day(time)";
                using (var s = db.Prepare($"SELECT {bucket} AS bucket,SUM(subtotal),SUM(tax) FROM purchases{where} GROUP BY bucket ORDER BY bucket DESC LIMIT 366",args))
                { ReadStatements++; while(s.Step()) { buckets.Add(new(s.Text(0),s.Int(1),s.Int(2))); RowsReturned++; } }
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
        (uint)s.Int(6),s.Text(7),s.Int(8)!=0,(uint)s.Int(9),(uint)s.Int(10),(uint)s.Int(11),s.Text(12),s.Text(13),s.Text(14),(int)s.Int(15));
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
                    writer.WriteLine("purchased_at_utc,purchased_at_local,character_id,character,home_world,purchase_world,item_id,item_name,hq,quantity,unit_price,subtotal,tax,total,listing_id,retainer_id,retainer_name,retainer_city_id");
                    while(s.Step())
                    {
                        cancellation.ThrowIfCancellationRequested();
                        var p = Read(s); var t = DateTimeOffset.FromUnixTimeSeconds(p.Time);
                        string[] cells = [t.ToString("O"),t.ToLocalTime().ToString("O"),p.Character.Id,p.Character.Name,p.Character.HomeWorld,p.Character.World,
                            p.ItemId.ToString(),p.ItemName,p.Hq?"true":"false",p.Quantity.ToString(),p.UnitPrice.ToString(),p.Subtotal.ToString(CultureInfo.InvariantCulture),
                            p.Tax.ToString(),p.Total.ToString(CultureInfo.InvariantCulture),p.ListingId,p.RetainerId,p.RetainerName,p.RetainerCity.ToString()];
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
