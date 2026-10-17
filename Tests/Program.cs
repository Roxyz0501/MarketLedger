using MarketLedger;
using System.Diagnostics;
using System.Text;

if(args.Length>0 && args[0]=="--probe")
{
    using var probe=new Sqlite(":memory:");
    foreach(string sql in new[]{"SELECT sqlite_version()","SELECT strftime('%Y-%m-%d',1792264569,'unixepoch')","SELECT strftime('%Y-%m-%d',1792264569,'unixepoch','localtime')","SELECT datetime(1792264569,'unixepoch')","SELECT datetime('now')"})
    {using var s=probe.Prepare(sql); s.Step(); Console.WriteLine(sql+" => "+s.Text(0));}
    return;
}

int checks=0;
void Check(bool condition,string message) { checks++; if(!condition) throw new Exception(message); }
var folder=Path.GetFullPath(args.Length>0?args[0]:"test-results/"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var now=DateTimeOffset.UtcNow;
var a=new Character("18446744073709551614","Alice Test","Alpha","Beta");
var b=new Character("2","Bob Test","Beta","Beta");
var tracker=new PurchaseTracker();
PurchaseRequest Request(ulong id=100,uint item=42,uint quantity=3,uint price=100,uint tax=15,bool hq=false,Character? who=null)
    =>new(id,555,item,hq,quantity,price,tax,1,who??a,"試験用アイテム",now);
tracker.Request(Request());
Check(tracker.Confirm(42,3,a,now) is {Tax:15,Subtotal:300,Total:315,RetainerName:""},"Record confirmed purchase with unknown seller");
Check(tracker.Confirm(42,3,a,now)==null,"Ignore duplicate confirmation");
tracker.Offer([new(101,555,42,true,3,100,"Seller Name",now)],now);
tracker.Request(Request(101,hq:true));
Check(tracker.Confirm(1_000_042,3,a,now) is {Hq:true,RetainerName:"Seller Name"},"HQ ID normalization and matching seller");
tracker.Request(Request(102));
Check(tracker.Confirm(42,3,b,now)==null,"Character switch does not misattribute");
tracker.Request(Request(103));
Check(tracker.Confirm(42,3,a with {World="Gamma"},now)==null,"World switch does not misattribute");
tracker.Request(Request(104));
Check(tracker.Confirm(42,3,a,now.AddSeconds(31))==null,"Expired request ignored");
tracker.Request(Request(105)); tracker.Request(Request(106));
Check(tracker.Confirm(42,3,a,now)==null,"Ambiguous matching requests not guessed");
tracker.Request(Request(107));
Check(tracker.Confirm(0,0,a,now)==null,"Failure response is not a purchase");
Check(tracker.Confirm(42,3,a,now)==null,"Failure clears pending");
tracker.Request(Request(108)); tracker.Clear();
Check(tracker.Confirm(42,3,a,now)==null,"Logout clears request");
tracker.Request(Request(109)); tracker.Request(Request(109));
Check(tracker.Confirm(42,3,a,now)!=null,"Repeated request is coalesced");
tracker.Offer([new(110,999,42,false,3,100,"Wrong Seller",now)],now);
tracker.Request(Request(110));
Check(tracker.Confirm(42,3,a,now)!.RetainerName=="","Retainer mismatch cannot supply name");
tracker.Offer([new(111,555,42,false,3,100,"Old Seller",now.AddMinutes(-11))],now);
tracker.Request(Request(111));
Check(tracker.Confirm(42,3,a,now)!.RetainerName=="","Expired seller cannot supply name");
for(uint i=0;i<1200;i++) tracker.Offer([new(i+5000,555,42,false,3,100,"Seller",now)],now);
Check(tracker.ListingCount<=1000,"Listing cache bounded");

string dbfile=Path.Combine(folder,"ledger.db");
using(var store=new LedgerStore(dbfile))
{
    Purchase P(int i,Character? who=null)=>new(i.ToString(),now.AddDays(-(i%400)).ToUnixTimeSeconds(),who??a,42,"試験用アイテム",i%2==0,3,100,15,(10000+i).ToString(),"555",i==0?"=HYPERLINK(\"bad\")":"Seller, \"Name\"",1);
    Check(store.Insert(P(0)),"First insert");
    Check(!store.Insert(P(0)),"Idempotent insert");
    Check(!store.Insert(P(0) with {Id="duplicate"}),"Listing ID deduplicates");
    var sw=Stopwatch.StartNew();
    for(int i=1;i<1000;i++) store.Insert(P(i,i%2==0?a:b));
    sw.Stop();
    var q=store.Query(new(),0,false);
    Check(q.Totals==new Totals(1000,3000,300000,15000),"Exact integer aggregate");
    Check(q.Rows.Count==100,"Bounded page");
    Check(q.Buckets.Count==366,$"Bounded graph: {q.Buckets.Count}; first={q.Buckets.FirstOrDefault()}");
    Check(q.Characters.Count==2,"Character index");
    long reads=store.ReadStatements,writes=store.PurchaseWrites,rows=store.RowsReturned;
    for(int i=0;i<100;i++) Check(ReferenceEquals(q,store.Query(new(),0,false)),"Unchanged query reuses snapshot");
    Check(store.ReadStatements==reads && store.PurchaseWrites==writes && store.RowsReturned==rows,"Unchanged views have zero database reads/writes/returned rows");
    var second=store.Query(new(),1,false);
    Check(store.ReadStatements==reads+1,"Page movement performs only one bounded row query");
    Check(!q.Rows.Select(x=>x.Id).Intersect(second.Rows.Select(x=>x.Id)).Any(),"Stable paging has no duplicates");
    Check(store.Query(new(CharacterId:a.Id),0,false).Totals.Count==500,"Character filter");
    Check(store.Query(new(Quality:1),0,false).Totals.Count==500,"HQ filter");
    Check(store.Query(new(Quality:2),0,false).Totals.Count==500,"NQ filter");
    Check(store.Query(new(Search:"42"),0,false).Totals.Count==1000,"Exact item ID search");
    Check(store.Query(new(Search:"555"),0,false).Totals.Count==1000,"Exact retainer ID search");
    Check(store.Query(new(Search:"試験"),0,false).Totals.Count==1000,"Japanese item substring");
    Check(store.Query(new(Search:"Seller"),0,false).Totals.Count==999,"Retainer substring");
    Check(store.Query(new(Search:"%"),0,false).Totals.Count==0,"Wildcard search treated literally");
    Check(store.Query(new(Search:"' OR 1=1 --"),0,false).Totals.Count==0,"SQL injection treated literally");
    Check(store.Query(new(World:"beta"),0,false).Totals.Count==1000,"World filter");
    Check(store.Query(new(Min:316),0,false).Totals.Count==0,"Minimum total filter");
    Check(store.Query(new(Max:314),0,false).Totals.Count==0,"Maximum total filter");
    Check(store.Query(new(From:now.ToUnixTimeSeconds(),Until:now.AddSeconds(1).ToUnixTimeSeconds()),0,false).Totals.Count==3,"Inclusive start and exclusive end");
    Check(store.Query(new(From:now.AddSeconds(1).ToUnixTimeSeconds()),0,false).Totals.Count==0,"Date exclusion");
    Check(store.Query(new(),0,true).Buckets.Count<=15,"Monthly grouping");
    Check(store.Query(new(Sort:3),0,false).Rows.First().Time<=q.Rows.First().Time,"Oldest sort");
    long before=store.ReadStatements;
    Parallel.For(0,20,_=>store.Query(new(Search:"Name"),0,true));
    Check(store.ReadStatements==before+4,"Concurrent identical queries coalesced by cache/lock");
    var old=store.Query(new(),0,false);
    store.Insert(P(1001) with {Quantity=1,UnitPrice=2_000_000_000,Tax=0});
    var updated=store.Query(new(),0,false);
    Check(updated.Revision>old.Revision && updated.Totals.Total==315000+2_000_000_000L,"Writes invalidate caches and preserve 64-bit sums");
    Check(store.Query(new(Sort:1),0,false).Rows[0].UnitPrice==2_000_000_000,"Highest first sort");
    string csv=Path.Combine(folder,"export.csv");
    Check(store.Export(new(CharacterId:a.Id),csv)==501,"CSV includes all filtered pages");
    byte[] bytes=File.ReadAllBytes(csv);
    Check(bytes.Take(3).SequenceEqual(new byte[]{239,187,191}),"UTF8 BOM");
    string text=Encoding.UTF8.GetString(bytes);
    Check(text.Contains("\"'=HYPERLINK(\"\"bad\"\")\""),"CSV formulas neutralized");
    Check(text.Contains("\"Seller, \"\"Name\"\"\""),"CSV quotes/commas escaped");
    Check(text.Contains("subtotal,tax,total"),"CSV has tax-exclusive/tax/inclusive fields");
    Check(text.Contains(a.Id),"Unsigned character IDs preserved as text");
    var cancel=new CancellationToken(true);
    bool cancelled=false;
    try { store.Export(new(),Path.Combine(folder,"cancel.csv"),cancel); } catch(OperationCanceledException) { cancelled=true; }
    Check(cancelled && !File.Exists(Path.Combine(folder,"cancel.csv.partial")),"Cancelled export cleaned up");
    Console.WriteLine($"1000 durable purchases: {sw.ElapsedMilliseconds}ms. Cold read: 4 SELECTs; unchanged reads: 0; page: 1 SELECT / <=100 rows; concurrent same-filter: 4 SELECTs total. No business writes during browsing. Network bytes: 0 by design.");
}
using(var reopened=new LedgerStore(dbfile)) Check(reopened.Query(new(),0,false).Totals.Count==1001,"Restart persistence");
Check(LedgerStore.CsvCell("\t=1").StartsWith("\"'"),"CSV leading tab protected");
Check(LedgerStore.CsvCell("  +1").StartsWith("\"'"),"CSV leading spaces protected");
Check(LedgerStore.CsvCell("line1\nline2")=="\"line1\nline2\"","CSV newlines quoted");
Loc.Validate();
foreach(var code in Loc.Codes)
    foreach(var pair in Loc.Text) Check(Loc.Get(code,pair.Key)==pair.Value[Array.IndexOf(Loc.Codes,code)],"Translation "+code+" "+pair.Key);
Check(Loc.Resolve("ko","ja","en")=="ko","Saved selection wins");
Check(Loc.Resolve(null,"ja","en")=="ja","Game language wins");
Check(Loc.Resolve(null,null,"de-DE")=="de","Dalamud fallback");
Check(Loc.Resolve("Auto","zh","bad")=="en","Ambiguous Chinese and Auto fall back");
Check(Loc.Normalize("ZH_tw")=="zh-Hant","Traditional normalization");
Check(Loc.Normalize("zh-SG")=="zh-Hans","Simplified normalization");
Check(Loc.Normalize("en-GB")=="en","Region normalization");
Check(Loc.Get("bad","History")==Loc.Get("en","History"),"English fallback");
string future=Path.Combine(folder,"future.db");
using(var db=new Sqlite(future)) db.Execute("PRAGMA user_version=99");
bool rejected=false; try { using var bad=new LedgerStore(future); } catch(InvalidDataException) { rejected=true; }
Check(rejected,"Future schema rejected without migration");
Console.WriteLine($"PASS: {checks} checks. Artifacts: {folder}");
