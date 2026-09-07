using Microsoft.Win32;

namespace HyperXBatteryTray;

public sealed class StartupManager
{
    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string AppName =
        "HyperXBatteryTray";

    public bool IsEnabled()
    {
        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(RunKey);

        if (key == null)
            return false;

        object? value = key.GetValue(AppName);

        if (value is not string command)
            return false;

        string executablePath = Application.ExecutablePath;

        return !string.IsNullOrWhiteSpace(command) &&
               command.Equals(
                   $"\"{executablePath}\"",
                   StringComparison.OrdinalIgnoreCase);
    }

    public void Enable()
    {
        string executablePath = Application.ExecutablePath;

        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException(
                "Não foi possível determinar o caminho do executável.");

        if (!File.Exists(executablePath))
            throw new FileNotFoundException(
                "O executável da aplicação não foi encontrado.",
                executablePath);

        using RegistryKey key =
            Registry.CurrentUser.CreateSubKey(RunKey)
            ?? throw new InvalidOperationException(
                "Não foi possível acessar a chave de inicialização do Windows.");

        string command = $"\"{executablePath}\"";

        key.SetValue(
            AppName,
            command,
            RegistryValueKind.String);

        object? savedValue = key.GetValue(AppName);

        if (savedValue is not string savedCommand ||
            !savedCommand.Equals(
                command,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A configuração de inicialização não pôde ser confirmada no Registro do Windows.");
        }
    }

    public void Disable()
    {
        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                writable: true);

        key?.DeleteValue(
            AppName,
            throwOnMissingValue: false);
    }
}
