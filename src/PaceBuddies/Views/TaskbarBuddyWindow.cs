using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using PaceBuddies.Animation;
using PaceBuddies.Services;

namespace PaceBuddies.Views;

public sealed class TaskbarBuddyWindow : Window
{
    private readonly StackPanel row=new(){Orientation=Orientation.Horizontal};
    private readonly Grid host=new(){Background=Brushes.Transparent,ClipToBounds=true};
    private readonly ScrollViewer viewport=new(){HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false,PanningMode=PanningMode.HorizontalOnly};
    private readonly Button previous=new(){Content="‹",Focusable=false,Padding=new Thickness(0),FontSize=20,Background=new SolidColorBrush(Color.FromArgb(160,32,49,74)),Foreground=Brushes.White,BorderThickness=new Thickness(0),ToolTip="向左查看夥伴"};
    private readonly Button next=new(){Content="›",Focusable=false,Padding=new Thickness(0),FontSize=20,Background=new SolidColorBrush(Color.FromArgb(160,32,49,74)),Foreground=Brushes.White,BorderThickness=new Thickness(0),ToolTip="向右查看夥伴"};
    private readonly DispatcherTimer tracker=new(){Interval=TimeSpan.FromMilliseconds(500)};
    private readonly Action<BuddyRuntime> open;
    private readonly Action<Action> edit;
    private readonly TaskbarWindowEvents windowEvents;
    private BuddyRuntime[] buddies=[];
    private string layoutKey="";
    private string status="";
    public TaskbarPlacement? Placement { get; private set; }
    public int BuddyCount=>buddies.Length;
    public string Status=>status;
    public event Action<string>? StatusChanged;
    public TaskbarBuddyWindow(Action<BuddyRuntime> open,Action<Action> edit,TimeSpan? pollingInterval=null)
    {
        this.open=open;this.edit=edit;
        Title="Pace Buddies · 工作列角色";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
        ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Content=host;
        host.ColumnDefinitions.Add(new(){Width=new GridLength(0)});host.ColumnDefinitions.Add(new());host.ColumnDefinitions.Add(new(){Width=new GridLength(0)});
        viewport.Content=row;Grid.SetColumn(viewport,1);Grid.SetColumn(next,2);host.Children.Add(previous);host.Children.Add(viewport);host.Children.Add(next);
        previous.Click+=(_,_)=>viewport.ScrollToHorizontalOffset(viewport.HorizontalOffset-Math.Max(48,viewport.ViewportWidth*.8));
        next.Click+=(_,_)=>viewport.ScrollToHorizontalOffset(viewport.HorizontalOffset+Math.Max(48,viewport.ViewportWidth*.8));
        viewport.ScrollChanged+=(_,_)=>{previous.IsEnabled=viewport.HorizontalOffset>.5;next.IsEnabled=viewport.HorizontalOffset<viewport.ScrollableWidth-.5;};
        host.PreviewMouseWheel+=(_,e)=>{if(Placement?.Scrollable!=true)return;viewport.ScrollToHorizontalOffset(viewport.HorizontalOffset-e.Delta);e.Handled=true;};
        SourceInitialized+=(_,_)=>
        {
            var handle=new WindowInteropHelper(this).Handle;
            var style=GetWindowLongPtr(handle,-20).ToInt64();
            SetWindowLongPtr(handle,-20,new IntPtr(style|0x80|0x08000000));
        };
        tracker.Interval=pollingInterval??TimeSpan.FromMilliseconds(500);
        tracker.Tick+=(_,_)=>RefreshAnchor();tracker.Start();
        windowEvents=new(Dispatcher,RefreshAnchor);
        Closed+=(_,_)=>{tracker.Stop();windowEvents.Dispose();};
    }
    public void Synchronize(IEnumerable<BuddyRuntime> values)
    {
        buddies=values.Where(b=>b.Config.ShowTray).ToArray();
        // Release removed buddies even while the taskbar is hidden or unavailable.
        if(row.Children.OfType<Border>().Any(cell=>cell.DataContext is BuddyRuntime b&&!buddies.Contains(b)))ClearCells();
        RefreshAnchor();
    }
    public void RefreshAnchor()
    {
        if(buddies.Length==0){ClearCells();Placement=null;Hide();SetStatus("工作列角色已關閉");return;}
        var anchor=NativeTaskbarLocator.Locate();
        if(anchor==null){Placement=null;Hide();SetStatus("無法定位工作列；角色不會移入隱藏圖示區。可先使用桌面模式。");return;}
        if(!anchor.Visible){Hide();SetStatus(anchor.HiddenReason==TaskbarHiddenReason.Fullscreen?"全螢幕程式使用中；離開全螢幕後會恢復角色":"跟隨工作列自動隱藏；工作列顯示後會恢復角色");return;}
        var placement=TaskbarLayout.Calculate(anchor.Bar,anchor.Notification,anchor.OccupiedRight,anchor.Scale,buddies.Select(b=>b.Config.Label==LabelMode.Always).ToArray(),buddies.Select(b=>b.Asset.AspectRatio).ToArray());
        Placement=placement;
        if(placement==null){Hide();SetStatus("工作列空間不足，或目前工作列配置不支援。可減少角色數或使用桌面模式。");return;}
        var key=$"{anchor.Scale}:{placement.Bounds.Height}:{placement.Bounds.Width}:{placement.Scrollable}:"+string.Join(",",buddies.Select(b=>$"{b.Config.Id}:{b.Config.Label}:{b.Asset.AspectRatio}"))+":"+string.Join(",",placement.CellWidths);
        if(key!=layoutKey){BuildCells(placement,anchor.Scale);layoutKey=key;}
        var r=placement.Bounds;
        Width=r.Width/anchor.Scale;Height=r.Height/anchor.Scale;
        if(!IsVisible){Left=r.Left/anchor.Scale;Top=r.Top/anchor.Scale;Show();}
        var handle=new WindowInteropHelper(this).Handle;
        if(!GetWindowRect(handle,out var actual)||actual!=r||NativeTaskbarLocator.IsAbove(anchor.Handle,handle))
            SetWindowPos(handle,new IntPtr(-1),r.Left,r.Top,r.Width,r.Height,0x10|0x40);
        SetStatus(placement.Scrollable?"GIF 保持完整高度；點左右箭頭或使用滾輪查看其他部分":"工作列角色固定在 ˄ 箭頭左側，GIF 依高度等比例展開");
    }
    private void BuildCells(TaskbarPlacement placement,double scale)
    {
        var oldOffset=viewport.HorizontalOffset;
        ClearCells();
        host.ColumnDefinitions[0].Width=new GridLength(placement.ScrollButtonWidth/scale);
        host.ColumnDefinitions[2].Width=new GridLength(placement.ScrollButtonWidth/scale);
        previous.Visibility=next.Visibility=placement.Scrollable?Visibility.Visible:Visibility.Collapsed;
        for(var i=0;i<buddies.Length;i++)
        {
            var buddy=buddies[i];
            var showValue=placement.ShowValues&&buddy.Config.Label==LabelMode.Always;
            var cell=new Border { Width=placement.CellWidths[i]/scale,Height=placement.Bounds.Height/scale,Background=Brushes.Transparent,Cursor=Cursors.Hand,DataContext=buddy };
            var stack=new StackPanel { Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center };
            var image=new Image { Width=placement.ImageWidths[i]/scale,Height=placement.ImageHeight/scale,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center };
            image.SetBinding(Image.SourceProperty,new Binding(nameof(BuddyRuntime.Frame)));
            image.SetBinding(RenderOptions.BitmapScalingModeProperty,new Binding(nameof(BuddyRuntime.ScalingMode)));
            stack.Children.Add(image);
            if(showValue)
            {
                var number=new TextBlock { Foreground=Brushes.White,FontFamily=new FontFamily("Consolas"),FontSize=10,HorizontalAlignment=HorizontalAlignment.Center };
                number.SetBinding(TextBlock.TextProperty,new Binding(nameof(BuddyRuntime.Reading)));
                stack.Children.Add(new Border { Width=52,Margin=new Thickness(4,0,0,0),Background=new SolidColorBrush(Color.FromArgb(220,32,49,74)),CornerRadius=new CornerRadius(4),Padding=new Thickness(2,1,2,1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Child=number });
            }
            cell.Child=stack;
            var tip=new StackPanel();var name=new TextBlock { FontWeight=FontWeights.SemiBold };name.SetBinding(TextBlock.TextProperty,new Binding(nameof(BuddyRuntime.Name)));
            var detail=new TextBlock();detail.SetBinding(TextBlock.TextProperty,new Binding(nameof(BuddyRuntime.Detail)));tip.Children.Add(name);tip.Children.Add(detail);
            cell.ToolTip=new ToolTip { Content=tip,DataContext=buddy,Placement=System.Windows.Controls.Primitives.PlacementMode.Top };
            cell.MouseLeftButtonUp+=(_,e)=>{open(buddy);e.Handled=true;};
            var menu=new ContextMenu();var manage=new MenuItem { Header="管理這隻夥伴" };manage.Click+=(_,_)=>open(buddy);menu.Items.Add(manage);
            var pause=new MenuItem { Header="暫停 / 繼續" };pause.Click+=(_,_)=>edit(()=>buddy.Config.Paused=!buddy.Config.Paused);menu.Items.Add(pause);
            cell.ContextMenu=menu;row.Children.Add(cell);
        }
        viewport.ScrollToHorizontalOffset(placement.Scrollable?oldOffset:0);
    }
    private void ClearCells()
    {
        foreach(var cell in row.Children.OfType<Border>())
        {
            if(cell.ContextMenu is { } menu){menu.IsOpen=false;menu.Items.Clear();cell.ContextMenu=null;}
            if(cell.ToolTip is ToolTip tip){tip.IsOpen=false;tip.Content=null;tip.DataContext=null;cell.ToolTip=null;}
            cell.DataContext=null;cell.Child=null;
        }
        row.Children.Clear();layoutKey="";
    }
    private void SetStatus(string value){if(status==value)return;status=value;StatusChanged?.Invoke(value);}
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr handle,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr handle,int index,IntPtr value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle,out PixelRect rect);
}
