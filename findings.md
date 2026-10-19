# Findings

- RunCat365 upstream uses WinForms and .NET 9. Current workstation has .NET SDK 8.0.426 and desktop runtime 8.0.32.
- Upstream custom runners use 2–30 imported still frames at small tray sizes; the requested arbitrary GIF and multi-buddy desktop architecture justify a focused independent implementation.
- Upstream: https://github.com/runcat-dev/RunCat365 (Apache-2.0). No upstream source or artwork copied into this project.
- Native APIs: GetSystemTimes, GlobalMemoryStatusEx, PDH English wildcard counters. Transparent WPF windows require WindowStyle=None.
- Microsoft references: https://learn.microsoft.com/en-us/windows/win32/api/pdh/nf-pdh-pdhgetformattedcounterarrayw and https://learn.microsoft.com/dotnet/api/system.windows.window.allowstransparency.
