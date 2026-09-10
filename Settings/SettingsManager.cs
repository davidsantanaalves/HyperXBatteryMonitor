using System.Text.Json;

namespace HyperXBatteryTray.Settings;

public sealed class SettingsManager
{
    private readonly string _settingsDirectory;
    private readonly string _settingsFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsManager()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "HyperXBatteryTray");

        _settingsFile = Path.Combine(
            _settingsDirectory,
            "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
                return AppSettings.CreateDefault();

            string json = File.ReadAllText(_settingsFile);

            AppSettings? settings =
                JsonSerializer.Deserialize<AppSettings>(
                    json,
                    JsonOptions);

            if (settings == null)
                return AppSettings.CreateDefault();

            Normalize(settings);

            return settings;
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);

        Directory.CreateDirectory(_settingsDirectory);

        string json = JsonSerializer.Serialize(
            settings,
            JsonOptions);

        string temporaryFile =
            _settingsFile + ".tmp";

        File.WriteAllText(
            temporaryFile,
            json);

        File.Move(
            temporaryFile,
            _settingsFile,
            overwrite: true);
    }

    public void Reset()
    {
        if (File.Exists(_settingsFile))
            File.Delete(_settingsFile);
    }

    private static void Normalize(AppSettings settings)
    {
        if (settings.SelectedDevice == null)
            settings.SelectedDevice = string.Empty;

        if (string.Equals(settings.SelectedDevice?.Trim(), "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase))
        {
            settings.SelectedDevice = "HyperX Cloud III";
        }

        string[] supportedDeviceNames =
        {
            "HyperX Cloud III",
            "HyperX Cloud III S",
            "HyperX Cloud 2 Core",
            "HyperX Cloud Alpha",
            "HyperX Cloud Stinger 2"
        };

        if (!string.IsNullOrWhiteSpace(settings.SelectedDevice) &&
            !supportedDeviceNames.Contains(settings.SelectedDevice.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            settings.SelectedDevice = string.Empty;
        }

        settings.GradientPercent =
            Math.Clamp(
                settings.GradientPercent,
                0,
                50);

        settings.CriticalBatteryPercent =
            Math.Clamp(
                settings.CriticalBatteryPercent,
                0,
                100);

        if (settings.BatteryColors == null ||
            settings.BatteryColors.Count != 3)
        {
            settings.BatteryColors =
                AppSettings.CreateDefault().BatteryColors;
        }

        foreach (BatteryColorSettings color
                 in settings.BatteryColors)
        {
            color.MinimumPercent =
                Math.Clamp(
                    color.MinimumPercent,
                    0,
                    100);
        }

        settings.BatteryColors =
            settings.BatteryColors
                .OrderByDescending(
                    color => color.MinimumPercent)
                .ToList();

        if (!Enum.IsDefined(settings.Language))
            settings.Language = AppLanguage.English;

        if (!Enum.IsDefined(settings.Theme))
            settings.Theme = AppTheme.System;

        if (!Enum.IsDefined(settings.DisplayMode))
            settings.DisplayMode = BatteryDisplayMode.StaticIcon;

        if (!Enum.IsDefined(settings.AdvancedDisplayMode))
            settings.AdvancedDisplayMode = AdvancedDisplayMode.BatteryGradient;
    }
}