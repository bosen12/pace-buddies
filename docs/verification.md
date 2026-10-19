# Release verification · 2026-10-19

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
