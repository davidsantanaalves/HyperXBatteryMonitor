using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using HyperXBatteryTray.Devices;

namespace HyperXBatteryTray.Settings;

public sealed partial class SettingsForm : Form
{
    private void DeviceSelector_SelectionChanged(object? sender, EventArgs e)
    {
        _pendingSelectedDevice = _deviceSelector.SelectedDeviceName;
        RefreshDeviceStatus();
    }

    private void RefreshDeviceStatus()
    {
        bool selected = _deviceSelector.SelectedIndex > 0;
        bool connected = selected && _device?.IsConnected == true && _device.Battery >= 0 && _device.Battery <= 100;

        // The main Device page no longer displays the legacy status card.
        // Keep these updates conditional so device selection/status refreshes
        // continue to update the sidebar and information cards without
        // dereferencing controls that are not created on the new layout.
        if (_deviceStatusLabel != null)
        {
            _deviceStatusLabel.Text = connected ? L("Connected") : selected ? L("Disconnected") : L("UnknownHeadphones");
            _deviceStatusLabel.Visible = true;
        }
        if (_deviceStatusDescriptionLabel != null)
        {
            _deviceStatusDescriptionLabel.Text = connected ? L("ConnectedDescription") : selected ? L("DisconnectedDescription") : L("UnknownDeviceDescription");
            _deviceStatusDescriptionLabel.Visible = true;
        }
        if (_batteryValueLabel != null)
            _batteryValueLabel.Text = connected ? $"{Math.Clamp(_device!.Battery, 0, 100)}%" : L("BatteryNA");
        if (_chargingLabel != null)
        {
            _chargingLabel.Text = _isCharging ? L("ChargingStatus") : string.Empty;
            _chargingLabel.Visible = connected && _isCharging;
        }

        Color primaryText = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        Color secondaryText = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
        if (_deviceStatusLabel != null) _deviceStatusLabel.ForeColor = primaryText;
        if (_deviceStatusDescriptionLabel != null) _deviceStatusDescriptionLabel.ForeColor = secondaryText;
        if (_batteryValueLabel != null) _batteryValueLabel.ForeColor = primaryText;
        if (_chargingLabel != null) _chargingLabel.ForeColor = secondaryText;
        if (_batteryIcon != null)
        {
            _batteryIcon.Connected = connected;
            _batteryIcon.Battery = connected && _device != null ? Math.Clamp(_device.Battery, 0, 100) : 0;
            _batteryIcon.DarkMode = EffectiveTheme == AppTheme.Dark;
            _batteryIcon.Charging = connected && _isCharging;
            _batteryIcon.Invalidate();
        }
        if (_deviceStatusDot != null)
        {
            _deviceStatusDot.Connected = connected;
            _deviceStatusDot.Invalidate();
        }

        UpdateSidebarDeviceStatus(selected, connected);
        UpdateDeviceInformation();
    }

