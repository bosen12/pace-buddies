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
    public static async Task Run(RuntimeCoordinator coordinator,MainWindow manager,string output)
    {
        Directory.CreateDirectory(output);var results=new List<string>();var skipped=new List<string>();var watch=Stopwatch.StartNew();
        var interactiveDesktop=GetCursorPos(out _);
        void Check(string name,bool value){if(!value)throw new InvalidOperationException(name);results.Add(name);}
        try
        {
            Check("Three independent desktop windows",coordinator.DesktopCount==3);
            Check("Three fixed taskbar buddies outside notification overflow",coordinator.TaskbarCount==3);
            Check("Only one static recovery notification icon",coordinator.ManagerTrayCount==1);
            var dock=Application.Current.Windows.OfType<TaskbarBuddyWindow>().Single();
            dock.RefreshAnchor();await Task.Delay(150);
            var anchor=NativeTaskbarLocator.Locate();
            Check("Native taskbar and notification bounds located",anchor!=null);
            Check("Fixed taskbar window is visible",dock.IsVisible);
            Check("Fixed taskbar window cannot steal keyboard focus",(GetWindowLongPtr(new WindowInteropHelper(dock).Handle,-20).ToInt64()&0x08000000)!=0);
            GetWindowRect(new WindowInteropHelper(dock).Handle,out var dockRect);
            Check("Actual window lies left of the hidden-icons chevron",anchor!=null&&dockRect.Right<=anchor.Notification.Left-2&&dockRect.Top>=anchor.Bar.Top&&dockRect.Bottom<=anchor.Bar.Bottom);
            Check("Actual window avoids active task buttons",anchor!=null&&dockRect.Left>=anchor.OccupiedRight);
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
            var added=coordinator.Add();Check("Add independent buddy",coordinator.Buddies.Count==4&&coordinator.DesktopCount==4);
            coordinator.Remove(added);Check("Remove releases windows and taskbar cells",coordinator.Buddies.Count==3&&coordinator.DesktopCount==3&&coordinator.TaskbarCount==3);
            coordinator.EditExternally(()=>{foreach(var b in coordinator.Buddies)b.Config.ShowTray=false;});
            Check("Disabling fixed buddies hides strip while retaining manager",!dock.IsVisible&&coordinator.TaskbarCount==0&&coordinator.ManagerTrayCount==1);
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
    [StructLayout(LayoutKind.Sequential)] private readonly record struct NativePoint(int X,int Y);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll",SetLastError=true)] private static extern bool GetCursorPos(out NativePoint point);
}
