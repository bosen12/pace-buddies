namespace PaceBuddies.Core;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public readonly record struct PixelRect(int Left,int Top,int Right,int Bottom)
{
    public int Width=>Right-Left;
    public int Height=>Bottom-Top;
}
public sealed record TaskbarPlacement(PixelRect Bounds,int[] CellWidths,bool ShowValues);

public static class TaskbarLayout
{
    public static TaskbarPlacement? Calculate(PixelRect bar,PixelRect notify,int occupiedRight,double scale,bool[] showValues)
    {
        if(!double.IsFinite(scale)||scale<=0)scale=1;
        if(showValues.Length==0||bar.Width<=bar.Height||bar.Height<24*scale||notify.Left<=bar.Left||notify.Left>bar.Right)return null;
        var gap=Math.Max(2,(int)Math.Round(4*scale));
        var right=notify.Left-gap;
        var available=right-Math.Max(bar.Left+gap,occupiedRight+gap);
        var widths=showValues.Select(v=>(int)Math.Round((v?64:38)*scale)).ToArray();
        var values=true;
        if(widths.Sum()>available)
        {
            var cell=Math.Min((int)Math.Round(38*scale),available/showValues.Length);
            if(cell<24*scale)return null;
            widths=Enumerable.Repeat(cell,showValues.Length).ToArray();values=false;
        }
        var height=Math.Max(1,bar.Height-gap);
        var top=bar.Top+(bar.Height-height)/2;
        return new(new(right-widths.Sum(),top,right,top+height),widths,values);
    }
}
