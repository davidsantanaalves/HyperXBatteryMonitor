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

    public IHyperXDevice? GetAvailableDevice(string? selectedDeviceName) =>
        CreateDevice(selectedDeviceName);

    public static IHyperXDevice? CreateDevice(string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Factory();
    }

    public static bool IsSameDevice(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right);

        return string.Equals(
            NormalizeDeviceName(left),
            NormalizeDeviceName(right),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool SupportsMicrophoneMuteMonitoring(
        string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Definition.SupportsMicrophoneMuteMonitoring == true;
    }

    internal static double? GetNominalBatteryLifeHours(
        string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Definition.NominalBatteryLifeHours;
    }

    internal static string NormalizeSupportedDeviceName(string value) =>
        NormalizeDeviceName(value);

    private static HyperXDeviceRegistration? FindRegistration(
        string? selectedDeviceName)
    {
        if (string.IsNullOrWhiteSpace(selectedDeviceName))
            return null;

        string normalized = NormalizeDeviceName(selectedDeviceName);

        return SupportedDevices.FirstOrDefault(item =>
            string.Equals(
                NormalizeDeviceName(item.Definition.Name),
                normalized,
                StringComparison.OrdinalIgnoreCase));
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
