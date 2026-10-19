# Height-first GIF display

Use the complete available taskbar host height for the GIF canvas, maintaining its source aspect ratio and expanding the cell horizontally. Keep the existing small physical safety margin around the taskbar. Always-on numeric labels sit beside the GIF, so label settings never change image height.

Taskbar cells calculate their own widths from asset pixel dimensions. If total natural width exceeds the safe gap before the notification area, retain natural dimensions in a horizontal viewport with previous/next buttons and mouse-wheel scrolling. No characters disappear merely because all do not fit at once. Native window bounds remain before the chevron and after task buttons. If even a minimal scroll viewport cannot fit, retain the explicit unavailable-space status.

Desktop Size becomes the GIF canvas height. Natural width follows aspect ratio; extremely wide canvases use a screen-bounded horizontal viewport with wheel scrolling. Preview uses a fixed height and proportional width in a horizontally scrollable habitat. Roster thumbnails remain compact navigation thumbnails.

Do not trim transparent source padding, stretch, crop source frames or resize original GIF files. Preserve timing, transparency, current settings and owned GIF imports. No startup behavior changes. Publish 0.3.0 separately; only restart the installed version if the desktop allows a graceful shutdown.

Verification: failing geometry checks for 4:1, portrait and square canvases, identical image height with/without labels, overflow viewport, DPI scaling and no overlap; Windows integration verifies actual Image dimensions and scroll reachability using square and wide transparent GIF fixtures. Rendering evidence must be labelled as app-rendered output, not a whole-screen screenshot.
