namespace HyperXBatteryTray.Devices;

public sealed class HyperXDeviceDefinition
{
    public required string Name { get; init; }

    public required ushort VendorId { get; init; }

    public required ushort ProductId { get; init; }

    public required string InterfacePattern { get; init; }

    public IReadOnlyList<string> AlternateInterfacePatterns { get; init; } = Array.Empty<string>();

    public required byte[] BatteryCommandBytes { get; init; }

    public required int BatteryByteIndex { get; init; }

    public bool SupportsMicrophoneMuteMonitoring { get; init; }

    public double? NominalBatteryLifeHours { get; init; }

    public bool Matches(string devicePath)
    {
        return devicePath.Contains(
            InterfacePattern,
            StringComparison.OrdinalIgnoreCase) ||
            AlternateInterfacePatterns.Any(pattern =>
                !string.IsNullOrWhiteSpace(pattern) &&
                devicePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
