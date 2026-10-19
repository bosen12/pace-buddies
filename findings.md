# Findings

- RunCat365 upstream uses WinForms and .NET 9. Current workstation has .NET SDK 8.0.426 and desktop runtime 8.0.32.
- Upstream custom runners use 2–30 imported still frames at small tray sizes; the requested arbitrary GIF and multi-buddy desktop architecture justify a focused independent implementation.
- Upstream: https://github.com/runcat-dev/RunCat365 (Apache-2.0). No upstream source or artwork copied into this project.
- Native APIs: GetSystemTimes, GlobalMemoryStatusEx, PDH English wildcard counters. Transparent WPF windows require WindowStyle=None.
- Microsoft references: https://learn.microsoft.com/en-us/windows/win32/api/pdh/nf-pdh-pdhgetformattedcounterarrayw and https://learn.microsoft.com/dotnet/api/system.windows.window.allowstransparency.
- Taskbar correction 2026-10-19: live workstation is Windows build 26200; Shell_TrayWnd bounds (0,1032)-(1920,1080), TrayNotifyWnd (1623,1032)-(1920,1080), current task buttons end at x1050. This leaves room for a separate transparent strip immediately before x1623.
- A standard notification icon does not reserve a slot left of the chevron. Use a separate no-activate transparent window anchored to the native taskbar notification rectangle, with no shell injection or registry changes. Poll geometry to follow tray size/DPI changes and shell recreation. Hide with auto-hidden taskbar and fullscreen applications; avoid overlap with task buttons, and report insufficient space explicitly.
- Primary implementation reference inspected for geometry only: https://github.com/zhongyang219/TrafficMonitor/blob/master/TrafficMonitor/Win11TaskbarDlg.cpp uses Shell_TrayWnd/TrayNotifyWnd rectangles. No source copied.

## Optimization implementation
- File.Replace must not rotate a known-corrupt primary over the backup used for recovery; preserve it through the first successful recovery save.
- WPF frozen BitmapSource frames can be shared across independent clocks. Keep cache metadata/refcount locks separate from serialized decoding so UI disposal never blocks on another buddy's decoder.
- InvalidDataException needs an explicit expected-error filter; it is not covered by the IOException pattern here.
- Failed decoding must remember the attempted target separately from the current asset. Retry on a changed target while avoiding 1Hz retry loops for unchanged failures.
- Login startup is a quoted per-user HKCU Run entry with --background by default. Keep registration commands functional for second-instance launches, suppress wakeup for background duplicates, and handle both registry reads and writes failing.
- 0.4.0 verification: 46 Core checks and 77 checks each for foreground/background packaged starts pass. Active frame payload budget checks are distinct from total process memory guarantees.

## Taskbar disappearance
- GetWindowRect contains nonclient borders/title bars; monitor-sized outer bounds do not prove fullscreen. Reproduced false hide with a real bordered foreground window against the original native locator.
- ClientToScreen + GetClientRect matches true content coverage; compare all four monitor edges within2px rather than any window containing the monitor. This excludes ordinary maximized and cross-monitor spanning windows.
- Verified fullscreen restoration through actual native foreground changes and repeated cycles. A bounded status log distinguishes fullscreen, shell autohide, missing locator and space shortage for future intermittent reports.

## Launch transitions and shell z-order
- Client bounds alone still do not establish that a fullscreen-sized surface covers the taskbar. Windows can keep the taskbar above it, including during window activation; only hide when the visible candidate is actually above the taskbar.
- A500ms anchor timer permits a brief shell-over-overlay interval after Explorer raises the taskbar. Native out-of-context events can refresh promptly; filter accessibility subobjects, coalesce on the dispatcher, skip own events, and avoid repeated SetWindowPos calls unless geometry/layering actually needs repair.
- Native tests must distinguish a borderless full-monitor window from actual taskbar occlusion. Windows may raise its taskbar during activation; observed foreground identity and client geometry can remain unchanged while z-order differs.
- Disabling event hooks with30s polling reliably reproduces delayed layer restoration; reinstating hooks passes with a separate native helper. 0.4.2 live repeated-window check recorded no hidden samples.
