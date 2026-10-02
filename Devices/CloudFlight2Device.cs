namespace HyperXBatteryTray.Devices;

public sealed class CloudFlight2Device : HyperXBatteryDeviceBase
{
    private const ushort DeviceVendorId = 0x03F0;
    private const ushort DeviceProductId = 0x0AC1;
    private const string ControlInterface = "VID_03F0&PID_0AC1&MI_03&Col04";
    private const byte ConnectionQueryId = 0x52;
    private const byte ConnectionResponseId = 0x53;
    private const byte ConnectionSubreport = 0x01;
    private const byte BatteryQueryId = 0x50;
    private const byte BatteryResponseId = 0x51;
    private const byte BatterySubreport = 0x02;
    private const byte AudioQueryId = 0x60;
    private const byte AudioResponseId = 0x61;
    private const byte AudioSubreport = 0x02;
    private const byte NotificationReportId = 0xFB;
    private const byte ConnectionNotification = 0x0A;
    private const byte ChargeNotification = 0x09;
    private const byte MicrophoneNotification = 0x0D;
    private const byte AckReportId = 0xFF;
    private const byte AckSubreport = 0x01;
    private const byte SubreportMask = 0x3F;
    private const byte RoutingMask = 0xC0;
    private const byte MainDeviceRouting = 0x00;
    private const byte AckSuccess = 0;
    private const byte AckFailure = 1;
    private const byte ConnectedValue = 0;
    private const byte WireChargingValue = 1;
    private const byte MicrophoneMutedValue = 1;
    private const int ReportIdIndex = 0;
    private const int SubreportIndex = 1;
    private const int ValueIndex = 2;
    private const int ChargeValueIndex = 3;
    private const int MicrophoneValueIndex = 4;
    private const int AckReportIndex = 14;
    private const int AckSubreportIndex = 15;
    private const int AckResultIndex = 16;
    private const int AckTimeoutMilliseconds = 500;
    private const int MaximumFailureRetries = 3;

    private static readonly byte[] ConnectionCommand = { ConnectionQueryId, ConnectionSubreport };
    private static readonly byte[] BatteryCommand = { BatteryQueryId, BatterySubreport };
    private static readonly byte[] AudioCommand = { AudioQueryId, AudioSubreport };

    internal static readonly HyperXDeviceDefinition DeviceDefinition = new()
    {
        Name = "HyperX Cloud Flight 2",
        VendorId = DeviceVendorId,
        ProductId = DeviceProductId,
        InterfacePattern = ControlInterface,
        BatteryCommandBytes = BatteryCommand,
        BatteryByteIndex = ValueIndex,
        SupportsMicrophoneMuteMonitoring = true,
        SupportsChargingMonitoring = true,
        RequiresDedicatedDongle = true,
        NominalBatteryLifeHours = 100
    };

    private readonly object _ackLock = new();
    private TaskCompletionSource<byte[]>? _ackRequest;
    private bool _chargeKnown;

    public CloudFlight2Device() : base(DeviceDefinition)
    {
    }

    protected override bool UsesContinuousReader => true;

    public override async Task<int?> QueryBatteryAsync(CancellationToken cancellationToken = default)
    {
        byte[]? connection = await QueryReportAsync(ConnectionCommand,
            r => IsReport(r, ConnectionResponseId, ConnectionSubreport, ValueIndex), cancellationToken)
            .ConfigureAwait(false);
        if (connection == null || connection[ValueIndex] != ConnectedValue)
            return null;

        byte[]? report = await QueryReportAsync(BatteryCommand, IsBattery, cancellationToken)
            .ConfigureAwait(false);
        return report == null ? null : report[ValueIndex];
    }

    public override Task<bool?> QueryChargeStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<bool?>(IsConnected && _chargeKnown ? IsCharging : null);

    public override async Task<bool?> QueryMicrophoneMuteStatusAsync(CancellationToken cancellationToken = default)
    {
        byte[]? report = await QueryReportAsync(AudioCommand, IsMicrophoneResponse, cancellationToken)
            .ConfigureAwait(false);
        return report == null ? null : report[MicrophoneValueIndex] == MicrophoneMutedValue;
    }

    protected override async Task<bool> SendReportAsync(byte[] command, CancellationToken cancellationToken)
    {
        for (int retry = 0; retry <= MaximumFailureRetries; retry++)
        {
            var ack = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_ackLock)
                _ackRequest = ack;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Connection.Write(command);
                byte[] report = await ack.Task.WaitAsync(
                    TimeSpan.FromMilliseconds(AckTimeoutMilliseconds), cancellationToken).ConfigureAwait(false);
                if (report[AckReportIndex] != command[ReportIdIndex] || report[AckSubreportIndex] != command[SubreportIndex])
                    break;
                if (report[AckResultIndex] == AckSuccess)
                    return true;
                if (report[AckResultIndex] != AckFailure)
                    break;
            }
            catch (TimeoutException)
            {
                break;
            }
            finally
            {
                lock (_ackLock)
                {
                    if (ReferenceEquals(_ackRequest, ack))
                        _ackRequest = null;
                }
            }
        }

        // No guessed recovery command: reset this dedicated receiver session.
        Disconnect();
        return false;
    }

    protected override void ProcessInputReport(byte[] report)
    {
        if (IsReport(report, AckReportId, AckSubreport, AckResultIndex))
        {
            lock (_ackLock)
                _ackRequest?.TrySetResult(report);
            return;
        }
        if (IsBattery(report))
        {
            _chargeKnown = true;
            UpdateChargingState(report[ChargeValueIndex] == WireChargingValue);
            UpdateBattery(report[ValueIndex]);
        }
        else if (IsMicrophoneResponse(report))
            UpdateMicrophoneMuteState(report[MicrophoneValueIndex] == MicrophoneMutedValue);
        else if (IsReport(report, NotificationReportId, ChargeNotification, ValueIndex))
        {
            _chargeKnown = true;
            UpdateChargingState(report[ValueIndex] == WireChargingValue);
        }
        else if (IsReport(report, NotificationReportId, MicrophoneNotification, ValueIndex))
            UpdateMicrophoneMuteState(report[ValueIndex] == MicrophoneMutedValue);
        else if (IsReport(report, NotificationReportId, ConnectionNotification, ValueIndex) &&
            report[ValueIndex] != ConnectedValue)
        {
            Disconnect();
            return;
        }
        CompleteReportRequest(report);
    }

    private static bool IsReport(byte[] report, byte reportId, byte subreport, int lastIndex) =>
        report.Length > lastIndex && report[ReportIdIndex] == reportId &&
        (report[SubreportIndex] & RoutingMask) == MainDeviceRouting && (report[SubreportIndex] & SubreportMask) == subreport;

    private static bool IsBattery(byte[] report) =>
        IsReport(report, BatteryResponseId, BatterySubreport, ChargeValueIndex) && report[ValueIndex] <= 100;

    private static bool IsMicrophoneResponse(byte[] report) =>
        IsReport(report, AudioResponseId, AudioSubreport, MicrophoneValueIndex) &&
        report[MicrophoneValueIndex] <= MicrophoneMutedValue;

    public override void Disconnect()
    {
        lock (_ackLock)
        {
            _ackRequest?.TrySetCanceled();
            _ackRequest = null;
        }
        _chargeKnown = false;
        base.Disconnect();
    }
}
