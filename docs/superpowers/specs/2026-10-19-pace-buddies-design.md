# Pace Buddies · 跑跑夥伴

**0.2.0 correction:** Animated taskbar buddies are fixed to the LEFT of the hidden-icons chevron, outside the notification overflow. Only the static recovery manager remains a NotifyIcon. See `2026-10-19-taskbar-placement.md` for the corrected requirements; it supersedes per-buddy tray-icon behavior below.

Native Windows 10/11 personal resource companions. Each buddy owns a name, built-in sprite or imported GIF, CPU/RAM/GPU source, speed minimum/maximum, response smoothing, pause state, desktop/tray visibility, desktop size, topmost/locked/click-through state, numeric label mode, and physical screen position.

## Product design
An ice-blue studio with ink text, white paper panels, apricot emphasis, and a restrained pixel-art playground. Palette: canvas #EFF4F8, paper #FFFFFF, ink #20314A, blue #3976B9, apricot #F5B677, muted #6F8199. Display: Segoe UI Semibold; body: Microsoft JhengHei UI; numerical readouts: Cascadia Mono / Consolas. A small animated habitat connects each resource reading to a character; configuration remains ordinary clear controls.

Main window: narrow branded navigation rail; live CPU/RAM/GPU strip; selectable buddy roster; selected buddy habitat and editable properties. Main actions: add buddy, import GIF, save settings, reset desktop position. Close hides manager while the tray manager remains available. Exit is explicit in manager and tray menu. Keyboard focus is visible, scrollable editor accommodates small screens, and all controls have labels.

## Runtime
Core library handles validated settings, finite speed interpolation, exponential time-based smoothing, variable-frame-duration animation with capped catch-up, GPU engine aggregation, and atomic JSON persistence. CPU is elapsed busy system time, RAM is physical memory utilization, GPU is the busiest physical engine across adapters (not VRAM or temperature). Unsupported counters display unavailable and animate at minimum speed. Sampling never blocks the UI. A manager tray icon always provides recovery from hidden or click-through buddies.

GIFs are decoded through GDI+ into composited frozen WPF frames with original per-frame delays. Imports are validated before copying to app-owned storage, with 20 MB input, 2,000,000 pixels per source frame, 300 frames, and 64 million decoded pixels maximum. Missing/corrupt assets fall back to a built-in buddy and display a warning. No file is kept locked. Imported GIFs remain usable after the original is moved.

Desktop windows are transparent, draggable unless locked, optionally topmost or click-through. Positions are saved physically and clamped to an available monitor working area after restore/drag/resize; system tray icons use separate small pixel art rendering. Per-buddy label supports always/hover/hidden; tray values are available in tooltips because Windows tray slots are small.

## Boundaries and verification
No updater, network service, accounts, temperature readings, or auto-start registration in this version. No changes to Windows settings on launch. Tests cover mapping extremes, smoothing, time jumps, engine aggregation, independent state, validation, persistence and corrupt recovery. Windows integration checks exercise GIF timing/transparency, live telemetry, multiple tray/desktop instances, settings edits, and WPF rendering. Deliver source, build scripts, and a portable Windows x64 executable.
