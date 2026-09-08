namespace HyperXBatteryTray.Settings;

public enum AppLanguage
{
    English,
    PortugueseBrazil,
    Spanish
}

public enum AppTheme
{
    Light,
    Dark
}

public static class Localization
{
    public static string Get(string key, AppLanguage language)
    {
        return language switch
        {
            AppLanguage.PortugueseBrazil => Portuguese.TryGetValue(key, out string? pt) ? pt : English[key],
            AppLanguage.Spanish => Spanish.TryGetValue(key, out string? es) ? es : English[key],
            _ => English.TryGetValue(key, out string? en) ? en : key
        };
    }

    public static string LanguageDisplay(AppLanguage language) => language switch
    {
        AppLanguage.PortugueseBrazil => "🇧🇷 Português (Brasil)",
        AppLanguage.Spanish => "🇪🇸 Español",
        _ => "🇺🇸 English"
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["WindowTitle"] = "Settings — HyperX Battery Tray",
        ["Device"] = "Device",
        ["DeviceLabel"] = "Device:",
        ["Interface"] = "Interface",
        ["Language"] = "Language:",
        ["Theme"] = "Theme:",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["BatteryDisplay"] = "Battery display in system tray",
        ["StaticIcon"] = "Keep static icon",
        ["ColoredIcon"] = "Color icon according to battery",
        ["IconAndBattery"] = "Icon + battery indicator",
        ["IconAndPercentage"] = "Icon + percentage",
        ["PercentageOnly"] = "Percentage only",
        ["BatteryColors"] = "Battery colors",
        ["Color"] = "Color",
        ["StartingAt"] = "Starting at",
        ["UseGradient"] = "Use gradient between colors",
        ["Transition"] = "Transition:",
        ["CriticalBattery"] = "Critical battery",
        ["BlinkCritical"] = "Blink the icon when battery is below the limit",
        ["BatteryLimit"] = "Battery limit:",
        ["Ok"] = "OK",
        ["Cancel"] = "Cancel",
        ["Apply"] = "Apply",
        ["RestoreDefaults"] = "Restore defaults",
        ["RestoreDefaultsQuestion"] = "Restore all settings to their default values?",
        ["InvalidSettings"] = "Invalid settings",
        ["ColorOrderError"] = "Color limits must be in descending order.\n\nExample:\nGreen > Yellow > Red.",
        ["SaveError"] = "Could not save settings.\n\n{0}",
        ["StartupError"] = "Could not change Windows startup settings.\n\n{0}",
        ["AboutTitle"] = "About",
        ["AboutVersion"] = "Version: v1.0 Beta",
        ["AboutCreatedBy"] = "Created by: David Santana (Dave Santana)",
        ["AboutSupport"] = "Buy me a coffee?",
        ["AboutBuyMeACoffee"] = "BuyMeACoffee",
        ["AboutPixEmail"] = "PIX:",
        ["AboutPixCopyPaste"] = "PIX (Copy & Paste):",
        ["AboutPixCopy"] = "Copy",
        ["AboutPixCopied"] = "PIX copy-and-paste key copied to clipboard.",
        ["AboutClose"] = "Close",
        ["TrayBattery"] = "Battery: {0}%",
        ["TrayBatteryNA"] = "Battery: N/A",
		["TrayCharging"] = "(Charging)",
        ["TrayConnected"] = "Status: Connected",
        ["TrayDisconnected"] = "Status: Disconnected",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Start with Windows",
        ["Settings"] = "Settings...",
        ["About"] = "About",
        ["Exit"] = "Exit"
    };

