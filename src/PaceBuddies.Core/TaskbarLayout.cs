namespace PaceBuddies.Core;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public readonly record struct PixelRect(int Left,int Top,int Right,int Bottom)
{
    public int Width=>Right-Left;
    public int Height=>Bottom-Top;
}
public sealed record TaskbarPlacement(PixelRect Bounds,int[] CellWidths,bool ShowValues)
{
    public int ImageHeight { get; init; }
    public int[] ImageWidths { get; init; }=[];
    public long ContentWidth=>CellWidths.Sum(w=>(long)w);
    public bool Scrollable=>ContentWidth>Bounds.Width;
    public int ScrollButtonWidth { get; init; }
}

public static class TaskbarLayout
{
    public static TaskbarPlacement? Calculate(PixelRect bar,PixelRect notify,int occupiedRight,double scale,bool[] showValues,double[]? aspectRatios=null)
    {
        if(!double.IsFinite(scale)||scale<=0)scale=1;
        if(showValues.Length==0||bar.Width<=bar.Height||bar.Height<24*scale||notify.Left<=bar.Left||notify.Left>bar.Right)return null;
        var gap=Math.Max(2,(int)Math.Round(4*scale));
        var right=notify.Left-gap;
        var available=right-Math.Max(bar.Left+gap,occupiedRight+gap);
        var height=Math.Max(1,bar.Height-gap);
        var imageWidths=new int[showValues.Length];var widths=new int[showValues.Length];
        for(var i=0;i<widths.Length;i++)
        {
            var ratio=aspectRatios is not null&&i<aspectRatios.Length?aspectRatios[i]:1;
            if(!double.IsFinite(ratio)||ratio<=0)ratio=1;
            var naturalWidth=Math.Ceiling(height*ratio);
            if(naturalWidth>int.MaxValue-128*scale)return null;
            imageWidths[i]=Math.Max(1,(int)naturalWidth);
            widths[i]=Math.Max((int)Math.Ceiling(24*scale),imageWidths[i]+gap+(showValues[i]?(int)Math.Ceiling(56*scale):0));
        }
        var contentWidth=widths.Sum(w=>(long)w);
        var scrolling=contentWidth>available;
        var buttonWidth=scrolling?(int)Math.Ceiling(20*scale):0;
        if(available<=0||(scrolling&&available<buttonWidth*2+24*scale))return null;
        var viewportWidth=(int)Math.Min(contentWidth,available);
        var top=bar.Top+(bar.Height-height)/2;
        return new(new(right-viewportWidth,top,right,top+height),widths,true)
        {ImageHeight=height,ImageWidths=imageWidths,ScrollButtonWidth=buttonWidth};
    }
}
