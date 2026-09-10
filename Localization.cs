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
    Dark,
    System
}

public static class Localization
{
    public static string Get(string key, AppLanguage language)
    {
        return language switch
        {
            AppLanguage.PortugueseBrazil => Portuguese.TryGetValue(key, out string? pt)
                ? pt
                : English.TryGetValue(key, out string? enPt) ? enPt : key,
            AppLanguage.Spanish => Spanish.TryGetValue(key, out string? es)
                ? es
                : English.TryGetValue(key, out string? enEs) ? enEs : key,
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
        ["InterfaceDescription"] = "Customize the appearance and behavior of the application.",
        ["Language"] = "Language:",
        ["Theme"] = "Theme:",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["ThemeSystem"] = "System",
        ["BatteryDisplay"] = "Battery display in system tray",
        ["StaticIcon"] = "Static",
        ["BatteryIndicatorMode"] = "Battery indicator",
        ["AdvancedDynamic"] = "Advanced dynamic",
        ["BatteryGradientMode"] = "Battery level gradient",
        ["AdvancedBatteryIndicatorMode"] = "Battery indicator",
        ["PercentageTextMode"] = "Text percentage",
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
        ["ChargingStatus"] = "Charging...",
        ["TrayConnected"] = "Status: Connected",
        ["TrayDisconnected"] = "Status: Disconnected",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Start with Windows",
        ["Settings"] = "Settings...",
        ["About"] = "About",
        ["Exit"] = "Exit",
        ["DeviceInformation"] = "Device information",
        ["DeviceInformationText"] = "Select a device to display the information.",
        ["DeviceLabelShort"] = "Device",
        ["DeviceDescription"] = "Select your HyperX device and view its current status.",
        ["LanguageShort"] = "Language",
        ["LanguageDescription"] = "Select the application language.",
        ["ThemeDescription"] = "Choose the application theme.",
        ["StartupDescription"] = "Launch HyperX Battery Tray automatically when Windows starts.",
        ["StartupShort"] = "Start with Windows",
        ["ThemeShort"] = "Theme",

        // Devices Information - HyperX Cloud III Wireless
        ["Cloud3Wireless_Connectivity"] = "Connectivity: 2.4 GHz wireless via USB dongle",
        ["Cloud3Wireless_Range"] = "Wireless Range: Up to 20 meters (65.6 feet)",
        ["Cloud3Wireless_Battery"] = "Battery Life: Up to 120 hours",
        ["Cloud3Wireless_ChargeTime"] = "Charge Time: Approximately 4.5 hours to full charge",

        // Devices Information - HyperX Cloud III S
        ["Cloud3S_Connectivity"] = "Connectivity: 2.4 GHz RF (via USB dongle) and Bluetooth 5.3",
        ["Cloud3S_Range"] = "Wireless Range: Up to 20 meters (65 feet)",
        ["Cloud3S_Battery"] = "Battery Life: Up to 120 hours on 2.4 GHz; up to 200 hours in Bluetooth",
        ["Cloud3S_ChargeTime"] = "Recharge Time: Approximately 5 hours",

        // Devices Information - HyperX Cloud 2 Core
        ["Cloud2Core_Connectivity"] = "Connection Type: 2.4GHz wireless via USB adapter",
        ["Cloud2Core_Range"] = "Wireless Range: Up to 20 meters",
        ["Cloud2Core_Battery"] = "Battery Life: Up to 80 hours",
        ["Cloud2Core_ChargeTime"] = "Recharge Time: 4.5 hours",

        // Devices Information - HyperX Cloud Alpha
        ["CloudAlpha_Connectivity"] = "Connectivity: 2.4 GHz RF via USB adapter",
        ["CloudAlpha_Range"] = "Wireless Range: Up to 20 meters",
        ["CloudAlpha_Battery"] = "Battery Life: Up to 300 hours",
        ["CloudAlpha_ChargeTime"] = "Charge Time: Approx. 4.5 hours",

        // Devices Information - HyperX Cloud Stinger 2
        ["CloudStinger2_Connectivity"] = "Connectivity: 2.4 GHz wireless via USB wireless adapter",
        ["CloudStinger2_Range"] = "Wireless Range: Up to 20 meters",
        ["CloudStinger2_Battery"] = "Battery Life: Up to 20 hours",
        ["CloudStinger2_ChargeTime"] = "Charge Time: Approx. 3.5 hours"
    };

    private static readonly Dictionary<string, string> Portuguese = new()
    {
        ["WindowTitle"] = "Configurações — HyperX Battery Tray",
        ["Device"] = "Dispositivo",
        ["DeviceLabel"] = "Dispositivo:",
        ["Interface"] = "Interface",
        ["InterfaceDescription"] = "Personalize a aparência e o comportamento do aplicativo.",
        ["Language"] = "Idioma:",
        ["Theme"] = "Tema:",
        ["ThemeLight"] = "Claro",
        ["ThemeDark"] = "Escuro",
        ["ThemeSystem"] = "Sistema",
        ["BatteryDisplay"] = "Exibição da bateria no Systray",
        ["StaticIcon"] = "Estático",
        ["BatteryIndicatorMode"] = "Indicador de bateria",
        ["AdvancedDynamic"] = "Dinâmico avançado",
        ["BatteryGradientMode"] = "Nível de bateria gradual",
        ["AdvancedBatteryIndicatorMode"] = "Indicador de Bateria",
        ["PercentageTextMode"] = "Texto Percentual",
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
		["ChargingStatus"] = "Carregando...",
        ["TrayConnected"] = "Status: Conectado",
        ["TrayDisconnected"] = "Status: Desconectado",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Iniciar com o Windows",
        ["Settings"] = "Configurações...",
        ["About"] = "Sobre",
        ["Exit"] = "Sair",
        ["DeviceInformation"] = "Informações do dispositivo",
        ["DeviceInformationText"] = "Selecione um dispositivo para exibir as informações.",
        ["DeviceLabelShort"] = "Dispositivo",
        ["DeviceDescription"] = "Selecione seu dispositivo HyperX e veja o status atual.",
        ["LanguageShort"] = "Idioma",
        ["LanguageDescription"] = "Selecione o idioma do aplicativo.",
        ["ThemeDescription"] = "Escolha o tema do aplicativo.",
        ["StartupDescription"] = "Inicie o HyperX Battery Tray automaticamente ao iniciar o Windows.",
        ["StartupShort"] = "Iniciar com o Windows",
        ["ThemeShort"] = "Tema",

        // Devices Information - HyperX Cloud III Wireless
        ["Cloud3Wireless_Connectivity"] = "Conectividade: Sem fio de 2.4 GHz via adaptador USB",
        ["Cloud3Wireless_Range"] = "Alcance Sem Fio: Até 20 metros",
        ["Cloud3Wireless_Battery"] = "Duração da Bateria: Até 120 horas",
        ["Cloud3Wireless_ChargeTime"] = "Tempo de Carga: Aproximadamente 4.5 horas para carga completa",

        // Devices Information - HyperX Cloud III S
        ["Cloud3S_Connectivity"] = "Conectividade: RF de 2.4 GHz (via adaptador USB) e Bluetooth 5.3",
        ["Cloud3S_Range"] = "Alcance Sem Fio: Até 20 metros",
        ["Cloud3S_Battery"] = "Duração da Bateria: Até 120 horas em 2.4 GHz; até 200 horas em Bluetooth",
        ["Cloud3S_ChargeTime"] = "Tempo de Recarga: Aproximadamente 5 horas",

        // Devices Information - HyperX Cloud 2 Core
        ["Cloud2Core_Connectivity"] = "Tipo de Conexão: Sem fio de 2.4 GHz via adaptador USB",
        ["Cloud2Core_Range"] = "Alcance Sem Fio: Até 20 metros",
        ["Cloud2Core_Battery"] = "Duração da Bateria: Até 80 horas",
        ["Cloud2Core_ChargeTime"] = "Tempo de Recarga: 4.5 horas",

        // Devices Information - HyperX Cloud Alpha
        ["CloudAlpha_Connectivity"] = "Conectividade: RF de 2.4 GHz via adaptador USB",
        ["CloudAlpha_Range"] = "Alcance Sem Fio: Até 20 metros",
        ["CloudAlpha_Battery"] = "Duração da Bateria: Até 300 horas",
        ["CloudAlpha_ChargeTime"] = "Tempo de Carga: Aprox. 4.5 horas",

        // Devices Information - HyperX Cloud Stinger 2
        ["CloudStinger2_Connectivity"] = "Conectividade: Sem fio de 2.4 GHz via adaptador sem fio USB",
        ["CloudStinger2_Range"] = "Alcance Sem Fio: Até 20 metros",
        ["CloudStinger2_Battery"] = "Duração da Bateria: Até 20 horas",
        ["CloudStinger2_ChargeTime"] = "Tempo de Carga: Aprox. 3.5 horas"
    };

    private static readonly Dictionary<string, string> Spanish = new()
    {
        ["WindowTitle"] = "Configuración — HyperX Battery Tray",
        ["Device"] = "Dispositivo",
        ["DeviceLabel"] = "Dispositivo:",
        ["Interface"] = "Interfaz",
        ["InterfaceDescription"] = "Personaliza la apariencia y el comportamiento de la aplicación.",
        ["Language"] = "Idioma:",
        ["Theme"] = "Tema:",
        ["ThemeLight"] = "Claro",
        ["ThemeDark"] = "Oscuro",
        ["ThemeSystem"] = "Sistema",
        ["BatteryDisplay"] = "Visualización de la batería en la bandeja del sistema",
        ["StaticIcon"] = "Estático",
        ["BatteryIndicatorMode"] = "Indicador de batería",
        ["AdvancedDynamic"] = "Dinámico avanzado",
        ["BatteryGradientMode"] = "Nivel de batería gradual",
        ["AdvancedBatteryIndicatorMode"] = "Indicador de batería",
        ["PercentageTextMode"] = "Texto porcentual",
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
        ["ChargingStatus"] = "Cargando...",
        ["TrayConnected"] = "Estado: Conectado",
        ["TrayDisconnected"] = "Estado: Desconectado",
        ["TrayTooltip"] = "HyperX Cloud III Wireless — {0}",
        ["Startup"] = "Iniciar con Windows",
        ["Settings"] = "Configuración...",
        ["About"] = "Acerca de",
        ["Exit"] = "Salir",
        ["DeviceInformation"] = "Información del dispositivo",
        ["DeviceInformationText"] = "Seleccione un dispositivo para visualizar la información.",
        ["DeviceLabelShort"] = "Dispositivo",
        ["DeviceDescription"] = "Seleccione su dispositivo HyperX y vea su estado actual.",
        ["LanguageShort"] = "Idioma",
        ["LanguageDescription"] = "Seleccione el idioma de la aplicación.",
        ["ThemeDescription"] = "Elija el tema de la aplicación.",
        ["StartupDescription"] = "Inicie HyperX Battery Tray automáticamente al iniciar Windows.",
        ["StartupShort"] = "Iniciar con Windows",
        ["ThemeShort"] = "Tema",

        // Devices Information - HyperX Cloud III Wireless
        ["Cloud3Wireless_Connectivity"] = "Conectividad: Inalámbrica de 2.4 GHz mediante adaptador USB",
        ["Cloud3Wireless_Range"] = "Alcance Inalámbrico: Hasta 20 metros",
        ["Cloud3Wireless_Battery"] = "Duración de la Batería: Hasta 120 horas",
        ["Cloud3Wireless_ChargeTime"] = "Tiempo de Carga: Aproximadamente 4.5 horas para carga completa",

        // Devices Information - HyperX Cloud III S
        ["Cloud3S_Connectivity"] = "Conectividad: RF de 2.4 GHz (vía adaptador USB) y Bluetooth 5.3",
        ["Cloud3S_Range"] = "Alcance Inalámbrico: Hasta 20 metros",
        ["Cloud3S_Battery"] = "Duración de la Batería: Hasta 120 horas en 2.4 GHz; hasta 200 horas en Bluetooth",
        ["Cloud3S_ChargeTime"] = "Tiempo de Recarga: Aproximadamente 5 horas",

        // Devices Information - HyperX Cloud 2 Core
        ["Cloud2Core_Connectivity"] = "Tipo de Conexión: Inalámbrica de 2.4 GHz mediante adaptador USB",
        ["Cloud2Core_Range"] = "Alcance Inalámbrico: Hasta 20 metros",
        ["Cloud2Core_Battery"] = "Duración de la Batería: Hasta 80 horas",
        ["Cloud2Core_ChargeTime"] = "Tiempo de Recarga: 4.5 horas",

        // Devices Information - HyperX Cloud Alpha
        ["CloudAlpha_Connectivity"] = "Conectividad: RF de 2.4 GHz mediante adaptador USB",
        ["CloudAlpha_Range"] = "Alcance Inalámbrico: Hasta 20 metros",
        ["CloudAlpha_Battery"] = "Duración de la Batería: Hasta 300 horas",
        ["CloudAlpha_ChargeTime"] = "Tiempo de Carga: Aprox. 4.5 horas",

        // Devices Information - HyperX Cloud Stinger 2
        ["CloudStinger2_Connectivity"] = "Conectividad: Inalámbrica de 2.4 GHz mediante adaptador USB",
        ["CloudStinger2_Range"] = "Alcance Inalámbrico: Hasta 20 metros",
        ["CloudStinger2_Battery"] = "Duración de la Batería: Hasta 20 horas",
        ["CloudStinger2_ChargeTime"] = "Tiempo de Carga: Aprox. 3.5 horas"
    };
}