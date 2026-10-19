using PaceBuddies.Core;

var count = 0;
void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }
bool Near(double a, double b) => Math.Abs(a-b) < .0001;
Check("idle maps to minimum", Near(Speed.Map(0, .5, 4), .5));
Check("full load maps to maximum", Near(Speed.Map(100, .5, 4), 4));
Check("midpoint interpolates", Near(Speed.Map(50, .5, 4), 2.25));
Check("unavailable load uses minimum", Near(Speed.Map(null, .5, 4), .5));
Check("bad reading cannot produce NaN", double.IsFinite(Speed.Map(double.NaN, .5, 4)));
Check("out of range readings clamp", Near(Speed.Map(400, .5, 4), 4));
Check("smoothing is time independent", Near(Speed.Smooth(Speed.Smooth(1,4,.5,2),4,.5,2), Speed.Smooth(1,4,1,2)));
Check("instant response works", Near(Speed.Smooth(1,4,1,0), 4));
var clock = new AnimationClock([100, 200, 100]);
Check("variable frame duration start", clock.Advance(99,1) == 0);
Check("variable frame duration next", clock.Advance(1,1) == 1);
Check("speed changes duration", clock.Advance(100,2) == 2);
Check("pause holds frame", clock.Advance(500,0) == 2);
Check("large time jump remains bounded", clock.Advance(1e12,8) is >= 0 and < 3);
var samples = new[] { new GpuSample("pid_1_luid_0x00_0x01_phys_0_eng_0_engtype_3D",30),new GpuSample("pid_2_luid_0x00_0x01_phys_0_eng_0_engtype_3D",40),new GpuSample("pid_3_luid_0x00_0x01_phys_0_eng_1_engtype_Copy",50) };
Check("GPU sums processes on each engine", Near(GpuAggregation.BusiestEngine(samples)!.Value,70));
Check("GPU missing stays unavailable", GpuAggregation.BusiestEngine([]) is null);
var buddy = new BuddyConfig { MinimumSpeed = 9, MaximumSpeed = .1, Size = 999, Name = "   " };
buddy.Normalize();
Check("invalid speed range normalizes", buddy.MinimumSpeed <= buddy.MaximumSpeed);
Check("size normalizes", buddy.Size <= 240);
Check("blank name normalizes", !string.IsNullOrWhiteSpace(buddy.Name));
var defaults = AppSettings.CreateDefault();
Check("three distinct default buddies", defaults.Buddies.Select(b=>b.Id).Distinct().Count() == 3);
defaults.Buddies[0].Name = "changed";
Check("buddy settings are independent", defaults.Buddies[1].Name != "changed");
var folder = Path.Combine(Path.GetTempPath(), "PaceBuddies-checks-"+Guid.NewGuid().ToString("N"));
try {
    var store = new SettingsStore(folder);
    store.Save(defaults);
    Check("settings round trip", store.Load().Settings.Buddies[0].Name == "changed");
    defaults.Buddies[0].Name = "newer";
    store.Save(defaults);
    File.WriteAllText(Path.Combine(folder,"settings.json"),"broken");
    var recovery = store.Load();
    Check("corrupt settings recover backup", recovery.Settings.Buddies[0].Name == "changed" && recovery.Warning != null);
    Check("corrupt settings preserved", Directory.GetFiles(folder,"settings.corrupt-*.json").Length == 1);
    File.WriteAllText(Path.Combine(folder,"settings.json"),"null");
    Check("null JSON recovers", store.Load().Settings.Buddies.Count > 0);
} finally { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
Console.WriteLine($"All {count} behavior checks passed.");
