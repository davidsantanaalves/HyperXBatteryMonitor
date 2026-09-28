# HyperX Battery Monitor – Settings refactor

This source is based on `HXBM-Source-2026.09.24-08.43.16(1).zip`.

## What changed
- `SettingsForm` is now a `partial` shell with responsibilities split into focused files.
- Navigation, Battery Monitor, About, Devices, State/Theme, and Graphics were moved without duplicating implementations.
- Nested custom controls/dialogs were moved one-per-file while remaining nested in `SettingsForm` to preserve access to the existing private state and behavior.
- `GraphicsExtensions` was moved to its own file.
- Added `PerMonitorV2` and explicit DPI autoscaling foundation.
- No functional feature was intentionally redesigned in this refactor.

## Important
Use this as a clean source tree. Do not merge it on top of the previously corrupted v21/v22 tree. Replace the source tree contents (preserving `.git` separately if needed), then build.

The previous `SettingsForm.*.cs` files from v21/v22 are intentionally absent from this archive because their implementations are now represented exactly once in the new files.
