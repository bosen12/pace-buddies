# Pace Buddies

## Goal
Deliver a usable native Windows app with independent animated resource buddies in the system tray and on the desktop, arbitrary GIF import, and a distinctive Traditional Chinese manager.

## Next Step
0.5.1 source and latest public Release published to GitHub; local fixed entry and existing startup updated. Await user feedback.

## Current Phase
0.5.1 release publication (complete)

## Phases
1. Design and upstream evaluation — complete.
2. Core models, speed mapping, bounded animation clock, atomic settings — complete.
3. Native CPU/RAM/GPU telemetry, GIF decoding, pixel buddies, tray and transparent desktop windows — complete.
4. WPF manager: previews, per-buddy editing, telemetry history, drag/drop import — complete.
5. Integration checks, render inspection, portable packaging and documentation — complete.
6. Replace per-buddy notification icons with a taskbar-anchored animation strip left of the notification-area chevron; preserve desktop mode and settings — complete.
7. Fill available GIF height, derive widths from aspect ratio, move values beside animations and support horizontal overflow — complete.
8. Fix audited backup/import/retention bugs; bounded shared GIF decoding, asynchronous startup, idle animation suspension and login startup — complete.

9. Correct outer-window fullscreen false positives and record bounded visibility transitions — complete.

10. Follow actual taskbar occlusion and react to native reorder/foreground/location events — complete.
11. Unlocked direct move/proportional resize and per-buddy true desktop-layer binding — complete, local0.5.0 active.

## Decisions
- Current request: direct movement/resizing when unlocked; add true desktop-layer attachment. Preserve taskbar mode and existing startup/preferences.
- User authorized autonomous design and implementation on 2026-10-19; no further design approval is required.
- Separate new repository; do not modify unrelated C:/projects planning files or projects.
- .NET 8 WPF + Windows Forms NotifyIcon; no third party runtime packages.
- Original implementation, inspired by RunCat; upstream inspected at ../resource-buddies-upstream.
- Use a single shared 1-second telemetry sampler and 30 Hz animation clock; rebuild tray icon only when the frame changes.
- GPU is busiest engine across adapters, summing per-process values for the same physical engine. Report unavailable honestly.
- User correction: animated characters belong to the LEFT of the hidden-icons chevron, outside the notification overflow. Keep only the fixed recovery manager as a NotifyIcon. Preserve the existing ShowTray serialized setting as the taskbar-strip toggle to migrate installed configurations.
- User wants GIF height to fill available space. Preserve aspect ratio and allocate natural width; values must not consume image height. Overflow horizontally scrolls rather than shrinking images.

## Errors
- Sandboxed git network DNS failed; explicit network escalation successfully fetched the public reference repository.
- dotnet --info cannot query the service manager in the sandbox; SDK and desktop runtime 8.0.32 are present.
- NuGet tried inaccessible roaming configuration; used project-local build environment. Sandboxed MSBuild workers stalled; out-of-sandbox .NET build/test completed normally.
- Review found stale editor drafts could overwrite external pause/hide and reopening could discard pending changes; reproduced regression failure, added dirty state and coordinated external edits, verified fixes.
- Review found fixed minimum sizes exceeded high-DPI logical work areas; clamped minimum dimensions and verified full-window and footer visibility at 911×512 DIP.
- Patch validation rejected out-of-order/duplicate file hunks; corrected the patch ordering before applying changes.
- Test fixture's GIF palette initially marked its orange body transparent; replaced with a deterministic indexed palette and verified opaque pixel alpha/color.
