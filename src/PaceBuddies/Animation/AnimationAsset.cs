using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat=System.Drawing.Imaging.PixelFormat;

namespace PaceBuddies.Animation;

internal sealed record FrameData(BitmapSource[] Frames,int[] Delays,string Description,bool PixelArt,double AspectRatio,int TargetHeight,long Bytes);

// Each buddy owns a lease; frozen frames can be shared safely.
public sealed class AnimationAsset : IDisposable
{
    private FrameData? data;
    private Action? release;
    private Icon[]? icons;
    private FrameData Data=>data??throw new ObjectDisposedException(nameof(AnimationAsset));
    public BitmapSource[] Frames=>Data.Frames;
    public int[] Delays=>Data.Delays;
    public string Description=>Data.Description;
    public bool PixelArt=>Data.PixelArt;
    public double AspectRatio=>Data.AspectRatio;
    public int TargetPixelHeight=>Data.TargetHeight;
    public long DecodedBytes=>Data.Bytes;
    public bool IsDisposed=>data==null;
    internal AnimationAsset(FrameData data,Action? release=null){this.data=data;this.release=release;}
    public AnimationAsset(List<Bitmap> bitmaps,int[] delays,string description,bool pixelArt)
    {
        try
        {
            var frames=bitmaps.Select(ToFrame).ToArray();
            data=new(frames,delays,description,pixelArt,(double)bitmaps[0].Width/bitmaps[0].Height,bitmaps[0].Height,frames.Sum(f=>(long)f.PixelWidth*f.PixelHeight*4));
        }
        finally{foreach(var b in bitmaps)b.Dispose();}
    }
    internal static BitmapSource ToFrame(Bitmap bitmap)
    {
        var bits=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try
        {
            var frame=BitmapSource.Create(bitmap.Width,bitmap.Height,96,96,PixelFormats.Bgra32,null,bits.Scan0,checked(bits.Stride*bitmap.Height),bits.Stride);
            frame.Freeze();return frame;
        }
        finally{bitmap.UnlockBits(bits);}
    }
    public Icon[] TrayFrames
    {
        get
        {
            if(icons!=null)return icons;
            var frame=Frames[0];
            using var bitmap=new Bitmap(frame.PixelWidth,frame.PixelHeight,PixelFormat.Format32bppArgb);
            var bits=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{frame.CopyPixels(Int32Rect.Empty,bits.Scan0,bits.Stride*bitmap.Height,bits.Stride);}finally{bitmap.UnlockBits(bits);}
            using var small=new Bitmap(bitmap,new System.Drawing.Size(32,32));
            var handle=small.GetHicon();
            try{using var borrowed=Icon.FromHandle(handle);icons=[(Icon)borrowed.Clone()];}
            finally{DestroyIcon(handle);}
            return icons;
        }
    }
    public void Dispose()
    {
        if(data==null)return;
        data=null;foreach(var icon in icons??[])icon.Dispose();icons=null;
        Interlocked.Exchange(ref release,null)?.Invoke();
    }
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr handle);
}
