# Findings

- RunCat365 upstream uses WinForms and .NET 9. Current workstation has .NET SDK 8.0.426 and desktop runtime 8.0.32.
- Upstream custom runners use 2–30 imported still frames at small tray sizes; the requested arbitrary GIF and multi-buddy desktop architecture justify a focused independent implementation.
- Upstream: https://github.com/runcat-dev/RunCat365 (Apache-2.0). No upstream source or artwork copied into this project.
- Native APIs: GetSystemTimes, GlobalMemoryStatusEx, PDH English wildcard counters. Transparent WPF windows require WindowStyle=None.
- Microsoft references: https://learn.microsoft.com/en-us/windows/win32/api/pdh/nf-pdh-pdhgetformattedcounterarrayw and https://learn.microsoft.com/dotnet/api/system.windows.window.allowstransparency.
- Taskbar correction 2026-10-19: live workstation is Windows build 26200; Shell_TrayWnd bounds (0,1032)-(1920,1080), TrayNotifyWnd (1623,1032)-(1920,1080), current task buttons end at x1050. This leaves room for a separate transparent strip immediately before x1623.
- A standard notification icon does not reserve a slot left of the chevron. Use a separate no-activate transparent window anchored to the native taskbar notification rectangle, with no shell injection or registry changes. Poll geometry to follow tray size/DPI changes and shell recreation. Hide with auto-hidden taskbar and fullscreen applications; avoid overlap with task buttons, and report insufficient space explicitly.
- Primary implementation reference inspected for geometry only: https://github.com/zhongyang219/TrafficMonitor/blob/master/TrafficMonitor/Win11TaskbarDlg.cpp uses Shell_TrayWnd/TrayNotifyWnd rectangles. No source copied.
