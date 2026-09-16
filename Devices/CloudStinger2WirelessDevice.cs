namespace HyperXBatteryTray.Devices;

public sealed class CloudStinger2WirelessDevice : HyperXBatteryDeviceBase
{
    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud Stinger 2",
        VendorId = 0x03F0,
        ProductId = 0x0D93,
        InterfacePattern = "VID_03F0&PID_0D93",
        ReportLength = 52,
        ResponseLength = 20,
        ReportId = 0x06,
        BatteryCommandBytes = new byte[] { 0x06, 0xFF, 0xBB, 0x02 },
        BatteryByteIndex = 7,
        PreferHighestUsage = true
    };

    public CloudStinger2WirelessDevice() : base(DeviceDefinition)
    {
    }
}
