namespace HyperXBatteryTray.Devices;

public interface IHyperXDevice : IDisposable
{
    string Name { get; }

    ushort VendorId { get; }

    ushort ProductId { get; }

    bool IsConnected { get; }

    int Battery { get; }

    event EventHandler<int>? BatteryChanged;

    bool Connect();

    void Disconnect();

    Task<int?> QueryBatteryAsync(
        CancellationToken cancellationToken = default);

	Task<bool?> QueryChargeStatusAsync(
    CancellationToken cancellationToken = default);
}