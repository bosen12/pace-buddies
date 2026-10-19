# Desktop-bound pixels above animated wallpaper

User reports buddies disappear after desktop binding, even with ordinary apps minimized. Wallpaper Engine remains running throughout investigation and validation.

Evidence: native parent/style/visible/uncloaked and z-order checks all look correct above the wallpaper, but a solid-green production buddy parented to Progman hits correctly while its actual screen pixel is wallpaper. Reparenting only the probe to SHELLDLL_DefView makes the expected green pixel appear. A non-topmost detach can leave the restored top-level role below the desktop because it keeps the prior z-order.

- [x] Add failing real-pixel regression; old AnyHit test cannot prove composition.
- [x] Choose the visible SHELLDLL_DefView as the attachment host; preserve DPI/coordinate/style handling and desktop input.
- [x] Restore non-topmost floating role above desktop on detach without activation or permanent topmost.
- [x] Verify bound pixels, non-topmost detach pixels, Show Desktop, capture/close recovery and prior GIF/taskbar/startup checks with Wallpaper Engine active.
- [x] Scoped read-only review, package0.5.2, preserve current live roster/GIF/preferences and activate fixed entry.

Final:66 Core checks;115 normal/115 background packaged Windows checks pass/no skips. Wallpaper Engine active in both. Four live buddies/GIF/settings preserved; configured desktop-bound roles now use actual composited icon-view host. Patch publication follows the user's existing GitHub/release request.

Do not alter Wallpaper Engine/Explorer windows or stop the wallpaper. Only our role HWND is reparented. Pixel sampling uses known test-fixture colors; private user GIFs and desktop images stay out of the repository.
