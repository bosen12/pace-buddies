using System.Windows.Threading;
using PaceBuddies.Services;
using PaceBuddies.Views;

namespace PaceBuddies;

public partial class App : Application
{
    private RuntimeCoordinator? coordinator;
    private MainWindow? manager;
    private Mutex? instance;
    private EventWaitHandle? wake;
    private RegisteredWaitHandle? registration;
    private bool exiting;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var smoke=e.Args.Length>=2&&e.Args[0]=="--smoke-test";
        var data=smoke?Path.Combine(Path.GetFullPath(e.Args[1]),"isolated-data"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PaceBuddies");
        if(!smoke)
        {
            instance=new Mutex(true,@"Local\PaceBuddies.Instance",out var first);
            wake=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PaceBuddies.Show");
            if(!first){wake.Set();Shutdown();return;}
            registration=ThreadPool.RegisterWaitForSingleObject(wake,(_,_)=>Dispatcher.BeginInvoke(()=>manager?.Reopen()),null,-1,false);
        }
        DispatcherUnhandledException+=(_,args)=>
        {
            try{Directory.CreateDirectory(data);File.AppendAllText(Path.Combine(data,"error.log"),DateTime.Now+" "+args.Exception+Environment.NewLine);}catch(IOException){}catch(UnauthorizedAccessException){}
            MessageBox.Show("程式遇到錯誤，將關閉以避免設定遺失。詳細資訊已寫入資料夾的 error.log。\n"+args.Exception.Message,"跑跑夥伴");
            args.Handled=true;ExitApplication();
        };
        var store=new SettingsStore(data);var loaded=store.Load();
        coordinator=new(loaded.Settings,store,()=>manager?.Reopen(),ExitApplication);
        manager=new MainWindow(coordinator);MainWindow=manager;
        if(loaded.Warning!=null)manager.ShowNotice(loaded.Warning);
        if(smoke)
        {
            manager.ShowActivated=false;
            manager.Loaded+=async(_,_)=>await SmokeChecks.Run(coordinator,manager,Path.GetFullPath(e.Args[1]));
        }
        manager.Show();
    }
    public void ExitApplication()
    {
        if(exiting)return;exiting=true;
        manager?.FlushEdits();coordinator?.Dispose();
        if(manager!=null){manager.AllowClose=true;manager.Close();}
        Shutdown();
    }
    protected override void OnExit(ExitEventArgs e)
    {registration?.Unregister(null);wake?.Dispose();instance?.Dispose();base.OnExit(e);}
}
