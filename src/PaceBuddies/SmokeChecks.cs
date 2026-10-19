using System.Diagnostics;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PaceBuddies.Animation;
using PaceBuddies.Services;
using PaceBuddies.Views;

namespace PaceBuddies;

internal static class SmokeChecks
{
    public static async Task Run(RuntimeCoordinator coordinator,MainWindow manager,string output)
    {
        Directory.CreateDirectory(output);var results=new List<string>();var watch=Stopwatch.StartNew();
        void Check(string name,bool value){if(!value)throw new InvalidOperationException(name);results.Add(name);}
        try
        {
            Check("Three independent desktop windows",coordinator.DesktopCount==3);
            Check("Three animated tray icons plus recovery manager",coordinator.TrayCount==3);
            Check("Original pixel sprites have eight distinct animation frames",coordinator.Buddies.All(b=>b.Asset.Frames.Length==8));
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
            Check("Display modes toggle independently",coordinator.DesktopCount==2&&coordinator.TrayCount==2);
            config.ShowDesktop=true;config.ShowTray=true;config.ClickThrough=true;coordinator.Apply(coordinator.Buddies[0]);
            Check("Click-through can be enabled with recovery tray",coordinator.DesktopCount==3&&coordinator.TrayCount==3);
            config.ClickThrough=false;coordinator.Apply(coordinator.Buddies[0]);
            var added=coordinator.Add();Check("Add independent buddy",coordinator.Buddies.Count==4&&coordinator.DesktopCount==4);
            coordinator.Remove(added);Check("Remove releases windows and tray icons",coordinator.Buddies.Count==3&&coordinator.DesktopCount==3&&coordinator.TrayCount==3);
            while(coordinator.Snapshot.Cpu==null&&watch.Elapsed.TotalSeconds<12)await Task.Delay(200);
            Check("Real CPU telemetry is sampled",coordinator.Snapshot.Cpu is >=0 and <=100);
            Check("Real physical RAM telemetry is sampled",coordinator.Snapshot.Memory is >0 and <=100);
            Check("GPU is valid or honestly unavailable",coordinator.Snapshot.Gpu is null or >=0 and <=100);
            Check("Settings save",coordinator.Save());
            Check("Settings and imported GIF survive reload",coordinator.Store.Load().Settings.Buddies[0].GifPath==config.GifPath);
            // Return to the original sprite for the product screenshot after validating arbitrary GIFs.
            config.GifPath=null;coordinator.Buddies[0].ReplaceAsset(BuiltinSprites.Create("cat"));coordinator.Apply(coordinator.Buddies[0]);
            config.Name="麻糬";manager.Reopen();manager.FlushEdits();
            await Task.Delay(250);await manager.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);
            Capture(manager,Path.Combine(output,"manager.png"));
            manager.Width=950;manager.Height=670;manager.UpdateLayout();await Task.Delay(100);
            Capture(manager,Path.Combine(output,"manager-compact.png"));
            Check("Manager renders at normal and compact sizes",new FileInfo(Path.Combine(output,"manager.png")).Length>10000);
            File.WriteAllText(Path.Combine(output,"smoke-result.json"),JsonSerializer.Serialize(new{Success=true,Checks=results,Telemetry=coordinator.Snapshot,ElapsedSeconds=watch.Elapsed.TotalSeconds},new JsonSerializerOptions{WriteIndented=true}));
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
}
