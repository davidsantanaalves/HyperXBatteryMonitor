namespace HyperXBatteryTray.Settings;

public sealed class BatteryHistoryData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; }

    public Dictionary<string, List<BatteryDischargeSample>> Devices { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static BatteryHistoryData CreateCurrent() =>
        new()
        {
            Version = CurrentVersion
        };
}

public sealed class BatteryDischargeSample
{
    public int DropPercent { get; set; }

    public double ElapsedSeconds { get; set; }
}
