using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PaceBuddies.Animation;

public sealed class AnimationAsset : IDisposable
{
    public BitmapSource[] Frames { get; }
    public int[] Delays { get; }
    public Icon[] TrayFrames { get; }
    public string Description { get; }
    public bool PixelArt { get; }
    public AnimationAsset(List<Bitmap> bitmaps,int[] delays,string description,bool pixelArt)
    {
        Description=description; Delays=delays; PixelArt=pixelArt;
        var frames=new List<BitmapSource>(); var icons=new List<Icon>();
        try
        {
            foreach(var bitmap in bitmaps)
            {
                using var png=new MemoryStream(); bitmap.Save(png,ImageFormat.Png); png.Position=0;
                var frame=BitmapFrame.Create(png,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad); frame.Freeze(); frames.Add(frame);
                using var small=new Bitmap(32,32,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(small))
                {
                    g.InterpolationMode=pixelArt?System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor:System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    var scale=Math.Min(32.0/bitmap.Width,32.0/bitmap.Height);
                    var w=(int)Math.Round(bitmap.Width*scale); var h=(int)Math.Round(bitmap.Height*scale);
                    g.DrawImage(bitmap,new Rectangle((32-w)/2,(32-h)/2,w,h));
                }
                var handle=small.GetHicon();
                try { using var borrowed=Icon.FromHandle(handle); icons.Add((Icon)borrowed.Clone()); }
                finally { DestroyIcon(handle); }
            }
            Frames=frames.ToArray(); TrayFrames=icons.ToArray();
        }
        catch { foreach(var icon in icons) icon.Dispose(); throw; }
        finally { foreach(var b in bitmaps) b.Dispose(); }
    }
    public void Dispose() { foreach(var icon in TrayFrames) icon.Dispose(); }
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr handle);
}
