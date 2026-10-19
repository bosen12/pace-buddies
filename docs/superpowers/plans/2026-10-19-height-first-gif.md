# Height-first GIF implementation

- [x] Extend taskbar geometry input with asset aspect ratios and output image sizes, content extent and scroll-button width. Add failing checks for 4:1, portrait, values, crowded viewport and DPI.
- [x] Implement height-first natural-width cells and a safe scroll viewport. Bind all frames to the same runtime and keep no-activate positioning.
- [x] Update desktop and habitat preview to use canvas height and natural width; make screen-bounded wide content horizontally accessible.
- [x] Generate a 4:1 transparent GIF fixture. Add integration checks for height, proportional width, unchanged height with labels, scrolling and imported asset sizing. Keep all existing functional checks.
- [x] Build, review, test and inspect app-rendered evidence; package 0.3.0 and document behavior. Try a graceful app update without killing a process or discarding edits if remote desktop access is unavailable.