    private void UpdateSidebarDeviceStatus(bool selected, bool connected)
    {
        if (_sidebarDeviceCard == null)
            return;

        string deviceName = _deviceSelector?.SelectedDeviceName ?? string.Empty;
        string normalized = string.Equals(deviceName, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III"
            : deviceName;

        string imageFile = normalized switch
        {
            "HyperX Cloud III" => "cloud3.png",
            "HyperX Cloud III S" => "cloud3.png",
            "HyperX Cloud 2 Core" => "cloud2core.png",
            "HyperX Cloud Alpha" => "cloudalpha.png",
            "HyperX Cloud Stinger 2" => "cloudstinger2.png",
            _ => "unknown-device.png"
        };

        _sidebarDeviceNameLabel.Text = selected ? normalized : L("UnknownHeadphones");
        _sidebarStatusLabel.Text = connected
            ? L("Connected")
            : selected ? L("Disconnected") : L("SidebarUnknown");
        _sidebarStatusDot.Connected = connected;
        _sidebarStatusDot.Invalidate();

        _sidebarBatteryIcon.Connected = connected;
        _sidebarBatteryIcon.Battery = connected && _device != null ? Math.Clamp(_device.Battery, 0, 100) : 0;
        _sidebarBatteryIcon.Charging = connected && _isCharging;
        _sidebarBatteryIcon.DarkMode = EffectiveTheme == AppTheme.Dark;
        _sidebarBatteryIcon.Invalidate();

        _sidebarBatteryLabel.Text = connected
            ? $"{Math.Clamp(_device!.Battery, 0, 100)}%"
            : L("BatteryNA");
        _sidebarChargingLabel.Text = _isCharging && connected ? L("ChargingStatus") : string.Empty;
        _sidebarChargingLabel.Visible = connected && _isCharging;

        Color foreground = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        Color secondary = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
        _sidebarDeviceNameLabel.ForeColor = foreground;
        _sidebarStatusTitleLabel.ForeColor = secondary;
        _sidebarStatusLabel.ForeColor = secondary;
        _sidebarBatteryTitleLabel.ForeColor = secondary;
        _sidebarBatteryLabel.ForeColor = foreground;
        _sidebarChargingLabel.ForeColor = secondary;

        _sidebarDeviceImage.Image?.Dispose();
        _sidebarDeviceImage.Image = null;

        if (!string.IsNullOrWhiteSpace(imageFile))
        {
            string imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Devices", imageFile);
            if (File.Exists(imagePath))
            {
                try
                {
                    using Image source = Image.FromFile(imagePath);
                    _sidebarDeviceImage.Image = new Bitmap(source);
                }
                catch
                {
                    _sidebarDeviceImage.Image = null;
                }
            }
        }

        _sidebarDeviceImage.Visible = _sidebarDeviceImage.Image != null;
    }

    private void UpdateDeviceInformation()
    {
        if (_wirelessTechnologyValueLabel == null) return;

        string device = _deviceSelector?.SelectedDeviceName ?? string.Empty;
        string normalized = string.Equals(device, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III"
            : device;

        string[] values = normalized switch
        {
            "HyperX Cloud III" => new[]
            {
                L("Cloud3Wireless_WirelessTechnology"),
                L("Cloud3Wireless_ConnectionMethod"),
                L("Cloud3Wireless_Range"),
                L("Cloud3Wireless_Battery"),
                L("Cloud3Wireless_ChargeTime")
            },
            "HyperX Cloud III S" => new[]
            {
                L("Cloud3S_WirelessTechnology"),
                L("Cloud3S_ConnectionMethod"),
                L("Cloud3S_Range"),
                L("Cloud3S_Battery"),
                L("Cloud3S_ChargeTime")
            },
            "HyperX Cloud 2 Core" => new[]
            {
                L("Cloud2Core_WirelessTechnology"),
                L("Cloud2Core_ConnectionMethod"),
                L("Cloud2Core_Range"),
                L("Cloud2Core_Battery"),
                L("Cloud2Core_ChargeTime")
            },
            "HyperX Cloud Alpha" => new[]
            {
                L("CloudAlpha_WirelessTechnology"),
                L("CloudAlpha_ConnectionMethod"),
                L("CloudAlpha_Range"),
                L("CloudAlpha_Battery"),
                L("CloudAlpha_ChargeTime")
            },
            "HyperX Cloud Stinger 2" => new[]
            {
                L("CloudStinger2_WirelessTechnology"),
                L("CloudStinger2_ConnectionMethod"),
                L("CloudStinger2_Range"),
                L("CloudStinger2_Battery"),
                L("CloudStinger2_ChargeTime")
            },
            _ => new[]
            {
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA")
            }
        };

        _wirelessTechnologyValueLabel.Text = values[0];
        _connectionMethodValueLabel.Text = values[1];
        _wirelessRangeValueLabel.Text = values[2];
        _batteryLifeValueLabel.Text = values[3];
        _chargeTimeValueLabel.Text = values[4];

        Color valueColor = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        _wirelessTechnologyValueLabel.ForeColor = valueColor;
        _connectionMethodValueLabel.ForeColor = valueColor;
        _wirelessRangeValueLabel.ForeColor = valueColor;
        _batteryLifeValueLabel.ForeColor = valueColor;
        _chargeTimeValueLabel.ForeColor = valueColor;
    }

    private void Device_BatteryChanged(object? sender, int battery)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefreshDeviceStatus));
            return;
        }
        RefreshDeviceStatus();
    }
}
