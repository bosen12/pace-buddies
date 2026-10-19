namespace PaceBuddies.Core;

public static class DesktopGeometry
{
    public const int MinimumHeight=48,MaximumHeight=480;
    public static bool CanManipulate(BuddyConfig buddy)=>!buddy.Locked&&!buddy.ClickThrough;
    public static int ClampHeight(int height)=>Math.Clamp(height,MinimumHeight,MaximumHeight);
    public static double FitHeight(int height,double workingHeight)=>Math.Min(ClampHeight(height),Math.Max(1,workingHeight-28));
    public static int ResizeHeight(int initial,double deltaX,double deltaY,double aspectRatio)
    {
        if(!double.IsFinite(aspectRatio)||aspectRatio<=0)aspectRatio=1;
        var horizontal=deltaX/aspectRatio;
        var delta=Math.Abs(deltaY)>=Math.Abs(horizontal)?deltaY:horizontal;
        return (int)Math.Round(Math.Clamp(initial+delta,MinimumHeight,MaximumHeight));
    }
}
