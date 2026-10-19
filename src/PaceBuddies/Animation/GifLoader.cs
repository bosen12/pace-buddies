using System.Drawing;
using System.Drawing.Imaging;

namespace PaceBuddies.Animation;

public static class GifLoader
{
    public static AnimationAsset Load(string path)
    {
        var info=new FileInfo(path);
        if(info.Length>20*1024*1024) throw new InvalidDataException("GIF 請小於 20 MB。");
        using var stream=File.OpenRead(path);
        using var image=Image.FromStream(stream,true,true);
        if(image.RawFormat.Guid!=ImageFormat.Gif.Guid) throw new InvalidDataException("請選擇 GIF 格式的動畫。");
        var pixels=(long)image.Width*image.Height;
        if(pixels>2_000_000) throw new InvalidDataException("GIF 每格請小於 200 萬像素。");
        var dimension=new FrameDimension(image.FrameDimensionsList[0]);
        var count=image.GetFrameCount(dimension);
        if(count>300 || pixels*count>64_000_000) throw new InvalidDataException("GIF 最多 300 格，解碼總量請小於 6400 萬像素。");
        byte[]? times=null;
        if(image.PropertyIdList.Contains(0x5100)) times=image.GetPropertyItem(0x5100)?.Value;
        var frames=new List<Bitmap>(); var delays=new int[count];
        try
        {
            for(int i=0;i<count;i++)
            {
                image.SelectActiveFrame(dimension,i);
                var frame=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(frame)) g.DrawImage(image,0,0,image.Width,image.Height);
                frames.Add(frame);
                delays[i]=times!=null && times.Length>=(i+1)*4 ? Math.Clamp(BitConverter.ToInt32(times,i*4)*10,20,10000) : 100;
            }
            return new(frames,delays,$"GIF · {image.Width} × {image.Height} · {count} 格",false);
        }
        catch { foreach(var frame in frames) frame.Dispose(); throw; }
    }
    public static (string Path,AnimationAsset Asset) Import(string source,string directory)
    {
        var asset=Load(source);
        try
        {
            Directory.CreateDirectory(directory);
            var destination=Path.Combine(directory,Guid.NewGuid().ToString("N")+".gif");
            File.Copy(source,destination);
            return(destination,asset);
        }
        catch { asset.Dispose(); throw; }
    }
}
