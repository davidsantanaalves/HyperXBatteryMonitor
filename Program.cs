using System.Diagnostics;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

internal static class Program
{
    private const string ResetUserDataArgument = "--reset-user-data";

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Any(arg =>
                string.Equals(
                    arg,
                    ResetUserDataArgument,
                    StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                new SettingsManager().DeleteUserData();
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Could not delete HyperX Battery Monitor user data: {ex.Message}");
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(
            new HyperXBatteryMonitorApplicationContext());

        return 0;
    }
}
