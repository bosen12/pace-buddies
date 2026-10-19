# Pace Buddies Implementation Plan

**Goal:** Ship the multi-buddy Windows companion described in the approved autonomous design.
**Architecture:** dependency-free Core library plus native WPF desktop app. One runtime per buddy shares a telemetry sampler and animation timer.
**Tech Stack:** .NET 8, WPF, WinForms NotifyIcon, GDI+, Win32/PDH; custom executable checks without external test packages.

## Global constraints
- Traditional Chinese UI; Windows 10/11 x64; no external runtime dependencies in portable package.
- No upstream code/art copied; original art and settings format.
- Never block UI on telemetry sampling; unavailable values remain nullable.

## Task 1 — behavior core
- [x] Create Core and Checks projects. Write failing executable assertions for Speed.Map(usage,min,max), Speed.Smooth(current,target,elapsed,response), AnimationClock.Advance(delta,speed), GpuAggregation.BusiestEngine(samples), BuddyConfig.Normalize(), SettingsStore.Load/Save().
- [x] Implement models, bounded clocks, validation and atomic recovery. Run `dotnet run --project tests/PaceBuddies.Checks` until all behavior checks pass.

## Task 2 — Windows runtime
- [x] Implement Services/SystemMonitor.cs: background GetSystemTimes/GlobalMemoryStatusEx/PDH wildcard sampling into immutable snapshots.
- [x] Implement Animation/AnimationAsset.cs, BuiltinSprites.cs, GifLoader.cs, BuddyRuntime.cs. GIF delays and transparent composition verified using fixture files.
- [x] Implement DesktopBuddyWindow and RuntimeCoordinator: frame sharing, safe HICON ownership, single manager recovery tray, per-buddy menus, physical-position persistence.

## Task 3 — manager
- [x] Implement App resources and MainWindow: live telemetry, buddy roster, pixel habitat, name/source/mode/label/size/speed/smoothing controls, toggles, import/drag-drop, add/remove, explicit exit.
- [x] Save all edits atomically; display readable failures; coalesce position saves; recover missing assets.

## Task 4 — verify and deliver
- [x] Add `--smoke-test <output>` for isolated, timed integration of GIF decode, real monitoring, native windows/tray resources, editing and manager render. It must not write to normal user settings.
- [x] Build Release and run checks + smoke test; inspect rendered PNG and fix layout.
- [x] Publish self-contained win-x64, repeat packaged smoke test. Add README, build/run scripts and release ZIP.
