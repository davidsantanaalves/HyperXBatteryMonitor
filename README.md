# HyperX Battery Monitor

A lightweight Windows system tray application for monitoring the battery, charging, connection, and microphone mute status of supported **HyperX wireless headsets**.

The application provides a convenient way to monitor your headset directly from the Windows notification area, without requiring a full companion application to remain open.

## Features

* 🔋 Battery level monitoring directly from the Windows system tray
* 🎧 Support for multiple HyperX wireless headsets
* 📊 Multiple battery indicator display modes
* 🎨 Customizable battery indicator colors
* 🌈 Optional battery color gradient
* ⚡ Charging status monitoring and indication
* 🎙️ Microphone mute status monitoring on supported headsets
* 🔇 Optional microphone mute indication in the system tray
* 🔄 Alternating battery/microphone-mute tray icon while the microphone is muted
* 🧾 Microphone status displayed in the sidebar, tray menu, and tray tooltip
* 🔴 Configurable critical battery notification
* ✅ Fully charged notification
* 🔌 Disconnected headset indication
* 🖥️ Windows startup option
* 🌙 Light, Dark, and System themes
* 🌐 English, Portuguese (Brazil), and Spanish interface
* ⚙️ Dedicated settings window
* 💾 Persistent application settings
* 🖼️ Custom application and tray icons
* 🪟 Native Windows / Windows Forms interface
* 🚀 Lightweight and designed to run in the background

## What's New in Version 2.1.0

Version 2.1.0 expands headset status monitoring beyond battery information.

### Microphone mute monitoring

Supported headsets can now report their microphone mute state directly to HyperX Battery Monitor.

The microphone state is displayed in:

* The device monitor in the Settings sidebar
* The system tray context menu
* The system tray tooltip

The application displays whether the microphone is:

* **Open**
* **Muted**

When microphone mute indication is enabled, the system tray icon alternates between the current battery-status icon and the microphone-mute icon while the microphone is muted.

When the microphone is unmuted, the tray returns to displaying only the current battery-status icon.

Microphone mute monitoring is currently supported on:

* **HyperX Cloud III Wireless**
* **HyperX Cloud III S**
* **HyperX Cloud 2 Core**
* **HyperX Cloud Alpha**

Microphone mute monitoring is not currently available for the **HyperX Cloud Stinger 2**.

### Improved charging monitoring

Charging-state support has been expanded across the supported headset implementations.

When a headset is charging, HyperX Battery Monitor can indicate the charging state through the system tray and device monitoring interface.

The sidebar and tray menu also display a charging indicator next to the battery percentage when applicable.

### Improved device monitoring

The device monitor now provides a more complete overview of the current headset state:

* Connection status
* Battery percentage
* Charging state
* Microphone state, when supported

The same information is also available from the system tray context menu and tooltip.

## Supported Devices

Currently supported:

* **HyperX Cloud III Wireless**
* **HyperX Cloud III S**
* **HyperX Cloud 2 Core**
* **HyperX Cloud Alpha**
* **HyperX Cloud Stinger 2**

Additional HyperX devices may be supported in future versions.

### Feature availability

Some headset capabilities vary by model.

| Device | Battery | Charging | Microphone Mute |
|---|:---:|:---:|:---:|
| HyperX Cloud III Wireless | ✅ | ✅ | ✅ |
| HyperX Cloud III S | ✅ | ✅ | ✅ |
| HyperX Cloud 2 Core | ✅ | ✅ | ✅ |
| HyperX Cloud Alpha | ✅ | ✅ | ✅ |
| HyperX Cloud Stinger 2 | ✅ | ✅ | — |

## Requirements

* Windows 10 or later
* .NET 10
* A supported HyperX wireless headset

## Screenshots

### Device selection

<img width="762" height="552" alt="image" src="https://github.com/user-attachments/assets/89cc6d77-a066-4d72-8cf3-17338ed3ee45" />

### Interface options

<img width="762" height="552" alt="image" src="https://github.com/user-attachments/assets/607a7533-edda-4953-b82e-5522a6199699" />

### Battery monitor options

<img width="762" height="552" alt="image" src="https://github.com/user-attachments/assets/41dc4eb7-67ef-4985-be3d-9d276f51579b" />

### Notifications options

<img width="762" height="552" alt="image" src="https://github.com/user-attachments/assets/61810f84-41fb-4430-9cbe-374b15ab1dbd" />

### System Tray Menu

<img width="168" height="245" alt="image" src="https://github.com/user-attachments/assets/02f9213e-a72a-4ced-ba90-96f44c6021a1" />

### System Tray Icon and device monitoring

<img width="129" height="143" alt="image" src="https://github.com/user-attachments/assets/3c5cf6ca-8e5d-433d-90df-138c92f74a41" />

## Installation

Download the latest version from the **Releases** section of this repository.

For version 2.1.1, download:

`HyperXBatteryMonitor-Setup-v2.1.1.exe`

