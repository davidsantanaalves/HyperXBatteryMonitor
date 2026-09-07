using HyperXBatteryTray.Devices;

namespace HyperXBatteryTray.Monitoring;

public sealed class BatteryMonitor : IDisposable
{
    private readonly IHyperXDevice _device;

    private CancellationTokenSource? _cancellation;
    private Task? _monitorTask;

    private bool _lastConnectionState;

    public BatteryMonitor(IHyperXDevice device)
    {
        _device = device;
    }

    public int IntervalMilliseconds { get; set; } = 5000;

    public event EventHandler<int>? BatteryChanged;

    public event EventHandler<bool>? ConnectionChanged;

    public void Start()
    {
        if (_monitorTask != null)
            return;

        _cancellation = new CancellationTokenSource();

        _monitorTask = MonitorLoopAsync(
            _cancellation.Token);
    }

    private async Task MonitorLoopAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_device.IsConnected)
                {
                    bool connected = _device.Connect();

                    UpdateConnectionState(connected);

                    if (!connected)
                    {
                        await Task.Delay(
                            IntervalMilliseconds,
                            cancellationToken);

                        continue;
                    }
                }

                int? battery = await _device.QueryBatteryAsync(
                    cancellationToken);

                if (battery.HasValue)
                {
                    BatteryChanged?.Invoke(
                        this,
                        battery.Value);
                }
                else
                {
                    _device.Disconnect();

                    UpdateConnectionState(false);
                }

                await Task.Delay(
                    IntervalMilliseconds,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                if (_device.IsConnected)
                    _device.Disconnect();

                UpdateConnectionState(false);

                try
                {
                    await Task.Delay(
                        IntervalMilliseconds,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void UpdateConnectionState(bool connected)
    {
        if (_lastConnectionState == connected)
            return;

        _lastConnectionState = connected;

        ConnectionChanged?.Invoke(
            this,
            connected);
    }

    public void Dispose()
    {
        _cancellation?.Cancel();

        _device.Disconnect();

        _cancellation?.Dispose();

        _cancellation = null;
        _monitorTask = null;

        _device.Dispose();
    }
}