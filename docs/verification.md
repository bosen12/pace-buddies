# Release verification · 2026-10-19

## 0.5.1 desktop drag events and process recovery

- Reproduced GIF Image→ScrollViewer→Window press routing: ScrollViewer marks the event handled and the old window handler never starts a gesture. The added routed-press check fails before handledEventsToo registration and passes after it. Locked and desktop-bound press cases, plus actual preview-release capture cleanup, are covered.
- Fast native-input probe recorded the MouseDown event at client110,76 while GetCursorPos had already advanced20 pixels to the drag endpoint. Start/move/release now use event-screen coordinates; capture the start before flushing editor drafts. Native polling applies only outside the role's HWND bounds. The user stopped Computer Use before final end-to-end physical-input verification; no claim of a completed physical-pointer pass is made.
- Reproduced active-capture Close teardown exception in isolated packaged checks: LostMouseCapture called FinishGesture→EditExternally→Apply→Show on a closing/closed HWND. OnClosing now marks the window closed and stops the gesture before native teardown; explicit captured-close/recreation regression passes.
- A SendMessage-only replay with the real cursor elsewhere is not a valid native drag simulation: CaptureMouse also generates live-position reports. Kept diagnostic artifacts locally and removed that invalid check.
- Final build:0 warnings/errors;66 Core checks pass. Packaged normal start:107 checks pass with one foreground-activation group skipped by the OS; background start:113 checks pass/no skips. Earlier development run also passed113 checks/no skips. Tests include actual parent/styles, Show Desktop exposure, closed-child recreation, taskbar visibility, GIF import/cache, settings and startup.
- User's disappearing-right-corner report coincided with the legacy unversioned entry running0.1.0, while startup pointed to0.5.0. Restored the modern taskbar strip, then replaced the fixed entry with verified0.5.1 and launched process94100. All existing fields of the latest two buddies unchanged; both GIFs present; startup points to the fixed entry with --background; no real-profile error.log. The fixed EXE hash matches the release EXE.
- Evidence: artifacts/drag-event-red/, artifacts/drag-route-red/, artifacts/drag-close-green/, artifacts/release-051-normal/, artifacts/release-051-background/; local settings backups remain ignored.

## 0.4.2 opening-application flicker

- User clarified that opening any application/window can briefly remove the GIF; supplied screenshot still shows the Windows taskbar. Real-profile0.4.1 log contained fullscreen hide/restore transitions during launch. Native polling also captured the taskbar briefly ahead of the visible overlay before the500ms fallback refreshed it.
- A monitor-sized client now hides the strip only when that visible window is actually above the taskbar in native z-order. Hidden launch surfaces cannot hide buddies. When Windows keeps the taskbar ahead of an application, the buddy strip remains present.
- Dispatcher-owned out-of-context foreground/reorder/taskbar-location hooks request a coalesced Render update. Own-process events are excluded; non-window accessibility objects are ignored except shell/Desktop client events. Closing the strip unhooks listeners and pending callbacks check disposal. The500ms fallback remains for shell restart/missed events.
- Native SetWindowPos runs only for changed physical bounds or when the taskbar is above the overlay, avoiding redundant topmost writes on unrelated events.
- Distinct RED evidence: removing taskbar occlusion guard fails `artifacts/launch-occlusion-red/smoke-result.json`; disabling hooks fails `artifacts/launch-events-red-3/smoke-result.json`. The separate helper succeeds in raising the taskbar; a test strip with30s fallback polling must return above it by100ms after helper completion. Helper exit/API failure and timeout are checked.
- Critical HWND-based occlusion tests always execute regardless of foreground activation permission. Native foreground scenarios now follow actual shell coverage: an activated borderless window is not assumed to cover a taskbar which Windows itself has raised above it.
- Release build: **0 warnings/errors**, **53 Core checks passed**. Packaged **88 normal +88 background Windows checks passed, no skips**, including launch-surface repetition and event restoration. Read-only review found no important issue.
- Activated **0.4.2.0 process65076** through the prior app's own Exit action. Preserved the user's latest **one-buddy** configuration exactly and retained startup preference, updating its existing enabled path to0.4.2.
- Repeated the live observer with five independent test windows after activation: no hidden sample and no sampled taskbar-ahead-of-buddy state (`artifacts/launch-release/live-window-launch-check.json`). This is a finite observed launch sequence, not a guarantee about every compositor/driver transition.
- Evidence: `artifacts/launch-release/`, `artifacts/launch-background-release/`, `artifacts/launch-foreground-after.json`.

