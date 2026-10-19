using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PaceBuddies.Animation;
using Forms=System.Windows.Forms;

namespace PaceBuddies.Views;

public sealed class DesktopBuddyWindow : Window
{
    private readonly BuddyRuntime runtime;
    private readonly TextBlock label;
    private readonly Action changed;
    private readonly int slot;
    private bool positioned;
    public DesktopBuddyWindow(BuddyRuntime buddy,int slot,Action open,Action changed)
    {
        runtime=buddy; this.slot=slot; this.changed=changed;
        WindowStyle=WindowStyle.None; AllowsTransparency=true; Background=Brushes.Transparent;
        ShowInTaskbar=false; ResizeMode=ResizeMode.NoResize; ShowActivated=false; DataContext=buddy;
        Title="Pace Buddy · "+buddy.Name;
        var panel=new Grid { Background=Brushes.Transparent };
        panel.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        panel.RowDefinitions.Add(new(){Height=new GridLength(28)});
        var image=new Image { Stretch=Stretch.Uniform };
        image.SetBinding(Image.SourceProperty,new Binding(nameof(BuddyRuntime.Frame)));
        RenderOptions.SetBitmapScalingMode(image,BitmapScalingMode.NearestNeighbor);
        panel.Children.Add(image);
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
        Item("暫停 / 繼續",()=>{buddy.Config.Paused=!buddy.Config.Paused;buddy.NotifySettings();changed();});
        Item("隱藏桌面角色",()=>{buddy.Config.ShowDesktop=false;changed();Hide();});
        ContextMenu=menu;
        ApplySettings();
    }
    public void ApplySettings()
    {
        var b=runtime.Config;
        Width=Math.Max(b.Size+24,180); Height=b.Size+28; Topmost=b.Topmost;
        ((Grid)Content).RowDefinitions[0].Height=new GridLength(b.Size);
        var image=(Image)((Grid)Content).Children[0];image.Width=image.Height=b.Size;
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
        SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x1|0x4|0x10); positioned=true; ClampPosition();
    }
    public void ClampPosition()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetWindowRect(handle,out var r)) return;
        var area=Forms.Screen.FromRectangle(new System.Drawing.Rectangle(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top)).WorkingArea;
        var x=Math.Clamp(r.Left,area.Left,Math.Max(area.Left,area.Right-(r.Right-r.Left)));
        var y=Math.Clamp(r.Top,area.Top,Math.Max(area.Top,area.Bottom-(r.Bottom-r.Top)));
        if(x!=r.Left||y!=r.Top) SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x1|0x4|0x10);
        runtime.Config.X=x;runtime.Config.Y=y;
    }
    private void SavePosition() { ClampPosition();changed(); }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
