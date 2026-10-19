using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using PaceBuddies.Animation;
using PaceBuddies.Views;
using Forms=System.Windows.Forms;

namespace PaceBuddies.Services;

public sealed class RuntimeCoordinator : IDisposable
{
    public AppSettings Settings { get; }
    public SettingsStore Store { get; }
    public ObservableCollection<BuddyRuntime> Buddies { get; }=[];
    public MetricSnapshot Snapshot { get; private set; }=MetricSnapshot.Empty;
    public event Action<MetricSnapshot>? Sampled;
    public event Action<string>? Notice;
    public event Action? ExternalSettingsChanging;
    public event Action? ExternalSettingsChanged;
    public event Action<BuddyRuntime>? BuddyOpened;
    public event Action<string>? TaskbarStatusChanged;
    private readonly Dictionary<Guid,DesktopBuddyWindow> desktops=[];
    private readonly TaskbarBuddyWindow taskbar;
    private readonly SystemMonitor monitor=new();
    private readonly DispatcherTimer animationTimer,saveTimer;
    private readonly Stopwatch stopwatch=Stopwatch.StartNew();
    private TimeSpan previous;
    private int tick;
    private readonly Action open;
    private readonly Forms.NotifyIcon manager;
    private readonly AnimationAsset managerAsset=BuiltinSprites.Create("cat");
    public int DesktopCount=>desktops.Count;
    public int TaskbarCount=>taskbar.BuddyCount;
    public int ManagerTrayCount=>manager.Visible?1:0;
    public string TaskbarStatus=>taskbar.Status;
    public RuntimeCoordinator(AppSettings settings,SettingsStore store,Action open,Action exit)
    {
        Settings=settings; Store=store;this.open=open;
        manager=new Forms.NotifyIcon { Text="跑跑夥伴 · 開啟管理視窗",Icon=managerAsset.TrayFrames[0],Visible=true };
        var menu=new Forms.ContextMenuStrip();
        menu.Items.Add("開啟跑跑夥伴",null,(_,_)=>open());
        menu.Items.Add("暫停全部",null,(_,_)=>SetAllPaused(true));
        menu.Items.Add("繼續全部",null,(_,_)=>SetAllPaused(false));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("結束程式",null,(_,_)=>exit());manager.ContextMenuStrip=menu;
        manager.MouseClick+=(_,e)=>{if(e.Button==Forms.MouseButtons.Left) open();};
        saveTimer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(600) };
        saveTimer.Tick+=(_,_)=>{saveTimer.Stop();Save();};
        taskbar=new(buddy=>{if(BuddyOpened!=null)BuddyOpened(buddy);else open();},EditExternally);
        taskbar.StatusChanged+=text=>TaskbarStatusChanged?.Invoke(text);
        foreach(var config in settings.Buddies) {var runtime=new BuddyRuntime(config);Buddies.Add(runtime);Apply(runtime,false);}
        animationTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(1000.0/30)};
        animationTimer.Tick+=(_,_)=>Animate();animationTimer.Start();
        monitor.Sampled+=sample=>Application.Current.Dispatcher.BeginInvoke(()=>
        {
            Snapshot=sample;
            foreach(var b in Buddies)b.SetSample(sample);
            Sampled?.Invoke(sample);
        });
        monitor.Start();
    }
    private void Animate()
    {
        var now=stopwatch.Elapsed; var elapsed=(now-previous).TotalSeconds;previous=now;
        foreach(var b in Buddies)
        {
            b.Advance(elapsed);
            if(tick%6==0) b.NotifySpeed();
        }
        tick++;
    }
    public BuddyRuntime Add()
    {
        if(Buddies.Count>=20) throw new InvalidOperationException("最多可以同時養 20 隻夥伴。");
        var config=new BuddyConfig { Name=$"新夥伴 {Buddies.Count+1}",Builtin=new[]{"cat","blob","bird"}[Buddies.Count%3] };
        Settings.Buddies.Add(config);var runtime=new BuddyRuntime(config);Buddies.Add(runtime);Apply(runtime);return runtime;
    }
    public void Remove(BuddyRuntime buddy)
    {
        if(desktops.Remove(buddy.Config.Id,out var window)) window.Close();
        Buddies.Remove(buddy);Settings.Buddies.Remove(buddy.Config);taskbar.Synchronize(Buddies);buddy.Dispose();SaveSoon();
    }
    public void Apply(BuddyRuntime buddy,bool save=true)
    {
        buddy.Config.Normalize();buddy.SetSample(Snapshot);buddy.NotifySettings();
        var b=buddy.Config;
        taskbar.Synchronize(Buddies);
        if(b.ShowDesktop)
        {
            if(!desktops.TryGetValue(b.Id,out var desktop))
            {desktop=new DesktopBuddyWindow(buddy,Buddies.IndexOf(buddy),open,()=>{SaveSoon();buddy.NotifySettings();},EditExternally);desktops[b.Id]=desktop;desktop.Show();}
            desktop.ApplySettings();if(!desktop.IsVisible)desktop.Show();
        }
        else if(desktops.Remove(b.Id,out var oldDesktop)) oldDesktop.Close();
        if(save) SaveSoon();
    }
    public void ResetPosition(BuddyRuntime buddy) {if(desktops.TryGetValue(buddy.Config.Id,out var desktop))desktop.ResetPosition();else {buddy.Config.X=buddy.Config.Y=null;SaveSoon();}}
    public void SetAllPaused(bool paused)=>EditExternally(()=>{foreach(var b in Buddies)b.Config.Paused=paused;});
    public void EditExternally(Action edit)
    {
        ExternalSettingsChanging?.Invoke();
        edit();
        foreach(var buddy in Buddies)Apply(buddy,false);
        ExternalSettingsChanged?.Invoke();SaveSoon();
    }
    public void SaveSoon() {saveTimer.Stop();saveTimer.Start();}
    public bool Save()
    {
        try {Store.Save(Settings);return true;}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException) {Notice?.Invoke("設定無法寫入，請確認資料夾權限。"+e.Message);return false;}
    }
    public void Dispose()
    {
        animationTimer.Stop();saveTimer.Stop();monitor.Dispose();Save();
        taskbar.Close();
        var managerMenu=manager.ContextMenuStrip;manager.Dispose();managerMenu?.Dispose();
        foreach(var window in desktops.Values)window.Close();desktops.Clear();
        foreach(var buddy in Buddies)buddy.Dispose();managerAsset.Dispose();
    }
}