## 0.4.1 taskbar visibility correction

- User reports intermittent disappearance while operating other apps; whether the Windows taskbar also hides is unknown. Found and reproduced one concrete cause rather than assuming it covers every instance.
- Red native integration regression: an ordinary bordered foreground window with monitor-sized outer bounds causes 0.4.0 NativeTaskbarLocator.Locate to return Visible=false. Evidence: `artifacts/visibility-native-red/smoke-result.json`.
- Fullscreen classification now uses client bounds converted to physical screen coordinates, with 2px edge tolerance. Resize borders/title bars, a normal maximized work-area window, a multi-monitor spanning window and fullscreen on another monitor do not qualify for primary-monitor fullscreen.
- **53 Core checks passed**. Release build **0 warnings, 0 errors**. Self-contained 0.4.1: **84 Windows checks in normal mode and 84 in background-start mode, all pass with no skips**. The native foreground activation, actual fullscreen hide, return-to-normal restoration and three repeated switch cycles ran on an accessible desktop.
- Distinct status explains fullscreen versus taskbar autohide. Status transitions record time/reason in `taskbar-status.log`, rotate after about64KiB with one previous file, and collect no application titles or content. Filesystem failures do not stop the app. Other disappearance sources (space shortage/Explorer availability) remain visible in status and log.
- Read-only review found no important issues. No change to GIF placement/scaling, taskbar collision policy or startup preferences.
- Gracefully invoked the prior user's own Exit command and saved settings; activated **0.4.1.0 process81808** in background mode. Existing four buddy configurations match the post-exit snapshot exactly. Startup enabled/disabled preference preserved; enabled Run path now points to 0.4.1 with --background. Latest real-profile status confirms fixed strip is showing left of the chevron.
- Evidence: `artifacts/visibility-release/`, `artifacts/visibility-background-release/`, `artifacts/visibility-release/activation.json`.

## 0.4.0 optimization and login startup

- Release build: **0 warnings, 0 errors**. **46 Core checks passed**.
- Self-contained executable passed **77 Windows integration checks in normal mode** and **77 in background-start mode**, with no skipped checks. Evidence: `artifacts/optimization-final/` and `artifacts/optimization-final-background/`.
- The accessible Windows 11 input desktop permitted actual on-screen hit testing this time. Native strip bounds x1475–1619 remain left of the notification area at x1623 and beyond app buttons ending x1050; no-activate style verified.
- Backup regression failed before the fix: saving recovered settings rotated corrupt primary text into the healthy backup. New tests verify a valid backup after saving and recovery after a second corruption.
- GIF test input: 1024×512, 48 frames. Original-resolution 32-bit frame payload is **96 MiB**; 128-pixel-high decoded payload is **6 MiB**, a **93.75% reduction**. This is frame data, not whole-process working set. Identical content/height shares frozen frame storage; final lease releases its cache entry. Per-asset 64 MiB and aggregate active-cache 128 MiB payload limits are tested.
- Serialized background decode retains source dimensions/ratio and original files, removes the PNG conversion pass and full original-size bitmap list. Saved GIFs load after the manager is prepared. Generation/cancellation checks prevent stale restore/import/resize results from replacing later choices; cancelled imports clean their owned copies.
- Cache bookkeeping locks are short; final lease release does not wait for an unrelated decode. A pressure regression initially exposed an uncaught InvalidDataException; this now produces a user-facing warning. Reducing height retries a previously failed placeholder; unchanged failed targets do not loop every second.
- All-paused and fully-hidden states stop the animation timer; visibility resumes it. Ordinary settings changes retain taskbar visuals, and final buddy removal clears editor/taskbar references.
- Startup commands quote executable paths and support background/visible launch. Tests use a dedicated temporary HKCU test key, never the actual Run value. UI retains prior state on write errors and remains usable on read errors. Second background launches do not wake the manager.
- Read-only review findings resolved: cross-buddy decode lock stalls, single-instance startup flags, registry read errors, and failed-GIF size recovery. Inspected normal and small-work-area app renders; startup settings collapse on short windows to preserve roster scrolling.
- Activated **0.4.0.0**, process 30956. Existing four buddies and their display/settings values were preserved. Current-user Run value verified as the quoted 0.4.0 executable plus `--background`; manager initially hidden. Actual Windows logout/reboot was not required or performed.
- Physical mixed-DPI transitions, other GPU drivers, Windows 10 and third-party taskbars retain their previously documented broader validation limits.

