using System.Text.RegularExpressions;
namespace PaceBuddies.Core;

public readonly record struct GpuSample(string Instance, double Usage);
public static partial class GpuAggregation
{
    [GeneratedRegex(@"^pid_\d+_(luid_.+?_phys_\d+_eng_\d+)_", RegexOptions.CultureInvariant)]
    private static partial Regex EngineKey();
    public static double? BusiestEngine(IEnumerable<GpuSample> samples)
    {
        var engines=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
        foreach(var sample in samples)
        {
            if(!double.IsFinite(sample.Usage)||sample.Usage<0) continue;
            var match=EngineKey().Match(sample.Instance);
            if(!match.Success) continue;
            var key=match.Groups[1].Value;
            engines[key]=engines.GetValueOrDefault(key)+sample.Usage;
        }
        return engines.Count==0 ? null : Math.Clamp(engines.Values.Max(),0,100);
    }
}
