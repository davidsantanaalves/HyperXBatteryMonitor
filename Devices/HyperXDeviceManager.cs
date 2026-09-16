using HyperXBatteryTray.Hid;

namespace HyperXBatteryTray.Devices;

public sealed class HyperXDeviceManager : IDisposable
{
    private readonly List<IHyperXDevice> _devices = new();

    private static readonly IReadOnlyList<HyperXDeviceRegistration>
        SupportedDevices =
        new List<HyperXDeviceRegistration>
        {
            new()
            {
                Definition = Cloud3WirelessDevice.Definition,
                Factory = () => new Cloud3WirelessDevice()
            },
            new()
            {
                Definition = Cloud3SWirelessDevice.DeviceDefinition,
                Factory = () => new Cloud3SWirelessDevice()
            },
            new()
            {
                Definition = Cloud2CoreWirelessDevice.DeviceDefinition,
                Factory = () => new Cloud2CoreWirelessDevice()
            },
            new()
            {
                Definition = CloudAlphaWirelessDevice.DeviceDefinition,
                Factory = () => new CloudAlphaWirelessDevice()
            },
            new()
            {
                Definition = CloudStinger2WirelessDevice.DeviceDefinition,
                Factory = () => new CloudStinger2WirelessDevice()
            }
        };

    public IReadOnlyList<IHyperXDevice> Devices => _devices;

    public IReadOnlyList<IHyperXDevice> DiscoverDevices()
    {
        DisposeDevices();

        foreach (HyperXDeviceRegistration registration in SupportedDevices)
        {
            string? devicePath = HidConnection.FindDevice(registration.Definition);

            if (string.IsNullOrWhiteSpace(devicePath))
                continue;

            _devices.Add(registration.Factory());
        }

        return _devices;
    }

    public IHyperXDevice? GetFirstAvailableDevice()
    {
        DiscoverDevices();

        return _devices.Count > 0
            ? _devices[0]
            : null;
    }

    public IHyperXDevice? GetAvailableDevice(string? selectedDeviceName)
    {
        if (string.IsNullOrWhiteSpace(selectedDeviceName))
            return null;

        string normalized = NormalizeDeviceName(selectedDeviceName);

        HyperXDeviceRegistration? registration =
            SupportedDevices.FirstOrDefault(item =>
                string.Equals(
                    NormalizeDeviceName(item.Definition.Name),
                    normalized,
                    StringComparison.OrdinalIgnoreCase));

        return registration?.Factory();
    }

    private static string NormalizeDeviceName(string value) =>
        string.Equals(
            value.Trim(),
            "HyperX Cloud III Wireless",
            StringComparison.OrdinalIgnoreCase)
                ? "HyperX Cloud III"
                : value.Trim();

    private void DisposeDevices()
    {
        foreach (IHyperXDevice device in _devices)
            device.Dispose();

        _devices.Clear();
    }

    public void Dispose()
    {
        DisposeDevices();
    }
}
