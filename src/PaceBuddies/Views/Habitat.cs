using System.Windows.Media;

namespace PaceBuddies.Views;

public sealed class Habitat : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var w=ActualWidth;var h=ActualHeight;
        var gridPen=new Pen(new SolidColorBrush(Color.FromRgb(222,235,245)),1);
        for(double x=16;x<w;x+=24)for(double y=12;y<h-24;y+=24)dc.DrawRectangle(gridPen.Brush,null,new Rect(x,y,2,2));
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(211,228,237)),null,new Rect(0,h-23,w,1));
        var grass=new SolidColorBrush(Color.FromRgb(120,169,156));
        foreach(var x in new[]{w*.12,w*.18,w*.81,w*.87})
        {dc.DrawRectangle(grass,null,new Rect(x,h-34,3,11));dc.DrawRectangle(grass,null,new Rect(x-4,h-31,4,3));dc.DrawRectangle(grass,null,new Rect(x+3,h-29,4,3));}
        var petal=new SolidColorBrush(Color.FromRgb(245,182,119));
        dc.DrawRectangle(petal,null,new Rect(w*.18-2,h-39,7,6));
    }
}