Run the installer and follow the installation instructions.

After installation, HyperX Battery Monitor runs in the Windows system tray.

## Usage

After launching the application, the HyperX Battery Monitor icon will appear in the Windows notification area.

Right-click the tray icon to access the device status and available options.

Double-click the tray icon to open the application settings.

From the Settings window you can configure:

* Device
* Battery monitor mode
* Battery colors
* Battery color gradient
* Charging indication
* Critical battery notification
* Fully charged notification
* Critical battery tray-icon blinking
* Microphone mute tray indication
* Language
* Theme
* Start with Windows

Microphone-related options are automatically unavailable for headset models that do not support microphone mute monitoring.

## Battery Monitor Modes

HyperX Battery Monitor provides multiple ways to display battery information in the Windows system tray.

### Static

Displays the standard headset icon, with a separate charging indication when applicable.

### Battery Indicator

Displays the battery status using predefined battery-level indicators.

The default battery ranges are:

* ≥ 50% — Green
* 30–49% — Yellow
* 15–29% — Orange
* < 15% — Red
* Charging — Charging indicator

### Advanced

Advanced display modes provide additional customization for the battery indicator.

The Battery Gradient mode can use custom battery colors and an optional gradient transition.

## Microphone Monitoring

For supported headsets, HyperX Battery Monitor monitors microphone mute-state changes and updates the interface automatically.

The current microphone state can be viewed in:

* Settings sidebar
* System tray menu
* System tray tooltip

When **Show muted microphone in the system tray** is enabled, the tray icon alternates between:

1. The current battery-status icon
2. The microphone-mute icon

This continues while the microphone remains muted.

When the microphone becomes active again, the tray displays only the normal battery-status icon.

This feature is enabled by default for headset models that support microphone mute monitoring.

## Charging Status

HyperX Battery Monitor monitors charging state on supported devices.

When charging is detected, the application can:

* Display a charging-specific tray icon or charging indicator
* Show the charging state in the device monitoring interface
* Display a charging indicator next to the battery percentage
* Notify the user when the headset reaches full charge

Charging behavior depends on the selected battery display mode.

## Notifications

The application can notify you when the headset reaches the configured critical battery level.

A fully charged notification can also be displayed when the headset finishes charging.

Available notification-related options include:

* Low battery notification
* Configurable critical battery level
* Critical battery tray-icon blinking
* Fully charged notification
* Microphone mute tray indication on supported headsets

These options can be configured independently through the Settings window.

## Project Status

**Version:** 2.1.1

**Status:** Stable release

Version 2.1.0 expands HyperX Battery Monitor with microphone mute monitoring, broader charging-state support, improved device-status information, and enhanced system tray integration.

The application can now display connection, battery, charging, and microphone information directly from the sidebar, system tray menu, and tray tooltip.

For supported headset models, microphone mute changes can also be represented visually in the system tray by alternating between the current battery icon and a microphone-mute icon.

The project remains under active development, and additional devices and features may be added in future versions.

## Technology

The application is built using:

* C#
* .NET 10
* Windows Forms
* Windows APIs
* Windows Registry
* Microsoft.Toolkit.Uwp.Notifications

The application communicates with supported headsets through Windows HID interfaces.

The project does not distribute third-party proprietary HyperX software or require HyperX NGENUITY to remain open.

## Independent Project

HyperX Battery Monitor is an independent, community-developed utility.

It is **not affiliated with, endorsed by, sponsored by, or officially associated with HyperX or HP Inc.**

HyperX and the respective headset names are trademarks of their respective owners.

## Third-Party Acknowledgement

Support for multiple HyperX devices uses code and references from the **HyperX-Cloud-2-Battery-Monitor** project by **auto94**, released under the MIT License.

The original project is acknowledged in the application's About page.

## License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for the complete license text.

## Author

**David Santana (Dave Santana)**

Independent developer.

## Support the Project

If you find HyperX Battery Monitor useful and would like to support its development:

☕ [Buy Me a Coffee](https://buymeacoffee.com/davesantana)

You can also support the project via PIX:

`davesantana@outlook.com.br`

## Contributing

Suggestions, bug reports, and contributions are welcome.

If you find a problem or have an idea for a new feature, please open an **Issue** in this repository.

## Disclaimer

This software is provided "as is", without warranty of any kind.

The developer is not responsible for any damage, data loss, hardware issues, or other consequences resulting from the use of this software.

Use the application at your own discretion.

## Special Thanks

A special thank you to auto94, the author of HyperX-Cloud-2-Battery-Monitor.

This project was an important reference during the development of HyperX Battery Monitor, particularly in expanding device support beyond the original HyperX Cloud III Wireless implementation.

We are grateful to the author for making the project available under the MIT License and for contributing to the HyperX community with an open-source solution that helped make further development possible.

Please visit the original project and consider supporting its development:

HyperX-Cloud-2-Battery-Monitor  
https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor

Thank you, auto94, for sharing your work with the community.
