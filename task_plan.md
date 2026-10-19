# Pace Buddies

## Goal
Deliver a usable native Windows app with independent animated resource buddies in the system tray and on the desktop, arbitrary GIF import, and a distinctive Traditional Chinese manager.

## Next Step
Write and run core behavior checks before implementing the animation and settings logic.

## Current Phase
Core behavior (in_progress)

## Phases
1. Design and upstream evaluation — complete.
2. Core models, speed mapping, bounded animation clock, atomic settings — in_progress.
3. Native CPU/RAM/GPU telemetry, GIF decoding, pixel buddies, tray and transparent desktop windows — pending.
4. WPF manager: previews, per-buddy editing, telemetry history, drag/drop import — pending.
5. Integration checks, render inspection, portable packaging and documentation — pending.

## Decisions
- User authorized autonomous design and implementation on 2026-10-19; no further design approval is required.
- Separate new repository; do not modify unrelated C:/projects planning files or projects.
- .NET 8 WPF + Windows Forms NotifyIcon; no third party runtime packages.
- Original implementation, inspired by RunCat; upstream inspected at ../resource-buddies-upstream.
- Use a single shared 1-second telemetry sampler and 30 Hz animation clock; rebuild tray icon only when the frame changes.
- GPU is busiest engine across adapters, summing per-process values for the same physical engine. Report unavailable honestly.

## Errors
- Sandboxed git network DNS failed; explicit network escalation successfully fetched the public reference repository.
- dotnet --info cannot query the service manager in the sandbox; SDK and desktop runtime 8.0.32 are present.
