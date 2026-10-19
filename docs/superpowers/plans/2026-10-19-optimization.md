# Optimization release implementation plan

**Goal:** Complete the accepted 0.3 audit improvements without changing height-first rendering, original GIFs, per-buddy settings or taskbar placement.
**Architecture:** cancellable per-buddy appearance requests, immutable shared frame data with disposable leases, serialized background GIF decoding, bounded active cache, native per-user startup registration.
**Tech stack:** existing .NET 8 WPF / Win32; no third-party packages.

## Constraints
- Preserve original imported GIFs and existing user settings.
- GIF cache: decoded frames sized to display height and DPI, rounded to 32-pixel buckets; 64 MiB per asset, 128 MiB aggregate active cache. One GIF decoder at a time.
- Shared frame data removed when its final lease is released; never force GC in product code.
- New appearance choices cancel older startup/import/resize results.
- Login startup registers only the current user's Run value. Default login launch uses --background. The user authorized this release and will have startup enabled, with a visible off switch.
- All smoke tests use isolated files and a dedicated temporary registry value, never the actual login Run entry.

## Tasks
- [x] Core regressions: save after recovery retains a valid backup; second corruption still recovers. LatestRequest invalidates earlier tickets; StartupCommand quotes executable and adds background flag.
- [x] Fix recovery rotation and add cancellable latest-request sequencing.
- [x] Replace full-resolution bitmap lists/PNG round-trips with streamed downscaled BGRA frames. Share immutable data through a content-hash/height cache with a byte budget and disposable leases. Preserve transparency, source ratio, delays and original files.
- [x] Restore saved GIFs after showing the UI. Use bounded background loads, generation checks and cancellation for restore/import/resize. Clear empty editor/taskbar references and release frame leases.
- [x] Rebuild taskbar visuals only for structural changes. Animate only visible unpaused buddies or the selected preview; stop timer when idle, restart on visibility/settings changes.
- [x] Add login/background startup checkboxes and per-user registration service. Support CLI --background and --enable-startup. Verify quoting and safe path registration in isolated registry tests.
- [x] Expand Windows integration: asset sharing/release, reduced dimensions and memory, stale import/builtin choice, background restore, empty-state ownership, idle suspension and startup registration. Review, publish 0.4.0, restart via the app's own exit action and enable login startup.
