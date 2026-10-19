using System.Runtime.InteropServices;
using System.Text;

namespace PaceBuddies.Services;

internal sealed class DesktopLayerAttachment(IntPtr window) : IDisposable
{
    private IntPtr host;
    private long floatingStyle;
    public bool IsAttached=>host!=IntPtr.Zero&&IsWindow(host)&&GetParent(window)==host;
    public bool SetBound(bool bound,out string status)
    {
        if(!bound){Detach();status="懸浮模式 · 未鎖定時可移動及縮放";return true;}
        var target=FindHost();
        if(target==IntPtr.Zero){Detach();status="桌面層暫時無法使用，目前使用懸浮模式；稍後會自動重試。";return false;}
        if(IsAttached&&host==target){status="已綁定桌面層 · 位於其他應用視窗下方";return true;}
        if(!AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(window),GetWindowDpiAwarenessContext(target)))
        {Detach();status="目前桌面 DPI 模式不相容，暫時使用懸浮模式。";return false;}
        Detach();
        if(!GetWindowRect(window,out var bounds)){status="無法讀取角色位置，暫時使用懸浮模式。";return false;}
        floatingStyle=GetWindowLongPtr(window,-16).ToInt64();
        var wasTopmost=(GetWindowLongPtr(window,-20).ToInt64()&8)!=0;
        SetWindowPos(window,new IntPtr(-2),0,0,0,0,0x13);
        SetWindowLongPtr(window,-16,new IntPtr((floatingStyle&~0x80000000L)|0x40000000L));
        SetParent(window,target);
        if(GetParent(window)!=target)
        {
            SetWindowLongPtr(window,-16,new IntPtr(floatingStyle));
            SetWindowPos(window,new IntPtr(wasTopmost?-1:-2),bounds.Left,bounds.Top,0,0,0x31);
            status="無法綁定目前桌面，暫時使用懸浮模式。";return false;
        }
        host=target;MoveScreen(bounds.Left,bounds.Top);
        SetWindowPos(window,IntPtr.Zero,0,0,0,0,0x33);
        status="已綁定桌面層 · 位於其他應用視窗下方";return true;
    }
    public void MoveScreen(int x,int y)
    {
        var parent=GetParent(window);
        var point=new NativePoint(x,y);
        if(parent!=IntPtr.Zero&&(GetWindowLongPtr(window,-16).ToInt64()&0x40000000L)!=0)ScreenToClient(parent,ref point);
        SetWindowPos(window,IntPtr.Zero,point.X,point.Y,0,0,0x15);
    }
    private void Detach()
    {
        if(host==IntPtr.Zero)return;
        if(IsWindow(window))
        {
            GetWindowRect(window,out var bounds);
            SetParent(window,IntPtr.Zero);
            SetWindowLongPtr(window,-16,new IntPtr(floatingStyle));
            SetWindowPos(window,IntPtr.Zero,bounds.Left,bounds.Top,0,0,0x35);
        }
        host=IntPtr.Zero;
    }
    public void Dispose()=>Detach();
    internal static IntPtr FindHost()
    {
        var candidate=FindWindow("Progman",null);
        if(IsIconHost(candidate))return candidate;
        var found=IntPtr.Zero;
        EnumWindows((handle,_)=>
        {
            var name=new StringBuilder(64);GetClassName(handle,name,name.Capacity);
            if(name.ToString() is "WorkerW" or "Progman"&&IsIconHost(handle)){found=handle;return false;}
            return true;
        },IntPtr.Zero);
        return found;
    }
    private static bool IsIconHost(IntPtr handle)=>handle!=IntPtr.Zero&&IsWindowVisible(handle)&&FindWindowEx(handle,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero;
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint(int x,int y){public int X=x,Y=y;}
    private delegate bool EnumWindow(IntPtr window,IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowW")] private static extern IntPtr FindWindow(string name,string? title);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowExW")] private static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string name,string? title);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="GetClassNameW")] private static extern int GetClassName(IntPtr window,StringBuilder text,int length);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr SetParent(IntPtr child,IntPtr parent);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out PixelRect rect);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr window,ref NativePoint point);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(IntPtr first,IntPtr second);
}
