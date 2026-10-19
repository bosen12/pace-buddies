using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace PaceBuddies.Animation;

public sealed class BuddyRuntime : INotifyPropertyChanged,IDisposable
{
    public BuddyConfig Config { get; }
    public AnimationAsset Asset { get; private set; }
    private AnimationClock clock;
    public int FrameIndex { get; private set; }
    public BitmapSource Frame => Asset.Frames[FrameIndex];
    public string Name => Config.Name;
    public string SourceName => MetricSnapshot.Name(Config.Source);
    public string Reading => MetricSnapshot.Format(usage);
    public string SpeedText => Config.Paused ? "已暫停" : $"{CurrentSpeed:0.00}×";
    public string Detail => $"{SourceName} · {Reading}  /  {SpeedText}";
    public string AssetDetail => Asset.Description;
    public double PreviewWidth=>106*Asset.AspectRatio;
    public BitmapScalingMode ScalingMode => Asset.PixelArt?BitmapScalingMode.NearestNeighbor:BitmapScalingMode.HighQuality;
    public string? Warning { get; private set; }
    public bool HasWarning=>Warning!=null;
    public double CurrentSpeed { get; private set; }
    private double? usage;
    private string? lastSpeed;
    public LatestRequest Appearance { get; }=new();
    public bool IsLoading { get; set; }
    public bool AppearanceInitialized { get; set; }
    public int AttemptedHeight { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public BuddyRuntime(BuddyConfig config)
    {
        Config=config;
        Asset=BuiltinSprites.Create(config.Builtin); clock=new(Asset.Delays); CurrentSpeed=config.MinimumSpeed;
    }
    public void SetWarning(string warning){Warning=warning;Changed(nameof(Warning));Changed(nameof(HasWarning));}
    public void ClearWarning(){if(Warning==null)return;Warning=null;Changed(nameof(Warning));Changed(nameof(HasWarning));}
    public void ReplaceAsset(AnimationAsset asset)
    {
        var old=Asset; Asset=asset; FrameIndex=0; clock=new(asset.Delays); Warning=null;
        Changed(nameof(Frame)); Changed(nameof(AssetDetail)); Changed(nameof(ScalingMode)); Changed(nameof(PreviewWidth));Changed(nameof(Warning));Changed(nameof(HasWarning)); old.Dispose();
    }
    public void SetSample(MetricSnapshot sample) { usage=sample.Get(Config.Source); Changed(nameof(Reading)); Changed(nameof(Detail)); }
    public bool Advance(double elapsed)
    {
        if(Config.Paused) return false;
        CurrentSpeed=Speed.Smooth(CurrentSpeed,Speed.Map(usage,Config.MinimumSpeed,Config.MaximumSpeed),Math.Min(elapsed,.25),Config.ResponseSeconds);
        var next=clock.Advance(elapsed*1000,CurrentSpeed);
        if(next==FrameIndex) return false;
        FrameIndex=next; Changed(nameof(Frame)); return true;
    }
    public void NotifySettings() { foreach(var p in new[]{nameof(Name),nameof(SourceName),nameof(Reading),nameof(SpeedText),nameof(Detail)}) Changed(p); }
    public void NotifySpeed() {if(lastSpeed==SpeedText)return;lastSpeed=SpeedText; Changed(nameof(SpeedText)); Changed(nameof(Detail)); }
    private void Changed([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new(name));
    public void Dispose(){Appearance.Dispose();Asset.Dispose();}
}
