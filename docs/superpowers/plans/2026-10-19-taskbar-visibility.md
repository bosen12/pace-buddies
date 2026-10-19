# Taskbar visibility correction

Goal: prevent normal application windows being mistaken for fullscreen while retaining taskbar autohide/fullscreen behavior and existing placements.

Evidence: the native smoke regression opens a bordered window with monitor-sized outer bounds, activates it, and calls the real NativeTaskbarLocator.Locate. 0.4.0 returns Visible=false even though the window has a title bar and the Windows taskbar is visible. Failure recorded in artifacts/visibility-native-red/smoke-result.json.

- [x] Use screen-coordinate client bounds for fullscreen classification, with a small edge tolerance, rather than the outer rectangle including borders. A window spanning several monitors must not count as a fullscreen primary-monitor window.
- [x] Distinguish taskbar autohide from fullscreen in displayed status; keep a bounded, local status-transition log to investigate other intermittent causes without collecting application titles or content.
- [x] Verify native bordered, maximized, borderless fullscreen, return-from-fullscreen and repeated switch cases. Keep existing GIF/cache/startup checks passing.
- [x] Review, publish 0.4.1, update the user's running instance through its own exit action, preserve buddy configuration and existing startup preference, and record activation evidence.
