namespace HyperXBatteryTray.Devices;

public sealed class Cloud3SWirelessDevice : HyperXBatteryDeviceBase
{
    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud III S",
        VendorId = 0x03F0,
        ProductId = 0x06BE,
        InterfacePattern = "VID_03F0&PID_06BE",
        ReportLength = 52,
        ResponseLength = 20,
        ReportId = 0x0C,
        BatteryCommandBytes = new byte[] { 0x0C, 0x02, 0x03, 0x01, 0x00, 0x06 },
        BatteryByteIndex = 6,
        RequiredUsagePage = 448,
        RequiredUsage = 1
    };

    public Cloud3SWirelessDevice() : base(DeviceDefinition)
    {
    }
}
