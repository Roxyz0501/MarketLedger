using MarketLedger;

internal static class ReviewTests
{
    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        int checks=0,failures=0;
        void Check(bool value,string label) { checks++; Console.WriteLine($"{(value?"PASS":"FAIL")}: {label}"); if(!value) failures++; }
        using var store=new LedgerStore(Path.Combine(directory,"review.db"));
        var actor=new Character("review","Synthetic","Alpha","Alpha");
        var valid=new Purchase("valid",DateTimeOffset.UtcNow.ToUnixTimeSeconds(),actor,42,"Sample",false,1,100,5,"valid-listing","","",0);
        store.Insert(valid);
        string occupied=Path.Combine(directory,"occupied.csv");
        File.WriteAllText(occupied+".partial","existing export must survive");
        bool collision=false;
        try { store.Export(new(),occupied); } catch(IOException) { collision=true; }
        Check(collision && File.Exists(occupied+".partial") && File.ReadAllText(occupied+".partial")=="existing export must survive","An existing partial export survives a name collision");
        string cancelled=Path.Combine(directory,"empty-cancel.csv"); bool cancelledResult=false;
        try { store.Export(new(Search:"no matching rows"),cancelled,new CancellationToken(true)); }
        catch(OperationCanceledException) { cancelledResult=true; }
        Check(cancelledResult && !File.Exists(cancelled) && !File.Exists(cancelled+".partial"),"Cancellation also prevents an empty CSV from being published");
        long before=store.PurchaseWrites;
        foreach(var bad in new[]{valid with {Id="",ListingId="empty-id"},valid with {Id="bad-time",ListingId="bad-time",Time=long.MaxValue},
            valid with {Id="wrong-total",ListingId="wrong-total",Source=ExpenseSource.Transfer,PaidAmount=200}})
        {
            bool rejected=false; try { store.Insert(bad); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Malformed recovery row rejected: "+bad.ListingId);
        }
        Check(store.PurchaseWrites==before,"Malformed recovery data performs zero transaction writes");
        Console.WriteLine($"Review: {checks-failures}/{checks} passed.");
        if(failures>0) throw new Exception($"{failures} review regressions");
    }
}
