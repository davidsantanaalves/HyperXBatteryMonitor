namespace HyperXBatteryTray.Devices;

public sealed class HyperXDeviceRegistration
{
    public required HyperXDeviceDefinition Definition { get; init; }

    public required Func<IHyperXDevice> Factory { get; init; }
}