using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace MarketLedger;

public sealed partial class Plugin
{
    private static readonly HashSet<GameInventoryType> PersonalInventories=Enum.GetValues<GameInventoryType>()
        .Where(t=>t is GameInventoryType.Inventory1 or GameInventoryType.Inventory2 or GameInventoryType.Inventory3
            or GameInventoryType.Inventory4 or GameInventoryType.Crystals or GameInventoryType.EquippedItems
            || t.ToString().StartsWith("Armory",StringComparison.Ordinal)).ToHashSet();
    private DateTimeOffset inventoryReadyAt;
    private DateTimeOffset nextDrain;
    private uint actorWorld;
    private ulong actorId;
    private void OnInventory(IReadOnlyCollection<InventoryEventArgs> changes)
    {
        if(disposed || !config.Recording || !client.IsLoggedIn) return;
        RefreshActor();
        var who=actor; var now=DateTimeOffset.UtcNow;
        if(who==null || now<inventoryReadyAt) return;
        long gil=0,transfer=0;
        var items=new Dictionary<(uint Id,bool Hq),long>();
        void Add(GameInventoryItem item,int sign)
        {
            if(item.ItemId==0) return;
            if(item.ItemId==1 && item.ContainerType==GameInventoryType.Currency) gil+=(long)sign*item.Quantity;
            else if(item.ItemId==1 && item.ContainerType is GameInventoryType.RetainerGil or GameInventoryType.FreeCompanyGil) transfer+=(long)sign*item.Quantity;
            else if(PersonalInventories.Contains(item.ContainerType))
            {
                var key=(item.BaseItemId,item.IsHq);
                items[key]=items.GetValueOrDefault(key)+(long)sign*item.Quantity;
            }
        }
        foreach(var change in changes)
        {
            switch(change)
            {
                case InventoryItemChangedArgs changed: Add(changed.OldItemState,-1); Add(changed.Item,1); break;
                case InventoryItemRemovedArgs removed: Add(removed.Item,-1); break;
                // A newly loaded currency container is a baseline, never income or an expense.
                case InventoryItemAddedArgs added when PersonalInventories.Contains(added.Item.ContainerType): Add(added.Item,1); break;
            }
        }
        if(gil>=0 && !spending.HasPending) return;
        spending.Observe(who,now,Math.Max(0,-gil),items.Where(x=>x.Value>0).Select(x=>new ItemGain(x.Key.Id,x.Key.Hq,x.Value)).ToArray(),Math.Max(0,transfer));
        DrainSpending(now);
    }
    private void DrainSpending(DateTimeOffset now,bool flush=false)
    {
        foreach(var purchase in spending.Drain(now,flush)) QueuePurchase(purchase);
    }
    private void QueuePurchase(Purchase purchase)
    { if(!writes.Writer.TryWrite(purchase)) RecordFailure(purchase); }
}
