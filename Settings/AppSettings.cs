using System.Drawing;
using System.Text.Json.Serialization;

namespace HyperXBatteryTray.Settings;

public enum BatteryDisplayMode
{
    StaticIcon,
    ColoredIcon,
    IconAndBattery,
    IconAndPercentage,
    PercentageOnly
}

public sealed class BatteryColorSettings
{
    public string Name { get; set; } = string.Empty;
    public int MinimumPercent { get; set; }
    public int Argb { get; set; }

    [JsonIgnore]
    public Color Color
    {
        get => Color.FromArgb(Argb);
        set => Argb = value.ToArgb();
    }
}

public sealed class AppSettings
{
    public string SelectedDevice { get; set; } = "HyperX Cloud III Wireless";
    public BatteryDisplayMode DisplayMode { get; set; } = BatteryDisplayMode.StaticIcon;
    public bool UseGradient { get; set; } = true;
    public int GradientPercent { get; set; } = 10;
    public bool BlinkOnCriticalBattery { get; set; } = true;
    public int CriticalBatteryPercent { get; set; } = 5;
    public List<BatteryColorSettings> BatteryColors { get; set; } = CreateDefaultColors();
    public AppLanguage Language { get; set; } = AppLanguage.English;
    public AppTheme Theme { get; set; } = AppTheme.Light;
    public bool ThemeConfigured { get; set; }

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            SelectedDevice = "HyperX Cloud III Wireless",
            DisplayMode = BatteryDisplayMode.StaticIcon,
            UseGradient = true,
            GradientPercent = 10,
            BlinkOnCriticalBattery = true,
            CriticalBatteryPercent = 5,
            BatteryColors = CreateDefaultColors(),
            Language = AppLanguage.English,
            Theme = AppTheme.Light,
            ThemeConfigured = false
        };
    }

    private static List<BatteryColorSettings> CreateDefaultColors()
    {
        return new List<BatteryColorSettings>
        {
            new() { Name = "Green", MinimumPercent = 60, Color = Color.LimeGreen },
            new() { Name = "Yellow", MinimumPercent = 30, Color = Color.Gold },
            new() { Name = "Red", MinimumPercent = 0, Color = Color.Red }
        };
    }
}
