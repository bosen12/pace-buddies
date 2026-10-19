using System.Runtime.InteropServices;
using System.Text;
using Forms=System.Windows.Forms;

namespace PaceBuddies.Services;

public sealed record TaskbarAnchor(IntPtr Handle,PixelRect Bar,PixelRect Notification,int OccupiedRight,double Scale,bool Visible);

public static class NativeTaskbarLocator
{
    public static TaskbarAnchor? Locate()
    {
        var bar=FindWindow("Shell_TrayWnd",null);
        var notification=FindWindowEx(bar,IntPtr.Zero,"TrayNotifyWnd",null);
        if(bar==IntPtr.Zero||notification==IntPtr.Zero||!GetWindowRect(bar,out var rect)||!GetWindowRect(notification,out var tray))return null;
        var occupied=rect.Left;
        EnumChildWindows(bar,(child,_)=>
        {
            var name=new StringBuilder(256);GetClassName(child,name,name.Capacity);
            // Windows 11 keeps useful compatibility rectangles on children that may be marked hidden.
            if(name.ToString() is "MSTaskListWClass" or "Start")
                if(GetWindowRect(child,out var buttons)&&buttons.Width>0&&buttons.Bottom>rect.Top&&buttons.Top<rect.Bottom)
                    occupied=Math.Max(occupied,buttons.Right);
            return true;
        },IntPtr.Zero);
        var scale=Math.Max(1,GetDpiForWindow(bar))/96.0;
        var monitor=Forms.Screen.FromHandle(bar).Bounds;
        var visible=IsWindowVisible(bar)&&rect.Top>=monitor.Top&&rect.Bottom<=monitor.Bottom+2&&rect.Height>=24*scale;
        // Auto-hidden taskbars often leave only a 1–2 pixel sliver on screen.
        var intersectionHeight=Math.Min(rect.Bottom,monitor.Bottom)-Math.Max(rect.Top,monitor.Top);
        visible&=intersectionHeight>=rect.Height*.75;
        var foreground=GetForegroundWindow();
        if(foreground!=IntPtr.Zero&&foreground!=bar)
        {
            var name=new StringBuilder(256);GetClassName(foreground,name,name.Capacity);
            if(name.ToString() is not ("Progman" or "WorkerW" or "Shell_TrayWnd")&&GetWindowRect(foreground,out var front))
                if(front.Left<=monitor.Left&&front.Top<=monitor.Top&&front.Right>=monitor.Right&&front.Bottom>=monitor.Bottom)
                    visible=false;
        }
        return new(bar,rect,tray,occupied,scale,visible);
    }
    private delegate bool EnumWindow(IntPtr handle,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowW")] private static extern IntPtr FindWindow(string name,string? title);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowExW")] private static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string name,string? title);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle,out PixelRect rect);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr handle,EnumWindow callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="GetClassNameW")] private static extern int GetClassName(IntPtr handle,StringBuilder name,int count);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
}
