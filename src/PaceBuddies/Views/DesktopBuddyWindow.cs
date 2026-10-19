using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PaceBuddies.Animation;
using PaceBuddies.Services;
using Forms=System.Windows.Forms;

namespace PaceBuddies.Views;

public sealed class DesktopBuddyWindow : Window
{
    private readonly BuddyRuntime runtime;
    private readonly Image image;
    private readonly ScrollViewer imageViewport;
    private readonly Border resizeGrip,editOutline;
    private readonly Action changed;
    private readonly Action<Action> externalEdit;
    private readonly int slot;
    private readonly DispatcherTimer shellTimer=new(){Interval=TimeSpan.FromSeconds(2)};
    private readonly DispatcherTimer gestureTimer=new(){Interval=TimeSpan.FromMilliseconds(16)};
    private DesktopLayerAttachment? layer;
    private bool positioned,fitQueued,closed,applying;
    private System.Drawing.Rectangle lastWorkingArea;
    private double lastScale;
    private enum Gesture { None,Move,Resize }
    private Gesture gesture;
    private NativePoint startPointer,lastPointer;
    private PixelRect startBounds;
    private int startSize;
    public bool IsDesktopBound=>layer?.IsAttached==true;

    public DesktopBuddyWindow(BuddyRuntime buddy,int slot,Action open,Action changed,Action<Action> externalEdit)
    {
        runtime=buddy;this.slot=slot;this.changed=changed;this.externalEdit=externalEdit;
        WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
        ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;ShowActivated=false;DataContext=buddy;
        Title="Pace Buddy · "+buddy.Name;
        var panel=new Grid{Background=Brushes.Transparent};
        panel.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        panel.RowDefinitions.Add(new(){Height=new GridLength(28)});
        image=new Image{Stretch=Stretch.Uniform};
        image.SetBinding(Image.SourceProperty,new Binding(nameof(BuddyRuntime.Frame)));
        image.SetBinding(RenderOptions.BitmapScalingModeProperty,new Binding(nameof(BuddyRuntime.ScalingMode)));
        imageViewport=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalContentAlignment=HorizontalAlignment.Center,Content=image,CanContentScroll=false};
        imageViewport.PreviewMouseWheel+=(_,e)=>{if(imageViewport.ScrollableWidth<=0)return;imageViewport.ScrollToHorizontalOffset(imageViewport.HorizontalOffset-e.Delta);e.Handled=true;};
        imageViewport.ToolTip="拖曳移動 · Ctrl＋滾輪縮放 · 一般滾輪捲動寬 GIF";
        panel.Children.Add(imageViewport);
        var label=new TextBlock{Foreground=Brushes.White,FontSize=11,FontFamily=new FontFamily("Microsoft JhengHei UI"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,2,8,2)};
        var badge=new Border{Background=new SolidColorBrush(Color.FromArgb(224,32,49,74)),CornerRadius=new CornerRadius(10),HorizontalAlignment=HorizontalAlignment.Center,Child=label,Name="Badge"};
        Grid.SetRow(badge,1);panel.Children.Add(badge);
        label.SetBinding(TextBlock.TextProperty,new Binding(nameof(BuddyRuntime.Detail)));
        editOutline=new Border{BorderBrush=new SolidColorBrush(Color.FromArgb(160,57,118,185)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),IsHitTestVisible=false,Visibility=Visibility.Hidden};
        Grid.SetRowSpan(editOutline,2);panel.Children.Add(editOutline);
        resizeGrip=new Border{Width=24,Height=24,CornerRadius=new CornerRadius(6),Background=new SolidColorBrush(Color.FromArgb(235,234,242,252)),BorderBrush=new SolidColorBrush(Color.FromRgb(57,118,185)),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Cursor=Cursors.SizeNWSE,ToolTip="拖曳等比例縮放",Name="ResizeGrip",Child=new TextBlock{Text="↘",Foreground=new SolidColorBrush(Color.FromRgb(57,118,185)),FontSize=16,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
        Grid.SetRowSpan(resizeGrip,2);panel.Children.Add(resizeGrip);Content=panel;
        resizeGrip.PreviewMouseLeftButtonDown+=(_,e)=>{BeginGesture(Gesture.Resize);e.Handled=true;};
        MouseEnter+=(_,_)=>UpdateChrome();MouseLeave+=(_,_)=>UpdateChrome();
        MouseLeftButtonDown+=(_,e)=>
        {
            if(e.ClickCount==2){open();return;}
            BeginGesture(Gesture.Move);
        };
        MouseMove+=(_,_)=>UpdateGesture();
        PreviewMouseLeftButtonUp+=(_,e)=>{if(gesture!=Gesture.None){FinishGesture();e.Handled=true;}};
        LostMouseCapture+=(_,_)=>{if(gesture!=Gesture.None)FinishGesture();};
        PreviewMouseWheel+=(_,e)=>
        {
            if((Keyboard.Modifiers&ModifierKeys.Control)==0)return;
            e.Handled=true;
            ResizeDirectlyBy(Math.Sign(e.Delta)*8);
        };
        gestureTimer.Tick+=(_,_)=>{if((GetAsyncKeyState(1)&0x8000)==0)FinishGesture();else UpdateGesture();};
        SourceInitialized+=(_,_)=>{layer=new(new WindowInteropHelper(this).Handle);ApplySettings();};
        Loaded+=(_,_)=>{Position();UpdateChrome();};
        var menu=new ContextMenu();
        void Item(string name,Action action){var item=new MenuItem{Header=name};item.Click+=(_,_)=>action();menu.Items.Add(item);}
        Item("管理夥伴",open);
        Item("暫停 / 繼續",()=>externalEdit(()=>buddy.Config.Paused=!buddy.Config.Paused));
        Item("鎖定 / 解鎖位置與大小",()=>externalEdit(()=>buddy.Config.Locked=!buddy.Config.Locked));
        Item("綁定桌面層 / 恢復懸浮",()=>externalEdit(()=>buddy.Config.BindDesktop=!buddy.Config.BindDesktop));
        Item("隱藏桌面角色",()=>externalEdit(()=>buddy.Config.ShowDesktop=false));
        ContextMenu=menu;
        LocationChanged+=(_,_)=>QueueMonitorFit();
        SystemEvents.DisplaySettingsChanged+=OnDisplaySettingsChanged;
        shellTimer.Tick+=(_,_)=>{if(runtime.Config.BindDesktop)ApplySettings();};
        Closed+=(_,_)=>{closed=true;StopGesture();shellTimer.Stop();layer?.Dispose();SystemEvents.DisplaySettingsChanged-=OnDisplaySettingsChanged;};
        ApplySettings();
    }
    private void UpdateChrome()
    {
        var b=runtime.Config;
        ((Border)((Grid)Content).Children[1]).Visibility=b.Label==LabelMode.Always||(b.Label==LabelMode.Hover&&IsMouseOver)?Visibility.Visible:Visibility.Hidden;
        var editing=DesktopGeometry.CanManipulate(b)&&(IsMouseOver||gesture!=Gesture.None);
        resizeGrip.Visibility=editOutline.Visibility=editing?Visibility.Visible:Visibility.Hidden;
        Cursor=DesktopGeometry.CanManipulate(b)?Cursors.SizeAll:Cursors.Arrow;
    }
    public void ApplySettings()
    {
        if(closed||applying)return;applying=true;
        try
        {
            var b=runtime.Config;
            if(!DesktopGeometry.CanManipulate(b))StopGesture();
            var handle=new WindowInteropHelper(this).Handle;
            var hasBounds=handle!=IntPtr.Zero&&GetWindowRect(handle,out _);
            GetWindowRect(handle,out var bounds);
            var scale=VisualTreeHelper.GetDpi(this).DpiScaleX;
            var area=DestinationScreen().WorkingArea;lastWorkingArea=area;lastScale=scale;
            var imageHeight=DesktopGeometry.FitHeight(b.Size,area.Height/scale);
            image.Height=imageHeight;image.Width=imageHeight*runtime.Asset.AspectRatio;
            Width=Math.Min(Math.Max(image.Width+24,180),Math.Max(48,area.Width/scale));Height=imageHeight+28;
            ((Grid)Content).RowDefinitions[0].Height=new GridLength(imageHeight);
            if(handle!=IntPtr.Zero&&layer!=null)
            {
                layer.SetBound(b.BindDesktop,out var status);runtime.SetDesktopStatus(status);
                Topmost=!layer.IsAttached&&b.Topmost;
                var style=(GetWindowLongPtr(handle,-20).ToInt64()|0x80|0x08000000)&~0x20L;
                if(b.ClickThrough)style|=0x20;
                SetWindowLongPtr(handle,-20,new IntPtr(style));
                if(hasBounds&&positioned)layer.MoveScreen(bounds.Left,bounds.Top);
                if(positioned)ClampPosition();
            }
            else Topmost=b.Topmost;
            if(b.BindDesktop)shellTimer.Start();else shellTimer.Stop();
            UpdateChrome();
        }
        finally{applying=false;}
    }
    private void BeginGesture(Gesture kind)
    {
        if(!DesktopGeometry.CanManipulate(runtime.Config))return;
        externalEdit(()=>{});
        if(closed||!DesktopGeometry.CanManipulate(runtime.Config)||!GetCursorPos(out startPointer))return;
        if(!GetWindowRect(new WindowInteropHelper(this).Handle,out startBounds))return;
        gesture=kind;lastPointer=startPointer;startSize=runtime.Config.Size;
        CaptureMouse();gestureTimer.Start();UpdateChrome();
    }
    private void UpdateGesture()
    {
        if(gesture==Gesture.None||closed||!GetCursorPos(out var pointer))return;
        if(pointer.X==lastPointer.X&&pointer.Y==lastPointer.Y)return;
        lastPointer=pointer;
        var dx=pointer.X-startPointer.X;var dy=pointer.Y-startPointer.Y;
        if(gesture==Gesture.Move)MoveUnchecked(startBounds.Left+dx,startBounds.Top+dy);
        else
        {
            var scale=VisualTreeHelper.GetDpi(this).DpiScaleX;
            runtime.Config.Size=DesktopGeometry.ResizeHeight(startSize,dx/scale,dy/scale,runtime.Asset.AspectRatio);
            ApplySettings();
        }
    }
    private void StopGesture()
    {
        gesture=Gesture.None;gestureTimer.Stop();
        if(IsMouseCaptured)ReleaseMouseCapture();
    }
    private void FinishGesture()
    {
        if(gesture==Gesture.None)return;
        StopGesture();ClampPosition();UpdateChrome();
        if(!closed)externalEdit(()=>{});
    }
    internal bool ResizeDirectlyTo(int height)=>ResizeDirectly(_=>height);
    internal bool ResizeDirectlyBy(int delta)=>ResizeDirectly(height=>height+delta);
    private bool ResizeDirectly(Func<int,int> transform)
    {
        if(closed||!DesktopGeometry.CanManipulate(runtime.Config))return false;
        var applied=false;
        externalEdit(()=>{if(!closed&&DesktopGeometry.CanManipulate(runtime.Config)){runtime.Config.Size=DesktopGeometry.ClampHeight(transform(runtime.Config.Size));applied=true;}});
        return applied;
    }
    internal bool MoveDirectlyTo(int x,int y)
    {
        if(closed||!DesktopGeometry.CanManipulate(runtime.Config))return false;
        var applied=false;
        externalEdit(()=>{if(!closed&&DesktopGeometry.CanManipulate(runtime.Config)){MoveUnchecked(x,y);applied=true;}});
        return applied;
    }
    private void MoveUnchecked(int x,int y){layer?.MoveScreen(x,y);ClampPosition();}
    public void ResetPosition(){runtime.Config.X=runtime.Config.Y=null;Position();changed();}
    private void Position()
    {
        var handle=new WindowInteropHelper(this).Handle;if(handle==IntPtr.Zero)return;
        var area=Forms.Screen.PrimaryScreen!.WorkingArea;var scale=VisualTreeHelper.GetDpi(this).DpiScaleX;
        var x=runtime.Config.X??(area.Right-(int)(210*scale)*(slot+1));
        var y=runtime.Config.Y??(area.Bottom-(int)Math.Ceiling(Height*scale)-16);
        layer?.MoveScreen(x,y);positioned=true;ApplySettings();ClampPosition();
    }
    public void ClampPosition()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetWindowRect(handle,out var rect))return;
        var area=DestinationScreen().WorkingArea;
        var x=Math.Clamp(rect.Left,area.Left,Math.Max(area.Left,area.Right-rect.Width));
        var y=Math.Clamp(rect.Top,area.Top,Math.Max(area.Top,area.Bottom-rect.Height));
        if(x!=rect.Left||y!=rect.Top)layer?.MoveScreen(x,y);
        runtime.Config.X=x;runtime.Config.Y=y;
    }
    private Forms.Screen DestinationScreen()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle!=IntPtr.Zero&&GetWindowRect(handle,out var rect))return Forms.Screen.FromPoint(new System.Drawing.Point(rect.Left+1,rect.Top+1));
        if(runtime.Config.X is {} x&&runtime.Config.Y is {} y)return Forms.Screen.FromPoint(new System.Drawing.Point(x+1,y+1));
        return Forms.Screen.PrimaryScreen!;
    }
    private void QueueMonitorFit()
    {
        if(!positioned||closed||fitQueued)return;fitQueued=true;
        Dispatcher.BeginInvoke(()=>
        {
            fitQueued=false;if(closed)return;
            if(DestinationScreen().WorkingArea!=lastWorkingArea||Math.Abs(VisualTreeHelper.GetDpi(this).DpiScaleX-lastScale)>.001){ApplySettings();ClampPosition();}
        });
    }
    private void OnDisplaySettingsChanged(object? sender,EventArgs e){if(!closed&&!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(QueueMonitorFit);}
    protected override void OnDpiChanged(DpiScale oldDpi,DpiScale newDpi){base.OnDpiChanged(oldDpi,newDpi);QueueMonitorFit();}
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint{public int X,Y;}
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out PixelRect rect);
}
