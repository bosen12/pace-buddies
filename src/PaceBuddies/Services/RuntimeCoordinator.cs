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
    private readonly SemaphoreSlim decoder=new(1,1);
    private bool disposed,managerVisible;
    public bool AnimationRunning=>animationTimer.IsEnabled;
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
        animationTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(1000.0/30)};
        animationTimer.Tick+=(_,_)=>Animate();
        taskbar=new(buddy=>{if(BuddyOpened!=null)BuddyOpened(buddy);else open();},EditExternally);
        taskbar.IsVisibleChanged+=(_,_)=>UpdateAnimationState();
        taskbar.StatusChanged+=text=>{RecordTaskbarStatus(text);TaskbarStatusChanged?.Invoke(text);};
        foreach(var config in settings.Buddies) {var runtime=new BuddyRuntime(config);Buddies.Add(runtime);Apply(runtime,false);}
        monitor.Sampled+=sample=>Application.Current.Dispatcher.BeginInvoke(()=>
        {
            if(disposed)return;
            Snapshot=sample;
            foreach(var b in Buddies)b.SetSample(sample);
            foreach(var b in Buddies)EnsureResolution(b);
            UpdateAnimationState();
            Sampled?.Invoke(sample);
        });
        monitor.Start();
    }
    private void Animate()
    {
        var now=stopwatch.Elapsed; var elapsed=(now-previous).TotalSeconds;previous=now;
        foreach(var b in Buddies)
        {
            if(!Visible(b)||b.Config.Paused)continue;
            b.Advance(elapsed);
            if(tick%6==0) b.NotifySpeed();
        }
        tick++;
    }
    private bool Visible(BuddyRuntime buddy)=>managerVisible||(buddy.Config.ShowTray&&taskbar.IsVisible)||(desktops.TryGetValue(buddy.Config.Id,out var window)&&window.IsVisible);
    public void SetManagerVisible(bool visible){managerVisible=visible;UpdateAnimationState();}
    private void UpdateAnimationState()
    {
        if(disposed)return;
        var active=Buddies.Any(b=>!b.Config.Paused&&Visible(b));
        if(active&&!animationTimer.IsEnabled){previous=stopwatch.Elapsed;animationTimer.Start();}
        else if(!active)animationTimer.Stop();
    }
    private int TargetHeight(BuddyRuntime buddy)
    {
        var scale=System.Windows.Media.VisualTreeHelper.GetDpi(taskbar).DpiScaleY;
        if(Application.Current.MainWindow is { } main)scale=Math.Max(scale,System.Windows.Media.VisualTreeHelper.GetDpi(main).DpiScaleY);
        if(desktops.TryGetValue(buddy.Config.Id,out var desktop))scale=Math.Max(scale,System.Windows.Media.VisualTreeHelper.GetDpi(desktop).DpiScaleY);
        return GifLoader.Bucket((int)Math.Ceiling(Math.Max(106,buddy.Config.ShowDesktop?buddy.Config.Size:0)*scale));
    }
    private void EnsureResolution(BuddyRuntime buddy)
    {
        if(disposed||buddy.IsLoading||!buddy.AppearanceInitialized||buddy.Config.GifPath is not { } path)return;
        var target=TargetHeight(buddy);
        if(target==buddy.AttemptedHeight)return;
        if(!buddy.Asset.PixelArt&&buddy.Asset.TargetPixelHeight==target){buddy.AttemptedHeight=target;buddy.ClearWarning();return;}
        _=LoadAppearanceAsync(buddy,path,false);
    }
    public Task RestoreSavedAssetsAsync()=>Task.WhenAll(Buddies.Where(b=>b.Config.GifPath!=null).Select(b=>LoadAppearanceAsync(b,b.Config.GifPath!,false)).ToArray());
    public void SetBuiltin(BuddyRuntime buddy,string kind)
    {
        buddy.Appearance.Invalidate();buddy.IsLoading=false;buddy.AppearanceInitialized=false;
        buddy.Config.Builtin=kind;buddy.Config.GifPath=null;buddy.ReplaceAsset(BuiltinSprites.Create(kind));Apply(buddy);
    }
    public async Task<bool> LoadAppearanceAsync(BuddyRuntime buddy,string path,bool import)
    {
        if(disposed||!Buddies.Contains(buddy))return false;
        var ticket=buddy.Appearance.Begin();buddy.IsLoading=true;
        var target=TargetHeight(buddy);
        buddy.AppearanceInitialized=true;buddy.AttemptedHeight=target;
        AnimationAsset? loaded=null;string? ownedPath=null;
        try
        {
            await decoder.WaitAsync(ticket.Cancellation);
            try
            {
                await Task.Run(()=>
                {
                    if(import){var result=GifLoader.Import(path,Path.Combine(Store.DirectoryPath,"gifs"),target,ticket.Cancellation);ownedPath=result.Path;loaded=result.Asset;}
                    else loaded=GifLoader.Load(path,target,ticket.Cancellation);
                });
            }
            finally{decoder.Release();}
            if(disposed||!Buddies.Contains(buddy)||!buddy.Appearance.IsCurrent(ticket))return false;
            if(import)buddy.Config.GifPath=ownedPath;
            buddy.ReplaceAsset(loaded!);loaded=null;ownedPath=null;buddy.IsLoading=false;Apply(buddy,import);
            return true;
        }
        catch(OperationCanceledException){return false;}
        catch(Exception ex) when(ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or System.Runtime.InteropServices.ExternalException)
        {
            if(!disposed&&buddy.Appearance.IsCurrent(ticket)){buddy.SetWarning("GIF 無法載入："+ex.Message);Notice?.Invoke(buddy.Warning!);}
            return false;
        }
        finally
        {
            loaded?.Dispose();
            if(ownedPath!=null){try{File.Delete(ownedPath);}catch(IOException){}catch(UnauthorizedAccessException){}}
            if(buddy.Appearance.IsCurrent(ticket))buddy.IsLoading=false;
        }
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
        Buddies.Remove(buddy);Settings.Buddies.Remove(buddy.Config);taskbar.Synchronize(Buddies);buddy.Dispose();UpdateAnimationState();SaveSoon();
    }
    public void Apply(BuddyRuntime buddy,bool save=true)
    {
        buddy.Config.Normalize();buddy.SetSample(Snapshot);buddy.NotifySettings();
        var b=buddy.Config;
        taskbar.Synchronize(Buddies);
        if(b.ShowDesktop)
        {
            if(!desktops.TryGetValue(b.Id,out var desktop))
            {
                desktop=new DesktopBuddyWindow(buddy,Buddies.IndexOf(buddy),open,()=>{SaveSoon();buddy.NotifySettings();ExternalSettingsChanged?.Invoke();},EditExternally);
                var created=desktop;
                created.Closed+=(_,_)=>
                {
                    if(disposed||!desktops.TryGetValue(b.Id,out var current)||!ReferenceEquals(current,created))return;
                    desktops.Remove(b.Id);
                    Application.Current.Dispatcher.BeginInvoke(()=>{if(!disposed&&b.ShowDesktop&&Buddies.Contains(buddy))Apply(buddy,false);});
                };
                desktops[b.Id]=desktop;desktop.Show();
            }
            desktop.ApplySettings();if(!desktop.IsVisible)desktop.Show();
        }
        else if(desktops.Remove(b.Id,out var oldDesktop)) oldDesktop.Close();
        if(!b.ShowDesktop)buddy.SetDesktopStatus("開啟桌面懸浮後，可直接移動、縮放或綁定桌面層。");
        EnsureResolution(buddy);UpdateAnimationState();
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
    private void RecordTaskbarStatus(string status)
    {
        try
        {
            Directory.CreateDirectory(Store.DirectoryPath);
            var path=Path.Combine(Store.DirectoryPath,"taskbar-status.log");
            if(File.Exists(path)&&new FileInfo(path).Length>64*1024)File.Move(path,path+".previous",true);
            File.AppendAllText(path,$"{DateTimeOffset.Now:O} {status}{Environment.NewLine}");
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){ }
    }
    public bool Save()
    {
        try {Store.Save(Settings);return true;}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException) {Notice?.Invoke("設定無法寫入，請確認資料夾權限。"+e.Message);return false;}
    }
    public void Dispose()
    {
        disposed=true;
        animationTimer.Stop();saveTimer.Stop();monitor.Dispose();Save();
        taskbar.Close();
        var managerMenu=manager.ContextMenuStrip;manager.Dispose();managerMenu?.Dispose();
        foreach(var window in desktops.Values)window.Close();desktops.Clear();
        foreach(var buddy in Buddies)buddy.Dispose();managerAsset.Dispose();
    }
}
