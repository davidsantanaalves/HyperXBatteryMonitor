namespace HyperXBatteryTray.Devices;

public interface IHyperXDevice : IDisposable
{
    string Name { get; }

    ushort VendorId { get; }

    ushort ProductId { get; }

    bool IsConnected { get; }

    int Battery { get; }

    bool IsCharging { get; }

    bool IsMicrophoneMuted { get; }

    event EventHandler<int>? BatteryChanged;

    event EventHandler<bool>? ChargingChanged;

    event EventHandler<bool>? MicrophoneMuteChanged;

    bool Connect();

    void Disconnect();

    Task<int?> QueryBatteryAsync(
        CancellationToken cancellationToken = default);

    Task<bool?> QueryChargeStatusAsync(
        CancellationToken cancellationToken = default);

    Task<bool?> QueryMicrophoneMuteStatusAsync(
        CancellationToken cancellationToken = default);
}
