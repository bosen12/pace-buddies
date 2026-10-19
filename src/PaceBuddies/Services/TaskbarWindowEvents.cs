using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace PaceBuddies.Services;

// Out-of-context notifications run on the WPF dispatcher; no code is injected into Explorer.
internal sealed class TaskbarWindowEvents : IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly Action refresh;
    private readonly WinEvent callback;
    private readonly List<IntPtr> hooks=[];
    private bool queued,disposed;
    public TaskbarWindowEvents(Dispatcher dispatcher,Action refresh)
    {
        this.dispatcher=dispatcher;this.refresh=refresh;callback=OnEvent;
        foreach(var kind in new uint[]{3,0x8004,0x800B})
        {
            var hook=SetWinEventHook(kind,kind,IntPtr.Zero,callback,0,0,2);
            if(hook!=IntPtr.Zero)hooks.Add(hook);
        }
    }
    private void OnEvent(IntPtr hook,uint kind,IntPtr window,int objectId,int childId,uint thread,uint time)
    {
        if(disposed||queued||dispatcher.HasShutdownStarted)return;
        if(kind!=3)
        {
            var bar=FindWindow("Shell_TrayWnd",null);
            var shell=window==bar||GetAncestor(window,2)==bar||window==GetDesktopWindow();
            if(objectId!=0&&!(objectId==-4&&shell))return;
            if(childId!=0&&!shell)return;
            if(kind==0x800B&&!shell)return;
        }
        queued=true;
        dispatcher.BeginInvoke(DispatcherPriority.Render,()=>
        {
            queued=false;if(!disposed)refresh();
        });
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        foreach(var hook in hooks)UnhookWinEvent(hook);hooks.Clear();
        GC.KeepAlive(callback);
    }
    private delegate void WinEvent(IntPtr hook,uint kind,IntPtr window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WinEvent callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowW")] private static extern IntPtr FindWindow(string name,string? title);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
}
