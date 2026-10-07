using System.Diagnostics;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

internal static class Program
{
    private const string ResetUserDataArgument = "--reset-user-data";
    private const string MigrateUserDataArgument = "--migrate-user-data";
    private const string ApplicationMutexName =
        "HyperBatteryMonitor_8D7E0A6C9D5B4F0B9C3E5F5E9C2A1B71";

    [STAThread]
    static int Main(string[] args)
    {
        SettingsManager settingsManager = new();

        if (HasArgument(args, ResetUserDataArgument))
        {
            try
            {
                settingsManager.DeleteUserData();
                new StartupManager().MigrateLegacyRegistration();
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Could not delete Hyper Battery Monitor user data: {ex.Message}");
                return 1;
            }
        }

        if (HasArgument(args, MigrateUserDataArgument))
        {
            try
            {
                settingsManager.MigrateLegacyUserData();
                new StartupManager().MigrateLegacyRegistration();
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Could not migrate Hyper Battery Monitor user data: {ex.Message}");
                return 1;
            }
        }

        try
        {
            settingsManager.MigrateLegacyUserData();
            new StartupManager().MigrateLegacyRegistration();
        }
        catch (Exception ex)
        {
            // The installer invokes the explicit migration command and stops cleanup on
            // failure. A direct/MSIX launch keeps the legacy data untouched and allows
            // the application to continue rather than deleting the only existing copy.
            Debug.WriteLine(
                $"Could not migrate legacy Hyper Battery Monitor state: {ex.Message}");
        }

        using Mutex applicationMutex = new(
            initiallyOwned: false,
            ApplicationMutexName);

        ApplicationConfiguration.Initialize();
        Application.Run(
            new HyperXBatteryMonitorApplicationContext());

        return 0;
    }

    private static bool HasArgument(
        IEnumerable<string> args,
        string expectedArgument)
    {
        return args.Any(arg =>
            string.Equals(
                arg,
                expectedArgument,
                StringComparison.OrdinalIgnoreCase));
    }
}
