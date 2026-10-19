# Release verification · 2026-10-19

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
