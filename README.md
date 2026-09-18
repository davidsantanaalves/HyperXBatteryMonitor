# HyperX Battery Monitor

A lightweight Windows system tray application for monitoring the battery, charging, and connection status of supported **HyperX wireless headsets**.

The application provides a convenient way to monitor your headset directly from the Windows notification area, without requiring a full companion application to remain open.

## Features

* 🔋 Battery level monitoring directly from the Windows system tray
* 🎧 Support for multiple HyperX wireless headsets
* 📊 Multiple battery indicator display modes
* 🎨 Customizable battery indicator colors
* 🌈 Optional battery color gradient
* ⚡ Charging status indication
* 🔴 Configurable critical battery notification
* 🔌 Disconnected headset indication
* 🖥️ Windows startup option
* 🌙 Light, Dark, and System themes
* 🌐 English, Portuguese (Brazil), and Spanish interface
* ⚙️ Dedicated settings window
* 💾 Persistent application settings
* 🖼️ Custom application and tray icons
* 🪟 Native Windows / Windows Forms interface
* 🚀 Lightweight and designed to run in the background

## Supported Devices

Currently supported:

* **HyperX Cloud III Wireless**
* **HyperX Cloud III S**
* **HyperX Cloud 2 Core**
* **HyperX Cloud Alpha**
* **HyperX Cloud Stinger 2**

Additional HyperX devices may be supported in future versions.

## Requirements

* Windows 10 or later
* .NET 10
* A supported HyperX wireless headset

## Screenshots

### Device selection

<img width="762" height="552" alt="device" src="https://github.com/user-attachments/assets/7e4392b7-4003-4592-a717-03a5bae7f522" />

### Interface options

<img width="762" height="552" alt="interface" src="https://github.com/user-attachments/assets/3a6a2990-64ff-4e63-8238-3cb143362266" />

### Battery monitor options

<img width="762" height="552" alt="battery_monitor" src="https://github.com/user-attachments/assets/cd59ce42-0cf9-4c3c-93e8-ba2352683d17" />

### Notifications options

<img width="762" height="552" alt="notifications" src="https://github.com/user-attachments/assets/65161280-2eed-43cb-a740-69548a40ea12" />

### System Tray Menu

<img width="168" height="225" alt="tray_menu" src="https://github.com/user-attachments/assets/4efcf509-13bc-472d-ae39-9bda50b7cb0a" />

### System Tray Icon and device monitoring

<img width="163" height="135" alt="tray_status" src="https://github.com/user-attachments/assets/0df62982-b7f9-4def-b773-037824681bb2" />

## Installation

Download the latest version from the **Releases** section of this repository.

For version 2.0.0, download:

`HyperXBatteryMonitor-Setup-v2.0.0.exe`

Run the installer and follow the installation instructions.

After installation, HyperX Battery Monitor runs in the Windows system tray.

## Usage

After launching the application, the HyperX Battery Monitor icon will appear in the Windows notification area.

Right-click the tray icon to access the available options.

Double-click the tray icon to open the application settings.

From the Settings window you can configure:

* Device
* Battery monitor mode
* Battery colors
* Battery color gradient
* Charging indication
* Critical battery notification
* Fully charged notification
* Language
* Theme
* Start with Windows

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

Advanced display modes provide additional customization for Battery Gradient

The Battery Gradient mode can use custom battery colors and an optional gradient transition.

## Notifications

The application can notify you when the headset reaches the configured critical battery level.

A fully charged notification can also be displayed when the headset finishes charging.

Notifications can be enabled or disabled independently through the Settings window.

## Project Status

**Version:** 2.0.0

**Status:** Stable release

Version 2.0.0 expands HyperX Battery Monitor beyond the original Cloud III Wireless implementation by introducing support for multiple HyperX wireless headsets, additional display modes, configurable battery colors, charging notifications, themes, localization, and an expanded settings interface.

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
