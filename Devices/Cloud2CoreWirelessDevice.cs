namespace HyperXBatteryTray.Devices;

public sealed class Cloud2CoreWirelessDevice : HyperXBatteryDeviceBase
{
    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud 2 Core",
        VendorId = 0x03F0,
        ProductId = 0x0995,
        InterfacePattern = "VID_03F0&PID_0995",
        ReportLength = 52,
        ResponseLength = 20,
        ReportId = 0x66,
        BatteryCommandBytes = new byte[] { 0x66, 0x89 },
        BatteryByteIndex = 4,
        PreferHighestUsage = true
    };

    public Cloud2CoreWirelessDevice() : base(DeviceDefinition)
    {
    }
}
