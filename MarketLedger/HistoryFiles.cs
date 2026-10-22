namespace MarketLedger;

public sealed record FileLocation(string Directory,long Count,long Bytes);
public sealed record FileMeasurement(IReadOnlyList<FileLocation> Locations)
{
    public long Count=>Locations.Sum(x=>x.Count);
    public long Bytes=>Locations.Sum(x=>x.Bytes);
}
public static class HistoryFiles
{
    public static FileMeasurement Measure(string directory,string archive,CancellationToken cancellation=default)
    {
        var result=new List<FileLocation>();
        void Count(string path,IEnumerable<string> files)
        {
            long count=0,bytes=0;
            foreach(var file in files) { cancellation.ThrowIfCancellationRequested(); var info=new FileInfo(file); bytes=checked(bytes+info.Length); count++; }
            result.Add(new(path,count,bytes));
        }
        Count(directory,Directory.EnumerateFiles(directory).Where(x=>Path.GetFileName(x).StartsWith("market-ledger.db",StringComparison.Ordinal)
            || Path.GetFileName(x).StartsWith("recovery-",StringComparison.Ordinal)));
        var options=new EnumerationOptions { RecurseSubdirectories=true,IgnoreInaccessible=false,AttributesToSkip=FileAttributes.ReparsePoint };
        Count(archive,Directory.Exists(archive)?Directory.EnumerateFiles(archive,"*",options):[]);
        string exports=Path.Combine(directory,"exports");
        Count(exports,Directory.Exists(exports)?Directory.EnumerateFiles(exports).Where(x=>x.EndsWith(".csv",StringComparison.OrdinalIgnoreCase)||x.EndsWith(".partial",StringComparison.OrdinalIgnoreCase)):[]);
        return new(result);
    }
}
