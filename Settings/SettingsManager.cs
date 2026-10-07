using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HyperXBatteryTray.Devices;

namespace HyperXBatteryTray.Settings;

public sealed class SettingsManager
{
    private const string CurrentSettingsDirectoryName = "Hyper Battery Monitor";
    private const string LegacySettingsDirectoryName = "HyperXBatteryTray";
    private const string SettingsFileName = "settings.json";
    private const string BatteryHistoryFileName = "battery-history.json";
    private const string TemporaryFileSuffix = ".tmp";
    private const string MigrationTemporaryFileSuffix = ".migration.tmp";

    private readonly string _settingsDirectory;
    private readonly string _settingsFile;
    private readonly string _batteryHistoryFile;
    private readonly string _legacySettingsDirectory;
    private readonly string _legacySettingsFile;
    private readonly string _legacyBatteryHistoryFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsManager()
    {
        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        _settingsDirectory = Path.Combine(
            localApplicationData,
            CurrentSettingsDirectoryName);

        _settingsFile = Path.Combine(
            _settingsDirectory,
            SettingsFileName);

        _batteryHistoryFile = Path.Combine(
            _settingsDirectory,
            BatteryHistoryFileName);

        _legacySettingsDirectory = Path.Combine(
            localApplicationData,
            LegacySettingsDirectoryName);

        _legacySettingsFile = Path.Combine(
            _legacySettingsDirectory,
            SettingsFileName);

        _legacyBatteryHistoryFile = Path.Combine(
            _legacySettingsDirectory,
            BatteryHistoryFileName);
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

    public void MigrateLegacyUserData()
    {
        if (!Directory.Exists(_legacySettingsDirectory) ||
            PathsEqual(_legacySettingsDirectory, _settingsDirectory))
        {
            return;
        }

        Directory.CreateDirectory(_settingsDirectory);

        MigrateJsonFile(
            _legacySettingsFile,
            _settingsFile,
            IsValidSettingsFile);

        MigrateJsonFile(
            _legacyBatteryHistoryFile,
            _batteryHistoryFile,
            IsValidBatteryHistoryFile);

        DeleteFileIfExists(
            _legacySettingsFile + TemporaryFileSuffix);
        DeleteFileIfExists(
            _legacyBatteryHistoryFile + TemporaryFileSuffix);

        DeleteFileIfExists(_legacySettingsFile);
        DeleteFileIfExists(_legacyBatteryHistoryFile);

        if (!Directory.EnumerateFileSystemEntries(
                _legacySettingsDirectory).Any())
        {
            Directory.Delete(_legacySettingsDirectory);
        }
    }

    public void DeleteUserData()
    {
        DeleteDirectoryIfExists(_settingsDirectory);

        if (!PathsEqual(
                _legacySettingsDirectory,
                _settingsDirectory))
        {
            DeleteDirectoryIfExists(_legacySettingsDirectory);
        }
    }

    private static void MigrateJsonFile(
        string sourcePath,
        string destinationPath,
        Func<string, bool> validator)
    {
        if (!File.Exists(sourcePath))
            return;

        if (File.Exists(destinationPath) && validator(destinationPath))
            return;

        string migrationTemporaryFile =
            destinationPath + MigrationTemporaryFileSuffix;

        DeleteFileIfExists(migrationTemporaryFile);

        try
        {
            File.Copy(
                sourcePath,
                migrationTemporaryFile,
                overwrite: true);

            if (!validator(migrationTemporaryFile))
            {
                throw new InvalidDataException(
                    $"Legacy user data is invalid and was preserved at '{sourcePath}'.");
            }

            File.Move(
                migrationTemporaryFile,
                destinationPath,
                overwrite: true);
        }
        finally
        {
            DeleteFileIfExists(migrationTemporaryFile);
        }
    }

    private static bool IsValidSettingsFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(
                json,
                JsonOptions) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidBatteryHistoryFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            BatteryHistoryData? history =
                JsonSerializer.Deserialize<BatteryHistoryData>(
                    json,
                    JsonOptions);

            return history?.Devices != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool PathsEqual(
        string firstPath,
        string secondPath)
    {
        return string.Equals(
            Path.GetFullPath(firstPath)
                .TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(secondPath)
                .TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
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
