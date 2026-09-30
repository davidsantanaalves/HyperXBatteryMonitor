namespace HyperXBatteryTray.Devices;

public sealed class CloudFlightSDevice : HyperXBatteryDeviceBase
{
    private const ushort DeviceVendorId = 0x0951;
    private const ushort DeviceProductId = 0x16EA;
    private const string ControlInterface = "VID_0951&PID_16EA&MI_05&Col03";
    private const byte BatteryReportId = 0x0B;
    private const byte StatusMarker = 0xBB;
    private const byte ConnectionSelector = 0x01;
    private const byte BatterySelector = 0x02;
    private const byte ChargeSelector = 0x03;
    private const byte ConnectedValue = 1;
    private const byte WireChargingValue = 1;
    private const byte ChargeErrorValue = 3;
    private const int ReportIdIndex = 0;
    private const int MarkerIndex = 2;
    private const int SelectorIndex = 3;
    private const int StatusValueIndex = 4;
    private const int BatteryValueIndex = 7;
    private const int CommandSelectorIndex = 15;

    private static readonly byte[] StatusCommandPrefix =
        { 0x06, 0x00, 0x02, 0x00, 0x9A, 0x00, 0x00, 0x68,
          0x4A, 0x8E, 0x0A, 0x00, 0x00, 0x00, StatusMarker, BatterySelector };

    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud Flight S",
        VendorId = DeviceVendorId,
        ProductId = DeviceProductId,
        InterfacePattern = ControlInterface,
        BatteryCommandBytes = StatusCommandPrefix,
        BatteryByteIndex = BatteryValueIndex,
        SupportsMicrophoneMuteMonitoring = false,
        NominalBatteryLifeHours = 30
    };

    public CloudFlightSDevice() : base(DeviceDefinition)
    {
    }

    protected override bool UsesContinuousReader => true;

    public override async Task<int?> QueryBatteryAsync(CancellationToken cancellationToken = default)
    {
        byte[]? connection = await QueryReportAsync(Command(ConnectionSelector),
            r => IsStatus(r, ConnectionSelector), cancellationToken).ConfigureAwait(false);
        if (connection == null || connection[StatusValueIndex] != ConnectedValue)
            return null;

        byte[]? report = await QueryReportAsync(Command(BatterySelector),
            IsBattery, cancellationToken).ConfigureAwait(false);
        if (report == null)
            return null;

        return report[BatteryValueIndex];
    }

    public override async Task<bool?> QueryChargeStatusAsync(CancellationToken cancellationToken = default)
    {
        byte[]? report = await QueryReportAsync(Command(ChargeSelector),
            IsCharge, cancellationToken).ConfigureAwait(false);
        return report == null ? null : report[StatusValueIndex] == WireChargingValue;
    }

    protected override void ProcessInputReport(byte[] report)
    {
        if (IsStatus(report, ConnectionSelector) && report[StatusValueIndex] != ConnectedValue)
        {
            Disconnect();
            return;
        }
        if (IsBattery(report))
            UpdateBattery(report[BatteryValueIndex]);
        if (IsCharge(report))
            UpdateChargingState(report[StatusValueIndex] == WireChargingValue);
        CompleteReportRequest(report);
    }

    private static byte[] Command(byte selector)
    {
        byte[] command = CreateCommand(StatusCommandPrefix);
        command[CommandSelectorIndex] = selector;
        return command;
    }

    private static bool IsStatus(byte[] report, byte selector) =>
        report.Length > StatusValueIndex && report[MarkerIndex] == StatusMarker &&
        report[SelectorIndex] == selector;

    private static bool IsBattery(byte[] report) =>
        IsStatus(report, BatterySelector) && report[ReportIdIndex] == BatteryReportId &&
        report.Length > BatteryValueIndex && report[BatteryValueIndex] <= 100;

    private static bool IsCharge(byte[] report) =>
        IsStatus(report, ChargeSelector) && report[StatusValueIndex] <= ChargeErrorValue;
}
