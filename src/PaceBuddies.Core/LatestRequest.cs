namespace PaceBuddies.Core;

// Called by the UI thread; worker code receives only the cancellation token.
public sealed class LatestRequest : IDisposable
{
    public readonly record struct Ticket(long Version,CancellationToken Cancellation);
    private CancellationTokenSource? source;
    private long version;
    private bool disposed;
    public Ticket Begin()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        Invalidate();source=new();return new(version,source.Token);
    }
    public bool IsCurrent(Ticket ticket)=>!disposed&&ticket.Version==version&&!ticket.Cancellation.IsCancellationRequested;
    public void Invalidate(){version++;source?.Cancel();source?.Dispose();source=null;}
    public void Dispose(){Invalidate();disposed=true;}
}
