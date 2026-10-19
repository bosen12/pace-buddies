# Taskbar placement correction

**0.3.0 update:** Full-height GIFs now use natural aspect-ratio widths and horizontal scrolling when crowded; numeric labels sit beside images. The compaction behavior described below is superseded by `2026-10-19-height-first-gif.md`.

The user means animations to the LEFT of the notification-area hidden-icons chevron, not notification icons placed inside its overflow. Implement a shared transparent tool window anchored immediately before the primary taskbar's TrayNotifyWnd left edge. Every enabled buddy appears in order with its own frame, tooltip, click to select/manage, and pause context action. An always-visible value may use a compact numeric line when space permits. Desktop buddies remain independent.

Use no-activate topmost positioning (not explorer injection), with 500 ms native geometry tracking. Follow taskbar moves, DPI, tray width and explorer recreation. Hide while the taskbar is auto-hidden/offscreen or another app fills the monitor. Do not overlap system controls or active task buttons; compress cells within a readable minimum and report unavailable space rather than covering controls. The host does not reserve shell layout space and should document this limitation.

Retain the serialized ShowTray boolean as the new taskbar-strip switch so existing on/off choices migrate. Keep the one manager NotifyIcon for recovery. Replace user-facing '系統匣動畫' with '工作列固定（箭頭左側）'. New defaults enable strip and desktop as before. No startup feature added.

Verify pure geometry for multiple displays/negative coordinates, compressed labels, small/vertical bars and insufficient space, plus live native placement left of the real TrayNotifyWnd. Verify no per-buddy NotifyIcons, shared frame updates, display toggles, selection, context-state synchronization, GIF import, pause and cleanup. Package 0.2.0 separately before restarting the user's running old version.
