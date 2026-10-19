using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace PaceBuddies.Animation;

public static class GifLoader
{
    public const long AssetBudget=64L*1024*1024,TotalBudget=128L*1024*1024;
    private sealed class Entry(FrameData data){public FrameData Data=data;public int References;}
    private static readonly object sync=new();
    private static readonly object decoding=new();
    private static readonly Dictionary<string,Entry> cache=[];
    public static long CachedBytes{get{lock(sync)return cache.Values.Sum(e=>e.Data.Bytes);}}
    public static int CachedAssets{get{lock(sync)return cache.Count;}}
    public static int Bucket(int height)=>Math.Clamp((Math.Max(1,height)+31)/32*32,32,2048);
    public static AnimationAsset Load(string path,int targetPixelHeight=256,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();
        if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("GIF 請小於 20 MB。");
        // Keep the source stable for both hash and decoder. Only one decoder allocates at a time.
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var height=Bucket(targetPixelHeight);
        var key=Convert.ToHexString(SHA256.HashData(stream))+":"+height;
        lock(decoding)
        {
            cancellation.ThrowIfCancellationRequested();
            Entry? entry;
            lock(sync)
            {
                if(cache.TryGetValue(key,out entry))
                {
                    entry.References++;return Lease(key,entry);
                }
            }
            stream.Position=0;
            entry=new(Decode(stream,height,cancellation)){References=1};
            lock(sync)cache.Add(key,entry);
            return Lease(key,entry);
        }
    }
    private static AnimationAsset Lease(string key,Entry entry)=>new(entry.Data,()=>{lock(sync){if(--entry.References==0)cache.Remove(key);}});
    private static FrameData Decode(Stream stream,int target,CancellationToken cancellation)
    {
        using var image=Image.FromStream(stream,true,true);
        if(image.RawFormat.Guid!=ImageFormat.Gif.Guid)throw new InvalidDataException("請選擇 GIF 格式的動畫。");
        var pixels=(long)image.Width*image.Height;
        if(pixels>2_000_000)throw new InvalidDataException("GIF 每格請小於 200 萬像素。");
        var dimension=new FrameDimension(image.FrameDimensionsList[0]);
        var count=image.GetFrameCount(dimension);
        if(count>300||pixels*count>64_000_000)throw new InvalidDataException("GIF 最多 300 格，解碼總量請小於 6400 萬像素。");
        var h=Math.Min(target,image.Height);var w=Math.Max(1,(int)Math.Round((double)image.Width*h/image.Height));
        var bytes=checked((long)w*h*4*count);
        if(bytes>AssetBudget||bytes+CachedBytes>TotalBudget)
            throw new InvalidDataException("GIF 顯示快取超過記憶體上限，請減少影格、降低角色高度或移除其他 GIF。");
        byte[]? times=image.PropertyIdList.Contains(0x5100)?image.GetPropertyItem(0x5100)?.Value:null;
        var frames=new BitmapSource[count];var delays=new int[count];
        for(var i=0;i<count;i++)
        {
            cancellation.ThrowIfCancellationRequested();image.SelectActiveFrame(dimension,i);
            using var bitmap=new Bitmap(w,h,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(bitmap))
            {
                g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(image,new Rectangle(0,0,w,h),0,0,image.Width,image.Height,GraphicsUnit.Pixel);
            }
            frames[i]=AnimationAsset.ToFrame(bitmap);
            delays[i]=times!=null&&times.Length>=(i+1)*4?(int)Math.Clamp((long)BitConverter.ToInt32(times,i*4)*10,20,10000):100;
        }
        cancellation.ThrowIfCancellationRequested();
        return new(frames,delays,$"GIF · {image.Width} × {image.Height} · {count} 格",false,(double)image.Width/image.Height,target,bytes);
    }
    public static (string Path,AnimationAsset Asset) Import(string source,string directory,int targetPixelHeight=256,CancellationToken cancellation=default)
    {
        if(new FileInfo(source).Length>20*1024*1024)throw new InvalidDataException("GIF 請小於 20 MB。");
        Directory.CreateDirectory(directory);
        var destination=Path.Combine(directory,Guid.NewGuid().ToString("N")+".gif");
        try
        {
            cancellation.ThrowIfCancellationRequested();File.Copy(source,destination);
            return(destination,Load(destination,targetPixelHeight,cancellation));
        }
        catch{File.Delete(destination);throw;}
    }
}
