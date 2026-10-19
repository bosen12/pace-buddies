# Application launch flicker

Goal: keep the taskbar GIF present while opening ordinary applications/windows, following actual taskbar visibility.

Evidence: real-profile log records fullscreen hides during launch; native polling also observed Shell_TrayWnd being above the still-visible strip before the next 500ms refresh (artifacts/launch-foreground-before.json). A native smoke regression reproduces false hiding for a monitor-sized client behind a visible taskbar (artifacts/launch-occlusion-red).

- [x] Require the visible fullscreen foreground to actually be above the taskbar before hiding the buddy strip.
- [x] Add dispatcher-owned out-of-context foreground/reorder/taskbar-location notifications, coalesced and disposed with the window. Keep 500ms fallback polling for shell recovery and missed events.
- [x] Verify taskbar remains visible behind launch surfaces, actual occluding fullscreen hide/restore, and event-based z-order restoration with polling held at30s using a separate native helper process.
- [x] Review, build 0.4.2, run normal/background packaged checks, gracefully activate with existing settings/startup preference retained.

Primary API references: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook and https://learn.microsoft.com/en-us/windows/win32/winauto/event-constants.

Event regression red evidence: with hooks disabled, separate helper successfully raises shell but the30s-polling strip stays behind it (`artifacts/launch-events-red-3/smoke-result.json`). Native foreground activation may be restricted; critical coverage/z-order checks use real HWNDs directly and run regardless of foreground permission.
