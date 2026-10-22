namespace MarketLedger;

public sealed class ArchivePump : IDisposable
{
    private readonly SemaphoreSlim signal=new(0,1);
    private readonly CancellationTokenSource stop=new();
    private readonly Func<int> archive;
    private readonly TimeSpan delay;
    private readonly Task worker;
    private int failures;
    private int disposed;
    public string? Error { get; private set; }
    public bool Stopped=>Volatile.Read(ref failures)>=3;
    public ArchivePump(Func<int> archive,TimeSpan? retryDelay=null)
    { this.archive=archive; delay=retryDelay??TimeSpan.FromSeconds(60); worker=Task.Run(Run); Signal(); }
    public void Signal() { if(Volatile.Read(ref disposed)==0 && signal.CurrentCount==0) { try { signal.Release(); } catch(SemaphoreFullException) { } catch(ObjectDisposedException) { } } }
    public void Retry() { Interlocked.Exchange(ref failures,0); Error=null; Signal(); }
    private async Task Run()
    {
        try
        {
            while(true)
            {
                await signal.WaitAsync(stop.Token);
                while(!Stopped)
                {
                    stop.Token.ThrowIfCancellationRequested();
                    try { int count=archive(); failures=0; Error=null; if(count==0) break; }
                    catch(Exception e)
                    {
                        Error=e.Message; Interlocked.Increment(ref failures);
                        if(Stopped) break;
                        await Task.Delay(delay,stop.Token);
                    }
                }
            }
        }
        catch(OperationCanceledException) when(stop.IsCancellationRequested) { }
    }
    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0) return;
        stop.Cancel(); worker.GetAwaiter().GetResult(); signal.Dispose(); stop.Dispose();
    }
}
