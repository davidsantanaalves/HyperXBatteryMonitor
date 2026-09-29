namespace HyperXBatteryTray.Settings;

public sealed class BatteryHistoryData
{
    public Dictionary<string, List<BatteryDischargeSample>> Devices { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class BatteryDischargeSample
{
    public int DropPercent { get; set; }

    public double ElapsedSeconds { get; set; }
}
