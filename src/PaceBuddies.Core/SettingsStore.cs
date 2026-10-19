using System.Text.Json;
using System.Text.Json.Serialization;
namespace PaceBuddies.Core;

public sealed class SettingsStore(string directory)
{
    public string DirectoryPath { get; } = directory;
    private string FilePath => Path.Combine(DirectoryPath,"settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented=true, Converters={new JsonStringEnumConverter()} };
    public (AppSettings Settings,string? Warning) Load()
    {
        if(!File.Exists(FilePath)) return (AppSettings.CreateDefault(),null);
        try { return (Read(FilePath),null); }
        catch(Exception ex) when(ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            var warning="設定檔無法讀取，已嘗試使用備份。";
            try { File.Copy(FilePath,Path.Combine(DirectoryPath,$"settings.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json")); } catch(IOException) { } catch(UnauthorizedAccessException) { }
            try { return (Read(FilePath+".bak"),warning); }
            catch(Exception backupError) when(backupError is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            { return (AppSettings.CreateDefault(),"設定與備份無法讀取，已載入預設夥伴。原檔已保留。"); }
        }
    }
    private static AppSettings Read(string path)
    {
        var value=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path),Options) ?? throw new JsonException("Empty settings.");
        value.Normalize(); return value;
    }
    public void Save(AppSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(DirectoryPath);
        var temp=FilePath+".tmp";
        try
        {
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            { JsonSerializer.Serialize(stream,settings,Options); stream.Flush(true); }
            if(File.Exists(FilePath)) File.Replace(temp,FilePath,FilePath+".bak");
            else File.Move(temp,FilePath);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}