## 0.3.0 height-first GIFs

- 39 Core checks cover full taskbar canvas height, natural width for 4:1 and portrait images, equal height with labels, horizontal overflow and high-DPI geometry.
- Final self-contained release: **50 Windows integration checks passed**, **1 on-screen hit test skipped**, exit 0. Build: **0 warnings, 0 errors**.
- Windows integration adds a deterministic transparent 4:1 fixture, verifies opaque pixels, actual WPF image sizes, side-label height independence, scroll arrows, end-of-content reachability, and a 64:1 desktop panorama bounded by monitor width.
- Read-only review identified monitor restoration/drag resizing for wide desktop viewports. Width is now recomputed after restore and drag, and on monitor/DPI/display changes, with monitor selection based on the destination origin rather than the oversized rectangle.
- Remote input desktop is currently inaccessible. The on-screen hit-test check is explicitly recorded as skipped; native coordinates and app-rendered PNGs can still be verified. Rendered outputs are not screenshots of the whole Windows taskbar.
- Final evidence: `artifacts/height-release-smoke/`.
- Gracefully restarted the user app into the verified 0.3.0 executable; retained existing settings and GIFs. Mixed-DPI physical monitor transitions remain a wider live-validation boundary.

## 0.2.0 taskbar placement correction

The animated characters are now in one transparent no-activate window immediately LEFT of the hidden-icons chevron. Only the static manager uses a notification icon. Existing settings and GIF paths are preserved.

- 33 Core checks cover prior behavior plus taskbar geometry, negative coordinates, compact cells, collision prevention and unsupported layout.
- Final self-contained 0.2.0 executable passed **40 Windows checks**, exit 0; Release build: **0 warnings, 0 errors**.
- Live Windows 11 build 26200 checks verify native placement before TrayNotifyWnd, window z-order using WindowFromPoint, no-activate style, selecting the clicked buddy, independent modes, cleanup, GIF decoding and persistence.
- Reviewed the strip's own render; no third-party shell source is copied or injected.
- The strip does not reserve taskbar space; crowded/unsupported layouts show a status and desktop fallback. Classic Windows 10 and modified taskbars are not validated for the new strip (the task list's container can conservatively indicate no available space). Mixed-DPI moves, autohide/fullscreen transitions and Explorer restart need broader live validation.

Final release evidence: `artifacts/taskbar-release-smoke/`.

## 0.1.0 initial release

Release 0.1.0, Windows x64. .NET SDK 8.0.426.

- Core executable checks: **24 passed**.
- Release compile: **0 warnings, 0 errors**.
- Packaged self-contained executable: **30 Windows integration checks passed**, exit 0.
- Native CPU, physical memory and GPU counters all returned valid values on the test workstation.
- GIF fixture verified frame count, variable 100/200/300 ms timing, transparency, file unlock, app-owned import, and persistence.
- Three desktop windows and three character tray icons verified; display toggles, add/remove, actual native click-through on/off verified. Separate recovery manager icon retained.
- Regression reproduced before fix: unchanged manager draft overwrote a tray pause. Dirty drafts and coordinated external changes fix it; tests cover pause, hide and reopen with pending edits.
- WPF renders reviewed at 1160×850, 950×670, and a 911×512 DIP logical work area (883×484 window). Footer and all settings remain accessible via scrolling.

Evidence files generated under `artifacts/final-smoke/`: `smoke-result.json`, `manager.png`, `manager-compact.png`, `manager-small-hidpi.png`, and `desktop-buddy.png`.

The suite verifies local Windows integration, not every GPU driver or physical multi-monitor/DPI arrangement. Unavailable GPU counters have an explicit fallback in the app. Binaries are locally built and unsigned.
