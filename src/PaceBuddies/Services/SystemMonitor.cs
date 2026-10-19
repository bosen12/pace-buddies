using System.Runtime.InteropServices;

namespace PaceBuddies.Services;

public sealed class SystemMonitor : IDisposable
{
    private readonly CancellationTokenSource cancellation=new();
    private Task? loop;
    public event Action<MetricSnapshot>? Sampled;
    public void Start()
    {
        loop=Task.Run(async ()=>
        {
            using var gpu=new GpuCounter();
            ulong? lastIdle=null,lastTotal=null;
            using var timer=new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                do
                {
                    double? cpu=null,memory=null;
                    if(GetSystemTimes(out var idle,out var kernel,out var user))
                    {
                        var total=kernel+user;
                        if(lastTotal is {} t && lastIdle is {} i && total>t && idle>=i)
                            cpu=Math.Clamp(100.0*(1-(double)(idle-i)/(total-t)),0,100);
                        lastTotal=total; lastIdle=idle;
                    }
                    var mem=new MemoryStatus { Length=(uint)Marshal.SizeOf<MemoryStatus>() };
                    if(GlobalMemoryStatusEx(ref mem) && mem.TotalPhysical>0)
                        memory=100.0*(mem.TotalPhysical-mem.AvailablePhysical)/mem.TotalPhysical;
                    var usage=gpu.Read();
                    Sampled?.Invoke(new(cpu,memory,usage,DateTimeOffset.Now,gpu.Status));
                } while(await timer.WaitForNextTickAsync(cancellation.Token));
            }
            catch(OperationCanceledException) { }
        });
    }
    public void Dispose() { cancellation.Cancel(); }
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length,Load;
        public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtendedVirtual;
    }
    [DllImport("kernel32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idle,out ulong kernel,out ulong user);
    [DllImport("kernel32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}

internal sealed class GpuCounter : IDisposable
{
    private IntPtr query,counter;
    public string Status { get; private set; }="GPU 引擎計數器尚未就緒";
    private DateTime retryAt;
    private bool Initialize()
    {
        Dispose();
        if(PdhOpenQuery(null,IntPtr.Zero,out query)!=0) { Status="無法開啟 GPU 計數器"; return false; }
        if(PdhAddEnglishCounter(query,@"\GPU Engine(*)\Utilization Percentage",IntPtr.Zero,out counter)!=0)
        { Status="驅動程式未提供 GPU 引擎計數器"; Dispose(); return false; }
        PdhCollectQueryData(query);
        Status="正在取得 GPU 取樣";
        return true;
    }
    public double? Read()
    {
        if(query==IntPtr.Zero)
        {
            if(DateTime.UtcNow<retryAt) return null;
            retryAt=DateTime.UtcNow.AddSeconds(30);
            Initialize(); return null;
        }
        if(PdhCollectQueryData(query)!=0) { Status="GPU 計數器暫時無法讀取"; Dispose(); return null; }
        uint bytes=0,count=0;
        var result=PdhGetFormattedCounterArray(counter,0x200|0x8000,ref bytes,out count,IntPtr.Zero);
        if(result!=0x800007D2 || bytes==0 || bytes>16*1024*1024) { Status="GPU 計數器沒有可用的引擎資料"; return null; }
        var buffer=Marshal.AllocHGlobal((int)bytes);
        try
        {
            if(PdhGetFormattedCounterArray(counter,0x200|0x8000,ref bytes,out count,buffer)!=0) return null;
            var samples=new List<GpuSample>();
            var stride=Marshal.SizeOf<CounterItem>();
            for(var index=0;index<count;index++)
            {
                var item=Marshal.PtrToStructure<CounterItem>(buffer+index*stride);
                if(item.Value.Status<=1) samples.Add(new(Marshal.PtrToStringUni(item.Name)??"",item.Value.Value));
            }
            var value=GpuAggregation.BusiestEngine(samples);
            Status=value is null ? "GPU 引擎尚無有效取樣" : "最忙碌 GPU 引擎（跨顯卡，含運算與複製引擎）";
            return value;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    public void Dispose() { if(query!=IntPtr.Zero) PdhCloseQuery(query); query=counter=IntPtr.Zero; }
    [StructLayout(LayoutKind.Explicit,Size=16)] private struct CounterValue
    { [FieldOffset(0)] public uint Status; [FieldOffset(8)] public double Value; }
    [StructLayout(LayoutKind.Sequential)] private struct CounterItem { public IntPtr Name; public CounterValue Value; }
    [DllImport("pdh.dll",CharSet=CharSet.Unicode,EntryPoint="PdhOpenQueryW")] private static extern uint PdhOpenQuery(string? source,IntPtr data,out IntPtr query);
    [DllImport("pdh.dll",CharSet=CharSet.Unicode,EntryPoint="PdhAddEnglishCounterW")] private static extern uint PdhAddEnglishCounter(IntPtr query,string path,IntPtr data,out IntPtr counter);
    [DllImport("pdh.dll")] private static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll",EntryPoint="PdhGetFormattedCounterArrayW")] private static extern uint PdhGetFormattedCounterArray(IntPtr counter,uint format,ref uint bytes,out uint count,IntPtr buffer);
    [DllImport("pdh.dll")] private static extern uint PdhCloseQuery(IntPtr query);
}