    private static readonly Dictionary<string, string> Portuguese = new()
    {
        ["WindowTitle"] = "Configurações — HyperX Battery Tray",
        ["Device"] = "Dispositivo",
        ["DeviceLabel"] = "Dispositivo:",
        ["Interface"] = "Interface",
        ["Language"] = "Idioma:",
        ["Theme"] = "Tema:",
        ["ThemeLight"] = "Claro",
        ["ThemeDark"] = "Escuro",
        ["BatteryDisplay"] = "Exibição da bateria no Systray",
        ["StaticIcon"] = "Manter ícone estático",
        ["ColoredIcon"] = "Ícone colorido conforme a bateria",
        ["IconAndBattery"] = "Ícone + indicador de bateria",
        ["IconAndPercentage"] = "Ícone + percentual",
        ["PercentageOnly"] = "Somente percentual",
        ["BatteryColors"] = "Cores da bateria",
        ["Color"] = "Cor",
        ["StartingAt"] = "A partir de",
        ["UseGradient"] = "Usar degradê entre as cores",
        ["Transition"] = "Transição:",
        ["CriticalBattery"] = "Bateria crítica",
        ["BlinkCritical"] = "Piscar o ícone quando a bateria estiver abaixo do limite",
        ["BatteryLimit"] = "Limite de bateria:",
        ["Ok"] = "OK",
        ["Cancel"] = "Cancelar",
        ["Apply"] = "Aplicar",
        ["RestoreDefaults"] = "Restaurar padrões",
        ["RestoreDefaultsQuestion"] = "Restaurar todas as configurações para os valores padrão?",
        ["InvalidSettings"] = "Configurações inválidas",
        ["ColorOrderError"] = "Os limites das cores devem estar em ordem decrescente.\n\nExemplo:\nVerde > Amarelo > Vermelho.",
        ["SaveError"] = "Não foi possível salvar as configurações.\n\n{0}",
        ["StartupError"] = "Não foi possível alterar a inicialização com o Windows.\n\n{0}",
        ["AboutTitle"] = "Sobre",
        ["AboutVersion"] = "Versão: v1.0 Beta",
        ["AboutCreatedBy"] = "Criado por: David Santana (Dave Santana)",
        ["AboutSupport"] = "Me paga um café?",
        ["AboutBuyMeACoffee"] = "BuyMeACoffee",
        ["AboutPixEmail"] = "PIX:",
        ["AboutPixCopyPaste"] = "PIX (Copia e Cola):",
        ["AboutPixCopy"] = "Copiar",
        ["AboutPixCopied"] = "Chave PIX Copia e Cola copiada para a área de transferência.",
        ["AboutClose"] = "Fechar",
        ["TrayBattery"] = "Bateria: {0}%",
        ["TrayBatteryNA"] = "Bateria: N/A",
		["TrayCharging"] = "(Carregando)",
        ["TrayConnected"] = "Status: Conectado",
        ["TrayDisconnected"] = "Status: Desconectado",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Iniciar com o Windows",
        ["Settings"] = "Configurações...",
        ["About"] = "Sobre",
        ["Exit"] = "Sair"
    };

    private static readonly Dictionary<string, string> Spanish = new()
    {
        ["WindowTitle"] = "Configuración — HyperX Battery Tray",
        ["Device"] = "Dispositivo",
        ["DeviceLabel"] = "Dispositivo:",
        ["Interface"] = "Interfaz",
        ["Language"] = "Idioma:",
        ["Theme"] = "Tema:",
        ["ThemeLight"] = "Claro",
        ["ThemeDark"] = "Oscuro",
        ["BatteryDisplay"] = "Visualización de la batería en la bandeja del sistema",
        ["StaticIcon"] = "Mantener icono estático",
        ["ColoredIcon"] = "Icono coloreado según la batería",
        ["IconAndBattery"] = "Icono + indicador de batería",
        ["IconAndPercentage"] = "Icono + porcentaje",
        ["PercentageOnly"] = "Solo porcentaje",
        ["BatteryColors"] = "Colores de la batería",
        ["Color"] = "Color",
        ["StartingAt"] = "A partir de",
        ["UseGradient"] = "Usar degradado entre los colores",
        ["Transition"] = "Transición:",
        ["CriticalBattery"] = "Batería crítica",
        ["BlinkCritical"] = "Parpadear el icono cuando la batería esté por debajo del límite",
        ["BatteryLimit"] = "Límite de batería:",
        ["Ok"] = "Aceptar",
        ["Cancel"] = "Cancelar",
        ["Apply"] = "Aplicar",
        ["RestoreDefaults"] = "Restaurar valores predeterminados",
        ["RestoreDefaultsQuestion"] = "¿Restaurar todos los ajustes a sus valores predeterminados?",
        ["InvalidSettings"] = "Configuración no válida",
        ["ColorOrderError"] = "Los límites de los colores deben estar en orden descendente.\n\nEjemplo:\nVerde > Amarillo > Rojo.",
        ["SaveError"] = "No se pudieron guardar los ajustes.\n\n{0}",
        ["StartupError"] = "No se pudo cambiar la configuración de inicio de Windows.\n\n{0}",
        ["AboutTitle"] = "Acerca de",
        ["AboutVersion"] = "Versión: v1.0 Beta",
        ["AboutCreatedBy"] = "Creado por: David Santana (Dave Santana)",
        ["AboutSupport"] = "¿Me invitas a un café?",
        ["AboutBuyMeACoffee"] = "BuyMeACoffee",
        ["AboutPixEmail"] = "PIX:",
        ["AboutPixCopyPaste"] = "PIX (Copiar y pegar):",
        ["AboutPixCopy"] = "Copiar",
        ["AboutPixCopied"] = "Clave PIX de copiar y pegar copiada al portapapeles.",
        ["AboutClose"] = "Cerrar",
        ["TrayBattery"] = "Batería: {0}%",
        ["TrayBatteryNA"] = "Batería: N/D",
		["TrayCharging"] = "(Cargando)",
        ["TrayConnected"] = "Estado: Conectado",
        ["TrayDisconnected"] = "Estado: Desconectado",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Iniciar con Windows",
        ["Settings"] = "Configuración...",
        ["About"] = "Acerca de",
        ["Exit"] = "Salir"
    };
}
