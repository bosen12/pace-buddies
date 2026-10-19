using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using PaceBuddies.Animation;
using Forms=System.Windows.Forms;

namespace PaceBuddies.Views;

public sealed class DesktopBuddyWindow : Window
{
    private readonly BuddyRuntime runtime;
    private readonly TextBlock label;
    private readonly Image image;
    private readonly ScrollViewer imageViewport;
    private readonly Action changed;
    private readonly int slot;
    private bool positioned;
    private bool fitQueued,closed;
    private System.Drawing.Rectangle lastWorkingArea;
    private double lastScale;
    public DesktopBuddyWindow(BuddyRuntime buddy,int slot,Action open,Action changed,Action<Action> externalEdit)
    {
        runtime=buddy; this.slot=slot; this.changed=changed;
        WindowStyle=WindowStyle.None; AllowsTransparency=true; Background=Brushes.Transparent;
        ShowInTaskbar=false; ResizeMode=ResizeMode.NoResize; ShowActivated=false; DataContext=buddy;
        Title="Pace Buddy · "+buddy.Name;
        var panel=new Grid { Background=Brushes.Transparent };
        panel.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        panel.RowDefinitions.Add(new(){Height=new GridLength(28)});
        image=new Image { Stretch=Stretch.Uniform };
        image.SetBinding(Image.SourceProperty,new Binding(nameof(BuddyRuntime.Frame)));
        image.SetBinding(RenderOptions.BitmapScalingModeProperty,new Binding(nameof(BuddyRuntime.ScalingMode)));
        imageViewport=new ScrollViewer { HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalContentAlignment=HorizontalAlignment.Center,Content=image,CanContentScroll=false };
        imageViewport.PreviewMouseWheel+=(_,e)=>{if(imageViewport.ScrollableWidth<=0)return;imageViewport.ScrollToHorizontalOffset(imageViewport.HorizontalOffset-e.Delta);e.Handled=true;};
        imageViewport.ToolTip="較寬的角色可用滾輪左右捲動";
        panel.Children.Add(imageViewport);
        label=new TextBlock { Foreground=Brushes.White,FontSize=11,FontFamily=new FontFamily("Microsoft JhengHei UI"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,2,8,2) };
        var badge=new Border { Background=new SolidColorBrush(Color.FromArgb(224,32,49,74)),CornerRadius=new CornerRadius(10),HorizontalAlignment=HorizontalAlignment.Center,Child=label };
        Grid.SetRow(badge,1); panel.Children.Add(badge); Content=panel;
        label.SetBinding(TextBlock.TextProperty,new Binding(nameof(BuddyRuntime.Detail)));
        badge.Name="Badge";
        void LabelVisibility()=>badge.Visibility=buddy.Config.Label==LabelMode.Always || (buddy.Config.Label==LabelMode.Hover && IsMouseOver) ? Visibility.Visible : Visibility.Hidden;
        MouseEnter+=(_,_)=>LabelVisibility(); MouseLeave+=(_,_)=>LabelVisibility();
        SourceInitialized+=(_,_)=>ApplySettings();
        Loaded+=(_,_)=>{Position(); LabelVisibility();};
        MouseLeftButtonDown+=(_,e)=>
        {
            if(e.ClickCount==2) {open();return;}
            if(buddy.Config.Locked||buddy.Config.ClickThrough) return;
            try { DragMove(); SavePosition(); } catch(InvalidOperationException) { }
        };
        var menu=new ContextMenu();
        void Item(string name,Action action) { var item=new MenuItem { Header=name };item.Click+=(_,_)=>action();menu.Items.Add(item); }
        Item("管理夥伴",open);
        Item("暫停 / 繼續",()=>externalEdit(()=>buddy.Config.Paused=!buddy.Config.Paused));
        Item("隱藏桌面角色",()=>externalEdit(()=>buddy.Config.ShowDesktop=false));
        ContextMenu=menu;
        LocationChanged+=(_,_)=>QueueMonitorFit();
        SystemEvents.DisplaySettingsChanged+=OnDisplaySettingsChanged;
        Closed+=(_,_)=>{closed=true;SystemEvents.DisplaySettingsChanged-=OnDisplaySettingsChanged;};
        ApplySettings();
    }
    public void ApplySettings()
    {
        var b=runtime.Config;
        var scale=VisualTreeHelper.GetDpi(this).DpiScaleX;
        var area=DestinationScreen().WorkingArea;
        lastWorkingArea=area;lastScale=scale;
        var availableWidth=area.Width/scale;
        image.Height=b.Size;image.Width=b.Size*runtime.Asset.AspectRatio;
        Width=Math.Min(Math.Max(image.Width+24,180),Math.Max(48,availableWidth)); Height=b.Size+28; Topmost=b.Topmost;
        ((Grid)Content).RowDefinitions[0].Height=new GridLength(b.Size);
        ((Border)((Grid)Content).Children[1]).Visibility=b.Label==LabelMode.Always ||(b.Label==LabelMode.Hover&&IsMouseOver)?Visibility.Visible:Visibility.Hidden;
        var handle=new WindowInteropHelper(this).Handle;
        if(handle!=IntPtr.Zero)
        {
            var style=GetWindowLongPtr(handle,-20).ToInt64();
            style=(style|0x80|0x08000000)&~0x20L;
            if(b.ClickThrough) style|=0x20;
            SetWindowLongPtr(handle,-20,new IntPtr(style));
            if(positioned) Dispatcher.BeginInvoke(ClampPosition);
        }
    }
    public void ResetPosition() { runtime.Config.X=runtime.Config.Y=null; Position(); changed(); }
    private void Position()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero) return;
        var rect=Forms.Screen.PrimaryScreen!.WorkingArea;
        var x=runtime.Config.X??(rect.Right-210*(slot+1));
        var y=runtime.Config.Y??(rect.Bottom-(int)Height-16);
        SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x1|0x4|0x10); positioned=true; ApplySettings();ClampPosition();
    }
    public void ClampPosition()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetWindowRect(handle,out var r)) return;
        var area=DestinationScreen().WorkingArea;
        var x=Math.Clamp(r.Left,area.Left,Math.Max(area.Left,area.Right-(r.Right-r.Left)));
        var y=Math.Clamp(r.Top,area.Top,Math.Max(area.Top,area.Bottom-(r.Bottom-r.Top)));
        if(x!=r.Left||y!=r.Top) SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x1|0x4|0x10);
        runtime.Config.X=x;runtime.Config.Y=y;
    }
    private void SavePosition() { ApplySettings();ClampPosition();changed(); }
    private Forms.Screen DestinationScreen()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle!=IntPtr.Zero&&GetWindowRect(handle,out var rect))
            return Forms.Screen.FromPoint(new System.Drawing.Point(rect.Left+1,rect.Top+1));
        if(runtime.Config.X is {} x&&runtime.Config.Y is {} y)return Forms.Screen.FromPoint(new System.Drawing.Point(x+1,y+1));
        return Forms.Screen.PrimaryScreen!;
    }
    private void QueueMonitorFit()
    {
        if(!positioned||closed||fitQueued)return;
        fitQueued=true;
        Dispatcher.BeginInvoke(()=>
        {
            fitQueued=false;if(closed)return;
            if(DestinationScreen().WorkingArea!=lastWorkingArea||Math.Abs(VisualTreeHelper.GetDpi(this).DpiScaleX-lastScale)>.001)
            {ApplySettings();ClampPosition();}
        });
    }
    private void OnDisplaySettingsChanged(object? sender,EventArgs e)
    {if(!closed&&!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(QueueMonitorFit);}
    protected override void OnDpiChanged(DpiScale oldDpi,DpiScale newDpi)
    {base.OnDpiChanged(oldDpi,newDpi);QueueMonitorFit();}
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
