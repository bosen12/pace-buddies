using System.Diagnostics;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using PaceBuddies.Animation;
using PaceBuddies.Services;
using PaceBuddies.Views;

namespace PaceBuddies;

internal static class SmokeChecks
{
    public static async Task Run(RuntimeCoordinator coordinator,MainWindow manager,string output,bool backgroundStart=false)
    {
        Directory.CreateDirectory(output);var results=new List<string>();var skipped=new List<string>();var watch=Stopwatch.StartNew();
        var interactiveDesktop=GetCursorPos(out _);
        void Check(string name,bool value){if(!value)throw new InvalidOperationException(name);results.Add(name);}
        try
        {
            Check(backgroundStart?"Background launch keeps manager hidden":"Normal launch shows manager",manager.IsVisible!=backgroundStart);
            if(backgroundStart)manager.Reopen();
            Check("Three independent desktop windows",coordinator.DesktopCount==3);
            Check("Three fixed taskbar buddies outside notification overflow",coordinator.TaskbarCount==3);
            Check("Only one static recovery notification icon",coordinator.ManagerTrayCount==1);
            Check("Desktop-layer binding is available in manager",manager.FindName("BindDesktopBox") is System.Windows.Controls.CheckBox);
            var dock=Application.Current.Windows.OfType<TaskbarBuddyWindow>().Single();
            dock.RefreshAnchor();await Task.Delay(150);
            var anchor=NativeTaskbarLocator.Locate();
            Check("Native taskbar and notification bounds located",anchor!=null);
            Check("Fixed taskbar window is visible",dock.IsVisible);
            Check("Fixed taskbar window cannot steal keyboard focus",(GetWindowLongPtr(new WindowInteropHelper(dock).Handle,-20).ToInt64()&0x08000000)!=0);
            GetWindowRect(new WindowInteropHelper(dock).Handle,out var dockRect);
            Check("Actual window lies left of the hidden-icons chevron",anchor!=null&&dockRect.Right<=anchor.Notification.Left-2&&dockRect.Top>=anchor.Bar.Top&&dockRect.Bottom<=anchor.Bar.Bottom);
            Check("Actual window avoids active task buttons",anchor!=null&&dockRect.Left>=anchor.OccupiedRight);
            var monitorBounds=System.Windows.Forms.Screen.FromHandle(anchor!.Handle).Bounds;
            var coverageProbe=new Window{WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowActivated=false,ShowInTaskbar=false};
            try
            {
                coverageProbe.Show();var coverageHandle=new WindowInteropHelper(coverageProbe).Handle;
                SetWindowPos(coverageHandle,IntPtr.Zero,monitorBounds.Left,monitorBounds.Top,monitorBounds.Width,monitorBounds.Height,0x4|0x10);
                var screen=new PixelRect(monitorBounds.Left,monitorBounds.Top,monitorBounds.Right,monitorBounds.Bottom);
                Check("Monitor-sized launch surface behind taskbar cannot hide buddies",!NativeTaskbarLocator.IsOccludingFullscreen(coverageHandle,anchor.Handle,screen));
                coverageProbe.Topmost=true;
                Check("Fullscreen above taskbar is recognized as occluding it",NativeTaskbarLocator.IsOccludingFullscreen(coverageHandle,anchor.Handle,screen));
                coverageProbe.Hide();
                Check("Hidden launch surface cannot hide taskbar buddies",!NativeTaskbarLocator.IsOccludingFullscreen(coverageHandle,anchor.Handle,screen));
            }
            finally{coverageProbe.Close();}
            var ordinary=new Window{Title="Pace Buddies · visibility regression",Width=600,Height=400,ShowActivated=false,ShowInTaskbar=false};
            try
            {
                ordinary.Show();
                var ordinaryHandle=new WindowInteropHelper(ordinary).Handle;
                SetWindowPos(ordinaryHandle,IntPtr.Zero,monitorBounds.Left,monitorBounds.Top,monitorBounds.Width,monitorBounds.Height,0x4|0x10);
                // The normal-window outer bounds can cover a monitor while its client has a title bar.
                ordinary.Activate();await Task.Delay(80);
                if(GetForegroundWindow()==ordinaryHandle)
                {
                    Check("Bordered monitor-sized window keeps taskbar buddies visible",NativeTaskbarLocator.Locate()?.Visible==true);
                    dock.RefreshAnchor();Check("Bordered normal application does not hide strip",dock.IsVisible);
                    ordinary.WindowState=WindowState.Maximized;await Task.Delay(80);
                    Check("Normal maximized application keeps strip visible",NativeTaskbarLocator.Locate()?.Visible==true);
                    var fullscreen=new Window{Title="Pace Buddies · fullscreen regression",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowActivated=false,ShowInTaskbar=false,Left=monitorBounds.Left/anchor.Scale,Top=monitorBounds.Top/anchor.Scale,Width=monitorBounds.Width/anchor.Scale,Height=monitorBounds.Height/anchor.Scale};
                    try
                    {
                        fullscreen.Show();var fullscreenHandle=new WindowInteropHelper(fullscreen).Handle;
                        SetWindowPos(fullscreenHandle,IntPtr.Zero,monitorBounds.Left,monitorBounds.Top,monitorBounds.Width,monitorBounds.Height,0x4|0x10);
                        fullscreen.Topmost=true;
                        fullscreen.Activate();await Task.Delay(80);
                        if(GetForegroundWindow()==fullscreenHandle)
                        {
                            SetWindowPos(fullscreenHandle,new IntPtr(-1),0,0,0,0,0x13);
                            GetWindowRect(fullscreenHandle,out var fullBounds);
                            File.WriteAllText(Path.Combine(output,"fullscreen-observation.json"),JsonSerializer.Serialize(new{Foreground=GetForegroundWindow().ToInt64(),Expected=fullscreenHandle.ToInt64(),FullBounds=fullBounds,Above=IsWindowAbove(fullscreenHandle,anchor.Handle),Native=NativeTaskbarLocator.Locate()?.ToString()},new JsonSerializerOptions{WriteIndented=true}));
                            var fullAnchor=NativeTaskbarLocator.Locate();
                            dock.RefreshAnchor();
                            if(fullAnchor is {Visible:false,HiddenReason:TaskbarHiddenReason.Fullscreen})
                                Check("Actual fullscreen occlusion hides the native strip",!dock.IsVisible);
                            else
                                Check("Shell-visible taskbar keeps native strip over a monitor-sized client",fullAnchor?.Visible==true&&dock.IsVisible);
                            fullscreen.Topmost=false;ordinary.Activate();await Task.Delay(80);dock.RefreshAnchor();
                            Check("Returning to a normal application restores strip",dock.IsVisible);
                            var stable=true;
                            for(var cycle=0;cycle<3;cycle++)
                            {
                                fullscreen.Topmost=false;fullscreen.Activate();await Task.Delay(40);dock.RefreshAnchor();stable&=dock.IsVisible;
                                ordinary.Activate();await Task.Delay(40);dock.RefreshAnchor();stable&=dock.IsVisible;
                            }
                            Check("Repeated launch surfaces behind visible taskbar do not blink",stable);
                        }
                        else skipped.Add("Fullscreen foreground activation requires an interactive desktop.");
                    }
                    finally{fullscreen.Close();}
                }
                else skipped.Add("Native foreground-window visibility regression requires an interactive desktop.");
            }
            finally{ordinary.Close();}
            dock.RefreshAnchor();
            var eventProbe=new TaskbarBuddyWindow(_=>{},action=>action(),TimeSpan.FromSeconds(30));
            try
            {
                eventProbe.Synchronize([coordinator.Buddies[0]]);
                var probeHandle=new WindowInteropHelper(eventProbe).Handle;
                var childInfo=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
                childInfo.ArgumentList.Add("--taskbar-raise-probe");
                using var child=Process.Start(childInfo)!;
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));await Task.Delay(100);
                Check("Native taskbar raise helper completed successfully",child.ExitCode==0);
                Check("Taskbar reorder restores strip without waiting for polling",eventProbe.IsVisible&&!IsWindowAbove(anchor.Handle,probeHandle));
            }
            finally{eventProbe.Close();dock.RefreshAnchor();}
            if(interactiveDesktop)Check("Fixed strip is on screen above taskbar surface",WindowFromPoint(new NativePoint((dockRect.Left+dockRect.Right)/2,(dockRect.Top+dockRect.Bottom)/2))==new WindowInteropHelper(dock).Handle);
            else skipped.Add("On-screen hit testing requires an accessible input desktop; native bounds and app rendering still verified.");
            var scroll=Descendants<System.Windows.Controls.ScrollViewer>(dock).Single();
            var cells=(System.Windows.Controls.StackPanel)scroll.Content;
            ((System.Windows.Controls.Border)cells.Children[1]).RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});
            Check("Clicking a fixed buddy opens its own settings",ReferenceEquals(((System.Windows.Controls.ListBox)manager.FindName("Roster")).SelectedItem,coordinator.Buddies[1]));
            manager.OpenBuddy(coordinator.Buddies[0]);
            Check("Original pixel sprites have eight distinct animation frames",coordinator.Buddies.All(b=>b.Asset.Frames.Length==8));
            coordinator.Settings.Buddies.ForEach(b=>b.Paused=true);
            manager.FlushEdits();
            Check("Unchanged editor never overwrites externally paused state",coordinator.Buddies.All(b=>b.Config.Paused));
            coordinator.Settings.Buddies.ForEach(b=>b.Paused=false);
            coordinator.SetAllPaused(true);
            Check("Paused buddies suspend animation timer",!coordinator.AnimationRunning);
            var unchangedCell=cells.Children[0];
            coordinator.Apply(coordinator.Buddies[0],false);
            Check("Ordinary settings updates retain taskbar visuals",ReferenceEquals(unchangedCell,cells.Children[0]));
            manager.ExerciseEditorForCheck();
            Check("Tray pause and later editor edits remain synchronized",coordinator.Buddies.All(b=>b.Config.Paused));
            var nameBox=(System.Windows.Controls.TextBox)manager.FindName("NameBox");
            nameBox.Text="保留尚未儲存的名稱";manager.Reopen();
            Check("Reopen preserves pending debounced edit",coordinator.Buddies[0].Config.Name=="保留尚未儲存的名稱");
            coordinator.SetAllPaused(false);
            var firstBuddy=coordinator.Buddies[0];
            coordinator.EditExternally(()=>firstBuddy.Config.ShowDesktop=false);
            manager.ExerciseEditorForCheck();
            Check("Desktop hide and later editor edits remain synchronized",!firstBuddy.Config.ShowDesktop&&coordinator.DesktopCount==2);
            coordinator.EditExternally(()=>firstBuddy.Config.ShowDesktop=true);
            var initial=coordinator.Buddies[0].FrameIndex;
            await Task.Delay(450);
            Check("Live animation advances",coordinator.Buddies[0].FrameIndex!=initial);
            var fixture=Path.Combine(output,"fixture.gif");
            var cacheBaseline=GifLoader.CachedBytes;
            var largeFixture=Path.Combine(output,"large-fixture.gif");
            var decodeWatch=Stopwatch.StartNew();
            using(var reduced=await Task.Run(()=>GifLoader.Load(largeFixture,128)))
            {
                Check("Large GIF decodes at display height",reduced.Frames[0].PixelHeight==128&&reduced.Frames[0].PixelWidth==256&&reduced.AspectRatio==2);
                Check("Downscaled frames use one sixteenth of source pixels",reduced.DecodedBytes==48L*256*128*4);
                using var shared=GifLoader.Load(largeFixture,128);
                Check("Identical GIFs share frozen frame storage",ReferenceEquals(reduced.Frames[0],shared.Frames[0])&&GifLoader.CachedBytes==cacheBaseline+reduced.DecodedBytes&&shared.Frames.All(f=>f.IsFrozen));
                reduced.Dispose();Check("Releasing one lease preserves other buddies",shared.Frames.Length==48);
            }
            Check("Final lease releases cache allocation",GifLoader.CachedBytes==cacheBaseline);
            var perAssetRejected=false;
            try{using var tooLarge=GifLoader.Load(largeFixture,512);}catch(InvalidDataException){perAssetRejected=true;}
            Check("Per-asset decoded memory budget is enforced",perAssetRejected&&GifLoader.CachedBytes==cacheBaseline);
            using(var one=GifLoader.Load(largeFixture,384))
            using(var two=GifLoader.Load(largeFixture,352))
            {
                var totalRejected=false;
                try{using var three=GifLoader.Load(largeFixture,320);}catch(InvalidDataException){totalRejected=true;}
                Check("Aggregate cache budget is enforced",totalRejected&&GifLoader.CachedBytes<=GifLoader.TotalBudget);
            }
            Check("Budget rejection leaves no retained cache entries",GifLoader.CachedBytes==cacheBaseline);
            File.WriteAllText(Path.Combine(output,"gif-memory.json"),JsonSerializer.Serialize(new{SourceBytes=48L*1024*512*4,DisplayBytes=48L*256*128*4,DisplayHeight=128,ElapsedSeconds=decodeWatch.Elapsed.TotalSeconds}));
            var raceBuddy=coordinator.Buddies[0];
            var gifFolder=Path.Combine(coordinator.Store.DirectoryPath,"gifs");
            var beforeCopies=Directory.Exists(gifFolder)?Directory.GetFiles(gifFolder).Length:0;
            var pendingImport=coordinator.LoadAppearanceAsync(raceBuddy,largeFixture,true);
            coordinator.SetBuiltin(raceBuddy,"bird");
            Check("Late import cannot overwrite a newer builtin choice",!await pendingImport&&raceBuddy.Config.GifPath==null&&raceBuddy.Config.Builtin=="bird"&&raceBuddy.Asset.PixelArt);
            Check("Cancelled import leaves no owned copy",!Directory.Exists(gifFolder)||Directory.GetFiles(gifFolder).Length==beforeCopies);
            var removing=coordinator.Add();var pendingRemoval=coordinator.LoadAppearanceAsync(removing,largeFixture,true);coordinator.Remove(removing);
            Check("Removing a loading buddy cancels its result",!await pendingRemoval&&removing.Asset.IsDisposed);
            raceBuddy.Config.GifPath=fixture;
            var restoring=coordinator.RestoreSavedAssetsAsync();coordinator.SetBuiltin(raceBuddy,"cat");await restoring;
            Check("Saved GIF restoration respects newer appearance choices",raceBuddy.Config.GifPath==null&&raceBuddy.Asset.PixelArt);
            raceBuddy.Config.GifPath=fixture;await coordinator.RestoreSavedAssetsAsync();
            Check("Saved GIFs restore asynchronously",raceBuddy.Asset.Frames.Length==3&&!raceBuddy.IsLoading);
            coordinator.SetBuiltin(raceBuddy,"cat");
            using(var pressureOne=GifLoader.Load(largeFixture,416))
            using(var pressureTwo=GifLoader.Load(largeFixture,352))
            {
                raceBuddy.Config.GifPath=largeFixture;raceBuddy.Config.Size=240;
                Check("Cache pressure can reject a startup placeholder",!await coordinator.LoadAppearanceAsync(raceBuddy,largeFixture,false)&&raceBuddy.Asset.PixelArt&&raceBuddy.HasWarning);
                raceBuddy.Config.Size=96;coordinator.Apply(raceBuddy,false);
                while(raceBuddy.IsLoading)await Task.Delay(20);
                Check("Reducing height retries a previously failed GIF",!raceBuddy.Asset.PixelArt&&!raceBuddy.HasWarning&&raceBuddy.Asset.TargetPixelHeight==128);
            }
            coordinator.SetBuiltin(raceBuddy,"cat");
            using(var placeholder=new BuddyRuntime(new BuddyConfig{GifPath=largeFixture}))
                Check("Runtime construction uses cheap placeholder before restoration",placeholder.Asset.PixelArt);
            var registryPath=@"Software\PaceBuddies\Tests\"+Guid.NewGuid().ToString("N");
            try
            {
                var startup=new WindowsStartupRegistration(registryPath);
                startup.SetEnabled(true,@"C:\My App\PaceBuddies.exe",true);
                Check("Startup stores quoted background command",startup.ReadCommand()=="\"C:\\My App\\PaceBuddies.exe\" --background");
                startup.SetEnabled(true,@"C:\My App\PaceBuddies.exe",false);
                Check("Startup can launch manager visibly",startup.ReadCommand()=="\"C:\\My App\\PaceBuddies.exe\"");
                startup.SetEnabled(false,@"C:\My App\PaceBuddies.exe",true);Check("Startup can be disabled",!startup.IsEnabled);
            }
            finally{Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(registryPath,false);}
            using(var gif=GifLoader.Load(fixture))
            {
                Check("GIF frame count",gif.Frames.Length==3);
                Check("GIF original variable timing",gif.Delays.SequenceEqual(new[]{100,200,300}));
                var bgra=new FormatConvertedBitmap(gif.Frames[0],PixelFormats.Bgra32,null,0);
                byte[] pixel=new byte[4];bgra.CopyPixels(new Int32Rect(0,0,1,1),pixel,4,0);
                Check("GIF transparent background",pixel[3]==0);
                File.Move(fixture,fixture+".move");File.Move(fixture+".move",fixture);
                Check("Decoded GIF does not lock original file",true);
            }
            await manager.ImportGif(fixture);
            Check("GIF import copies into owned storage",coordinator.Buddies[0].Config.GifPath is {} p&&File.Exists(p)&&p!=fixture);
            Check("GIF replaces live runtime",coordinator.Buddies[0].Asset.Frames.Length==3);
            manager.ExerciseEditorForCheck();
            Check("Editor applies source speed size and label",coordinator.Buddies[0].Config is {Name:"麻糬 · 測試",MinimumSpeed:.6,MaximumSpeed:4.5,Size:104,Label:LabelMode.Always});
            Check("Editor isolates other buddies",coordinator.Buddies[1].Config.MinimumSpeed==.5);
            var config=coordinator.Buddies[0].Config;
            config.ShowDesktop=false;config.ShowTray=false;coordinator.Apply(coordinator.Buddies[0]);
            Check("Display modes toggle independently",coordinator.DesktopCount==2&&coordinator.TaskbarCount==2);
            config.ShowDesktop=true;config.ShowTray=true;config.ClickThrough=true;coordinator.Apply(coordinator.Buddies[0]);
            Check("Click-through desktop retains fixed taskbar buddies",coordinator.DesktopCount==3&&coordinator.TaskbarCount==3);
            var desktop=Application.Current.Windows.OfType<DesktopBuddyWindow>().First(w=>ReferenceEquals(w.DataContext,coordinator.Buddies[0]));
            Check("Native click-through style is enabled",(GetWindowLongPtr(new WindowInteropHelper(desktop).Handle,-20).ToInt64()&0x20)!=0);
            config.ClickThrough=false;coordinator.Apply(coordinator.Buddies[0]);
            Check("Native click-through style can be disabled",(GetWindowLongPtr(new WindowInteropHelper(desktop).Handle,-20).ToInt64()&0x20)==0);
            var desktopDragImage=Descendants<System.Windows.Controls.Image>(desktop).Single();
            var dragRoute=new List<string>();
            foreach(var element in new UIElement[]{desktopDragImage,Descendants<System.Windows.Controls.ScrollViewer>(desktop).Single(),desktop})
                element.AddHandler(UIElement.MouseLeftButtonDownEvent,new System.Windows.Input.MouseButtonEventHandler((sender,args)=>dragRoute.Add($"{sender.GetType().Name}: handled={args.Handled}, captured={desktop.IsMouseCaptured}")),true);
            desktopDragImage.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.MouseDownEvent});
            var imageDragCaptured=desktop.IsMouseCaptured;
            File.WriteAllText(Path.Combine(output,"drag-route.json"),JsonSerializer.Serialize(new{Route=dragRoute,Captured=imageDragCaptured},new JsonSerializerOptions{WriteIndented=true}));
            desktop.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.PreviewMouseUpEvent});
            Check("Mouse press on GIF starts the drag gesture",imageDragCaptured);
            Check("GIF release finishes its captured gesture",!desktop.IsMouseCaptured);
            var pendingDesktopName="保留桌面操作前的草稿";
            nameBox.Text=pendingDesktopName;
            Check("Unlocked desktop buddy can resize directly",desktop.ResizeDirectlyTo(160)&&config.Size==160);
            Check("Desktop resize flushes drafts and synchronizes editor",config.Name==pendingDesktopName&&Math.Abs(((System.Windows.Controls.Slider)manager.FindName("SizeSlider")).Value-160)<.01);
            ((System.Windows.Controls.Slider)manager.FindName("SizeSlider")).Value=200;
            Check("Relative resize respects pending size draft",desktop.ResizeDirectlyBy(8)&&config.Size==208&&((System.Windows.Controls.Slider)manager.FindName("SizeSlider")).Value==208);
            desktop.ResizeDirectlyTo(160);
            var desktopArea=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(desktop).Handle).WorkingArea;
            Check("Unlocked desktop buddy can move directly",desktop.MoveDirectlyTo(desktopArea.Left+80,desktopArea.Top+80));
            GetWindowRect(new WindowInteropHelper(desktop).Handle,out var movedDesktop);
            Check("Desktop movement persists physical coordinates",config.X==movedDesktop.Left&&config.Y==movedDesktop.Top&&movedDesktop.Left==desktopArea.Left+80);
            coordinator.EditExternally(()=>config.Locked=true);
            desktopDragImage.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.MouseDownEvent});
            Check("Locked GIF press cannot start a gesture",!desktop.IsMouseCaptured);
            Check("Locked desktop blocks resize and movement",!desktop.ResizeDirectlyTo(240)&&!desktop.MoveDirectlyTo(0,0)&&config.Size==160);
            coordinator.EditExternally(()=>{config.Locked=false;config.ClickThrough=true;});
            Check("Click-through desktop blocks direct manipulation",!desktop.ResizeDirectlyTo(240)&&!desktop.MoveDirectlyTo(0,0));
            coordinator.EditExternally(()=>config.ClickThrough=false);
            Check("Desktop resize supports larger proportional canvas",desktop.ResizeDirectlyTo(400)&&Descendants<System.Windows.Controls.Image>(desktop).Single().Height==400);
            desktop.ResizeDirectlyTo(104);
            GetWindowRect(new WindowInteropHelper(desktop).Handle,out var beforeBinding);
            var layerBox=(System.Windows.Controls.CheckBox)manager.FindName("BindDesktopBox");
            layerBox.IsChecked=true;layerBox.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));manager.FlushEdits();
            await Task.Delay(100);desktop.UpdateLayout();
            var desktopHandle=new WindowInteropHelper(desktop).Handle;
            var actualHost=GetParent(desktopHandle);
            Check("Desktop binding attaches the actual native window",config.BindDesktop&&desktop.IsDesktopBound&&actualHost==DesktopLayerAttachment.FindHost());
            Check("Desktop binding uses child style without topmost",(GetWindowLongPtr(desktopHandle,-16).ToInt64()&0x40000000)!=0&&(GetWindowLongPtr(desktopHandle,-20).ToInt64()&8)==0&&!desktop.Topmost);
            Check("Desktop binding preserves floating topmost preference",config.Topmost&&!((System.Windows.Controls.CheckBox)manager.FindName("TopmostBox")).IsEnabled);
            layerBox.BringIntoView();manager.UpdateLayout();Capture(manager,Path.Combine(output,"desktop-settings.png"));
            GetWindowRect(desktopHandle,out var afterBinding);
            Check("Binding preserves physical screen position",Math.Abs(beforeBinding.Left-afterBinding.Left)<=1&&Math.Abs(beforeBinding.Top-afterBinding.Top)<=1);
            desktopDragImage.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.MouseDownEvent});
            Check("Desktop-bound GIF press starts a gesture",desktop.IsMouseCaptured);
            desktop.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.PreviewMouseUpEvent});
            Check("Bound buddy still resizes and moves",desktop.ResizeDirectlyTo(200)&&desktop.MoveDirectlyTo(desktopArea.Left+150,desktopArea.Top+150)&&desktop.IsDesktopBound);
            GetWindowRect(desktopHandle,out var boundPosition);
            Check("Bound moves map to screen coordinates",boundPosition.Left==desktopArea.Left+150&&boundPosition.Top==desktopArea.Top+150);
            if(interactiveDesktop)
            {
                var shellType=Type.GetTypeFromProgID("Shell.Application")!;
                dynamic shell=Activator.CreateInstance(shellType)!;
                try
                {
                    shell.MinimizeAll();
                    // Shell minimization is asynchronous; wait for desktop exposure instead of assuming a fixed animation duration.
                    var desktopExposure=Stopwatch.StartNew();
                    while(!AnyHit(desktopHandle,boundPosition)&&desktopExposure.ElapsedMilliseconds<2000)await Task.Delay(50);
                    Check("Desktop-bound buddy survives Show Desktop",IsWindowVisible(desktopHandle)&&GetParent(desktopHandle)==actualHost);
                    Check("Desktop-bound buddy renders above desktop surface",AnyHit(desktopHandle,boundPosition));
                    Capture(desktop,Path.Combine(output,"desktop-bound-buddy.png"));
                }
                finally{shell.UndoMinimizeAll();Marshal.FinalReleaseComObject(shell);manager.Reopen();}
            }
            else skipped.Add("Show Desktop visual hit testing requires an accessible input desktop; native parent/style still checked.");
            desktopDragImage.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,System.Windows.Input.MouseButton.Left){RoutedEvent=System.Windows.Input.Mouse.MouseDownEvent});
            Check("Desktop gesture is captured before host closure",desktop.IsMouseCaptured);
            var retiredHandle=desktopHandle;desktop.Close();await Task.Delay(120);
            desktop=Application.Current.Windows.OfType<DesktopBuddyWindow>().Single(window=>ReferenceEquals(window.DataContext,firstBuddy));
            desktopHandle=new WindowInteropHelper(desktop).Handle;
            Check("Closed desktop host child is recreated with binding intact",desktopHandle!=retiredHandle&&desktop.IsDesktopBound&&GetParent(desktopHandle)==DesktopLayerAttachment.FindHost());
            GetWindowRect(desktopHandle,out var beforeDetach);
            layerBox.IsChecked=false;layerBox.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));manager.FlushEdits();await Task.Delay(80);
            Check("Desktop binding detaches back into floating mode",!desktop.IsDesktopBound&&GetParent(desktopHandle)==IntPtr.Zero&&desktop.Topmost&&config.Topmost);
            GetWindowRect(desktopHandle,out var afterDetach);
            Check("Detaching preserves screen position",Math.Abs(beforeDetach.Left-afterDetach.Left)<=1&&Math.Abs(beforeDetach.Top-afterDetach.Top)<=1);
            desktop.ResizeDirectlyTo(104);
            var added=coordinator.Add();Check("Add independent buddy",coordinator.Buddies.Count==4&&coordinator.DesktopCount==4);
            coordinator.Remove(added);Check("Remove releases windows and taskbar cells",coordinator.Buddies.Count==3&&coordinator.DesktopCount==3&&coordinator.TaskbarCount==3);
            coordinator.EditExternally(()=>{foreach(var b in coordinator.Buddies)b.Config.ShowTray=false;});
            Check("Disabling fixed buddies hides strip while retaining manager",!dock.IsVisible&&coordinator.TaskbarCount==0&&coordinator.ManagerTrayCount==1);
            Check("Empty taskbar releases all visual references",cells.Children.Count==0);
            coordinator.EditExternally(()=>{foreach(var b in coordinator.Buddies)b.Config.ShowTray=true;});
            Check("Fixed buddies can be restored",dock.IsVisible&&coordinator.TaskbarCount==3);
            await manager.ImportGif(Path.Combine(output,"wide-fixture.gif"));
            var widePixels=new FormatConvertedBitmap(coordinator.Buddies[0].Asset.Frames[0],PixelFormats.Bgra32,null,0);
            var opaque=new byte[4];widePixels.CopyPixels(new Int32Rect(64,0,1,1),opaque,4,0);
            Check("Wide GIF opaque body survives transparent decode",opaque[3]==255&&opaque[2]==245);
            dock.RefreshAnchor();dock.UpdateLayout();await Task.Delay(100);
            var wideImage=Descendants<System.Windows.Controls.Image>(dock).First();
            var desktopImage=Descendants<System.Windows.Controls.Image>(desktop).Single();
            Check("Wide GIF fills actual taskbar canvas height",Math.Abs(wideImage.ActualHeight-dock.ActualHeight)<1);
            Check("Wide GIF expands actual image width proportionally",Math.Abs(wideImage.ActualWidth/wideImage.ActualHeight-4)<.01);
            Check("Wide desktop GIF uses configured height and natural width",Math.Abs(desktopImage.Height-config.Size)<.01&&Math.Abs(desktopImage.Width/config.Size-4)<.01);
            var labelHeight=wideImage.ActualHeight;
            coordinator.EditExternally(()=>config.Label=LabelMode.Hidden);dock.UpdateLayout();
            Check("Hiding numeric label does not change GIF height",Math.Abs(Descendants<System.Windows.Controls.Image>(dock).First().ActualHeight-labelHeight)<1);
            coordinator.EditExternally(()=>config.Label=LabelMode.Always);dock.UpdateLayout();
            Capture(dock,Path.Combine(output,"wide-taskbar-buddy.png"));Capture(desktop,Path.Combine(output,"wide-desktop-buddy.png"));
            var extraWide=new List<BuddyRuntime>();
            for(var i=0;i<7;i++)
            {
                var extra=coordinator.Add();extra.Config.GifPath=config.GifPath;extra.ReplaceAsset(GifLoader.Load(config.GifPath!));extra.Config.ShowDesktop=false;coordinator.Apply(extra);extraWide.Add(extra);
            }
            dock.RefreshAnchor();dock.UpdateLayout();await Task.Delay(100);
            Check("Many wide GIFs use horizontal overflow without shrinking",dock.Placement?.Scrollable==true&&scroll.ScrollableWidth>0&&Descendants<System.Windows.Controls.Image>(dock).All(image=>Math.Abs(image.ActualHeight-labelHeight)<1));
            var forward=Descendants<System.Windows.Controls.Button>(dock).Single(button=>Equals(button.Content,"›"));
            forward.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));dock.UpdateLayout();
            Check("Next arrow scrolls to reach overflow characters",scroll.HorizontalOffset>0);
            Capture(dock,Path.Combine(output,"wide-overflow-buddies.png"));
            scroll.ScrollToRightEnd();dock.UpdateLayout();
            Check("Last overflow character remains reachable",Math.Abs(scroll.HorizontalOffset-scroll.ScrollableWidth)<1);
            foreach(var extra in extraWide)coordinator.Remove(extra);
            await manager.ImportGif(Path.Combine(output,"panorama-fixture.gif"));
            desktop.UpdateLayout();await Task.Delay(100);
            var desktopViewport=Descendants<System.Windows.Controls.ScrollViewer>(desktop).Single();
            var workArea=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(desktop).Handle).WorkingArea;
            GetWindowRect(new WindowInteropHelper(desktop).Handle,out var panoramaBounds);
            Check("Panorama desktop viewport stays within monitor width",panoramaBounds.Width<=workArea.Width);
            Check("Panorama desktop retains full image height with scrolling",Descendants<System.Windows.Controls.Image>(desktop).Single().Height==config.Size&&desktopViewport.ScrollableWidth>0);
            desktopViewport.ScrollToRightEnd();desktop.UpdateLayout();
            Check("Panorama desktop right edge remains reachable",Math.Abs(desktopViewport.HorizontalOffset-desktopViewport.ScrollableWidth)<1);
            await manager.ImportGif(Path.Combine(output,"wide-fixture.gif"));
            while(coordinator.Snapshot.Cpu==null&&watch.Elapsed.TotalSeconds<12)await Task.Delay(200);
            Check("Real CPU telemetry is sampled",coordinator.Snapshot.Cpu is >=0 and <=100);
            Check("Real physical RAM telemetry is sampled",coordinator.Snapshot.Memory is >0 and <=100);
            Check("GPU is valid or honestly unavailable",coordinator.Snapshot.Gpu is null or >=0 and <=100);
            Check("Settings save",coordinator.Save());
            Check("Settings and imported GIF survive reload",coordinator.Store.Load().Settings.Buddies[0].GifPath==config.GifPath);
            // Return to the original sprite for the product screenshot after validating arbitrary GIFs.
            config.GifPath=null;coordinator.Buddies[0].ReplaceAsset(BuiltinSprites.Create("cat"));coordinator.Apply(coordinator.Buddies[0]);
            config.Name="麻糬";coordinator.Buddies[0].NotifySettings();manager.Reopen();manager.FlushEdits();
            await Task.Delay(250);await manager.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);
            Capture(manager,Path.Combine(output,"manager.png"));
            manager.Width=950;manager.Height=670;manager.UpdateLayout();await Task.Delay(100);
            Capture(manager,Path.Combine(output,"manager-compact.png"));
            Check("Manager renders at normal and compact sizes",new FileInfo(Path.Combine(output,"manager.png")).Length>10000);
            manager.FitToWorkArea(new Rect(0,0,911,512));manager.UpdateLayout();await Task.Delay(100);
            Check("High DPI small work area keeps whole window visible",manager.ActualWidth<=911&&manager.ActualHeight<=512&&manager.MinWidth<=911&&manager.MinHeight<=512);
            Capture(manager,Path.Combine(output,"manager-small-hidpi.png"));
            var footer=(System.Windows.Controls.TextBlock)manager.FindName("Status");
            Check("Small work area keeps footer inside window",footer.TranslatePoint(new Point(0,footer.ActualHeight),manager).Y<=manager.ActualHeight);
            Capture(desktop,Path.Combine(output,"desktop-buddy.png"));
            Capture(dock,Path.Combine(output,"fixed-taskbar-buddies.png"));
            manager.Hide();
            coordinator.EditExternally(()=>{foreach(var b in coordinator.Buddies){b.Config.ShowTray=false;b.Config.ShowDesktop=false;}});
            Check("Invisible buddies suspend animation timer",!coordinator.AnimationRunning);
            manager.Reopen();Check("Visible manager resumes animation timer",coordinator.AnimationRunning);
            foreach(var b in coordinator.Buddies.ToArray())coordinator.Remove(b);
            Check("Last buddy removal clears editor and taskbar references",((System.Windows.Controls.StackPanel)manager.FindName("Editor")).DataContext==null&&cells.Children.Count==0);
            File.WriteAllText(Path.Combine(output,"smoke-result.json"),JsonSerializer.Serialize(new{Success=true,Checks=results,SkippedChecks=skipped,InteractiveDesktopAvailable=interactiveDesktop,Taskbar=new{Bar=anchor?.Bar,Notification=anchor?.Notification,OccupiedRight=anchor?.OccupiedRight,Scale=anchor?.Scale},ActualTaskbarBounds=dockRect,Telemetry=coordinator.Snapshot,ElapsedSeconds=watch.Elapsed.TotalSeconds},new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception ex)
        {File.WriteAllText(Path.Combine(output,"smoke-result.json"),JsonSerializer.Serialize(new{Success=false,Checks=results,Error=ex.ToString()},new JsonSerializerOptions{WriteIndented=true}));Environment.ExitCode=1;}
        finally{((App)Application.Current).ExitApplication();}
    }
    private static void Capture(FrameworkElement element,string path)
    {
        element.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
    }
    internal static void RaiseTaskbarForProbe()
    {
        var bar=FindWindow("Shell_TrayWnd",null);
        if(bar==IntPtr.Zero){Environment.ExitCode=1;return;}
        if(!SetWindowPos(bar,new IntPtr(-1),0,0,0,0,0x13)){Environment.ExitCode=2;return;}
        NotifyWinEvent(0x8004,GetDesktopWindow(),0,0);
    }
    private static bool IsWindowAbove(IntPtr candidate,IntPtr target)
    {
        var window=GetWindow(target,3);
        for(var count=0;window!=IntPtr.Zero&&count<512;count++,window=GetWindow(window,3))
            if(window==candidate)return true;
        return false;
    }
    private static bool AnyHit(IntPtr window,PixelRect rect)
    {
        for(var y=rect.Top+2;y<rect.Bottom;y+=4)
            for(var x=rect.Left+2;x<rect.Right;x+=4)
                if(WindowFromPoint(new NativePoint(x,y))==window)return true;
        return false;
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T match)yield return match;
            foreach(var descendant in Descendants<T>(child))yield return descendant;
        }
    }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out PixelRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr FindWindow(string? name,string? title);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window,uint command);
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern void NotifyWinEvent(uint kind,IntPtr window,int objectId,int childId);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct NativePoint(int X,int Y);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll",SetLastError=true)] private static extern bool GetCursorPos(out NativePoint point);
}
