# Desktop editing and desktop-layer binding

Goal: unlocked desktop buddies can be moved/resized directly; each buddy can independently attach to the actual desktop layer below ordinary applications.

Design: preserve proportional image height sizing. Add an unobtrusive bottom-right resize grip when hovered/unlocked, plus Ctrl+wheel sizing; ordinary wheel retains panorama scrolling. Lock blocks direct movement and resizing; click-through retains recovery through manager/taskbar. Increase desktop height range to48–480 DIP.

Desktop binding: attach only our own layered HWND as a child of the visible Progman/WorkerW hosting SHELLDLL_DefView, above its icon-view sibling so direct interaction remains possible. Do not reparent or modify Explorer's windows. Desktop binding overrides effective topmost while preserving the user's floating preference. Map physical screen coordinates to parent client coordinates and retain physical saved positions. Fail visibly back to floating if hosting fails; retry when shell returns and recreate a buddy window if its parent destroys it.

Local shell discovery: Windows11 build26200 has visible Progman132344, SHELLDLL_DefView132310, wallpaper WorkerW132048 as its sibling child. Numerous hidden WorkerW windows are unrelated and must not be selected. Desktop host DPI context34; validate compatibility before cross-process SetParent.

- [x] Core red/green: proportional resize deltas, bounds48–480, lock/input rules and settings roundtrip/backward default.
- [x] Implement pointer capture movement, grip/Ctrl-wheel resize, hover feedback, end-of-interaction autosave and editor synchronization. Preserve wide-GIF scrolling and taskbar behavior.
- [x] Implement attach/detach with native style/DPI/coordinate handling; expose per-buddy BindDesktop UI and status, preserve topmost preference, recover closed/replaced host windows.
- [x] Windows checks: unlocked/locked resizing/movement, pending editor changes, actual parent/style/coordinates, desktop-layer drawing and Show Desktop visibility, detach, recovery, prior regressions.
- [x] Review, publish local0.5.0, gracefully update current user app preserving all configured choices. Keep GitHub0.4.2 intact; user did not request another public release this turn.

Verification:66 Core checks;108 foreground and108 background packaged Windows checks, all pass/no skips. Build0 warnings/errors. Fixed review findings: pending-size relative edits, work-area effective height, explicit native topmost rollback after failed parenting. One initial background run hit the fixed350ms shell-minimization timing assumption; unchanged-package rerun passed, so the check now waits up to2s for actual desktop exposure. Final both modes passed. Closed-child recreation is controlled in-process, not a destructive Explorer restart test. Multi-monitor/high-DPI hardware transitions and physical mouse gestures were not automated; geometry/input policy and the shared edit path are covered.

References: Microsoft SetParent docs https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setparent and layered child support https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles.
