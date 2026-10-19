# Progress

## 2026-10-19
- Confirmed user choices: both display modes, multiple independent buddies, per-buddy always/hover values.
- Inspected public RunCat source and local .NET tooling.
- Chose native WPF, original pixel art, and a shared lightweight monitoring/animation pipeline.
- Implemented Core, native monitor, original sprites, GIF import, per-buddy runtime, transparent desktop windows, tray and WPF manager.
- Red-green verified load mapping; 24 behavior checks now pass.
- Debugged build sandbox restrictions with project-local NuGet config and authorized SDK build execution.
- Release build: 0 warnings, 0 errors. Self-contained x64 single executable published (about 154 MiB; compressed bundle about 65 MiB).
- 22 integration checks pass in both development and packaged executables, with real CPU/RAM/GPU samples and GIF transparency/timing tests.
- Inspected normal and compact WPF renders; improved dropdown surfaces, primary button contrast, and GIF scaling.
- Read-only reviewer dispatched under requesting-code-review skill; packaging final polish in progress.
- Review complete: fixed stale pause/hide draft overwrite, pending draft loss on reopen, and minimum-size overflow on high-DPI small screens.
- Reproduced stale pause regression in an isolated Windows run before fix; expanded packaged smoke suite now passes all 30 checks.
- Inspected final 911×512 DIP work-area render and transparent desktop character render. Verified native click-through flag both enabled and disabled.
- Final Release build has 0 warnings and 0 errors; 24 core + 30 packaged Windows checks pass. Delivery: dist/PaceBuddies-win-x64/PaceBuddies.exe and dist/PaceBuddies-0.1.0-win-x64.zip.

## Taskbar placement correction
- User clarified that animated buddies belong LEFT of the hidden-icons chevron, not inside notification overflow.
- Added shared no-activate taskbar overlay; removed per-buddy NotifyIcons, retained one static recovery manager. Existing ShowTray settings migrate to the fixed strip.
- Added native taskbar/tray/button geometry tracking, fullscreen/autohide behavior, conservative collision checks and readable status.
- Red-green geometry check reproduced missing fixed placement; 33 core checks pass.
- First live integration run passed 39 checks, proving actual x1505–1619 window bounds are before TrayNotifyWnd starting x1623 and visible above taskbar surface. Added no-activate check and accounted for hidden Windows 11 compatibility control bounds.
- Final 0.2.0 portable build: 0 warnings/errors; 33 Core + 40 packaged Windows checks passed. Actual strip x1505–1619, notification area begins x1623, app buttons end x1028; on-screen z-order and no-activate confirmed.
- Read-only review found no blocking defects. Documentation now covers fixed placement, non-reservation, unsupported/no-space fallback and Windows 10 validation limits.
- Invoked the old app's own Exit button through its UI Automation interface, confirming graceful exit and settings save; launched dist/PaceBuddies-0.2.0-win-x64/PaceBuddies.exe with existing user data.

## Height-first GIFs
- User requests GIF height to fill the available area. Added natural aspect-ratio widths in taskbar, desktop and habitat preview; numeric labels sit beside taskbar images.
- Taskbar overflow retains full-height images in a horizontal viewport with arrows and wheel scrolling. Wide desktop canvases are bounded by the destination monitor with wheel scrolling.
- Red-green regression: previous sizing failed 'wide GIF fills taskbar height'; 39 Core checks now pass.
- Wide GIF fixture initially assigned transparency to the body palette color; corrected with an explicit indexed palette and added opaque-pixel verification.
- Isolated Windows integration verifies full height, width ratio, label independence and scroll reachability. The remote input desktop is inaccessible; on-screen hit testing is explicitly skipped, not reported passed.
- Read-only review found oversized desktop viewports after restoring/dragging onto a smaller monitor. Recompute bounds after position/drag and on monitor/DPI/display changes, selecting the monitor from the destination origin.
- Final self-contained 0.3.0: 39 Core checks and 50 Windows integration checks passed; one on-screen hit-test explicitly skipped because input-desktop access is unavailable. Build has 0 warnings/errors.
- Reviewed app-rendered wide GIF output and overflow viewport; both preserve full height and natural ratio. A 64:1 desktop panorama stays screen-bounded and remains scrollable to its right edge.
- Used the prior app's own Exit action to save settings and close cleanly, then launched dist/PaceBuddies-0.3.0-win-x64/PaceBuddies.exe. Existing user settings/GIF files preserved. No process force-kill or security setting changes.

## Optimization release 0.4.0
- Implemented backup recovery rotation, latest-choice cancellation, empty editor/strip cleanup, downscaled frozen shared GIF frames with byte budgets and serialized background restoration.
- Added idle animation suspension, speed-notification deduplication, structural taskbar cell reuse and current-user login/background startup controls.
- Added regression fixtures and checks for cache sharing/release/budgets, stale import/removal/restore, failed-load size recovery, hidden/background launch and registry command enable/disable.
- Resolved read-only review findings. Budget failure regression exposed InvalidDataException escaping the expected-error filter; included it and verified the failure now becomes a warning and lower height recovers.
- Final portable build: 46 Core checks; 77 normal + 77 background Windows checks, all pass, no skips. Build 0 warnings/errors. Render inspection includes short 911×512 DIP work area.
- Test GIF frame payload: 96 MiB at source dimensions to 6 MiB at height128. This does not measure whole-process memory savings.
- Launched verified 0.4.0.0 with --enable-startup --background; startup Run command and unchanged existing four-buddy settings verified. No prior process remained at activation; no force-kill used.
