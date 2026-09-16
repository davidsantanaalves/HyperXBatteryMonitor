using HyperXBatteryTray.Hid;

namespace HyperXBatteryTray.Devices;

public abstract class HyperXBatteryDeviceBase : IHyperXDevice
{
    private readonly HidConnection _connection = new();
    private int _battery = -1;
    private bool _isConnected;

    protected HyperXBatteryDeviceBase(HyperXDeviceDefinition definition)
    {
        Definition = definition;
    }

    protected HyperXDeviceDefinition Definition { get; }

    protected HidConnection Connection => _connection;

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

        string? devicePath = HidConnection.FindDevice(Definition);

        if (string.IsNullOrWhiteSpace(devicePath))
            return false;

        try
        {
            if (!_connection.Open(devicePath))
                return false;

            _isConnected = true;
            return true;
        }
        catch
        {
            _connection.Close();
            _isConnected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _connection.Close();
        SetBatteryUnavailable();
    }

    public async Task<int?> QueryBatteryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return null;

        try
        {
            byte[] command = CreateBatteryCommand();
            _connection.Write(command);

            using var timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(2));

            byte[]? response = await _connection.ReadAsync(
                Definition.ResponseLength,
                timeoutCancellation.Token);

            if (response is null ||
                Definition.BatteryByteIndex < 0 ||
                Definition.BatteryByteIndex >= response.Length)
            {
                return null;
            }

            int battery = response[Definition.BatteryByteIndex];

            if (battery < 0 || battery > 100)
                return null;

            UpdateBattery(battery);
            return battery;
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
    }

    public virtual Task<bool?> QueryChargeStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<bool?>(null);
    }

    protected virtual byte[] CreateBatteryCommand()
    {
        byte[] command = new byte[Definition.ReportLength];
        Array.Copy(
            Definition.BatteryCommandBytes,
            command,
            Math.Min(
                Definition.BatteryCommandBytes.Length,
                command.Length));
        return command;
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
