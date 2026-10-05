using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HyperXBatteryTray.Devices;

namespace HyperXBatteryTray.Settings;

public sealed class SettingsManager
{
    private const string TemporaryFileSuffix = ".tmp";

    private readonly string _settingsDirectory;
    private readonly string _settingsFile;
    private readonly string _batteryHistoryFile;

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

        _batteryHistoryFile = Path.Combine(
            _settingsDirectory,
            "battery-history.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
            {
                AppSettings defaults = AppSettings.CreateDefault();
                defaults.Language = DetectWindowsLanguage();
                defaults.IsNewSettingsProfile = true;

                try
                {
                    Save(defaults);
                }
                catch
                {
                    // Keep the detected language for this session even if persistence fails.
                }

                return defaults;
            }

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
            _settingsFile + TemporaryFileSuffix;

        File.WriteAllText(
            temporaryFile,
            json);

        File.Move(
            temporaryFile,
            _settingsFile,
            overwrite: true);
    }

    public BatteryHistoryData LoadBatteryHistory()
    {
        try
        {
            if (!File.Exists(_batteryHistoryFile))
                return BatteryHistoryData.CreateCurrent();

            string json = File.ReadAllText(_batteryHistoryFile);

            BatteryHistoryData? history =
                JsonSerializer.Deserialize<BatteryHistoryData>(
                    json,
                    JsonOptions);

            if (history?.Devices == null ||
                history.Version != BatteryHistoryData.CurrentVersion)
            {
                return ResetBatteryHistory();
            }

            return history;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Could not load battery history; resetting it: {ex.Message}");
            return ResetBatteryHistory();
        }
    }

    public void SaveBatteryHistory(BatteryHistoryData history)
    {
        history.Version = BatteryHistoryData.CurrentVersion;

        Directory.CreateDirectory(_settingsDirectory);

        string json = JsonSerializer.Serialize(
            history,
            JsonOptions);

        string temporaryFile =
            _batteryHistoryFile + TemporaryFileSuffix;

        File.WriteAllText(
            temporaryFile,
            json);

        File.Move(
            temporaryFile,
            _batteryHistoryFile,
            overwrite: true);
    }

    public void DeleteUserData()
    {
        DeleteFileIfExists(_settingsFile);
        DeleteFileIfExists(_settingsFile + TemporaryFileSuffix);
        DeleteFileIfExists(_batteryHistoryFile);
        DeleteFileIfExists(_batteryHistoryFile + TemporaryFileSuffix);
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private BatteryHistoryData ResetBatteryHistory()
    {
        BatteryHistoryData history = BatteryHistoryData.CreateCurrent();

        try
        {
            SaveBatteryHistory(history);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Could not persist the reset battery history: {ex.Message}");
        }

        return history;
    }

    private static AppLanguage DetectWindowsLanguage()
    {
        return Localization.DetectLanguage(CultureInfo.CurrentUICulture);
    }

    private static void Normalize(AppSettings settings)
    {
        if (settings.SelectedDevice == null)
            settings.SelectedDevice = string.Empty;

        if (!string.IsNullOrWhiteSpace(settings.SelectedDevice))
        {
            settings.SelectedDevice =
                HyperXDeviceManager.NormalizeSupportedDeviceName(
                    settings.SelectedDevice);
        }

        if (!string.IsNullOrWhiteSpace(settings.SelectedDevice) &&
            !HyperXDeviceManager.IsSupportedDeviceName(settings.SelectedDevice))
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
                1,
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