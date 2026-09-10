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
                Definition = new HyperXDeviceDefinition
                {
                    Name = "HyperX Cloud III Wireless",
                    VendorId = 0x03F0,
                    ProductId = 0x05B7,
                    InterfacePattern =
                        "VID_03F0&PID_05B7&MI_03&Col01",
                    ReportLength = 62,
                    ReportId = 0x66,
                    BatteryCommand = 0x89,
                    BatteryByteIndex = 4
                },

                Factory = () =>
                    new Cloud3WirelessDevice()
            }
        };

    public IReadOnlyList<IHyperXDevice> Devices => _devices;

    public IReadOnlyList<IHyperXDevice> DiscoverDevices()
    {
        DisposeDevices();

        foreach (HyperXDeviceRegistration registration
                 in SupportedDevices)
        {
            string? devicePath =
                HidConnection.FindDevice(
                    registration.Definition.InterfacePattern);

            if (string.IsNullOrWhiteSpace(devicePath))
                continue;

            IHyperXDevice device =
                registration.Factory();

            _devices.Add(device);
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

    private void DisposeDevices()
    {
        foreach (IHyperXDevice device in _devices)
        {
            device.Dispose();
        }

        _devices.Clear();
    }

    public void Dispose()
    {
        DisposeDevices();
    }
}