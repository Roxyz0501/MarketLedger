namespace MarketLedger;

public sealed record Character(string Id, string Name, string HomeWorld, string World);
public enum ExpenseSource { Market = 0, NpcShop = 1, Unclassified = 2, Transfer = 3 }
public sealed record Purchase(string Id, long Time, Character Character, uint ItemId, string ItemName,
    bool Hq, uint Quantity, uint UnitPrice, uint Tax, string ListingId, string RetainerId,
    string RetainerName, int RetainerCity, ExpenseSource Source = ExpenseSource.Market,
    long? PaidAmount = null, bool TaxKnown = true)
{
    public long Subtotal => checked((long)Quantity * UnitPrice);
    public long Total => PaidAmount ?? checked(Subtotal + Tax);
}
public sealed record Listing(ulong Id, ulong RetainerId, uint ItemId, bool Hq, uint Quantity,
    uint Price, string RetainerName, DateTimeOffset Received);
public sealed record PurchaseRequest(ulong ListingId, ulong RetainerId, uint ItemId, bool Hq,
    uint Quantity, uint Price, uint Tax, int City, Character Character, string ItemName, DateTimeOffset Time);
public sealed record Filter(string CharacterId = "", string Search = "", string World = "",
    int Quality = 0, long From = 0, long Until = 0, long Min = 0, long Max = 0, int Sort = 0, int Source = -1);
public sealed record Totals(long Count, long Quantity, long Subtotal, long Tax, long UnknownTaxTotal = 0)
{
    public long Total => checked(Subtotal + Tax + UnknownTaxTotal);
}
public sealed record Bucket(string Date, long Subtotal, long Tax, long UnknownTaxTotal = 0)
{
    public long Total => checked(Subtotal + Tax + UnknownTaxTotal);
}
public sealed record QueryResult(IReadOnlyList<Purchase> Rows, Totals Totals, IReadOnlyList<Bucket> Buckets,
    IReadOnlyList<Character> Characters, long Revision, int Page);

// Market transactions use matched server confirmations and reported taxes, never guessed tax rates.
public sealed class PurchaseTracker
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, Listing> listings = new();
    private readonly List<PurchaseRequest> pending = new();
    public int Unmatched { get; private set; }
    public int ListingCount { get { lock (gate) return listings.Count; } }
    public void Clear() { lock (gate) { listings.Clear(); pending.Clear(); } }
    public void Offer(IEnumerable<Listing> batch, DateTimeOffset now)
    {
        lock (gate)
        {
            foreach (var id in listings.Where(x => now - x.Value.Received > TimeSpan.FromMinutes(10)).Select(x => x.Key).ToArray()) listings.Remove(id);
            foreach (var listing in batch.Take(100)) listings[listing.Id] = listing;
            while (listings.Count > 1000) listings.Remove(listings.MinBy(x => x.Value.Received).Key);
        }
    }
    public void Request(PurchaseRequest request)
    {
        lock (gate)
        {
            pending.RemoveAll(x => request.Time - x.Time > TimeSpan.FromSeconds(30));
            // Repeated observation of the same request is not a second purchase.
            if (pending.Any(x => x.ListingId == request.ListingId)) return;
            if (pending.Count >= 16) { pending.Clear(); Unmatched++; }
            pending.Add(request);
        }
    }
    public Purchase? Confirm(uint rawItem, uint quantity, Character? current, DateTimeOffset now)
    {
        lock (gate)
        {
            pending.RemoveAll(x => now - x.Time > TimeSpan.FromSeconds(30));
            uint item = rawItem >= 1_000_000 ? rawItem - 1_000_000 : rawItem;
            var matches = pending.Where(x => x.ItemId == item && x.Quantity == quantity
                && x.Character.Id == current?.Id && x.Character.World == current.World).ToArray();
            // Confirmation lacks listing ID: never guess when multiple requests could match.
            if (item == 0 || quantity == 0 || matches.Length != 1)
            {
                pending.Clear(); Unmatched++; return null;
            }
            var r = matches[0]; pending.Remove(r);
            string name = "";
            if (listings.TryGetValue(r.ListingId, out var l) && l.RetainerId == r.RetainerId
                && l.ItemId == r.ItemId && l.Hq == r.Hq && l.Quantity == r.Quantity && l.Price == r.Price
                && now - l.Received <= TimeSpan.FromMinutes(10)) name = l.RetainerName;
            listings.Remove(r.ListingId);
            return new Purchase(Guid.NewGuid().ToString("N"), now.ToUnixTimeSeconds(), r.Character,
                r.ItemId, r.ItemName, r.Hq, r.Quantity, r.Price, r.Tax, r.ListingId.ToString(),
                r.RetainerId.ToString(), name, r.City);
        }
    }
}
