namespace HyperXBatteryTray.Devices;

public sealed class CloudFlightWirelessDevice : HyperXBatteryDeviceBase
{
    private const ushort DeviceVendorId = 0x0951;
    private const ushort DeviceProductId = 0x16C4;
    private const string ControlInterface = "VID_0951&PID_16C4&MI_03&Col03";
    private const string AlternateKingstonInterface = "VID_0951&PID_1723&MI_03&Col03";
    private const string AlternateHpInterface = "VID_03F0&PID_0E90&MI_03&Col03";
    private const byte StatusReportId = 0x21;
    private const byte StatusMarker = 0xFF;
    private const byte StatusSelector = 0x05;
    private const int ReportIdIndex = 0;
    private const int MarkerIndex = 1;
    private const int SelectorIndex = 2;
    private const int HighIndex = 3;
    private const int LowIndex = 4;
    private const byte LowBatteryHigh = 0x0D;
    private const byte MidBatteryHigh = 0x0E;
    private const byte UpperBatteryHigh = 0x0F;
    private const byte ChargingHigh = 0x10;
    private const byte ChargingAlwaysHigh = 0x11;
    private const byte ChargingLowThreshold = 0x14;
    private const int UnknownVoltageBattery = 1;
    private const int LowVoltageBattery = 5;
    private const int BatteryStep = 5;
    private const int MidBatteryStart = 10;
    private const int UpperBatteryStart = 70;

    // Inclusive upper bounds of the confirmed voltage lookup intervals.
    private static readonly byte[] MidVoltageBounds =
        { 89, 119, 148, 159, 169, 179, 189, 199, 209, 219, 239, 255 };
    private static readonly byte[] UpperVoltageBounds =
        { 19, 49, 69, 99, 119, 129, 255 };
    private static readonly byte[] StatusCommand = { StatusReportId, StatusMarker, StatusSelector };

    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud Flight Wireless",
        VendorId = DeviceVendorId,
        ProductId = DeviceProductId,
        InterfacePattern = ControlInterface,
        AlternateInterfacePatterns = new[] { AlternateKingstonInterface, AlternateHpInterface },
        BatteryCommandBytes = StatusCommand,
        BatteryByteIndex = HighIndex,
        SupportsMicrophoneMuteMonitoring = false,
        NominalBatteryLifeHours = 30
    };

    public CloudFlightWirelessDevice() : base(DeviceDefinition)
    {
    }

    protected override bool UsesContinuousReader => true;

    public override async Task<int?> QueryBatteryAsync(CancellationToken cancellationToken = default)
    {
        // An accepted status report is also the connection heartbeat. Use the
        // monitor's query timeout/cadence rather than a second polling timer.
        byte[]? report = await QueryReportAsync(StatusCommand, IsStatus, cancellationToken)
            .ConfigureAwait(false);
        return report == null ? null : ConvertBattery(report[HighIndex], report[LowIndex]);
    }

    public override Task<bool?> QueryChargeStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<bool?>(IsConnected && Battery >= 0 ? IsCharging : null);

    protected override void ProcessInputReport(byte[] report)
    {
        if (IsStatus(report))
        {
            byte high = report[HighIndex];
            byte low = report[LowIndex];
            UpdateChargingState((high == ChargingHigh && low >= ChargingLowThreshold) ||
                high == ChargingAlwaysHigh);
            UpdateBattery(ConvertBattery(high, low));
        }
        CompleteReportRequest(report);
    }

    private static bool IsStatus(byte[] report) =>
        report.Length > LowIndex && report[ReportIdIndex] == StatusReportId &&
        report[MarkerIndex] == StatusMarker && report[SelectorIndex] == StatusSelector;

    internal static int ConvertBattery(byte high, byte low) => high switch
    {
        LowBatteryHigh => LowVoltageBattery,
        MidBatteryHigh => LookupBattery(low, MidVoltageBounds, MidBatteryStart),
        UpperBatteryHigh => LookupBattery(low, UpperVoltageBounds, UpperBatteryStart),
        ChargingHigh or ChargingAlwaysHigh => 100,
        _ => UnknownVoltageBattery
    };

    private static int LookupBattery(byte low, byte[] bounds, int startingPercent)
    {
        for (int i = 0; i < bounds.Length; i++)
        {
            if (low <= bounds[i])
                return startingPercent + BatteryStep * i;
        }
        return UnknownVoltageBattery;
    }
}
