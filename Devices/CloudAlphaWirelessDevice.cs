namespace HyperXBatteryTray.Devices;

public sealed class CloudAlphaWirelessDevice : HyperXBatteryDeviceBase
{
    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud Alpha",
        VendorId = 0x03F0,
        ProductId = 0x098D,
        InterfacePattern = "VID_03F0&PID_098D",
        ReportLength = 52,
        ResponseLength = 20,
        ReportId = 0x21,
        BatteryCommandBytes = new byte[] { 0x21, 0xBB, 0x0B },
        BatteryByteIndex = 3,
        PreferHighestUsage = true
    };

    public CloudAlphaWirelessDevice() : base(DeviceDefinition)
    {
    }
}
