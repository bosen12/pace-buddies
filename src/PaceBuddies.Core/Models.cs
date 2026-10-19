using System.Text.Json.Serialization;

namespace PaceBuddies.Core;

public enum MetricSource { Cpu, Memory, Gpu }
public enum LabelMode { Hover, Always, Hidden }

public sealed class BuddyConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "新夥伴";
    public string Builtin { get; set; } = "cat";
    public string? GifPath { get; set; }
    public MetricSource Source { get; set; }
    // Retained serialized name for 0.1 settings; now controls fixed taskbar placement.
    public bool ShowTray { get; set; } = true;
    public bool ShowDesktop { get; set; } = true;
    public bool Paused { get; set; }
    public LabelMode Label { get; set; } = LabelMode.Hover;
    public double MinimumSpeed { get; set; } = .5;
    public double MaximumSpeed { get; set; } = 4;
    public double ResponseSeconds { get; set; } = 1.5;
    public int Size { get; set; } = 96;
    public bool Topmost { get; set; } = true;
    public bool Locked { get; set; }
    public bool ClickThrough { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }

    public void Normalize()
    {
        if (Id == Guid.Empty) Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(Name) ? "新夥伴" : Name.Trim()[..Math.Min(Name.Trim().Length, 40)];
        if (Builtin is not ("cat" or "blob" or "bird")) Builtin = "cat";
        if (!Enum.IsDefined(Source)) Source = MetricSource.Cpu;
        if (!Enum.IsDefined(Label)) Label = LabelMode.Hover;
        Size = Math.Clamp(Size, 48, 240);
        MinimumSpeed = double.IsFinite(MinimumSpeed) ? Math.Clamp(MinimumSpeed, .1, 8) : .5;
        MaximumSpeed = double.IsFinite(MaximumSpeed) ? Math.Clamp(MaximumSpeed, MinimumSpeed, 8) : Math.Max(4,MinimumSpeed);
        ResponseSeconds = double.IsFinite(ResponseSeconds) ? Math.Clamp(ResponseSeconds,0,5) : 1.5;
        if (X is null || Y is null) X = Y = null;
    }
}

public sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public List<BuddyConfig> Buddies { get; set; } = [];
    public static AppSettings CreateDefault() => new() { Buddies = [
        new() { Name = "麻糬", Builtin = "cat", Source = MetricSource.Cpu },
        new() { Name = "布丁", Builtin = "blob", Source = MetricSource.Memory },
        new() { Name = "啾啾", Builtin = "bird", Source = MetricSource.Gpu }
    ] };
    public void Normalize()
    {
        Buddies ??= [];
        Buddies = Buddies.Where(b => b != null).Take(20).ToList();
        var ids = new HashSet<Guid>();
        foreach (var b in Buddies) { b.Normalize(); if (!ids.Add(b.Id)) { b.Id=Guid.NewGuid(); ids.Add(b.Id); } }
    }
}

public sealed record MetricSnapshot(double? Cpu, double? Memory, double? Gpu, DateTimeOffset Timestamp, string? GpuStatus = null)
{
    public double? Get(MetricSource source) => source switch { MetricSource.Cpu=>Cpu, MetricSource.Memory=>Memory, _=>Gpu };
    public static MetricSnapshot Empty => new(null,null,null,DateTimeOffset.Now);
    public static string Name(MetricSource source) => source switch { MetricSource.Cpu=>"CPU", MetricSource.Memory=>"記憶體", _=>"GPU" };
    public static string Format(double? value) => value is { } v && double.IsFinite(v) ? $"{v:0}%" : "無法讀取";
}
