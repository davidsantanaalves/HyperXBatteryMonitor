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
        if (string.IsNullOrWhiteSpace(settings.SelectedDevice))
        {
            settings.SelectedDevice =
                "HyperX Cloud III Wireless";
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
            settings.BatteryColors.Count != 5)
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
    }
}