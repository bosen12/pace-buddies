namespace PaceBuddies.Core;

public static class Speed
{
    public static double Map(double? usage, double min, double max)
        => min+(max-min)*(usage is { } v && double.IsFinite(v) ? Math.Clamp(v,0,100)/100 : 0);
    public static double Smooth(double current, double target, double seconds, double response)
        => response <= 0 ? target : current + (target-current)*(1-Math.Exp(-Math.Max(0,seconds)/response));
}

public sealed class AnimationClock
{
    private readonly double[] delays;
    private readonly double duration;
    private double position;
    public int Frame { get; private set; }
    public AnimationClock(IEnumerable<int> frameDelays)
    {
        delays=frameDelays.Select(d=>(double)Math.Clamp(d,20,10000)).ToArray();
        if(delays.Length==0) throw new ArgumentException("Animation needs frames.");
        duration=delays.Sum();
    }
    public int Advance(double milliseconds, double speed)
    {
        if(!double.IsFinite(milliseconds)||!double.IsFinite(speed)||speed<=0||milliseconds<=0) return Frame;
        position=(position+Math.Min(milliseconds,250)*Math.Min(speed,8))%duration;
        var remaining=position;
        Frame=0;
        while(Frame<delays.Length-1 && remaining>=delays[Frame]) remaining-=delays[Frame++];
        return Frame;
    }
}
