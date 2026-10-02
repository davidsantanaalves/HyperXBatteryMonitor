using HyperXBatteryTray.Hid;

namespace HyperXBatteryTray.Devices;

public sealed class HyperXDeviceManager : IDisposable
{
    private const string ThreeInOneReceiverInterfacePattern = "VID_03F0&PID_01BF";

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
            },
            new()
            {
                Definition = CloudFlightSDevice.DeviceDefinition,
                Factory = () => new CloudFlightSDevice()
            },
            new()
            {
                Definition = CloudFlightWirelessDevice.DeviceDefinition,
                Factory = () => new CloudFlightWirelessDevice()
            },
            new()
            {
                Definition = CloudStingerCoreWirelessDevice.DeviceDefinition,
                Factory = () => new CloudStingerCoreWirelessDevice()
            },
            new()
            {
                Definition = CloudFlight2Device.DeviceDefinition,
                Factory = () => new CloudFlight2Device()
            },
            new()
            {
                Definition = CloudMix2Device.DeviceDefinition,
                Factory = () => new CloudMix2Device()
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

    public static bool SupportsChargingMonitoring(
        string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Definition.SupportsChargingMonitoring == true;
    }

    internal static bool RequiresDedicatedDongle(
        string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Definition.RequiresDedicatedDongle == true;
    }

    public static async Task<IReadOnlyList<string>> ProbeResponsiveDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        List<string> detectedDevices = new();

        foreach (HyperXDeviceRegistration registration in SupportedDevices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? devicePath = HidConnection.FindDevice(registration.Definition);
            if (string.IsNullOrWhiteSpace(devicePath))
                continue;

            using IHyperXDevice device = registration.Factory();

            try
            {
                if (!device.Connect())
                    continue;

                int? battery = await device.QueryBatteryAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (battery is >= 0 and <= 100)
                    detectedDevices.Add(registration.Definition.Name);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Automatic device detection could not probe '{registration.Definition.Name}': {ex.Message}");
            }
        }

        return detectedDevices;
    }

    internal static double? GetNominalBatteryLifeHours(
        string? selectedDeviceName)
    {
        HyperXDeviceRegistration? registration =
            FindRegistration(selectedDeviceName);

        return registration?.Definition.NominalBatteryLifeHours;
    }

    internal static bool IsSupportedDeviceName(string? selectedDeviceName) =>
        FindRegistration(selectedDeviceName) != null;

    internal static bool IsThreeInOneReceiverPresent() =>
        !string.IsNullOrWhiteSpace(
            HidConnection.FindDeviceByInterfacePattern(
                ThreeInOneReceiverInterfacePattern));

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

    private static string NormalizeDeviceName(string value)
    {
        string normalized = value.Trim();

        if (string.Equals(
                normalized,
                "HyperX Cloud III Wireless",
                StringComparison.OrdinalIgnoreCase))
        {
            return "HyperX Cloud III";
        }

        if (string.Equals(
                normalized,
                Cloud3SWirelessDevice.DeviceDefinition.Name,
                StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(
                Cloud3SWirelessDevice.DeviceDefinition.Name + " (",
                StringComparison.OrdinalIgnoreCase))
        {
            return Cloud3SWirelessDevice.DeviceDefinition.Name;
        }

        if (string.Equals(
                normalized,
                CloudFlight2Device.DeviceDefinition.Name,
                StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(
                CloudFlight2Device.DeviceDefinition.Name + " (",
                StringComparison.OrdinalIgnoreCase))
        {
            return CloudFlight2Device.DeviceDefinition.Name;
        }

        return normalized;
    }

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
