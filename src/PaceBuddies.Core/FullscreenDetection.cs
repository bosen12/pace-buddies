namespace PaceBuddies.Core;

public static class FullscreenDetection
{
    // Client bounds exclude title bars and resize borders, which can extend beyond a monitor.
    public static bool CoversMonitor(PixelRect client,PixelRect monitor,int tolerance=2)
    {
        return client.Width>0&&client.Height>0&&monitor.Width>0&&monitor.Height>0
            &&Math.Abs((long)client.Left-monitor.Left)<=tolerance
            &&Math.Abs((long)client.Top-monitor.Top)<=tolerance
            &&Math.Abs((long)client.Right-monitor.Right)<=tolerance
            &&Math.Abs((long)client.Bottom-monitor.Bottom)<=tolerance;
    }
}
