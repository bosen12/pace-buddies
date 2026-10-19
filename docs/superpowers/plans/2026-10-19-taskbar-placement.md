# Taskbar placement implementation plan

Goal: place all enabled character animations immediately left of the system-tray chevron while keeping desktop mode and saved settings.

- [x] Add failing Core geometry checks for `TaskbarLayout.Calculate(PixelRect bar, PixelRect notify, int occupiedRight, double scale, bool[] showValues)`: anchored right edge, independent cells, constrained widths, negative coordinates, unsupported vertical layout, no available space.
- [x] Implement geometry and a native locator for Shell_TrayWnd, TrayNotifyWnd, task-button extent, monitor visibility and fullscreen detection.
- [x] Implement `TaskbarBuddyWindow`: shared no-activate transparent host, bound images/labels, tooltip, clicked buddy selection and context pause, 500 ms re-anchoring.
- [x] Replace per-buddy NotifyIcons in RuntimeCoordinator; preserve one recovery manager icon and the persisted ShowTray toggle. Update manager labels and synchronization.
- [x] Expand Windows smoke checks to verify actual host bounds, icon count, animation, independent modes and selection. Re-run existing checks and inspect its own strip render.
- [x] Build and test self-contained 0.2.0 separately; gracefully stop the old app if possible, restart updated version, retain settings/GIF assets, update documentation.
