# HXBM Responsive Layout v1

Based on HXBM-Source-2026.09.25-Settings-Organized-v3.

This package contains only the files changed for the responsive settings-page layout refactor.

Changes:
- Replaced page-internal absolute positioning with TableLayoutPanel/FlowLayoutPanel layouts.
- Device, Interface, Battery Monitor, Notifications and About pages now distribute content according to available width.
- Preserved existing controls, state fields, localization, theme behavior and event handlers.
- Kept PerMonitorV2 and AutoScaleMode.Dpi unchanged.

Validation performed here: brace/structural checks and ZIP integrity. The .NET SDK is not installed in the execution environment, so no build result is claimed.
