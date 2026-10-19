using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PaceBuddies.Views;

public sealed class MetricTile : Border
{
    private readonly TextBlock value,status;
    private readonly Polyline line;
    private readonly Queue<double?> history=[];
    public MetricTile(string name,string description,string color)
    {
        Background=Brushes.White;CornerRadius=new CornerRadius(12);Padding=new Thickness(16,12,16,10);Margin=new Thickness(0,0,10,0);
        var grid=new Grid();grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=new GridLength(96)});
        var stack=new StackPanel();
        stack.Children.Add(new TextBlock { Text=name,FontSize=11,Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),FontWeight=FontWeights.Bold });
        value=new TextBlock { Text="—",FontFamily=new FontFamily("Cascadia Mono, Consolas"),FontSize=26,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,2,0,2) };stack.Children.Add(value);
        status=new TextBlock { Text=description,FontSize=10,Foreground=(Brush)Application.Current.FindResource("Muted") };stack.Children.Add(status);grid.Children.Add(stack);
        var canvas=new Canvas { Width=96,Height=45,VerticalAlignment=VerticalAlignment.Center,ClipToBounds=true };
        line=new Polyline { Stroke=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),StrokeThickness=2,StrokeLineJoin=PenLineJoin.Round };
        canvas.Children.Add(line);Grid.SetColumn(canvas,1);grid.Children.Add(canvas);Child=grid;
    }
    public void Update(double? number,string? tooltip=null)
    {
        value.Text=number is {} n?$"{n:0}%":"—";status.Text=number is null?"無法讀取":"最近 60 秒";
        ToolTip=tooltip;history.Enqueue(number);if(history.Count>60)history.Dequeue();
        line.Points=new PointCollection(history.Select((v,i)=>new Point(i*96.0/59,43-(v??0)*.4)));
        line.Opacity=number is null?.25:1;
    }
}
