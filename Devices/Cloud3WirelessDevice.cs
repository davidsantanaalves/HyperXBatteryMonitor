using HyperXBatteryTray.Hid;

namespace HyperXBatteryTray.Devices;

public sealed class Cloud3WirelessDevice : IHyperXDevice
{
    private static readonly HyperXDeviceDefinition Definition = new()
    {
        Name = "HyperX Cloud III Wireless",
        VendorId = 0x03F0,
        ProductId = 0x05B7,
        InterfacePattern = "VID_03F0&PID_05B7&MI_03&Col01",
        ReportLength = 62,
        ReportId = 0x66,
        BatteryCommand = 0x89,
        BatteryByteIndex = 4
    };

    private readonly HidConnection _connection = new();
    private readonly object _batteryRequestLock = new();

    private CancellationTokenSource? _readerCancellation;
    private Task? _readerTask;
    private TaskCompletionSource<int>? _batteryRequest;

    private int _battery = -1;
    private bool _isConnected;

    public string Name => Definition.Name;

    public ushort VendorId => Definition.VendorId;

    public ushort ProductId => Definition.ProductId;

    public bool IsConnected => _isConnected;

    public int Battery => _battery;

    public event EventHandler<int>? BatteryChanged;

    public bool Connect()
    {
        if (_isConnected)
            return true;

        string? devicePath = HidConnection.FindDevice(
            Definition.InterfacePattern);

        if (string.IsNullOrWhiteSpace(devicePath))
            return false;

        try
        {
            if (!_connection.Open(devicePath))
                return false;

            _readerCancellation = new CancellationTokenSource();

            _isConnected = true;

            CancellationToken token = _readerCancellation.Token;

            _readerTask = Task.Run(
                () => ReaderLoopAsync(token));

            return true;
        }
        catch
        {
            _connection.Close();

            _readerCancellation?.Dispose();
            _readerCancellation = null;

            _isConnected = false;

            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;

        lock (_batteryRequestLock)
        {
            _batteryRequest?.TrySetCanceled();
            _batteryRequest = null;
        }

        _readerCancellation?.Cancel();

        _connection.Close();

        _readerCancellation?.Dispose();
        _readerCancellation = null;

        _readerTask = null;

        SetBatteryUnavailable();
    }

    public async Task<int?> QueryBatteryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return null;

        var response = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_batteryRequestLock)
        {
            _batteryRequest = response;
        }

        try
        {
            byte[] command = new byte[Definition.ReportLength];

            command[0] = Definition.ReportId;
            command[1] = Definition.BatteryCommand;

            _connection.Write(command);

            using var timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCancellation.CancelAfter(
                TimeSpan.FromSeconds(2));

            return await response.Task.WaitAsync(
                timeoutCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            lock (_batteryRequestLock)
            {
                if (ReferenceEquals(_batteryRequest, response))
                    _batteryRequest = null;
            }
        }
    }

    private async Task ReaderLoopAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                byte[]? report = await _connection.ReadAsync(
                    Definition.ReportLength,
                    cancellationToken);

                if (report is null || report.Length == 0)
                    continue;

                ProcessInputReport(report);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }
    }

    private void ProcessInputReport(byte[] report)
    {
        if (report.Length < 5)
            return;

        if (report[0] != Definition.ReportId)
            return;

        // Cloud III Wireless battery response:
        //
        // 66 89 xx xx battery ...
        //
        // NGENUITY also accepts command 0x0D.
        if (report[1] != 0x89 && report[1] != 0x0D)
            return;

        if (report[2] == 0 && report[3] == 0)
            return;

        int battery = report[Definition.BatteryByteIndex];

        if (battery < 0 || battery > 100)
            return;

        UpdateBattery(battery);

        lock (_batteryRequestLock)
        {
            _batteryRequest?.TrySetResult(battery);
        }
    }

    private void UpdateBattery(int battery)
    {
        if (_battery == battery)
            return;

        _battery = battery;

        BatteryChanged?.Invoke(this, battery);
    }

    private void SetBatteryUnavailable()
    {
        if (_battery == -1)
            return;

        _battery = -1;

        BatteryChanged?.Invoke(this, -1);
    }

    public void Dispose()
    {
        Disconnect();
        _connection.Dispose();
    }
}