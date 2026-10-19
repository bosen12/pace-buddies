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

## Taskbar visibility correction 0.4.1
- Reproduced a real NativeTaskbarLocator false positive using an ordinary bordered monitor-sized foreground window. Outer bounds cover the screen while the client has a title bar/borders; old code hid the strip. Red result saved before fix.
- Use screen-coordinate client bounds with small edge tolerance; distinguish fullscreen/autohide reasons and keep bounded local status-transition logs.
- 53 Core checks and 84 normal/84 background packaged Windows checks pass with no skipped checks. Verified normal/maximized visibility, borderless fullscreen hide, return and repeated transitions. Read-only review found no important issue.
- Gracefully exited 0.4.0 through its own UI Automation Exit action; started 0.4.1 process81808. Verified all four buddy configs unchanged, GIF paths valid, startup preference preserved/path updated and actual status shows strip active.
- This addresses a reproduced cause; other intermittent shell/space causes can now be distinguished from log/status, without recording other-app titles/content.

## Application/window opening flicker0.4.2
- User clarified all applications/windows can trigger a short disappearance; screenshot shows taskbar present. Measured0.4.1 fullscreen hide logs during launches and a native taskbar-ahead-of-strip interval.
- Require actual fullscreen foreground occlusion of taskbar, not client dimensions alone. Add coalesced native foreground/reorder/taskbar-location notifications with safe lifetime, object filtering and own-process exclusion; retain fallback polling. Avoid redundant SetWindowPos writes.
- Proved separate red failures with guard removed and hooks disabled; restored fixes. Helper verifies SetWindowPos/exit success, with a timeout. Critical HWND checks run even when native foreground activation is unavailable.
- Final53 Core and88 normal/88 background packaged Windows checks pass, no skips, build0 warnings/errors. Read-only review found no important concerns.
- Gracefully replaced0.4.1 with0.4.2 process65076. User reduced roster to one buddy while work was ongoing; preserved that latest configuration exactly and retained startup preference/path.
- Live five-window launch observation after activation recorded no hidden sample and no sampled taskbar-ahead state. Added evidence under artifacts/launch-release; this is finite testing rather than an all-environment claim.

## Desktop editing and desktop layer0.5.0
- Added unlocked direct movement, proportional corner/Ctrl-wheel resize, hover editing affordances and48–480 requested height. Lock/click-through block direct manipulation. Work-area fitting keeps controls on screen while preserving requested height.
- Added per-buddy desktop binding to actual visible Progman/WorkerW icon-host parent, child styles, compatible DPI guard, screen/client mapping, floating fallback/status/retry and controlled closed-child recreation. Desktop layer overrides effective topmost; detaching restores its preference.
- Kept pending editor drafts synchronized with direct edits; relative wheel changes compute after draft flush. Read-only review findings on high-DPI height and native topmost rollback resolved.
- Final portable build0 warnings/errors;66 Core and108 foreground/108 background packaged Windows checks all pass, no skips. Includes actual native parent/styles/coordinates, Show Desktop exposure, direct edit path, lock/input guards, controlled recreation and prior taskbar/GIF/startup regressions. Physical pointer gestures and real multi-monitor DPI transitions remain manual validation areas.
- Initial background test failed its fixed350ms shell transition assertion; unchanged binary recheck passed. Replaced timing assumption with bounded actual-exposure wait; final both modes passed. Inspected manager/settings renders.
- Gracefully exited0.4.2 using its own UI Automation Exit action and launched0.5.0 process62532. Latest roster had two buddies; all existing fields identical, both GIF paths valid, startup hidden preference preserved and Run command points to0.5.0. No error.log. Old configs retain default unbound desktop mode.
- Local delivery dist/PaceBuddies-0.5.0-win-x64/PaceBuddies.exe and matching ZIP; public GitHub0.4.2 release unchanged.

## Drag/recovery0.5.1 and requested GitHub publication
- Reproduced ScrollViewer swallowing the GIF press, plus fast pointer sampling ahead of the down event. Use handledEventsToo, snapshot event-screen start before draft flush, apply event move/release points and reserve native polling for outside-HWND capture fallback.
- Reproduced whole-app exit when closing a captured role caused LostMouseCapture→FinishGesture→Apply→Show during teardown. OnClosing now stops capture before teardown; deliberate captured-close/recreation regression passes. Scoped read-only review found no new important concern.
- Core66 pass; final packaged normal107 pass with one OS-denied foreground-activation group skipped, background113 pass/no skips. Do not claim final physical-pointer validation: user stopped Computer Use. Invalid targeted SendMessage replay was diagnosed and removed; event capture/release and native lifecycle tests retained.
- Right-corner disappearance investigation found process38896 running stale fixed entry0.1.0. Restored0.5.0 first, then updated fixed entry to0.5.1 and started process94100. Latest two-buddy fields unchanged; GIFs valid; existing login startup updated to fixed entry; no real-profile error.log; strip status active.
- User explicitly requested GitHub publication of this revision. Source1d2b326 and annotatedv0.5.1 tag pushed; public non-prerelease release417449563 published and verified latest. EXE,ZIP,SHA256SUMS asset digests all match local SHA256. Release: https://github.com/bosen12/pace-buddies/releases/tag/v0.5.1 . README downloads and project screenshot updated.
- Final live native check: taskbar role visible at1496,1034–1619,1078; both configured desktop roles visible. No global mouse input was performed during recovery/publication.
