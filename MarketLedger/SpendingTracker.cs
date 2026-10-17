namespace MarketLedger;

public sealed record NpcRequest(string Id, DateTimeOffset Time, Character Character, uint ItemId,
    string ItemName, bool Hq, uint Quantity, uint UnitPrice, string Seller)
{
    public long Total => checked((long)Quantity * UnitPrice);
}
public sealed record ItemGain(uint ItemId, bool Hq, long Quantity);

// Reconcile event deltas in memory; never poll balances or infer purchases from a button click alone.
public sealed class SpendingTracker
{
    private sealed class Money(Character who, DateTimeOffset time, long amount)
    { public Character Who=who; public DateTimeOffset Time=time; public long Remaining=amount; }
    private sealed class Gain(Character who, DateTimeOffset time, ItemGain item)
    { public Character Who=who; public DateTimeOffset Time=time; public ItemGain Item=item; public long Remaining=item.Quantity; }
    private readonly object gate=new();
    private readonly List<Money> debits=[];
    private readonly List<Money> known=[];
    private readonly List<Gain> gains=[];
    private readonly List<NpcRequest> requests=[];
    private readonly Dictionary<string,(Character Who,DateTimeOffset Time)> marketRequests=new();
    private readonly Queue<Purchase> ready=new();
    public int Overflow { get; private set; }
    public bool HasPending { get { lock(gate) return debits.Count+known.Count+gains.Count+requests.Count+ready.Count+marketRequests.Count>0; } }
    private static bool Same(Character a,Character b)=>a.Id==b.Id && a.World==b.World;
    private static bool Near(DateTimeOffset a,DateTimeOffset b)=>(a-b).Duration()<=TimeSpan.FromSeconds(30);
    public void Clear() { lock(gate) { debits.Clear(); known.Clear(); gains.Clear(); requests.Clear(); ready.Clear(); marketRequests.Clear(); } }
    public void MarketRequested(string listingId,Character who,DateTimeOffset now)
    { lock(gate) { if(marketRequests.Count<64) marketRequests[listingId]=(who,now); else Overflow++; } }
    public void Request(NpcRequest request)
    {
        lock(gate)
        {
            if(request.ItemId==0 || request.Quantity==0 || request.UnitPrice==0 || request.Total>999_999_999) return;
            if(requests.Count>=64) { Overflow++; return; }
            requests.Add(request);
        }
    }
    // A known market purchase is already recorded separately; consume its observed wallet delta once.
    public void Known(Purchase purchase,DateTimeOffset now)
    {
        lock(gate)
        {
            marketRequests.Remove(purchase.ListingId);
            if(known.Count>=256) { Overflow++; return; }
            known.Add(new(purchase.Character,now,purchase.Total)); ReconcileKnown();
        }
    }
    public void Observe(Character who,DateTimeOffset now,long decrease,IReadOnlyList<ItemGain> items,long transferIncrease=0)
    {
        lock(gate)
        {
            // Only an exact same-batch balance increase is labelled as a transfer.
            if(decrease>0 && transferIncrease==decrease)
            { Enqueue(Outflow(who,now,decrease,ExpenseSource.Transfer)); decrease=0; }
            if(decrease>0)
            {
                if(debits.Count>=256) { Overflow++; Enqueue(Outflow(who,now,decrease,ExpenseSource.Unclassified)); }
                else debits.Add(new(who,now,decrease));
            }
            foreach(var item in items.Where(x=>x.Quantity>0))
            {
                // Keep gains only while an observed NPC buy request could use them.
                if(!requests.Any(r=>Same(r.Character,who) && r.ItemId==item.ItemId && r.Hq==item.Hq && now>=r.Time && Near(now,r.Time))) continue;
                if(gains.Count>=512) { Overflow++; break; }
                gains.Add(new(who,now,item));
            }
            ReconcileKnown(); ReconcileNpc(now);
        }
    }
    private void ReconcileKnown()
    {
        foreach(var k in known)
            foreach(var d in debits.Where(d=>Same(d.Who,k.Who) && Near(d.Time,k.Time)))
            { long amount=Math.Min(k.Remaining,d.Remaining); k.Remaining-=amount; d.Remaining-=amount; }
        known.RemoveAll(x=>x.Remaining==0); debits.RemoveAll(x=>x.Remaining==0);
    }
    private void ReconcileNpc(DateTimeOffset now)
    {
        foreach(var r in requests.ToArray())
        {
            if(now-r.Time>TimeSpan.FromSeconds(30)) { requests.Remove(r); continue; }
            if(marketRequests.Values.Any(x=>Same(x.Who,r.Character))) continue;
            var money=debits.Where(d=>Same(d.Who,r.Character) && d.Time>=r.Time && Near(d.Time,r.Time)).ToArray();
            var items=gains.Where(g=>Same(g.Who,r.Character) && g.Item.ItemId==r.ItemId && g.Item.Hq==r.Hq && g.Time>=r.Time && Near(g.Time,r.Time)).ToArray();
            var similar=requests.Where(x=>Same(x.Character,r.Character) && x.ItemId==r.ItemId && x.Hq==r.Hq).ToArray();
            // A failed request followed by another buy of the same item cannot be assigned a guessed price.
            if(similar.Length>1 && (items.Sum(x=>x.Remaining)<similar.Sum(x=>(long)x.Quantity) || money.Sum(x=>x.Remaining)<similar.Sum(x=>x.Total))) continue;
            if(money.Sum(x=>x.Remaining)<r.Total || items.Sum(x=>x.Remaining)<r.Quantity) continue;
            long cost=r.Total,quantity=r.Quantity;
            foreach(var d in money) { long n=Math.Min(cost,d.Remaining); cost-=n; d.Remaining-=n; }
            foreach(var g in items) { long n=Math.Min(quantity,g.Remaining); quantity-=n; g.Remaining-=n; }
            Enqueue(new(r.Id,now.ToUnixTimeSeconds(),r.Character,r.ItemId,r.ItemName,r.Hq,r.Quantity,r.UnitPrice,0,
                "npc:"+r.Id,"",r.Seller,0,ExpenseSource.NpcShop));
            requests.Remove(r);
        }
        debits.RemoveAll(x=>x.Remaining==0); gains.RemoveAll(x=>x.Remaining==0);
    }
    private static Purchase Outflow(Character who,DateTimeOffset now,long amount,ExpenseSource source)
    {
        string id=Guid.NewGuid().ToString("N");
        return new(id,now.ToUnixTimeSeconds(),who,0,"",false,0,0,0,"outflow:"+id,"","",0,source,amount,false);
    }
    private void Enqueue(Purchase p) { if(ready.Count<512) ready.Enqueue(p); else Overflow++; }
    public IReadOnlyList<Purchase> Drain(DateTimeOffset now,bool flush=false)
    {
        lock(gate)
        {
            ReconcileKnown(); ReconcileNpc(now);
            foreach(var d in debits.Where(d=>flush || now-d.Time>TimeSpan.FromSeconds(30)).ToArray())
            { Enqueue(Outflow(d.Who,d.Time,d.Remaining,ExpenseSource.Unclassified)); debits.Remove(d); }
            known.RemoveAll(x=>flush || now-x.Time>TimeSpan.FromSeconds(30));
            gains.RemoveAll(x=>flush || now-x.Time>TimeSpan.FromSeconds(30));
            requests.RemoveAll(x=>flush || now-x.Time>TimeSpan.FromSeconds(30));
            foreach(var key in marketRequests.Where(x=>flush || now-x.Value.Time>TimeSpan.FromSeconds(30)).Select(x=>x.Key).ToArray()) marketRequests.Remove(key);
            var result=ready.ToArray(); ready.Clear(); return result;
        }
    }
}
