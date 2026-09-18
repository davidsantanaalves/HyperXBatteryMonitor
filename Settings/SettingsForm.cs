using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using HyperXBatteryTray.Devices;

namespace HyperXBatteryTray.Settings;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly StartupManager _startupManager;
    private readonly PngIconCache _iconCache;
    private DeviceSelector _deviceSelector = null!;
    private RoundedLanguageSelector _languageComboBox = null!;
    private ToggleSwitchControl _startupToggle = null!;
    private ThemeOptionControl _lightThemeOption = null!;
    private ThemeOptionControl _darkThemeOption = null!;
    private ThemeOptionControl _systemThemeOption = null!;
    private Label _deviceStatusLabel = null!;
    private Label _deviceStatusDescriptionLabel = null!;
    private Label _batteryValueLabel = null!;
    private Label _chargingLabel = null!;
    private Label _wirelessTechnologyValueLabel = null!;
    private Label _connectionMethodValueLabel = null!;
    private Label _wirelessRangeValueLabel = null!;
    private Label _batteryLifeValueLabel = null!;
    private Label _chargeTimeValueLabel = null!;
    private StatusDotControl _deviceStatusDot = null!;
    private BatteryIconControl _batteryIcon = null!;
    private readonly Panel _pageHost;
    private readonly Panel _sidebar;
    private Panel _footer = null!;
    private Button _resetButton = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;
    private Button _applyButton = null!;
    private Label _versionLabel = null!;
    private Label _applicationNameLabel = null!;
    private RoundedPanel _sidebarDeviceCard = null!;
    private PictureBox _sidebarDeviceImage = null!;
    private Label _sidebarDeviceNameLabel = null!;
    private Label _sidebarStatusTitleLabel = null!;
    private Label _sidebarStatusLabel = null!;
    private StatusDotControl _sidebarStatusDot = null!;
    private Label _sidebarBatteryTitleLabel = null!;
    private Label _sidebarBatteryLabel = null!;
    private Label _sidebarChargingLabel = null!;
    private BatteryIconControl _sidebarBatteryIcon = null!;
    private readonly Dictionary<string, SidebarItem> _navButtons = new();
    private IHyperXDevice? _device;
    private AppLanguage _selectedLanguage;
    private AppTheme _selectedTheme;
    private string _pendingSelectedDevice;
    private bool _pendingStartupEnabled;
    private bool _pendingNotifyOnLowBattery;
    private bool _pendingNotifyWhenFullyCharged;
    private bool _pendingBlinkOnCriticalBattery;
    private int _pendingCriticalBatteryPercent;
    private BatteryDisplayMode _pendingDisplayMode;
    private AdvancedDisplayMode _pendingAdvancedDisplayMode;
    private List<BatteryColorSettings> _pendingBatteryColors;
    private bool _pendingUseGradient;
    private int _pendingGradientPercent;
    private readonly List<(RoundedPanel Card, BatteryModeCard Radio)> _batteryModeCards = new();
    private ToggleSwitchControl _notifyOnLowBatteryToggle = null!;
    private ToggleSwitchControl _notifyWhenFullyChargedToggle = null!;
    private ToggleSwitchControl _blinkOnCriticalBatteryToggle = null!;
    private CriticalBatteryNumericControl _criticalBatteryPercentInput = null!;
    private bool _updatingLanguage;
    private bool _isCharging;
    private string _currentPage = "Device";

    private static readonly Color Accent = Color.FromArgb(0, 122, 255);
    private static readonly Color LightBackground = Color.FromArgb(250, 251, 253);
    private static readonly Color DarkBackground = Color.FromArgb(31, 34, 37);
    private static readonly Color LightSidebar = Color.FromArgb(247, 249, 252);
    private static readonly Color DarkSidebar = Color.FromArgb(36, 39, 42);
    private static readonly Color LightBorder = Color.FromArgb(222, 227, 234);
    private static readonly Color DarkBorder = Color.FromArgb(66, 71, 76);
    private static readonly Color LightText = Color.FromArgb(24, 32, 45);
    private static readonly Color LightSecondary = Color.FromArgb(82, 95, 115);
    private static readonly Color DarkSecondary = Color.FromArgb(196, 201, 207);

    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme(EffectiveTheme == AppTheme.Dark);
        _iconCache.ClearBitmaps();
        WarmUpIcons();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        _iconCache.ClearBitmaps();
        WarmUpIcons();
    }

    private void ApplyTitleBarTheme(bool dark)
    {
        if (!IsHandleCreated)
            return;

        try
        {
            int useDarkMode = dark ? 1 : 0;
            _ = DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
        }
        catch
        {
            // Keep the application functional on Windows versions without this DWM attribute.
        }
    }

    public event EventHandler? SettingsApplied;

    public SettingsForm(AppSettings settings, IHyperXDevice? device = null, bool isCharging = false)
    {
        _settings = settings;
        _device = device;
        _isCharging = isCharging;
        _startupManager = new StartupManager();
        _iconCache = new PngIconCache();
        _selectedLanguage = settings.Language;
        _selectedTheme = settings.Theme;
        _pendingSelectedDevice = settings.SelectedDevice;
        _pendingStartupEnabled = _startupManager.IsEnabled();
        _pendingNotifyOnLowBattery = settings.NotifyOnLowBattery;
        _pendingNotifyWhenFullyCharged = settings.NotifyWhenFullyCharged;
        _pendingBlinkOnCriticalBattery = settings.BlinkOnCriticalBattery;
        _pendingCriticalBatteryPercent = Math.Clamp(settings.CriticalBatteryPercent, 1, 100);
        _pendingDisplayMode = settings.DisplayMode;
        _pendingAdvancedDisplayMode = settings.AdvancedDisplayMode;
        _pendingBatteryColors = CloneBatteryColors(settings.BatteryColors);
        _pendingUseGradient = settings.UseGradient;
        _pendingGradientPercent = Math.Clamp(settings.GradientPercent, 0, 50);

        Text = L("WindowTitle");
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = true;
        ClientSize = new Size(760, 520);
        TopMost = true;
        DoubleBuffered = true;
        BackColor = LightBackground;
        Font = new Font("Segoe UI", 9.5f);
        Icon = LoadApplicationIcon();

        _sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 190,
            BackColor = LightSidebar,
            Padding = new Padding(10, 14, 10, 12)
        };
        _sidebar.Paint += Sidebar_Paint;

        _pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 22, 20, 0),
            BackColor = LightBackground
        };

        Controls.Add(_pageHost);
        Controls.Add(_sidebar);

        _sidebarDeviceCard = new RoundedPanel
        {
            Location = new Point(10, 272),
            Size = new Size(_sidebar.ClientSize.Width - 20, 170),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BorderColor = DarkBorder,
            OutsideBackColor = LightSidebar,
            BackColor = Color.FromArgb(248, 249, 251)
        };

        _sidebarDeviceImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(64, 64),
            Location = new Point((_sidebarDeviceCard.Width - 64) / 2, 6),
            BackColor = Color.Transparent
        };

        _sidebarDeviceNameLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebarDeviceCard.Width - 12, 20),
            Location = new Point(6, 69),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarStatusTitleLabel = new Label
        {
            AutoSize = false,
            Size = new Size(52, 20),
            Location = new Point(10, 96),
            Text = L("SidebarStatus"),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarStatusDot = new StatusDotControl
        {
            Size = new Size(18, 18),
            Location = new Point(62, 97)
        };

        _sidebarStatusLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebarDeviceCard.Width - 83, 20),
            Location = new Point(83, 96),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarBatteryTitleLabel = new Label
        {
            AutoSize = false,
            Size = new Size(52, 20),
            Location = new Point(10, 122),
            Text = L("SidebarBattery"),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarBatteryIcon = new BatteryIconControl
        {
            Size = new Size(32, 32),
            Location = new Point(62, 116),
            DarkMode = false
        };

        _sidebarBatteryLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebarDeviceCard.Width - 95, 20),
            Location = new Point(98, 122),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarChargingLabel = new Label
        {
            AutoSize = false,
            Size = new Size(96, 18),
            Location = new Point(47, 147),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent,
            Visible = false
        };

        _sidebarDeviceCard.Controls.Add(_sidebarDeviceImage);
        _sidebarDeviceCard.Controls.Add(_sidebarDeviceNameLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusTitleLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusDot);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryTitleLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryIcon);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarChargingLabel);
        _sidebar.Controls.Add(_sidebarDeviceCard);

        _applicationNameLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebar.ClientSize.Width - 20, 20),
            Text = Application.ProductName,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Location = new Point(10, ClientSize.Height - 49),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _versionLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebar.ClientSize.Width - 20, 18),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(10, ClientSize.Height - 27),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _sidebar.Controls.Add(_applicationNameLabel);
        _sidebar.Controls.Add(_versionLabel);

        BuildSidebar();
        ShowPage("Device");

        _resetButton = CreateFooterButton(L("RestoreDefaults"), false);
        _okButton = CreateFooterButton(L("Ok"), true);
        _cancelButton = CreateFooterButton(L("Cancel"), false);
        _applyButton = CreateFooterButton(L("Apply"), false);
        _resetButton.Click += ResetButton_Click;
        _okButton.Click += OkButton_Click;
        _cancelButton.Click += (_, _) => Close();
        _applyButton.Click += ApplyButton_Click;
        BuildFooter();

        ApplyTheme(_selectedTheme);
        PositionWindowAtTop();
        RefreshDeviceStatus();

        if (_device != null)
            _device.BatteryChanged += Device_BatteryChanged;
        FormClosed += SettingsForm_FormClosed;
    }

    internal void SetDevice(IHyperXDevice? device)
    {
        if (ReferenceEquals(_device, device))
        {
            RefreshDeviceStatus();
            return;
        }

        if (_device != null)
            _device.BatteryChanged -= Device_BatteryChanged;

        _device = device;

        if (_device != null)
            _device.BatteryChanged += Device_BatteryChanged;

        RefreshDeviceStatus();
    }

    internal void SetCharging(bool charging)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetCharging(charging)));
            return;
        }

        _isCharging = charging;
        RefreshDeviceStatus();
    }

    private void BuildSidebar()
    {
        string[] keys = { "Device", "Interface", "BatteryMonitor", "Notifications", "About" };
        Glyph[] glyphs = { Glyph.Headphones, Glyph.Monitor, Glyph.Battery, Glyph.Bell, Glyph.Info };
        string[] iconKeys =
        {
            "device",
            "interface",
            "battery_monitor",
            "notification",
            "about"
        };
        int y = 4;

        for (int i = 0; i < keys.Length; i++)
        {
            SidebarItem item = new SidebarItem(glyphs[i], iconKeys[i], _iconCache)
            {
                Text = L(keys[i]),
                Tag = keys[i],
                Location = new Point(6, y),
                Size = new Size(168, 40),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            item.Click += Navigation_Click;
            _sidebar.Controls.Add(item);
            _navButtons[keys[i]] = item;
            y += 46;
        }
    }

    private void BuildDevicePage()
    {
        AddDevicePageHeader();

        const int deviceGroupTop = 74;
        const int deviceCardGap = 10;
        Color deviceCardBackground = EffectiveTheme == AppTheme.Dark
            ? Color.FromArgb(42, 45, 48)
            : Color.FromArgb(248, 249, 251);

        RoundedPanel selectorCard = CreateDeviceCard(new Point(20, deviceGroupTop), new Size(525, 88));
        selectorCard.BackColor = deviceCardBackground;
        selectorCard.Tag = "device-card";
        _pageHost.Controls.Add(selectorCard);

        Label selectorLabel = CreateDeviceLabel(L("DeviceLabelShort"), true, new Point(20, 0), 9.5f);
        selectorCard.Controls.Add(selectorLabel);

        _deviceSelector = new DeviceSelector
        {
            Location = new Point(122, 18),
            Size = new Size(398, 56),
            SelectedIndex = 0,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _deviceSelector.SetPlaceholder(L("LocateDevice"));
        _deviceSelector.SelectedDeviceName = _pendingSelectedDevice;
        _deviceSelector.SelectionChanged += DeviceSelector_SelectionChanged;
        selectorCard.Controls.Add(_deviceSelector);
        selectorLabel.Location = new Point(
            selectorLabel.Left,
            _deviceSelector.Top + (_deviceSelector.Height - selectorLabel.Height) / 2);

        RoundedPanel connectionCard = CreateDeviceCard(new Point(20, deviceGroupTop + 88 + deviceCardGap), new Size(525, 138));
        connectionCard.BackColor = deviceCardBackground;
        connectionCard.Tag = "device-card";
        _pageHost.Controls.Add(connectionCard);
        AddDeviceSectionHeader(connectionCard, "connection", "Connection", "ConnectionDescription");

        AddDeviceInfoColumn(
            connectionCard, "usb", "WirelessTechnology", "Cloud3Wireless_WirelessTechnology",
            18, 58, 140, 0, 25, 32, 32, true);
        AddDeviceInfoColumn(
            connectionCard, "connection", "ConnectionMethod", "Cloud3Wireless_ConnectionMethod",
            188, 58, 164, 0, 25, 32, 40, true);
        AddDeviceInfoColumn(
            connectionCard, "location", "WirelessRange", "Cloud3Wireless_Range",
            378, 58, 130, 0, 25, 32, 40, true);
        AddDeviceColumnDivider(connectionCard, 176, 58, 64);
        AddDeviceColumnDivider(connectionCard, 364, 58, 64);

        RoundedPanel batteryCard = CreateDeviceCard(new Point(20, deviceGroupTop + 88 + deviceCardGap + 138 + deviceCardGap), new Size(525, 125));
        batteryCard.BackColor = deviceCardBackground;
        batteryCard.Tag = "device-card";
        _pageHost.Controls.Add(batteryCard);
        AddDeviceSectionHeader(batteryCard, "battery", "Battery", "BatteryDescription");

        AddDeviceInfoColumn(
            batteryCard, "clock", "BatteryLife", "Cloud3Wireless_Battery",
            22, 58, 175);
        AddDeviceInfoColumn(
            batteryCard, "energy", "ChargeTime", "Cloud3Wireless_ChargeTime",
            291, 58, 180);
        AddDeviceColumnDivider(batteryCard, 258, 58, 52);
    }

    private void AddDeviceSectionHeader(
        RoundedPanel card,
        string iconKey,
        string titleKey,
        string descriptionKey)
    {
        card.Controls.Add(new PngIconControl(_iconCache, iconKey)
        {
            Location = new Point(18, 15),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });

        card.Controls.Add(CreateDeviceLabel(
            L(titleKey), true, new Point(66, 12), 11f));

        card.Controls.Add(CreateDeviceLabel(
            L(descriptionKey), false, new Point(66, 34), 8.5f));
    }

    private void AddDeviceInfoColumn(
        RoundedPanel card,
        string iconKey,
        string labelKey,
        string valueKey,
        int left,
        int top,
        int valueWidth,
        int descriptionOffset = 40,
        int iconSize = 25,
        int valueTopOffset = 24,
        int titleOffset = 40,
        bool verticallyCenterTitle = false)
    {
        card.Controls.Add(new PngIconControl(_iconCache, iconKey)
        {
            Location = new Point(left, top),
            Size = new Size(iconSize, iconSize),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });

        int textLeft = left + titleOffset;
        Label titleLabel = CreateDeviceLabel(
            L(labelKey), true, new Point(textLeft, top), 8.8f);
        titleLabel.Location = new Point(
            textLeft,
            top + (iconSize - titleLabel.Height) / 2);
        card.Controls.Add(titleLabel);

        Label valueLabel = CreateDeviceLabel(
            L(valueKey), false, new Point(left + descriptionOffset, top + valueTopOffset), 8.8f);
        valueLabel.MaximumSize = new Size(valueWidth, 0);
        card.Controls.Add(valueLabel);

        SetDeviceValueLabel(labelKey, valueLabel);
    }

    private void AddDeviceColumnDivider(RoundedPanel card, int left, int top, int height)
    {
        card.Controls.Add(new Panel
        {
            Location = new Point(left, top),
            Size = new Size(1, height),
            BackColor = EffectiveTheme == AppTheme.Dark ? DarkBorder : LightBorder,
            Tag = "device-divider"
        });
    }

    private void SetDeviceValueLabel(string labelKey, Label valueLabel)
    {
        switch (labelKey)
        {
            case "WirelessTechnology":
                _wirelessTechnologyValueLabel = valueLabel;
                break;
            case "ConnectionMethod":
                _connectionMethodValueLabel = valueLabel;
                break;
            case "WirelessRange":
                _wirelessRangeValueLabel = valueLabel;
                break;
            case "BatteryLife":
                _batteryLifeValueLabel = valueLabel;
                break;
            case "ChargeTime":
                _chargeTimeValueLabel = valueLabel;
                break;
        }
    }

    private void ShowInterfacePage()
    {
        _pageHost.Controls.Clear();
        AddInterfacePageHeader();

        // Interface uses independent cards so future layout changes stay isolated
        // from the finalized Device page.
        RoundedPanel languageCard = CreateInterfaceCard(new Point(20, 70), new Size(528, 68), true);
        _pageHost.Controls.Add(languageCard);
        languageCard.Controls.Add(new PngIconControl(_iconCache, "language") { Location = new Point(18, 18), Size = new Size(25, 25), DarkMode = EffectiveTheme == AppTheme.Dark });
        languageCard.Controls.Add(CreateInterfaceLabel(L("LanguageShort"), true, new Point(58, 12), 9.5f));
        languageCard.Controls.Add(CreateInterfaceLabel(L("LanguageDescription"), false, new Point(58, 33), 8.5f));

        _languageComboBox = new RoundedLanguageSelector
        {
            Location = new Point(270, 15),
            Size = new Size(208, 34),
            Font = new Font("Segoe UI", 9f),
            ForeColor = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.English));
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.PortugueseBrazil));
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.Spanish));
        _languageComboBox.SelectedIndex = (int)_selectedLanguage;
        _languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
        languageCard.Controls.Add(_languageComboBox);

        RoundedPanel themeCard = CreateInterfaceCard(new Point(20, 150), new Size(528, 202), true);
        _pageHost.Controls.Add(themeCard);
        themeCard.Controls.Add(new PngIconControl(_iconCache, "theme") { Location = new Point(18, 19), Size = new Size(25, 25), DarkMode = EffectiveTheme == AppTheme.Dark });
        themeCard.Controls.Add(CreateInterfaceLabel(L("ThemeShort"), true, new Point(58, 12), 9.5f));
        themeCard.Controls.Add(CreateInterfaceLabel(L("ThemeDescription"), false, new Point(58, 33), 8.5f));

        Panel themePanel = new Panel
        {
            Location = new Point(16, 58),
            Size = new Size(496, 130),
            BackColor = Color.Transparent
        };
        _lightThemeOption = new ThemeOptionControl(AppTheme.Light, "light", ThemeText(AppTheme.Light), _iconCache)
        { Location = new Point(0, 0), Size = new Size(156, 130), Selected = _selectedTheme == AppTheme.Light, DarkMode = EffectiveTheme == AppTheme.Dark };
        _darkThemeOption = new ThemeOptionControl(AppTheme.Dark, "dark", ThemeText(AppTheme.Dark), _iconCache)
        { Location = new Point(166, 0), Size = new Size(156, 130), Selected = _selectedTheme == AppTheme.Dark, DarkMode = EffectiveTheme == AppTheme.Dark };
        _systemThemeOption = new ThemeOptionControl(AppTheme.System, "interface", ThemeText(AppTheme.System), _iconCache)
        { Location = new Point(332, 0), Size = new Size(156, 130), Selected = _selectedTheme == AppTheme.System, DarkMode = EffectiveTheme == AppTheme.Dark };
        _lightThemeOption.Click += ThemeOption_Click;
        _darkThemeOption.Click += ThemeOption_Click;
        _systemThemeOption.Click += ThemeOption_Click;
        themePanel.Controls.AddRange(new Control[] { _lightThemeOption, _darkThemeOption, _systemThemeOption });
        themeCard.Controls.Add(themePanel);

        RoundedPanel startupCard = CreateInterfaceCard(new Point(20, 364), new Size(528, 68), true);
        _pageHost.Controls.Add(startupCard);
        startupCard.Controls.Add(new PngIconControl(_iconCache, "windows") { Location = new Point(18, 19), Size = new Size(25, 25), DarkMode = EffectiveTheme == AppTheme.Dark });
        startupCard.Controls.Add(CreateInterfaceLabel(L("StartupShort"), true, new Point(58, 12), 9.5f));
        startupCard.Controls.Add(CreateInterfaceLabel(L("StartupDescription"), false, new Point(58, 34), 8.5f));
        _startupToggle = new ToggleSwitchControl
        {
            Location = new Point(444, 22),
            Size = new Size(42, 24),
            Checked = _pendingStartupEnabled,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _startupToggle.CheckedChanged += (_, _) => _pendingStartupEnabled = _startupToggle.Checked;
        startupCard.Controls.Add(_startupToggle);
    }

    private void ShowNotificationsPage()
    {
        _pageHost.Controls.Clear();
        AddPageHeader("notification", Glyph.Bell, "Notifications", "NotificationsDescription", 42);

        RoundedPanel lowBatteryCard = CreateCard(new Point(20, 97), new Size(528, 137), true);
        _pageHost.Controls.Add(lowBatteryCard);
        lowBatteryCard.Controls.Add(new PngIconControl(_iconCache, "battery_critical")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        lowBatteryCard.Controls.Add(CreateLabel(L("NotifyOnLowBattery"), true, new Point(68, 12), 9.5f));
        Label lowBatteryDescription = CreateLabel(L("NotifyOnLowBatteryDescription").TrimEnd('.'), false, new Point(68, 34), 8.5f);
        lowBatteryDescription.MaximumSize = new Size(280, 0);
        lowBatteryCard.Controls.Add(lowBatteryDescription);
        lowBatteryCard.Controls.Add(CreateLabel(L("CriticalBatteryLevel"), true, new Point(68, 96), 8.8f));

        _notifyOnLowBatteryToggle = new ToggleSwitchControl
        {
            Location = new Point(466, 18),
            Size = new Size(42, 24),
            Checked = _pendingNotifyOnLowBattery,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _notifyOnLowBatteryToggle.CheckedChanged += (_, _) => _pendingNotifyOnLowBattery = _notifyOnLowBatteryToggle.Checked;
        lowBatteryCard.Controls.Add(_notifyOnLowBatteryToggle);

        _criticalBatteryPercentInput = new CriticalBatteryNumericControl
        {
            Location = new Point(418, 88),
            Size = new Size(70, 34),
            Minimum = 1,
            Maximum = 100,
            Value = Math.Clamp(_pendingCriticalBatteryPercent, 1, 100),
            Increment = 1,
            Font = new Font("Segoe UI", 9.5f),
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _criticalBatteryPercentInput.ValueChanged += (_, _) => _pendingCriticalBatteryPercent = _criticalBatteryPercentInput.Value;
        lowBatteryCard.Controls.Add(_criticalBatteryPercentInput);
        lowBatteryCard.Controls.Add(CreateLabel("%", false, new Point(490, 97), 9f));

        RoundedPanel fullCard = CreateCard(new Point(20, 246), new Size(528, 82), true);
        _pageHost.Controls.Add(fullCard);
        fullCard.Controls.Add(new PngIconControl(_iconCache, "battery_full")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        fullCard.Controls.Add(CreateLabel(L("NotifyWhenFullyCharged"), true, new Point(68, 18), 9.5f));
        Label fullyChargedDescription = CreateLabel(L("NotifyWhenFullyChargedDescription").TrimEnd('.'), false, new Point(68, 40), 8.5f);
        fullyChargedDescription.MaximumSize = new Size(300, 0);
        fullCard.Controls.Add(fullyChargedDescription);
        _notifyWhenFullyChargedToggle = new ToggleSwitchControl
        {
            Location = new Point(466, 28),
            Size = new Size(42, 24),
            Checked = _pendingNotifyWhenFullyCharged,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _notifyWhenFullyChargedToggle.CheckedChanged += (_, _) => _pendingNotifyWhenFullyCharged = _notifyWhenFullyChargedToggle.Checked;
        fullCard.Controls.Add(_notifyWhenFullyChargedToggle);

        RoundedPanel blinkCard = CreateCard(new Point(20, 340), new Size(528, 82), true);
        _pageHost.Controls.Add(blinkCard);
        blinkCard.Controls.Add(new PngIconControl(_iconCache, "blink")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        blinkCard.Controls.Add(CreateLabel(L("FlashSystrayIcon"), true, new Point(68, 18), 9.5f));
        Label blinkDescription = CreateLabel(L("FlashSystrayIconDescription").TrimEnd('.'), false, new Point(68, 40), 8.5f);
        blinkDescription.MaximumSize = new Size(300, 0);
        blinkCard.Controls.Add(blinkDescription);
        _blinkOnCriticalBatteryToggle = new ToggleSwitchControl
        {
            Location = new Point(466, 28),
            Size = new Size(42, 24),
            Checked = _pendingBlinkOnCriticalBattery,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _blinkOnCriticalBatteryToggle.CheckedChanged += (_, _) => _pendingBlinkOnCriticalBattery = _blinkOnCriticalBatteryToggle.Checked;
        blinkCard.Controls.Add(_blinkOnCriticalBatteryToggle);
    }

    // Device layout must remain stable even when the Interface page is redesigned.
    private void AddDevicePageHeader() =>
        AddPageHeader("device", Glyph.Headphones, "Device", "DeviceDescription", 42);

    private RoundedPanel CreateDeviceCard(Point location, Size size, bool inner = false) =>
        CreateCard(location, size, inner);

    private Label CreateDeviceLabel(string text, bool semibold, Point location, float size) =>
        CreateLabel(text, semibold, location, size);

    // Interface has its own layout entry points so future Interface-only adjustments
    // do not require changing shared helpers used by finalized pages.
    private void AddInterfacePageHeader() =>
        AddPageHeader("interface", Glyph.Monitor, "Interface", "InterfaceDescription", 40);

    private RoundedPanel CreateInterfaceCard(Point location, Size size, bool inner = false) =>
        CreateCard(location, size, inner);

    private Label CreateInterfaceLabel(string text, bool semibold, Point location, float size) =>
        CreateLabel(text, semibold, location, size);

    private void AddPageHeader(string? iconKey, Glyph fallbackGlyph, string titleKey, string descriptionKey, int descriptionY = 37)
    {
        if (!string.IsNullOrWhiteSpace(iconKey))
            _pageHost.Controls.Add(new PngIconControl(_iconCache, iconKey) { Location = new Point(20, 3), Size = new Size(36, 36), DarkMode = EffectiveTheme == AppTheme.Dark });
        else
            _pageHost.Controls.Add(new GlyphControl(fallbackGlyph) { Location = new Point(20, 3), Size = new Size(38, 38) });

        _pageHost.Controls.Add(new Label { Text = L(titleKey), AutoSize = true, Font = new Font("Segoe UI Semibold", 23f), Location = new Point(70, 0), BackColor = Color.Transparent });
        _pageHost.Controls.Add(new Label { Text = L(descriptionKey), AutoSize = true, Font = new Font("Segoe UI", 9.5f), Location = new Point(71, descriptionY), BackColor = Color.Transparent });
    }

    private void AddPageHeader(Glyph glyph, string titleKey, string descriptionKey) =>
        AddPageHeader(null, glyph, titleKey, descriptionKey);

    private RoundedPanel CreateCard(Point location, Size size, bool inner = false)
    {
        return new RoundedPanel
        {
            Location = location,
            Size = size,
            Tag = inner ? "inner" : "outer",
            BorderColor = EffectiveTheme == AppTheme.Dark ? DarkBorder : LightBorder,
            OutsideBackColor = inner
                ? (EffectiveTheme == AppTheme.Dark ? Color.FromArgb(34, 37, 40) : Color.White)
                : (EffectiveTheme == AppTheme.Dark ? Color.FromArgb(32, 35, 38) : LightBackground),
            BackColor = inner
                ? (EffectiveTheme == AppTheme.Dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251))
                : (EffectiveTheme == AppTheme.Dark ? Color.FromArgb(34, 37, 40) : Color.White)
        };
    }

    private Label CreateLabel(string text, bool semibold, Point location, float size)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Location = location,
            Font = new Font(semibold ? "Segoe UI Semibold" : "Segoe UI", size),
            BackColor = Color.Transparent
        };
    }

    private void BuildFooter()
    {
        _footer = new Panel
        {
            Location = new Point(_sidebar.Width, ClientSize.Height - 62),
            Size = new Size(ClientSize.Width - _sidebar.Width, 62),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            BackColor = LightBackground
        };
        _footer.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen pen = new(EffectiveTheme == AppTheme.Dark ? DarkBorder : LightBorder, 1f);
            e.Graphics.DrawLine(pen, 0, 0, _footer.Width, 0);
        };

        _footer.Controls.Add(_resetButton);
        _footer.Controls.Add(_okButton);
        _footer.Controls.Add(_cancelButton);
        _footer.Controls.Add(_applyButton);
        Controls.Add(_footer);
        _footer.BringToFront();
        Resize += (_, _) => PositionFooterButtons();
        PositionFooterButtons();
    }

    private void PositionFooterButtons()
    {
        if (_footer == null) return;
        _footer.Location = new Point(_sidebar.Width, ClientSize.Height - _footer.Height);
        _footer.Width = Math.Max(0, ClientSize.Width - _sidebar.Width);
        _resetButton.Location = new Point(20, 13);
        _applyButton.Location = new Point(_footer.ClientSize.Width - _applyButton.Width - 20, 13);
        _cancelButton.Location = new Point(_applyButton.Left - _cancelButton.Width - 10, 13);
        _okButton.Location = new Point(_cancelButton.Left - _okButton.Width - 10, 13);
    }

    private Button CreateFooterButton(string text, bool primary)
    {
        return new ActionButton(_iconCache)
        {
            Text = text,
            Size = new Size(primary ? 84 : (text == L("RestoreDefaults") ? 178 : 92), 36),
            Font = new Font("Segoe UI", 9f),
            Primary = primary,
            ShowResetIcon = !primary && text == L("RestoreDefaults")
        };
    }

    private void Navigation_Click(object? sender, EventArgs e)
    {
        if (sender is not SidebarItem item || item.Tag is not string key)
            return;
        ShowPage(key);
    }

    private void ShowPage(string key)
    {
        _currentPage = key;
        _languageComboBox?.ClosePopup();
        foreach ((string name, SidebarItem item) in _navButtons)
            item.Selected = name == key;

        _pageHost.SuspendLayout();
        _pageHost.Visible = false;
        try
        {
            if (key == "Device")
            {
                RebuildDevicePage();
                return;
            }
            if (key == "Interface")
            {
                ShowInterfacePage();
                return;
            }
            if (key == "BatteryMonitor")
            {
                ShowBatteryMonitorPage();
                return;
            }
            if (key == "Notifications")
            {
                ShowNotificationsPage();
                return;
            }
            if (key == "About")
            {
                ShowAboutPage();
                return;
            }

            _pageHost.Controls.Clear();
            AddPageHeader(key switch
            {
                "BatteryMonitor" => "battery_monitor",
                "Notifications" => "notification",
                "About" => "about",
                _ => "about"
            }, Glyph.Info, key, key switch
            {
                "BatteryMonitor" => "ComingSoonBatteryMonitor",
                "Notifications" => "ComingSoonNotifications",
                "About" => "ComingSoonAbout",
                _ => string.Empty
            });
        }
        finally
        {
            // The host is hidden only while the page is rebuilt. Always restore it
            // so navigation cannot leave the entire content area invisible.
            _pageHost.Visible = true;
            _pageHost.ResumeLayout(true);
            _pageHost.Invalidate(true);
        }
    }

    private void RebuildDevicePage()
    {
        _pageHost.Controls.Clear();
        BuildDevicePage();
        RefreshDeviceStatus();
    }

    private void ShowBatteryMonitorPage()
    {
        _pageHost.Controls.Clear();
        _batteryModeCards.Clear();
        AddPageHeader("battery_monitor", Glyph.Battery, "BatteryMonitorTitle", "BatteryMonitorDescription", 42);

        const int cardLeft = 20;
        const int cardWidth = 528;
        const int cardGap = 9;

        RoundedPanel staticCard = CreateCard(new Point(cardLeft, 74), new Size(cardWidth, 104), true);
        staticCard.Tag = "battery-monitor-card";
        _pageHost.Controls.Add(staticCard);
        BuildBatteryModeCard(
            staticCard,
            BatteryDisplayMode.StaticIcon,
            L("BatteryMonitorStaticTitle"),
            L("BatteryMonitorStaticDescription"),
            new[]
            {
                new BatteryPreviewItem(BatteryPreviewKind.Normal, Color.WhiteSmoke, L("BatteryPreviewNormal"), showTile: true),
                new BatteryPreviewItem(BatteryPreviewKind.Charging, Color.WhiteSmoke, L("BatteryPreviewCharging"), showTile: true)
            });

        RoundedPanel dynamicCard = CreateCard(new Point(cardLeft, 74 + 104 + cardGap), new Size(cardWidth, 124), true);
        dynamicCard.Tag = "battery-monitor-card";
        _pageHost.Controls.Add(dynamicCard);
        BuildBatteryModeCard(
            dynamicCard,
            BatteryDisplayMode.BatteryIndicator,
            L("BatteryMonitorDynamicTitle"),
            L("BatteryMonitorDynamicDescription"),
            new[]
            {
                new BatteryPreviewItem(BatteryPreviewKind.Level, Color.Empty, "≥ 50%", "green"),
                new BatteryPreviewItem(BatteryPreviewKind.Level, Color.Empty, "49 – 30%", "yellow"),
                new BatteryPreviewItem(BatteryPreviewKind.Level, Color.Empty, "29 – 15%", "orange"),
                new BatteryPreviewItem(BatteryPreviewKind.Level, Color.Empty, "< 15%", "red"),
                new BatteryPreviewItem(BatteryPreviewKind.Charging, Color.WhiteSmoke, L("BatteryPreviewCharging"))
            });

        RoundedPanel customCard = CreateCard(new Point(cardLeft, 74 + 104 + cardGap + 124 + cardGap), new Size(cardWidth, 124), true);
        customCard.Tag = "battery-monitor-card";
        _pageHost.Controls.Add(customCard);
        List<BatteryColorSettings> customColors = _pendingBatteryColors
            .OrderByDescending(c => c.MinimumPercent)
            .Take(3)
            .ToList();

        IReadOnlyList<BatteryPreviewItem> customPreviews = customColors.Count >= 3
            ? new BatteryPreviewItem[]
            {
                new BatteryPreviewItem(
                    BatteryPreviewKind.Solid,
                    customColors[0].Color,
                    $">= {customColors[0].MinimumPercent}%"),
                new BatteryPreviewItem(
                    BatteryPreviewKind.Solid,
                    customColors[1].Color,
                    $"{Math.Max(customColors[0].MinimumPercent - 1, customColors[1].MinimumPercent)} – {customColors[1].MinimumPercent}%"),
                new BatteryPreviewItem(
                    BatteryPreviewKind.Solid,
                    customColors[2].Color,
                    $"{Math.Max(customColors[1].MinimumPercent - 1, customColors[2].MinimumPercent)} – {customColors[2].MinimumPercent}%"),
                new BatteryPreviewItem(
                    BatteryPreviewKind.Charging,
                    Color.WhiteSmoke,
                    L("BatteryPreviewCharging"))
            }
            : new BatteryPreviewItem[]
            {
                new BatteryPreviewItem(
                    BatteryPreviewKind.Charging,
                    Color.WhiteSmoke,
                    L("BatteryPreviewCharging"))
            };

        BuildBatteryModeCard(
            customCard,
            BatteryDisplayMode.Advanced,
            L("BatteryMonitorCustomTitle"),
            L("BatteryMonitorCustomDescription"),
            customPreviews,
            showCustomizeButton: true);

        UpdateBatteryMonitorModeCards();
    }

    private void BuildBatteryModeCard(
        RoundedPanel card,
        BatteryDisplayMode mode,
        string title,
        string description,
        IReadOnlyList<BatteryPreviewItem> previews,
        bool showCustomizeButton = false)
    {
        BatteryModeCard modeSelector = new BatteryModeCard
        {
            Location = new Point(16, Math.Max(0, (card.Height - 30) / 2)),
            Size = new Size(30, 30),
            Selected = IsBatteryModeSelected(mode),
            DarkMode = EffectiveTheme == AppTheme.Dark,
            Cursor = Cursors.Hand
        };
        modeSelector.Click += (_, _) => SelectBatteryDisplayMode(mode);
        card.Controls.Add(modeSelector);
        _batteryModeCards.Add((card, modeSelector));

        Label titleLabel = CreateLabel(title, true, new Point(68, 12), 10.5f);
        titleLabel.Cursor = Cursors.Hand;
        titleLabel.Click += (_, _) => SelectBatteryDisplayMode(mode);
        card.Controls.Add(titleLabel);

        Label descriptionLabel = CreateLabel(description, false, new Point(68, 36), 8.8f);
        descriptionLabel.MaximumSize = new Size(showCustomizeButton || previews.Count > 2 ? 444 : 235, 0);
        descriptionLabel.Cursor = Cursors.Hand;
        descriptionLabel.Click += (_, _) => SelectBatteryDisplayMode(mode);
        card.Controls.Add(descriptionLabel);

        int previewStartX = showCustomizeButton ? 58 : (previews.Count <= 2 ? 318 : 58);
        int previewY = showCustomizeButton ? 60 : (previews.Count <= 2 ? 10 : 55);
        int previewWidth = previews.Count <= 2 ? 94 : (showCustomizeButton ? 80 : 82);
        int previewGap = previews.Count <= 2 ? 10 : (showCustomizeButton ? 4 : 2);

        for (int i = 0; i < previews.Count; i++)
        {
            BatteryPreviewItem preview = previews[i];
            preview.DarkMode = EffectiveTheme == AppTheme.Dark;
            preview.Location = new Point(previewStartX + i * (previewWidth + previewGap), previewY);
            preview.Size = new Size(previewWidth, previews.Count <= 2 ? 84 : (showCustomizeButton ? 60 : 64));
            preview.Click += (_, _) => SelectBatteryDisplayMode(mode);
            card.Controls.Add(preview);
        }

        if (showCustomizeButton)
        {
            AboutActionButton customizeButton = new AboutActionButton(_iconCache)
            {
                Text = L("BatteryMonitorCustomize"),
                Location = new Point(407, 65),
                Size = new Size(113, 36),
                Font = new Font("Segoe UI", 9f),
                DarkMode = EffectiveTheme == AppTheme.Dark,
                CustomIconPath = GetBatteryMonitorThemeIconPath(EffectiveTheme == AppTheme.Dark),
                OutsideBackColor = EffectiveTheme == AppTheme.Dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251),
                Cursor = Cursors.Hand
            };
            customizeButton.Click += (_, _) =>
            {
                SelectBatteryDisplayMode(mode);

                using CustomizeDynamicIconColorsDialog dialog = new(
                    _iconCache,
                    EffectiveTheme == AppTheme.Dark,
                    _selectedLanguage,
                    _pendingBatteryColors,
                    _pendingUseGradient,
                    _pendingGradientPercent);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                _pendingBatteryColors = CloneBatteryColors(dialog.BatteryColors);
                _pendingUseGradient = dialog.UseGradient;
                _pendingGradientPercent = dialog.GradientPercent;
                ShowPage(_currentPage);
            };
            card.Controls.Add(customizeButton);
        }
    }

    private bool IsBatteryModeSelected(BatteryDisplayMode mode) => _pendingDisplayMode == mode;

    private static string GetBatteryMonitorThemeIconPath(bool dark)
    {
        string themeFolder = dark ? "Dark" : "Light";
        string themeName = dark ? "dark" : "light";
        return Path.Combine(
            AppContext.BaseDirectory,
            "Icons",
            themeFolder,
            $"theme-{themeName}-25x25.png");
    }

    private void SelectBatteryDisplayMode(BatteryDisplayMode mode)
    {
        _pendingDisplayMode = mode;
        if (mode == BatteryDisplayMode.Advanced)
            _pendingAdvancedDisplayMode = AdvancedDisplayMode.BatteryGradient;

        // Selecting a card changes only the existing persisted display mode.
        // The tray icon rendering/threshold logic remains untouched.

        UpdateBatteryMonitorModeCards();
    }

    private void UpdateBatteryMonitorModeCards()
    {
        if (_batteryModeCards.Count == 0)
            return;

        bool dark = EffectiveTheme == AppTheme.Dark;
        foreach ((RoundedPanel card, BatteryModeCard radio) in _batteryModeCards)
        {
            radio.DarkMode = dark;
            radio.Selected = false;
            card.BorderColor = dark ? DarkBorder : LightBorder;
            card.BackColor = dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251);
            card.OutsideBackColor = dark ? Color.FromArgb(34, 37, 40) : Color.White;
            card.Invalidate();
        }

        BatteryDisplayMode[] modes =
        {
            BatteryDisplayMode.StaticIcon,
            BatteryDisplayMode.BatteryIndicator,
            BatteryDisplayMode.Advanced
        };

        for (int i = 0; i < Math.Min(modes.Length, _batteryModeCards.Count); i++)
        {
            bool selected = _pendingDisplayMode == modes[i];
            _batteryModeCards[i].Radio.Selected = selected;
            _batteryModeCards[i].Card.BorderColor = selected ? Accent : (dark ? DarkBorder : LightBorder);
            _batteryModeCards[i].Card.Invalidate();
        }
    }

    private void ShowAboutPage()
    {
        _pageHost.Controls.Clear();

        bool dark = EffectiveTheme == AppTheme.Dark;
        Color foreground = dark ? Color.WhiteSmoke : LightText;
        Color secondary = dark ? DarkSecondary : LightSecondary;

        PictureBox logo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(64, 64),
            Location = new Point(20, 20),
            BackColor = Color.Transparent
        };

        try
        {
            string logoPath = Path.Combine(
                AppContext.BaseDirectory,
                "Icons",
                "All",
                "hxbm-logo-64x64.png");

            if (File.Exists(logoPath))
            {
                using Image source = Image.FromFile(logoPath);
                logo.Image = new Bitmap(source);
            }
        }
        catch
        {
            // Keep the About page functional if the optional logo asset cannot be loaded.
        }

        _pageHost.Controls.Add(logo);

        _pageHost.Controls.Add(new Label
        {
            Text = "HyperX Battery Monitor",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15f),
            ForeColor = foreground,
            Location = new Point(100, 20),
            BackColor = Color.Transparent
        });

        _pageHost.Controls.Add(new Label
        {
            Text = string.Format(L("AboutVersion"), Application.ProductVersion.Split('+')[0]),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = secondary,
            Location = new Point(101, 47),
            BackColor = Color.Transparent
        });

        _pageHost.Controls.Add(new Label
        {
            Text = L("AboutTagline"),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = foreground,
            Location = new Point(101, 66),
            BackColor = Color.Transparent
        });

        Label description = new Label
        {
            Text = L("AboutDescription"),
            AutoSize = false,
            Size = new Size(520, 38),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = secondary,
            Location = new Point(20, 97),
            BackColor = Color.Transparent
        };
        _pageHost.Controls.Add(description);

        AboutActionButton githubButton = CreateAboutActionButton(L("AboutGitHub"), AboutActionIcon.GitHub, dark, 20, 146, 118);
        githubButton.Click += (_, _) => OpenExternalUrl("https://github.com/davidsantanaalves/HyperXBatteryTray");

        AboutActionButton supportButton = CreateAboutActionButton(L("AboutSupportButton"), AboutActionIcon.Support, dark, 150, 146, 118);
        supportButton.Click += (_, _) => OpenExternalUrl("https://buymeacoffee.com/davesantana");

        AboutActionButton documentationButton = CreateAboutActionButton(L("AboutDocumentation"), AboutActionIcon.Documentation, dark, 280, 146, 138);
        documentationButton.Click += (_, _) => OpenExternalUrl("https://github.com/davidsantanaalves/HyperXBatteryTray#readme");

        AboutActionButton hyperXButton = CreateAboutActionButton(L("AboutHyperX"), AboutActionIcon.External, dark, 430, 146, 118);
        hyperXButton.Click += (_, _) => OpenExternalUrl("https://hyperx.com/");

        _pageHost.Controls.Add(githubButton);
        _pageHost.Controls.Add(supportButton);
        _pageHost.Controls.Add(documentationButton);
        _pageHost.Controls.Add(hyperXButton);

        Panel separator = new Panel
        {
            Location = new Point(20, 198),
            Size = new Size(528, 1),
            BackColor = dark ? DarkBorder : LightBorder
        };
        _pageHost.Controls.Add(separator);

        PngIconControl legalIcon = new(_iconCache, "legal")
        {
            Location = new Point(20, 212),
            Size = new Size(25, 25),
            DarkMode = dark
        };
        _pageHost.Controls.Add(legalIcon);

        _pageHost.Controls.Add(new Label
        {
            Text = L("AboutLegalTitle"),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = foreground,
            Location = new Point(50, 210),
            BackColor = Color.Transparent
        });

        Label legalText = new Label
        {
            Text = L("AboutLegalText"),
            AutoSize = false,
            Size = new Size(498, 42),
            Font = new Font("Segoe UI", 8f),
            ForeColor = secondary,
            Location = new Point(50, 230),
            BackColor = Color.Transparent
        };
        _pageHost.Controls.Add(legalText);

        LinkLabel licensesLink = new LinkLabel
        {
            Text = L("AboutThirdPartyLicenses"),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(50, 270),
            BackColor = Color.Transparent,
            LinkColor = dark ? Accent : Color.FromArgb(0, 102, 204),
            ActiveLinkColor = dark ? Accent : Color.FromArgb(0, 102, 204),
            VisitedLinkColor = dark ? Accent : Color.FromArgb(0, 102, 204)
        };
        licensesLink.LinkClicked += (_, _) => OpenExternalUrl("https://github.com/davidsantanaalves/HyperXBatteryMonitor/blob/main/LICENSE");
        _pageHost.Controls.Add(licensesLink);

        RoundedPanel acknowledgementsCard = new RoundedPanel
        {
            Location = new Point(20, 307),
            Size = new Size(528, 135),
            BorderColor = dark ? DarkBorder : LightBorder,
            OutsideBackColor = dark ? Color.FromArgb(34, 37, 40) : Color.White,
            BackColor = dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251)
        };
        _pageHost.Controls.Add(acknowledgementsCard);

        Label acknowledgementsTitle = new Label
        {
            Text = L("AboutAcknowledgementsTitle"),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = foreground,
            Location = new Point(48, 15),
            BackColor = Color.Transparent
        };
        acknowledgementsCard.Controls.Add(acknowledgementsTitle);

        PngIconControl acknowledgementsIcon = new(_iconCache, "code")
        {
            Size = new Size(25, 25),
            DarkMode = dark
        };
        acknowledgementsIcon.Location = new Point(
            18,
            acknowledgementsTitle.Top + (acknowledgementsTitle.Height - acknowledgementsIcon.Height) / 2);
        acknowledgementsCard.Controls.Add(acknowledgementsIcon);

        Label acknowledgementsText = new Label
        {
            Text = L("AboutAcknowledgementsText"),
            AutoSize = false,
            Size = new Size(455, 40),
            Font = new Font("Segoe UI", 8f),
            ForeColor = secondary,
            Location = new Point(48, 39),
            BackColor = Color.Transparent
        };
        acknowledgementsCard.Controls.Add(acknowledgementsText);

        acknowledgementsCard.Controls.Add(new Label
        {
            Text = L("AboutAcknowledgementsThanks"),
            AutoSize = true,
            Font = new Font("Segoe UI", 8f),
            ForeColor = secondary,
            Location = new Point(48, 80),
            BackColor = Color.Transparent
        });

        PngIconControl acknowledgementsGithubIcon = new(_iconCache, "git")
        {
            Size = new Size(25, 25),
            DarkMode = dark
        };

        LinkLabel acknowledgementsRepositoryLink = new LinkLabel
        {
            Text = L("AboutAcknowledgementsRepository"),
            AutoSize = false,
            Size = new Size(430, 24),
            Font = new Font("Segoe UI", 8f),
            Location = new Point(78, 104),
            BackColor = Color.Transparent,
            LinkColor = dark ? Accent : Color.FromArgb(0, 102, 204),
            ActiveLinkColor = dark ? Accent : Color.FromArgb(0, 102, 204),
            VisitedLinkColor = dark ? Accent : Color.FromArgb(0, 102, 204),
            AutoEllipsis = true
        };
        acknowledgementsGithubIcon.Location = new Point(
            48,
            acknowledgementsRepositoryLink.Top + (acknowledgementsRepositoryLink.Height - acknowledgementsGithubIcon.Height) / 2);
        acknowledgementsCard.Controls.Add(acknowledgementsGithubIcon);

        acknowledgementsRepositoryLink.LinkClicked += (_, _) => OpenExternalUrl(L("AboutAcknowledgementsRepository"));
        acknowledgementsCard.Controls.Add(acknowledgementsRepositoryLink);
    }

    private AboutActionButton CreateAboutActionButton(string text, AboutActionIcon icon, bool dark, int x, int y, int width)
    {
        return new AboutActionButton(_iconCache)
        {
            Text = text,
            Icon = icon,
            DarkMode = dark,
            OutsideBackColor = dark ? DarkBackground : LightBackground,
            Location = new Point(x, y),
            Size = new Size(width, 38),
            Font = new Font("Segoe UI", 8f)
        };
    }

    private static void OpenExternalUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch failures; the About page remains usable.
        }
    }

    private static void OpenLocalFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch failures; the About page remains usable.
        }
    }

    private void DeviceSelector_SelectionChanged(object? sender, EventArgs e)
    {
        _pendingSelectedDevice = _deviceSelector.SelectedDeviceName;
        RefreshDeviceStatus();
    }

    private void RefreshDeviceStatus()
    {
        bool selected = _deviceSelector.SelectedIndex > 0;
        bool connected = selected && _device?.IsConnected == true && _device.Battery >= 0 && _device.Battery <= 100;

        // The main Device page no longer displays the legacy status card.
        // Keep these updates conditional so device selection/status refreshes
        // continue to update the sidebar and information cards without
        // dereferencing controls that are not created on the new layout.
        if (_deviceStatusLabel != null)
        {
            _deviceStatusLabel.Text = connected ? L("Connected") : selected ? L("Disconnected") : L("UnknownHeadphones");
            _deviceStatusLabel.Visible = true;
        }
        if (_deviceStatusDescriptionLabel != null)
        {
            _deviceStatusDescriptionLabel.Text = connected ? L("ConnectedDescription") : selected ? L("DisconnectedDescription") : L("UnknownDeviceDescription");
            _deviceStatusDescriptionLabel.Visible = true;
        }
        if (_batteryValueLabel != null)
            _batteryValueLabel.Text = connected ? $"{Math.Clamp(_device!.Battery, 0, 100)}%" : L("BatteryNA");
        if (_chargingLabel != null)
        {
            _chargingLabel.Text = _isCharging ? L("ChargingStatus") : string.Empty;
            _chargingLabel.Visible = connected && _isCharging;
        }

        Color primaryText = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        Color secondaryText = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
        if (_deviceStatusLabel != null) _deviceStatusLabel.ForeColor = primaryText;
        if (_deviceStatusDescriptionLabel != null) _deviceStatusDescriptionLabel.ForeColor = secondaryText;
        if (_batteryValueLabel != null) _batteryValueLabel.ForeColor = primaryText;
        if (_chargingLabel != null) _chargingLabel.ForeColor = secondaryText;
        if (_batteryIcon != null)
        {
            _batteryIcon.Connected = connected;
            _batteryIcon.Battery = connected && _device != null ? Math.Clamp(_device.Battery, 0, 100) : 0;
            _batteryIcon.DarkMode = EffectiveTheme == AppTheme.Dark;
            _batteryIcon.Charging = connected && _isCharging;
            _batteryIcon.Invalidate();
        }
        if (_deviceStatusDot != null)
        {
            _deviceStatusDot.Connected = connected;
            _deviceStatusDot.Invalidate();
        }

        UpdateSidebarDeviceStatus(selected, connected);
        UpdateDeviceInformation();
    }

    private void UpdateSidebarDeviceStatus(bool selected, bool connected)
    {
        if (_sidebarDeviceCard == null)
            return;

        string deviceName = _deviceSelector?.SelectedDeviceName ?? string.Empty;
        string normalized = string.Equals(deviceName, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III"
            : deviceName;

        string imageFile = normalized switch
        {
            "HyperX Cloud III" => "cloud3.png",
            "HyperX Cloud III S" => "cloud3.png",
            "HyperX Cloud 2 Core" => "cloud2core.png",
            "HyperX Cloud Alpha" => "cloudalpha.png",
            "HyperX Cloud Stinger 2" => "cloudstinger2.png",
            _ => "unknown-device.png"
        };

        _sidebarDeviceNameLabel.Text = selected ? normalized : L("UnknownHeadphones");
        _sidebarStatusLabel.Text = connected
            ? L("Connected")
            : selected ? L("Disconnected") : L("SidebarUnknown");
        _sidebarStatusDot.Connected = connected;
        _sidebarStatusDot.Invalidate();

        _sidebarBatteryIcon.Connected = connected;
        _sidebarBatteryIcon.Battery = connected && _device != null ? Math.Clamp(_device.Battery, 0, 100) : 0;
        _sidebarBatteryIcon.Charging = connected && _isCharging;
        _sidebarBatteryIcon.DarkMode = EffectiveTheme == AppTheme.Dark;
        _sidebarBatteryIcon.Invalidate();

        _sidebarBatteryLabel.Text = connected
            ? $"{Math.Clamp(_device!.Battery, 0, 100)}%"
            : L("BatteryNA");
        _sidebarChargingLabel.Text = _isCharging && connected ? L("ChargingStatus") : string.Empty;
        _sidebarChargingLabel.Visible = connected && _isCharging;

        Color foreground = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        Color secondary = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
        _sidebarDeviceNameLabel.ForeColor = foreground;
        _sidebarStatusTitleLabel.ForeColor = secondary;
        _sidebarStatusLabel.ForeColor = secondary;
        _sidebarBatteryTitleLabel.ForeColor = secondary;
        _sidebarBatteryLabel.ForeColor = foreground;
        _sidebarChargingLabel.ForeColor = secondary;

        _sidebarDeviceImage.Image?.Dispose();
        _sidebarDeviceImage.Image = null;

        if (!string.IsNullOrWhiteSpace(imageFile))
        {
            string imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Devices", imageFile);
            if (File.Exists(imagePath))
            {
                try
                {
                    using Image source = Image.FromFile(imagePath);
                    _sidebarDeviceImage.Image = new Bitmap(source);
                }
                catch
                {
                    _sidebarDeviceImage.Image = null;
                }
            }
        }

        _sidebarDeviceImage.Visible = _sidebarDeviceImage.Image != null;
    }

    private void UpdateDeviceInformation()
    {
        if (_wirelessTechnologyValueLabel == null) return;

        string device = _deviceSelector?.SelectedDeviceName ?? string.Empty;
        string normalized = string.Equals(device, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III"
            : device;

        string[] values = normalized switch
        {
            "HyperX Cloud III" => new[]
            {
                L("Cloud3Wireless_WirelessTechnology"),
                L("Cloud3Wireless_ConnectionMethod"),
                L("Cloud3Wireless_Range"),
                L("Cloud3Wireless_Battery"),
                L("Cloud3Wireless_ChargeTime")
            },
            "HyperX Cloud III S" => new[]
            {
                L("Cloud3S_WirelessTechnology"),
                L("Cloud3S_ConnectionMethod"),
                L("Cloud3S_Range"),
                L("Cloud3S_Battery"),
                L("Cloud3S_ChargeTime")
            },
            "HyperX Cloud 2 Core" => new[]
            {
                L("Cloud2Core_WirelessTechnology"),
                L("Cloud2Core_ConnectionMethod"),
                L("Cloud2Core_Range"),
                L("Cloud2Core_Battery"),
                L("Cloud2Core_ChargeTime")
            },
            "HyperX Cloud Alpha" => new[]
            {
                L("CloudAlpha_WirelessTechnology"),
                L("CloudAlpha_ConnectionMethod"),
                L("CloudAlpha_Range"),
                L("CloudAlpha_Battery"),
                L("CloudAlpha_ChargeTime")
            },
            "HyperX Cloud Stinger 2" => new[]
            {
                L("CloudStinger2_WirelessTechnology"),
                L("CloudStinger2_ConnectionMethod"),
                L("CloudStinger2_Range"),
                L("CloudStinger2_Battery"),
                L("CloudStinger2_ChargeTime")
            },
            _ => new[]
            {
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA"),
                L("DeviceInformationNA")
            }
        };

        _wirelessTechnologyValueLabel.Text = values[0];
        _connectionMethodValueLabel.Text = values[1];
        _wirelessRangeValueLabel.Text = values[2];
        _batteryLifeValueLabel.Text = values[3];
        _chargeTimeValueLabel.Text = values[4];

        Color valueColor = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        _wirelessTechnologyValueLabel.ForeColor = valueColor;
        _connectionMethodValueLabel.ForeColor = valueColor;
        _wirelessRangeValueLabel.ForeColor = valueColor;
        _batteryLifeValueLabel.ForeColor = valueColor;
        _chargeTimeValueLabel.ForeColor = valueColor;
    }

    private void Device_BatteryChanged(object? sender, int battery)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefreshDeviceStatus));
            return;
        }
        RefreshDeviceStatus();
    }

    private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingLanguage || _languageComboBox.SelectedIndex < 0) return;
        _selectedLanguage = (AppLanguage)_languageComboBox.SelectedIndex;
        ApplyLocalizedText();
        ShowPage(_currentPage);
        ApplyTheme(_selectedTheme);
    }

    private void ThemeOption_Click(object? sender, EventArgs e)
    {
        if (sender is not ThemeOptionControl option) return;
        _selectedTheme = option.Theme;
        _lightThemeOption.Selected = _selectedTheme == AppTheme.Light;
        _darkThemeOption.Selected = _selectedTheme == AppTheme.Dark;
        _systemThemeOption.Selected = _selectedTheme == AppTheme.System;
        ApplyTheme(_selectedTheme);
    }

    private void ApplyLocalizedText()
    {
        Text = L("WindowTitle");
        _resetButton.Text = L("RestoreDefaults");
        _okButton.Text = L("Ok");
        _cancelButton.Text = L("Cancel");
        _applyButton.Text = L("Apply");
        foreach ((string key, SidebarItem item) in _navButtons)
            item.Text = L(key);
        if (_sidebarStatusTitleLabel != null)
            _sidebarStatusTitleLabel.Text = L("SidebarStatus");
        if (_sidebarBatteryTitleLabel != null)
            _sidebarBatteryTitleLabel.Text = L("SidebarBattery");
        if (_deviceSelector != null)
            _deviceSelector.SetPlaceholder(L("LocateDevice"));
        UpdateDeviceInformation();

        if (_lightThemeOption != null) _lightThemeOption.LabelText = ThemeText(AppTheme.Light);
        if (_darkThemeOption != null) _darkThemeOption.LabelText = ThemeText(AppTheme.Dark);
        if (_systemThemeOption != null) _systemThemeOption.LabelText = ThemeText(AppTheme.System);
        if (_startupToggle != null) _startupToggle.Invalidate();

        if (_languageComboBox != null && _languageComboBox.Items.Count == 3)
        {
            _updatingLanguage = true;
            _languageComboBox.Items[0] = Localization.LanguageDisplay(AppLanguage.English);
            _languageComboBox.Items[1] = Localization.LanguageDisplay(AppLanguage.PortugueseBrazil);
            _languageComboBox.Items[2] = Localization.LanguageDisplay(AppLanguage.Spanish);
            _updatingLanguage = false;
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        // Theme changes restyle existing controls only. Keep the page host visible
        // and suspend layout until all properties have been updated. Hiding it here
        // can leave the entire content area invisible during navigation/theme events.
        _pageHost.SuspendLayout();
        try
        {
        bool dark = ResolveTheme(theme) == AppTheme.Dark;
        Color background = dark ? DarkBackground : LightBackground;
        Color sidebar = dark ? DarkSidebar : LightSidebar;
        Color foreground = dark ? Color.WhiteSmoke : LightText;
        Color secondary = dark ? DarkSecondary : LightSecondary;

        BackColor = background;
        ApplyTitleBarTheme(dark);
        _sidebar.BackColor = sidebar;
        _pageHost.BackColor = background;
        _pageHost.ForeColor = foreground;
        if (_footer != null) _footer.BackColor = background;
        _versionLabel.ForeColor = secondary;
        _applicationNameLabel.ForeColor = foreground;
        _versionLabel.Text = "v" + Application.ProductVersion.Split('+')[0];

        Icon = LoadApplicationIcon();
        WarmUpIcons();
        ApplyThemeRecursive(this, foreground, dark);
        ApplyDeviceCardTheme(dark);

        if (_sidebarDeviceCard != null)
        {
            _sidebarDeviceCard.BackColor = dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251);
            _sidebarDeviceCard.BorderColor = dark ? DarkBorder : LightBorder;
            _sidebarDeviceCard.OutsideBackColor = sidebar;
        }

        if (_lightThemeOption != null) _lightThemeOption.DarkMode = dark;
        if (_darkThemeOption != null) _darkThemeOption.DarkMode = dark;
        if (_systemThemeOption != null) _systemThemeOption.DarkMode = dark;
        if (_startupToggle != null) _startupToggle.DarkMode = dark;
        if (_notifyOnLowBatteryToggle != null) _notifyOnLowBatteryToggle.DarkMode = dark;
        if (_notifyWhenFullyChargedToggle != null) _notifyWhenFullyChargedToggle.DarkMode = dark;
        if (_blinkOnCriticalBatteryToggle != null) _blinkOnCriticalBatteryToggle.DarkMode = dark;
        if (_criticalBatteryPercentInput != null)
            _criticalBatteryPercentInput.DarkMode = dark;
        UpdateBatteryMonitorModeCards();

        foreach (SidebarItem item in _navButtons.Values)
        {
            item.DarkMode = dark;
            item.Invalidate();
        }

        if (_sidebarDeviceCard != null)
        {
            _sidebarDeviceCard.BackColor = dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251);
            _sidebarDeviceCard.BorderColor = dark ? DarkBorder : LightBorder;
            _sidebarDeviceCard.OutsideBackColor = sidebar;
        }
        if (_sidebarDeviceNameLabel != null)
            _sidebarDeviceNameLabel.ForeColor = foreground;
        if (_sidebarStatusTitleLabel != null)
            _sidebarStatusTitleLabel.ForeColor = secondary;
        if (_sidebarStatusLabel != null)
            _sidebarStatusLabel.ForeColor = secondary;
        if (_sidebarBatteryTitleLabel != null)
            _sidebarBatteryTitleLabel.ForeColor = secondary;
        if (_sidebarBatteryLabel != null)
            _sidebarBatteryLabel.ForeColor = foreground;
        if (_sidebarBatteryIcon != null)
        {
            _sidebarBatteryIcon.DarkMode = dark;
            _sidebarBatteryIcon.Invalidate();
        }
        UpdateSidebarDeviceStatus(
            _deviceSelector != null && _deviceSelector.SelectedIndex > 0,
            _device != null && _device.IsConnected && _device.Battery >= 0 && _device.Battery <= 100);

        if (_deviceStatusLabel != null)
        {
            _deviceStatusLabel.ForeColor = foreground;
            _deviceStatusLabel.BackColor = Color.Transparent;
        }
        if (_deviceStatusDescriptionLabel != null)
        {
            _deviceStatusDescriptionLabel.ForeColor = secondary;
            _deviceStatusDescriptionLabel.BackColor = Color.Transparent;
        }
        if (_batteryValueLabel != null)
        {
            _batteryValueLabel.ForeColor = foreground;
            _batteryValueLabel.BackColor = Color.Transparent;
        }
        if (_chargingLabel != null)
        {
            _chargingLabel.ForeColor = secondary;
            _chargingLabel.BackColor = Color.Transparent;
        }

        if (_deviceSelector != null)
        {
            _deviceSelector.DarkMode = dark;
            _deviceSelector.Invalidate();
        }

        StyleFooterButton(_resetButton, dark, false);
        StyleFooterButton(_cancelButton, dark, false);
        StyleFooterButton(_applyButton, dark, false);
        StyleFooterButton(_okButton, dark, true);
        _sidebar.Invalidate();
        _pageHost.Invalidate(true);
        UpdateDeviceInformation();
        RefreshDeviceStatus();
        }
        finally
        {
            _pageHost.ResumeLayout(true);
            _pageHost.Invalidate(true);
        }
    }

    private void ApplyDeviceCardTheme(bool dark)
    {
        Color cardBackground = dark
            ? Color.FromArgb(42, 45, 48)
            : Color.FromArgb(248, 249, 251);
        Color outsideBackground = dark ? DarkBackground : LightBackground;

        foreach (Control control in _pageHost.Controls)
        {
            if (control is not RoundedPanel panel || panel.Tag is not string tag || tag != "device-card")
                continue;

            panel.BackColor = cardBackground;
            panel.OutsideBackColor = outsideBackground;
            panel.BorderColor = dark ? DarkBorder : LightBorder;
            panel.Invalidate();
        }
    }

    private void ApplyThemeRecursive(Control parent, Color foreground, bool dark)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is PngIconControl pngIcon)
            {
                pngIcon.DarkMode = dark;
                continue;
            }

            if (c is BatteryPreviewItem previewItem)
            {
                previewItem.DarkMode = dark;
                continue;
            }

            if (c is BatteryModeCard modeCard)
            {
                modeCard.DarkMode = dark;
                continue;
            }

            if (c is SidebarItem || c is Button || c is DeviceSelector || c is StatusDotControl || c is BatteryIconControl || c == _footer)
                continue;

            if (c.Tag is string tag && tag == "device-divider")
            {
                c.BackColor = dark ? DarkBorder : LightBorder;
                continue;
            }

            c.ForeColor = foreground;

            if (c is RoundedPanel panel)
            {
                bool isDeviceCard = panel.Tag is string cardTag && cardTag == "device-card";
                panel.BackColor = isDeviceCard
                    ? (dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251))
                    : panel.Tag is string s && s == "outer"
                        ? (dark ? Color.FromArgb(34, 37, 40) : Color.White)
                        : (dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251));
                panel.BorderColor = dark ? DarkBorder : LightBorder;
                panel.OutsideBackColor = isDeviceCard
                    ? (dark ? Color.FromArgb(32, 35, 38) : LightBackground)
                    : panel.Tag is string innerTag && innerTag == "inner"
                        ? (dark ? Color.FromArgb(34, 37, 40) : Color.White)
                        : (dark ? Color.FromArgb(32, 35, 38) : LightBackground);
            }
            else if (c is RoundedLanguageSelector languageSelector)
            {
                languageSelector.DarkMode = dark;
                languageSelector.ForeColor = foreground;
            }
            else if (c is ComboBox combo)
            {
                combo.BackColor = dark ? Color.FromArgb(38, 41, 44) : Color.White;
                combo.ForeColor = foreground;
            }
            else if (c is RadioButton || c is CheckBox || c is Label)
            {
                c.BackColor = Color.Transparent;
            }
            else if (c is Panel panel2)
            {
                panel2.BackColor = Color.Transparent;
            }

            ApplyThemeRecursive(c, foreground, dark);
        }
    }

    private void StyleFooterButton(Button button, bool dark, bool primary)
    {
        if (button is ActionButton actionButton)
        {
            actionButton.DarkMode = dark;
            actionButton.Primary = primary;
            actionButton.OutsideBackColor = dark ? Color.FromArgb(32, 35, 38) : LightBackground;
            actionButton.Invalidate();
            return;
        }

        button.BackColor = primary ? Accent : (dark ? Color.FromArgb(38, 41, 44) : Color.White);
        button.ForeColor = primary ? Color.White : (dark ? Color.WhiteSmoke : LightText);
    }

    private void ComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool dark = EffectiveTheme == AppTheme.Dark;
        e.DrawBackground();
        using Brush brush = new SolidBrush(dark ? Color.WhiteSmoke : LightText);
        Font font = e.Font ?? Control.DefaultFont;
        TextRenderer.DrawText(e.Graphics, _languageComboBox.Items[e.Index]?.ToString() ?? string.Empty, font, e.Bounds, ((SolidBrush)brush).Color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        e.DrawFocusRectangle();
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        using RestoreDefaultsDialog dialog = new(
            L("RestoreDefaults"),
            L("RestoreDefaultsQuestion"),
            L("Yes"),
            L("No"),
            EffectiveTheme == AppTheme.Dark);

        DialogResult result = dialog.ShowDialog(this);
        if (result != DialogResult.Yes) return;

        AppSettings defaults = AppSettings.CreateDefault();
        _pendingSelectedDevice = defaults.SelectedDevice;
        _pendingStartupEnabled = _startupManager.IsEnabled();
        _pendingNotifyOnLowBattery = defaults.NotifyOnLowBattery;
        _pendingNotifyWhenFullyCharged = defaults.NotifyWhenFullyCharged;
        _pendingBlinkOnCriticalBattery = defaults.BlinkOnCriticalBattery;
        _pendingCriticalBatteryPercent = defaults.CriticalBatteryPercent;
        _pendingDisplayMode = defaults.DisplayMode;
        _pendingAdvancedDisplayMode = defaults.AdvancedDisplayMode;
        _pendingBatteryColors = CloneBatteryColors(defaults.BatteryColors);
        _pendingUseGradient = defaults.UseGradient;
        _pendingGradientPercent = defaults.GradientPercent;
        _selectedLanguage = defaults.Language;
        _selectedTheme = defaults.Theme;

        if (_deviceSelector != null) _deviceSelector.SelectedIndex = 0;
        if (_languageComboBox != null) _languageComboBox.SelectedIndex = (int)_selectedLanguage;
        if (_lightThemeOption != null) _lightThemeOption.Selected = false;
        if (_darkThemeOption != null) _darkThemeOption.Selected = false;
        if (_systemThemeOption != null) _systemThemeOption.Selected = true;

        ApplyLocalizedText();
        ShowPage(_currentPage);
        ApplyTheme(_selectedTheme);
        RefreshDeviceStatus();
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (ApplySettings()) Close();
    }

    private void ApplyButton_Click(object? sender, EventArgs e) => ApplySettings();

    private static List<BatteryColorSettings> CloneBatteryColors(IEnumerable<BatteryColorSettings>? colors)
    {
        return (colors ?? Enumerable.Empty<BatteryColorSettings>())
            .Where(c => c != null)
            .Select(c => new BatteryColorSettings
            {
                Name = c.Name,
                MinimumPercent = Math.Clamp(c.MinimumPercent, 0, 100),
                Argb = c.Argb
            })
            .ToList();
    }

    private bool ApplySettings()
    {
        _settings.SelectedDevice = _pendingSelectedDevice;
        _settings.DisplayMode = _pendingDisplayMode;
        _settings.AdvancedDisplayMode = _pendingAdvancedDisplayMode;
        _settings.BatteryColors = CloneBatteryColors(_pendingBatteryColors);
        _settings.UseGradient = _pendingUseGradient;
        _settings.GradientPercent = Math.Clamp(_pendingGradientPercent, 0, 50);
        _settings.NotifyOnLowBattery = _pendingNotifyOnLowBattery;
        _settings.NotifyWhenFullyCharged = _pendingNotifyWhenFullyCharged;
        _settings.BlinkOnCriticalBattery = _pendingBlinkOnCriticalBattery;
        _settings.CriticalBatteryPercent = Math.Clamp(_pendingCriticalBatteryPercent, 1, 100);
        _settings.Language = _selectedLanguage;
        _settings.Theme = _selectedTheme;
        _settings.ThemeConfigured = true;
        try
        {
            if (_pendingStartupEnabled) _startupManager.Enable();
            else _startupManager.Disable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(L("StartupError"), ex.Message), "HyperX Battery Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        SettingsApplied?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void SettingsForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_device != null) _device.BatteryChanged -= Device_BatteryChanged;
        _iconCache.Dispose();
    }

    private void WarmUpIcons()
    {
        _iconCache.Prewarm(new[]
        {
            new PngIconRequest("device", 25),
            new PngIconRequest("interface", 25),
            new PngIconRequest("battery_monitor", 25),
            new PngIconRequest("notification", 25),
            new PngIconRequest("about", 25),
            new PngIconRequest("device", 36),
            new PngIconRequest("interface", 36),
            new PngIconRequest("battery_monitor", 36),
            new PngIconRequest("notification", 36),
            new PngIconRequest("about", 36),
            new PngIconRequest("language", 25),
            new PngIconRequest("theme", 25),
            new PngIconRequest("windows", 25),
            new PngIconRequest("light", 36),
            new PngIconRequest("dark", 36),
            new PngIconRequest("interface", 36),
            new PngIconRequest("reset", 20),
            new PngIconRequest("git", 25),
            new PngIconRequest("support", 25),
            new PngIconRequest("documentation", 25),
            new PngIconRequest("external", 25),
            new PngIconRequest("legal", 25)
        }, EffectiveTheme == AppTheme.Dark, DeviceDpi);
    }

    private string ThemeText(AppTheme theme) => theme switch
    {
        AppTheme.Dark => L("ThemeDark"),
        AppTheme.System => L("ThemeSystem"),
        _ => L("ThemeLight")
    };

    private string L(string key) => Localization.Get(key, _selectedLanguage);
    private AppTheme EffectiveTheme => ResolveTheme(_selectedTheme);

    private static AppTheme ResolveTheme(AppTheme theme)
    {
        if (theme != AppTheme.System) return theme;
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is int i) return i == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch { }
        return AppTheme.Light;
    }

    private static Icon? LoadApplicationIcon()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Icons", "All", "hxbm-logo.ico");
        if (!File.Exists(path)) return null;
        using FileStream stream = File.OpenRead(path);
        return new Icon(stream);
    }

    private static Icon? LoadThemeIcon(AppTheme theme)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Icons", theme == AppTheme.Dark ? "Dark" : "Light", theme == AppTheme.Dark ? "dark.ico" : "light.ico");
        if (!File.Exists(path)) return null;
        using FileStream stream = File.OpenRead(path);
        return new Icon(stream);
    }

    private void PositionWindowAtTop()
    {
        if (Owner != null)
        {
            StartPosition = FormStartPosition.CenterParent;
            return;
        }
        Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(workArea.Left + Math.Max(0, (workArea.Width - Width) / 2), workArea.Top + Math.Max(0, (workArea.Height - Height) / 2));
    }

    private void Sidebar_Paint(object? sender, PaintEventArgs e)
    {
        using Pen pen = new(EffectiveTheme == AppTheme.Dark ? Color.FromArgb(55, 59, 63) : Color.FromArgb(229, 233, 239));
        e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
        e.Graphics.DrawLine(pen, 16, 236, _sidebar.Width - 16, 236);
        e.Graphics.DrawLine(pen, 16, 458, _sidebar.Width - 16, 458);
    }

    private enum Glyph { Headphones, Monitor, Battery, Bell, Gear, Info, Globe, Palette, Windows, Document }

    private sealed class SidebarItem : Control
    {
        private readonly Glyph _glyph;
        private readonly string? _iconKey;
        private readonly PngIconCache _iconCache;
        private bool _hover;
        private bool _selected;
        private bool _dark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        public SidebarItem(Glyph glyph, string? iconKey, PngIconCache iconCache)
        {
            _glyph = glyph;
            _iconKey = iconKey;
            _iconCache = iconCache;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = LightText;
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color text = _dark ? Color.WhiteSmoke : Color.FromArgb(33, 45, 64);
            Color selectedBack = _dark ? Color.FromArgb(30, 66, 99) : Color.FromArgb(224, 238, 255);
            Color hoverBack = _dark ? Color.FromArgb(43, 47, 51) : Color.FromArgb(241, 245, 250);
            if (_selected || _hover)
            {
                using Brush b = new SolidBrush(_selected ? selectedBack : hoverBack);
                e.Graphics.FillRoundedRectangle(b, new Rectangle(0, 0, Width - 1, Height - 1), 7);
            }
            if (_selected)
            {
                using Brush accent = new SolidBrush(Accent);
                e.Graphics.FillRoundedRectangle(accent, new Rectangle(0, 6, 3, Height - 12), 2);
            }

            Color iconColor = text;
            if (!string.IsNullOrWhiteSpace(_iconKey))
                _iconCache.Draw(e.Graphics, _iconKey, new RectangleF(14, 7, 25, 25), _dark, DeviceDpi);
            else
                DrawGlyph(e.Graphics, _glyph, new Rectangle(15, 8, 23, 23), iconColor, 1.65f);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Width - 54, Height), text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }

    private sealed class RoundedLanguageSelector : UserControl
    {
        private bool _darkMode;
        private LanguagePopupControl? _popup;
        private LanguagePopupMessageFilter? _popupMessageFilter;
        private int _selectedIndex = -1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                if (_darkMode == value) return;
                _darkMode = value;
                _popup?.ApplyTheme(_darkMode);
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<string> Items { get; } = new List<string>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int normalized = value >= 0 && value < Items.Count ? value : -1;
                if (_selectedIndex == normalized) return;
                _selectedIndex = normalized;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

        public event EventHandler? SelectedIndexChanged;

        public RoundedLanguageSelector()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Padding = new Padding(0);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = _darkMode ? Color.FromArgb(31, 34, 37) : Color.White;
            Color border = _darkMode ? Color.FromArgb(105, 112, 120) : Color.FromArgb(194, 201, 211);
            Color foreground = _darkMode ? Color.WhiteSmoke : LightText;

            Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using GraphicsPath path = RoundedPath(bounds, 6);
            using SolidBrush backgroundBrush = new(background);
            using Pen borderPen = new(border, 1f);
            e.Graphics.FillPath(backgroundBrush, path);
            e.Graphics.DrawPath(borderPen, path);

            string text = SelectedItem ?? string.Empty;
            Rectangle textBounds = new(11, 1, Math.Max(1, Width - 43), Math.Max(1, Height - 2));
            TextRenderer.DrawText(e.Graphics, text, Font, textBounds, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            int centerX = Width - 13;
            int centerY = Height / 2;
            using SolidBrush arrowBrush = new(foreground);
            Point[] arrow =
            {
                new(centerX - 4, centerY - 2),
                new(centerX + 4, centerY - 2),
                new(centerX, centerY + 3)
            };
            e.Graphics.FillPolygon(arrowBrush, arrow);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            if (_popup != null)
                ClosePopup();
            else
                OpenPopup();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                if (_popup != null)
                    ClosePopup();
                else
                    OpenPopup();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Down)
            {
                SelectedIndex = Math.Min(Items.Count - 1, Math.Max(0, _selectedIndex + 1));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                SelectedIndex = Math.Max(0, _selectedIndex - 1);
                e.Handled = true;
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (_popup == null) return;

            BeginInvoke(new Action(() =>
            {
                if (_popup != null && !_popup.ContainsFocus && !ContainsFocus)
                    ClosePopup();
            }));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_popup != null)
                ClosePopup();
            Invalidate();
        }

        private void OpenPopup()
        {
            Form? form = FindForm();
            if (form == null || Items.Count == 0) return;

            _popup = new LanguagePopupControl
            {
                Items = Items.ToArray(),
                SelectedIndex = _selectedIndex,
                Font = Font,
                DarkMode = _darkMode,
                Size = new Size(Width, Items.Count * 30)
            };
            _popup.ItemClicked += Popup_ItemClicked;
            _popup.Dismissed += Popup_Dismissed;
            _popupMessageFilter = new LanguagePopupMessageFilter(this);
            Application.AddMessageFilter(_popupMessageFilter);

            Point screenLocation = PointToScreen(new Point(0, Height));
            Point formLocation = form.PointToClient(screenLocation);
            _popup.Location = formLocation;
            form.Controls.Add(_popup);
            _popup.BringToFront();
            _popup.Focus();
        }

        private void Popup_ItemClicked(int index)
        {
            SelectedIndex = index;
            ClosePopup();
        }

        private void Popup_Dismissed(object? sender, EventArgs e)
        {
            ClosePopup();
        }

        public void ClosePopup()
        {
            if (_popup == null) return;

            LanguagePopupControl popup = _popup;
            _popup = null;
            if (_popupMessageFilter != null)
            {
                Application.RemoveMessageFilter(_popupMessageFilter);
                _popupMessageFilter = null;
            }
            popup.ItemClicked -= Popup_ItemClicked;
            popup.Dismissed -= Popup_Dismissed;
            if (popup.Parent != null)
                popup.Parent.Controls.Remove(popup);
            popup.Dispose();
            Focus();
            Invalidate();
        }

        private sealed class LanguagePopupMessageFilter : IMessageFilter
        {
            private readonly RoundedLanguageSelector _owner;

            public LanguagePopupMessageFilter(RoundedLanguageSelector owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != 0x0201 || _owner._popup == null)
                    return false;

                Point screenPoint = Control.MousePosition;
                bool insideSelector = _owner.RectangleToScreen(_owner.ClientRectangle).Contains(screenPoint);
                bool insidePopup = _owner._popup.RectangleToScreen(_owner._popup.ClientRectangle).Contains(screenPoint);

                if (!insideSelector && !insidePopup)
                    _owner.ClosePopup();

                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ClosePopup();
            base.Dispose(disposing);
        }

        private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new();
            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class LanguagePopupControl : Control
    {
        private string[] _items = Array.Empty<string>();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _darkMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<string>();
                Height = _items.Length * ItemHeight;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value >= 0 && value < _items.Length ? value : -1;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                _darkMode = value;
                Invalidate();
            }
        }

        public event Action<int>? ItemClicked;
        public event EventHandler? Dismissed;

        private const int ItemHeight = 30;
        private const int BorderWidth = 1;

        public LanguagePopupControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(38, 41, 44);
        }

        public void ApplyTheme(bool dark)
        {
            DarkMode = dark;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = _darkMode ? Color.FromArgb(38, 41, 44) : Color.White;
            Color border = _darkMode ? Color.FromArgb(105, 112, 120) : Color.FromArgb(194, 201, 211);
            Color hover = _darkMode ? Color.FromArgb(30, 66, 99) : Color.FromArgb(224, 238, 255);
            Color foreground = _darkMode ? Color.WhiteSmoke : LightText;

            using SolidBrush backgroundBrush = new(background);
            using Pen borderPen = new(border, 1f);
            e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            for (int i = 0; i < _items.Length; i++)
            {
                Rectangle itemBounds = new(BorderWidth, BorderWidth + i * ItemHeight,
                    Math.Max(1, Width - BorderWidth * 2), ItemHeight);
                bool highlighted = i == _hoverIndex || (i == _selectedIndex && _hoverIndex < 0);

                if (highlighted)
                {
                    using SolidBrush hoverBrush = new(hover);
                    e.Graphics.FillRectangle(hoverBrush, itemBounds);
                }

                TextRenderer.DrawText(e.Graphics, _items[i], Font,
                    new Rectangle(itemBounds.X + 10, itemBounds.Y, itemBounds.Width - 20, itemBounds.Height),
                    foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexFromPoint(e.Location);
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;

            int index = IndexFromPoint(e.Location);
            if (index >= 0)
                ItemClicked?.Invoke(index);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Down)
            {
                int next = Math.Min(_items.Length - 1, Math.Max(0, (_hoverIndex >= 0 ? _hoverIndex : _selectedIndex) + 1));
                _hoverIndex = next;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                int next = Math.Max(0, (_hoverIndex >= 0 ? _hoverIndex : _selectedIndex) - 1);
                _hoverIndex = next;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                int index = _hoverIndex >= 0 ? _hoverIndex : _selectedIndex;
                if (index >= 0)
                    ItemClicked?.Invoke(index);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private int IndexFromPoint(Point point)
        {
            int index = (point.Y - BorderWidth) / ItemHeight;
            return point.X >= BorderWidth && point.X < Width - BorderWidth &&
                   index >= 0 && index < _items.Length ? index : -1;
        }
    }

    private sealed class RestoreDefaultsDialog : Form
    {
        private readonly bool _dark;

        public RestoreDefaultsDialog(string title, string question, string yesText, string noText, bool dark)
        {
            _dark = dark;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(390, 120);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            TopMost = true;
            DoubleBuffered = true;

            BackColor = dark ? DarkBackground : Color.White;
            ForeColor = dark ? Color.WhiteSmoke : LightText;

            PictureBox questionIcon = new()
            {
                Location = new Point(20, 18),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = SystemIcons.Question.ToBitmap(),
                BackColor = Color.Transparent
            };

            Label questionLabel = new()
            {
                AutoSize = false,
                Location = new Point(62, 18),
                Size = new Size(305, 42),
                Text = question,
                ForeColor = ForeColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel buttonPanel = new()
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(245, 246, 248)
            };

            Button yesButton = CreateButton(yesText, DialogResult.Yes, true);
            Button noButton = CreateButton(noText, DialogResult.No, false);

            yesButton.Location = new Point(216, 10);
            noButton.Location = new Point(300, 10);
            buttonPanel.Controls.Add(yesButton);
            buttonPanel.Controls.Add(noButton);

            Controls.Add(questionIcon);
            Controls.Add(questionLabel);
            Controls.Add(buttonPanel);

            AcceptButton = yesButton;
            CancelButton = noButton;

            Shown += (_, _) =>
            {
                ApplyDialogTitleBarTheme(_dark);
                yesButton.Focus();
            };
        }

        private Button CreateButton(string text, DialogResult result, bool primary)
        {
            Button button = new()
            {
                Text = text,
                DialogResult = result,
                Size = new Size(74, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary
                    ? (_dark ? Color.FromArgb(0, 122, 255) : Color.White)
                    : (_dark ? Color.FromArgb(52, 56, 60) : Color.White),
                ForeColor = primary
                    ? (_dark ? Color.White : Accent)
                    : (_dark ? Color.WhiteSmoke : LightText),
                Font = new Font("Segoe UI", 9f),
                UseVisualStyleBackColor = false,
                TabStop = true
            };

            button.FlatAppearance.BorderColor = primary
                ? Accent
                : (_dark ? Color.FromArgb(82, 87, 93) : Color.FromArgb(190, 196, 204));
            button.FlatAppearance.BorderSize = 1;

            return button;
        }

        private void ApplyDialogTitleBarTheme(bool dark)
        {
            if (!IsHandleCreated)
                return;

            try
            {
                int useDarkMode = dark ? 1 : 0;
                _ = DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
            }
            catch
            {
                // Keep the dialog functional if the DWM attribute is unavailable.
            }
        }
    }

    private sealed class ThemeOptionControl : Control
    {
        private readonly string? _iconKey;
        private readonly PngIconCache _iconCache;
        private bool _selected;
        private bool _dark;
        private bool _hover;

        public AppTheme Theme { get; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LabelText { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        public ThemeOptionControl(AppTheme theme, string? iconKey, string labelText, PngIconCache iconCache)
        {
            Theme = theme;
            _iconKey = iconKey;
            _iconCache = iconCache;
            LabelText = labelText;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color border = _selected ? Accent : (_dark ? Color.FromArgb(52, 57, 62) : Color.FromArgb(222, 227, 234));
            Color background = _selected
                ? (Theme == AppTheme.System
                    ? (_dark ? DarkBackground : LightBackground)
                    : (_dark ? Color.FromArgb(32, 44, 58) : Color.FromArgb(246, 249, 253)))
                : (_hover ? (_dark ? Color.FromArgb(38, 42, 47) : Color.FromArgb(248, 250, 253)) : (_dark ? Color.FromArgb(31, 34, 37) : Color.White));

            using (Brush fill = new SolidBrush(background))
                e.Graphics.FillRoundedRectangle(fill, new Rectangle(1, 1, Width - 3, Height - 3), 6);
            using (Pen pen = new(border, _selected ? 1.5f : 1f))
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 6);

            Color text = _dark ? Color.WhiteSmoke : LightText;
            Color secondary = _dark ? DarkSecondary : LightSecondary;
            const float iconSize = 36f;
            float iconY = (Height - 73f) / 2f;
            if (!string.IsNullOrWhiteSpace(_iconKey))
                _iconCache.Draw(e.Graphics, _iconKey, new RectangleF((Width - iconSize) / 2f, iconY, iconSize, iconSize), _dark, DeviceDpi);

            string label = LabelText;
            using Font labelFont = new("Segoe UI", 9f);
            Size textSize = TextRenderer.MeasureText(label, labelFont);
            float labelY = iconY + 53f;
            float groupWidth = textSize.Width + 18f;
            float groupX = (Width - groupWidth) / 2f;
            float radioX = groupX;
            float textX = radioX + 16f;
            using (Brush radio = new SolidBrush(_selected ? Accent : secondary))
                e.Graphics.FillEllipse(radio, radioX, labelY + 3f, 10f, 10f);
            if (_selected)
            {
                using Brush dot = new SolidBrush(Color.White);
                e.Graphics.FillEllipse(dot, radioX + 3f, labelY + 6f, 4f, 4f);
            }
            TextRenderer.DrawText(e.Graphics, label, labelFont, new Point((int)textX, (int)labelY), text, TextFormatFlags.NoPrefix);
        }
    }

    private sealed class CriticalBatteryNumericControl : UserControl
    {
        private readonly TextBox _textBox;
        private int _minimum = 1;
        private int _maximum = 100;
        private int _increment = 1;
        private int _value = 10;
        private bool _dark;
        private NumericInputMouseFilter? _mouseFilter;

        public event EventHandler? ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = Math.Min(value, _maximum);
                Value = Math.Max(_minimum, _value);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(value, _minimum);
                Value = Math.Min(_maximum, _value);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Increment
        {
            get => _increment;
            set => _increment = Math.Max(1, value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Clamp(value, _minimum, _maximum);
                if (_value == clamped)
                {
                    UpdateText();
                    return;
                }

                _value = clamped;
                UpdateText();
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                _dark = value;
                ApplyTheme();
                Invalidate();
            }
        }

        public CriticalBatteryNumericControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            TabStop = true;
            Padding = new Padding(8, 0, 24, 0);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Dock = DockStyle.None,
                Location = new Point(8, 5),
                Size = new Size(38, 24),
                Margin = Padding.Empty,
                Multiline = false,
                TabStop = true,
                Font = Font,
                BackColor = Color.FromArgb(38, 41, 44),
                ForeColor = Color.WhiteSmoke
            };
            _textBox.KeyPress += TextBox_KeyPress;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.MouseDown += TextBox_MouseDown;
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.Validating += TextBox_Validating;
            _textBox.TextChanged += (_, _) => Invalidate();
            Controls.Add(_textBox);
            ApplyTheme();
            UpdateText();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_textBox != null)
                _textBox.Font = Font;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_textBox != null)
            {
                _textBox.Location = new Point(8, Math.Max(0, (Height - _textBox.Height) / 2));
                _textBox.Width = Math.Max(1, Width - Padding.Left - Padding.Right);
            }
            Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            EnsureMouseFilter();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _textBox.DeselectAll();
            RemoveMouseFilter();
            base.OnLostFocus(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta > 0)
                ChangeValue(_increment);
            else if (e.Delta < 0)
                ChangeValue(-_increment);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle borderRect = new(1, 1, Width - 3, Height - 3);
            Color background = _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251);
            Color border = _dark ? Color.FromArgb(91, 96, 102) : Color.FromArgb(150, 157, 168);
            Color chevron = _dark ? Color.FromArgb(224, 227, 231) : Color.FromArgb(82, 89, 99);

            using Brush backgroundBrush = new SolidBrush(background);
            using Pen borderPen = new(border, 1f);
            e.Graphics.FillRoundedRectangle(backgroundBrush, borderRect, 7);
            e.Graphics.DrawRoundedRectangle(borderPen, borderRect, 7);

            int centerX = Width - 13;
            using Pen chevronPen = new(chevron, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            // Compact 26px control: keep both chevrons fully inside the field.
            // The previous fixed coordinates placed the lower chevron outside the
            // control, making the spinner appear vertically displaced.
            float upY = Math.Max(5f, Height * 0.31f);
            float downY = Math.Min(Height - 5f, Height * 0.69f);
            float chevronHalfHeight = Math.Max(2f, Math.Min(2.6f, Height * 0.10f));

            PointF[] up =
            {
                new(centerX - 3, upY + chevronHalfHeight),
                new(centerX, upY),
                new(centerX + 3, upY + chevronHalfHeight)
            };
            PointF[] down =
            {
                new(centerX - 3, downY - chevronHalfHeight),
                new(centerX, downY),
                new(centerX + 3, downY - chevronHalfHeight)
            };
            e.Graphics.DrawLines(chevronPen, up);
            e.Graphics.DrawLines(chevronPen, down);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            if (e.X >= Width - 28)
            {
                if (e.Y < Height / 2)
                    ChangeValue(_increment);
                else
                    ChangeValue(-_increment);
            }
            else
            {
                _textBox.Focus();
                _textBox.SelectAll();
            }
        }

        private void ChangeValue(int delta)
        {
            Value = Math.Clamp(_value + delta, _minimum, _maximum);
            _textBox.Focus();
            _textBox.SelectAll();
        }

        private void TextBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            BeginInvoke(new Action(() =>
            {
                if (!_textBox.IsDisposed)
                {
                    EnsureMouseFilter();
                    _textBox.Focus();
                    _textBox.SelectAll();
                }
            }));
        }

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            _textBox.DeselectAll();
        }

        private void EnsureMouseFilter()
        {
            if (_mouseFilter != null)
                return;

            _mouseFilter = new NumericInputMouseFilter(this);
            Application.AddMessageFilter(_mouseFilter);
        }

        private void RemoveMouseFilter()
        {
            if (_mouseFilter == null)
                return;

            Application.RemoveMessageFilter(_mouseFilter);
            _mouseFilter = null;
        }

        private void ClearFocusFromInput()
        {
            _textBox.DeselectAll();
            FindForm()?.Focus();
        }

        private sealed class NumericInputMouseFilter : IMessageFilter
        {
            private readonly CriticalBatteryNumericControl _owner;

            public NumericInputMouseFilter(CriticalBatteryNumericControl owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != 0x0201 || _owner.IsDisposed || !_owner.Visible)
                    return false;

                Point screenPoint = Control.MousePosition;
                Rectangle ownerBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                if (!ownerBounds.Contains(screenPoint))
                    _owner.ClearFocusFromInput();

                return false;
            }
        }

        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                ChangeValue(_increment);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ChangeValue(-_increment);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                CommitText();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                UpdateText();
                e.Handled = true;
            }
        }

        private void TextBox_Validating(object? sender, CancelEventArgs e) => CommitText();

        private void CommitText()
        {
            if (int.TryParse(_textBox.Text, out int parsed))
                Value = parsed;
            else
                UpdateText();
        }

        private void UpdateText()
        {
            if (_textBox == null)
                return;

            string text = _value.ToString();
            if (_textBox.Text != text)
                _textBox.Text = text;
        }

        private void ApplyTheme()
        {
            if (_textBox == null)
                return;

            _textBox.BackColor = _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251);
            _textBox.ForeColor = _dark ? Color.WhiteSmoke : LightText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveMouseFilter();
                _textBox.LostFocus -= TextBox_LostFocus;
            }

            base.Dispose(disposing);
        }
    }

    private sealed class ToggleSwitchControl : Control
    {
        private bool _checked;
        private bool _dark;

        public event EventHandler? CheckedChanged;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        public ToggleSwitchControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new(0, 2, Width - 1, Height - 5);
            Color trackColor = _checked ? Accent : (_dark ? Color.FromArgb(76, 81, 87) : Color.FromArgb(205, 211, 220));
            using (Brush b = new SolidBrush(trackColor))
                e.Graphics.FillRoundedRectangle(b, track, track.Height / 2);

            int knobSize = Math.Max(10, track.Height - 4);
            int knobX = _checked ? track.Right - knobSize - 2 : track.Left + 2;
            using Brush knob = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(knob, knobX, track.Top + 2, knobSize, knobSize);
        }
    }

    private sealed class PngIconControl : Control
    {
        private readonly PngIconCache _iconCache;
        private readonly string _iconKey;
        private bool _dark;

        public PngIconControl(PngIconCache iconCache, string iconKey)
        {
            _iconCache = iconCache;
            _iconKey = iconKey;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            int size = Math.Max(1, Math.Min(Width, Height));
            _iconCache.Draw(e.Graphics, _iconKey, new RectangleF((Width - size) / 2f, (Height - size) / 2f, size, size), _dark, DeviceDpi);
        }
    }

    private static class SvgIconRenderer
    {
        private static readonly System.Globalization.CultureInfo Invariant = System.Globalization.CultureInfo.InvariantCulture;
        private static readonly System.Text.RegularExpressions.Regex TokenRegex = new(@"[AaCcHhLlMmQqSsTtVvZz]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex TransformRegex = new(@"(?<name>matrix|translate|scale|rotate|skewX|skewY)\s*\((?<args>[^)]*)\)", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        public static void Draw(Graphics graphics, string? filePath, RectangleF bounds, Color color)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            try
            {
                XDocument document = XDocument.Load(filePath);
                XElement? root = document.Root;
                if (root == null) return;

                XNamespace ns = root.Name.Namespace;
                List<(GraphicsPath Path, bool HasFill, bool HasStroke, float StrokeWidth)> paths = new();
                RectangleF sourceBounds = RectangleF.Empty;

                // Normalize the artwork using the actual transformed path bounds rather
                // than the SVG viewport. The supplied Potrace files have no viewBox and
                // their artwork does not necessarily occupy the complete canvas.
                foreach (XElement element in document.Descendants(ns + "path"))
                {
                    string? rawData = element.Attribute("d")?.Value;
                    if (string.IsNullOrWhiteSpace(rawData)) continue;

                    // Parse the full, untouched path data first so that relative
                    // commands following the Potrace canvas sub-path keep the correct
                    // current point (see ParseFigures for details), then drop the
                    // canvas figure afterwards instead of slicing the raw text.
                    List<GraphicsPath> figures = ParseFigures(rawData);
                    if (figures.Count == 0) continue;
                    if (HasLeadingCanvasFigure(rawData))
                    {
                        figures[0].Dispose();
                        figures.RemoveAt(0);
                    }
                    if (figures.Count == 0) continue;

                    string fillRule = GetInheritedStyle(element, "fill-rule") ?? "evenodd";
                    FillMode fillMode = string.Equals(fillRule, "nonzero", StringComparison.OrdinalIgnoreCase)
                        ? FillMode.Winding
                        : FillMode.Alternate;

                    GraphicsPath path = new(fillMode);
                    foreach (GraphicsPath figure in figures)
                    {
                        path.AddPath(figure, false);
                        figure.Dispose();
                    }
                    if (path.PointCount == 0) { path.Dispose(); continue; }

                    using Matrix sourceTransform = GetCumulativeTransform(element, root);
                    if (!sourceTransform.IsIdentity)
                        path.Transform(sourceTransform);

                    RectangleF pathBounds = path.GetBounds();
                    if (pathBounds.Width <= 0 || pathBounds.Height <= 0)
                    {
                        path.Dispose();
                        continue;
                    }

                    sourceBounds = sourceBounds.IsEmpty
                        ? pathBounds
                        : RectangleF.Union(sourceBounds, pathBounds);

                    string fill = GetInheritedStyle(element, "fill") ?? "black";
                    string stroke = GetInheritedStyle(element, "stroke") ?? "none";
                    string strokeWidthText = GetInheritedStyle(element, "stroke-width") ?? "1";

                    bool hasFill = !string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(fill, "transparent", StringComparison.OrdinalIgnoreCase);
                    bool hasStroke = !string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase) &&
                                     !string.Equals(stroke, "transparent", StringComparison.OrdinalIgnoreCase);

                    float strokeWidth = 1.8f;
                    if (float.TryParse(strokeWidthText, System.Globalization.NumberStyles.Float, Invariant, out float parsed) && parsed > 0)
                        strokeWidth = parsed;

                    paths.Add((path, hasFill, hasStroke, strokeWidth));
                }

                if (paths.Count == 0 || sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
                {
                    foreach (var item in paths) item.Path.Dispose();
                    return;
                }

                float scale = Math.Min(
                    bounds.Width / sourceBounds.Width,
                    bounds.Height / sourceBounds.Height);
                float ox = bounds.X + (bounds.Width - sourceBounds.Width * scale) / 2f - sourceBounds.X * scale;
                float oy = bounds.Y + (bounds.Height - sourceBounds.Height * scale) / 2f - sourceBounds.Y * scale;

                using Matrix matrix = new(scale, 0, 0, scale, ox, oy);
                using Brush brush = new SolidBrush(color);

                foreach (var item in paths)
                {
                    using GraphicsPath path = item.Path;
                    path.Transform(matrix);

                    if (item.HasFill)
                        graphics.FillPath(brush, path);

                    if (item.HasStroke)
                    {
                        using Pen pen = new(color, Math.Max(1f, item.StrokeWidth * scale))
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        };
                        graphics.DrawPath(pen, path);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Detects whether <paramref name="data"/> begins with the Potrace full-canvas
        /// sub-path (a rectangle covering the whole document) that these icon files use
        /// as a base for their nonzero-winding cutouts. This only inspects the raw text
        /// to decide whether the first parsed figure should be discarded - the actual
        /// removal happens on the parsed geometry in <see cref="ParseFigures"/> callers,
        /// never by slicing the string itself (doing so would desynchronize the current
        /// point for any relative command that follows, shifting the real artwork).
        /// </summary>
        public static bool HasLeadingCanvasFigure(string data)
        {
            int firstMove = data.IndexOf('M');
            if (firstMove < 0) firstMove = data.IndexOf('m');
            if (firstMove < 0) return false;

            // The Potrace canvas sub-path always closes before the actual artwork starts.
            int closeUpper = data.IndexOf('Z', firstMove + 1);
            int closeLower = data.IndexOf('z', firstMove + 1);
            int close;
            if (closeUpper < 0) close = closeLower;
            else if (closeLower < 0) close = closeUpper;
            else close = Math.Min(closeUpper, closeLower);

            if (close < 0) return false;

            string prefix = data[firstMove..(close + 1)];
            return LooksLikeCanvasSubpath(prefix);
        }

        private static bool LooksLikeCanvasSubpath(string data)
        {
            // Every supplied Potrace icon starts with the 1254px canvas rectangle
            // represented in source coordinates by the 6270/12540 contour.
            return data.Contains("M0 6270", StringComparison.Ordinal) ||
                   data.Contains("M0 6270", StringComparison.OrdinalIgnoreCase);
        }

        private static Matrix GetCumulativeTransform(XElement element, XElement root)
        {
            Matrix result = new();
            List<XElement> chain = new();
            for (XElement? current = element; current != null; current = current.Parent)
            {
                chain.Add(current);
                if (current == root) break;
            }
            chain.Reverse();

            foreach (XElement current in chain)
            {
                string? transform = current.Attribute("transform")?.Value;
                if (string.IsNullOrWhiteSpace(transform)) continue;
                using Matrix next = ParseTransform(transform);
                result.Multiply(next, MatrixOrder.Prepend);
            }
            return result;
        }

        private static Matrix ParseTransform(string value)
        {
            Matrix result = new();
            foreach (System.Text.RegularExpressions.Match match in TransformRegex.Matches(value))
            {
                string name = match.Groups["name"].Value.ToLowerInvariant();
                float[] a = match.Groups["args"].Value
                    .Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => float.Parse(v, System.Globalization.NumberStyles.Float, Invariant))
                    .ToArray();
                using Matrix next = name switch
                {
                    "matrix" when a.Length >= 6 => new Matrix(a[0], a[1], a[2], a[3], a[4], a[5]),
                    "translate" when a.Length >= 1 => new Matrix(1, 0, 0, 1, a[0], a.Length > 1 ? a[1] : 0),
                    "scale" when a.Length >= 1 => new Matrix(a[0], 0, 0, a.Length > 1 ? a[1] : a[0], 0, 0),
                    "rotate" when a.Length >= 1 && a.Length < 3 => CreateRotation(a[0]),
                    "rotate" when a.Length >= 3 => CreateRotation(a[0], a[1], a[2]),
                    "skewx" when a.Length >= 1 => CreateSkewX(a[0]),
                    "skewy" when a.Length >= 1 => CreateSkewY(a[0]),
                    _ => new Matrix()
                };
                result.Multiply(next, MatrixOrder.Prepend);
            }
            return result;
        }

        private static Matrix CreateRotation(float degrees) { Matrix m = new(); m.Rotate(degrees, MatrixOrder.Append); return m; }
        private static Matrix CreateRotation(float degrees, float cx, float cy) { Matrix m = new(); m.Translate(cx, cy, MatrixOrder.Append); m.Rotate(degrees, MatrixOrder.Append); m.Translate(-cx, -cy, MatrixOrder.Append); return m; }
        private static Matrix CreateSkewX(float degrees) => new(1, 0, (float)Math.Tan(degrees * Math.PI / 180.0), 1, 0, 0);
        private static Matrix CreateSkewY(float degrees) => new(1, (float)Math.Tan(degrees * Math.PI / 180.0), 0, 1, 0, 0);

        private static string? GetInheritedStyle(XElement element, string name)
        {
            for (XElement? current = element; current != null; current = current.Parent)
            {
                string? direct = current.Attribute(name)?.Value;
                if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();

                string? style = current.Attribute("style")?.Value;
                if (!string.IsNullOrWhiteSpace(style))
                {
                    foreach (string declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] pair = declaration.Split(':', 2);
                        if (pair.Length == 2 && string.Equals(pair[0].Trim(), name, StringComparison.OrdinalIgnoreCase))
                            return pair[1].Trim();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Parses raw SVG path data into one <see cref="GraphicsPath"/> per sub-figure
        /// (i.e. one per M/m command), returned in document order.
        ///
        /// Crucially, this always parses the *entire* original data string in a single
        /// continuous pass, exactly like a real SVG renderer would. The current point
        /// (x, y) and current sub-path start point (sx, sy) are tracked across figure
        /// boundaries, so a relative command that immediately follows a closed figure
        /// (e.g. "...z m9855 2532...") still resolves to the correct absolute
        /// coordinates. Splitting figures out this way - rather than slicing the figure
        /// we don't want out of the *text* beforehand - is what lets callers safely
        /// discard the leading Potrace canvas rectangle (see
        /// <see cref="HasLeadingCanvasFigure"/>) without shifting every subsequent
        /// sub-path by that rectangle's own extent.
        /// </summary>
        public static List<GraphicsPath> ParseFigures(string data)
        {
            List<GraphicsPath> figures = new();
            GraphicsPath? current = null;
            string[] tokens = TokenRegex.Matches(data).Select(m => m.Value).ToArray();
            int i = 0;
            char command = 'M';
            float x = 0, y = 0, sx = 0, sy = 0;
            float lastCubicX = 0, lastCubicY = 0, lastQuadX = 0, lastQuadY = 0;
            char previousCommand = '\0';

            bool IsCommandToken() => i < tokens.Length && tokens[i].Length == 1 && char.IsLetter(tokens[i][0]);
            bool Has(int count) => i + count <= tokens.Length;
            float Number() => float.Parse(tokens[i++], Invariant);
            GraphicsPath Figure() => current ??= new GraphicsPath(FillMode.Winding);

            while (i < tokens.Length)
            {
                if (IsCommandToken()) command = tokens[i++][0];
                char upper = char.ToUpperInvariant(command);
                bool rel = char.IsLower(command);

                try
                {
                    switch (upper)
                    {
                        case 'M':
                            if (!Has(2)) return figures;
                            float mx = Number(), my = Number();
                            if (rel) { mx += x; my += y; }
                            current = new GraphicsPath(FillMode.Winding);
                            figures.Add(current);
                            current.StartFigure();
                            x = mx; y = my; sx = x; sy = y;
                            command = rel ? 'l' : 'L';
                            previousCommand = 'M';
                            break;

                        case 'L':
                            if (!Has(2)) return figures;
                            float lx = Number(), ly = Number();
                            if (rel) { lx += x; ly += y; }
                            Figure().AddLine(x, y, lx, ly); x = lx; y = ly;
                            previousCommand = 'L';
                            break;

                        case 'H':
                            if (!Has(1)) return figures;
                            float hx = Number(); if (rel) hx += x;
                            Figure().AddLine(x, y, hx, y); x = hx;
                            previousCommand = 'H';
                            break;

                        case 'V':
                            if (!Has(1)) return figures;
                            float vy = Number(); if (rel) vy += y;
                            Figure().AddLine(x, y, x, vy); y = vy;
                            previousCommand = 'V';
                            break;

                        case 'C':
                            if (!Has(6)) return figures;
                            float c1x = Number(), c1y = Number(), c2x = Number(), c2y = Number(), cx = Number(), cy = Number();
                            if (rel) { c1x += x; c1y += y; c2x += x; c2y += y; cx += x; cy += y; }
                            Figure().AddBezier(x, y, c1x, c1y, c2x, c2y, cx, cy);
                            x = cx; y = cy; lastCubicX = c2x; lastCubicY = c2y; previousCommand = 'C';
                            break;

                        case 'S':
                            if (!Has(4)) return figures;
                            float sc2x = Number(), sc2y = Number(), sx2 = Number(), sy2 = Number();
                            if (rel) { sc2x += x; sc2y += y; sx2 += x; sy2 += y; }
                            float sc1x = (previousCommand is 'C' or 'S') ? 2 * x - lastCubicX : x;
                            float sc1y = (previousCommand is 'C' or 'S') ? 2 * y - lastCubicY : y;
                            Figure().AddBezier(x, y, sc1x, sc1y, sc2x, sc2y, sx2, sy2);
                            x = sx2; y = sy2; lastCubicX = sc2x; lastCubicY = sc2y; previousCommand = 'S';
                            break;

                        case 'Q':
                            if (!Has(4)) return figures;
                            float qx = Number(), qy = Number(), qex = Number(), qey = Number();
                            if (rel) { qx += x; qy += y; qex += x; qey += y; }
                            float q1x = x + 2f * (qx - x) / 3f;
                            float q1y = y + 2f * (qy - y) / 3f;
                            float q2x = qex + 2f * (qx - qex) / 3f;
                            float q2y = qey + 2f * (qy - qey) / 3f;
                            Figure().AddBezier(x, y, q1x, q1y, q2x, q2y, qex, qey);
                            x = qex; y = qey; lastQuadX = qx; lastQuadY = qy; previousCommand = 'Q';
                            break;

                        case 'T':
                            if (!Has(2)) return figures;
                            float tex = Number(), tey = Number(); if (rel) { tex += x; tey += y; }
                            float tqx = (previousCommand is 'Q' or 'T') ? 2 * x - lastQuadX : x;
                            float tqy = (previousCommand is 'Q' or 'T') ? 2 * y - lastQuadY : y;
                            float tq1x = x + 2f * (tqx - x) / 3f;
                            float tq1y = y + 2f * (tqy - y) / 3f;
                            float tq2x = tex + 2f * (tqx - tex) / 3f;
                            float tq2y = tey + 2f * (tqy - tey) / 3f;
                            Figure().AddBezier(x, y, tq1x, tq1y, tq2x, tq2y, tex, tey);
                            x = tex; y = tey; lastQuadX = tqx; lastQuadY = tqy; previousCommand = 'T';
                            break;

                        case 'A':
                            if (!Has(7)) return figures;
                            float rx = Math.Abs(Number()), ry = Math.Abs(Number()), rotation = Number();
                            bool largeArc = Number() != 0, sweep = Number() != 0;
                            float ax = Number(), ay = Number(); if (rel) { ax += x; ay += y; }
                            AddArc(Figure(), x, y, rx, ry, rotation, largeArc, sweep, ax, ay);
                            x = ax; y = ay; previousCommand = 'A';
                            break;

                        case 'Z':
                            Figure().CloseFigure(); x = sx; y = sy; previousCommand = 'Z';
                            break;

                        default:
                            return figures;
                    }
                }
                catch { return figures; }
            }
            return figures;
        }

        private static void AddArc(GraphicsPath path, float x1, float y1, float rx, float ry, float rotation, bool largeArc, bool sweep, float x2, float y2)
        {
            if (Math.Abs(x1 - x2) < 0.0001f && Math.Abs(y1 - y2) < 0.0001f) return;
            if (rx < 0.0001f || ry < 0.0001f) { path.AddLine(x1, y1, x2, y2); return; }

            double phi = rotation * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);
            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double xp = cosPhi * dx + sinPhi * dy;
            double yp = -sinPhi * dx + cosPhi * dy;
            double rxd = rx, ryd = ry;
            double lambda = (xp * xp) / (rxd * rxd) + (yp * yp) / (ryd * ryd);
            if (lambda > 1) { double k = Math.Sqrt(lambda); rxd *= k; ryd *= k; }

            double sign = largeArc == sweep ? -1 : 1;
            double numerator = Math.Max(0, (rxd * rxd * ryd * ryd - rxd * rxd * yp * yp - ryd * ryd * xp * xp));
            double denominator = rxd * rxd * yp * yp + ryd * ryd * xp * xp;
            double coef = denominator < 1e-12 ? 0 : sign * Math.Sqrt(numerator / denominator);
            double cxp = coef * (rxd * yp / ryd);
            double cyp = coef * (-ryd * xp / rxd);
            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

            double ux = (xp - cxp) / rxd, uy = (yp - cyp) / ryd;
            double vx = (-xp - cxp) / rxd, vy = (-yp - cyp) / ryd;
            double theta1 = Math.Atan2(uy, ux);
            double delta = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            if (!sweep && delta > 0) delta -= 2 * Math.PI;
            if (sweep && delta < 0) delta += 2 * Math.PI;

            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2)));
            double step = delta / segments;
            double theta = theta1;
            for (int segment = 0; segment < segments; segment++)
            {
                double next = theta + step;
                double alpha = 4.0 / 3.0 * Math.Tan((next - theta) / 4.0);
                PointF p1 = ArcPoint(cx, cy, rxd, ryd, cosPhi, sinPhi, theta);
                PointF p2 = ArcPoint(cx, cy, rxd, ryd, cosPhi, sinPhi, next);
                double d1x = -rxd * cosPhi * Math.Sin(theta) - ryd * sinPhi * Math.Cos(theta);
                double d1y = -rxd * sinPhi * Math.Sin(theta) + ryd * cosPhi * Math.Cos(theta);
                double d2x = -rxd * cosPhi * Math.Sin(next) - ryd * sinPhi * Math.Cos(next);
                double d2y = -rxd * sinPhi * Math.Sin(next) + ryd * cosPhi * Math.Cos(next);
                PointF c1 = new((float)(p1.X + alpha * d1x), (float)(p1.Y + alpha * d1y));
                PointF c2 = new((float)(p2.X - alpha * d2x), (float)(p2.Y - alpha * d2y));
                path.AddBezier(p1, c1, c2, p2);
                theta = next;
            }
        }

        private static PointF ArcPoint(double cx, double cy, double rx, double ry, double cosPhi, double sinPhi, double theta)
        {
            double ct = Math.Cos(theta), st = Math.Sin(theta);
            return new PointF((float)(cx + rx * cosPhi * ct - ry * sinPhi * st),
                              (float)(cy + rx * sinPhi * ct + ry * cosPhi * st));
        }
    }

    private sealed class GlyphControl : Control
    {
        private readonly Glyph _glyph;
        private readonly Color? _forcedColor;
        public GlyphControl(Glyph glyph, Color? color = null)
        {
            _glyph = glyph;
            _forcedColor = color;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawGlyph(e.Graphics, _glyph, new Rectangle(3, 3, Math.Max(8, Width - 6), Math.Max(8, Height - 6)), _forcedColor ?? ForeColor, 1.8f);
        }
    }

    private sealed class StatusDotControl : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Connected { get; set; }
        public StatusDotControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color statusColor = Connected ? Color.FromArgb(52, 211, 85) : Color.FromArgb(239, 68, 68);
            using Brush brush = new SolidBrush(statusColor);
            e.Graphics.FillEllipse(brush, 2.5f, 2.5f, 13f, 13f);
            using Pen glow = new(Color.FromArgb(90, statusColor.R, statusColor.G, statusColor.B), 1.5f);
            e.Graphics.DrawEllipse(glow, 1f, 1f, 16f, 16f);
        }
    }

    private sealed class BatteryIconControl : Control
    {
        private const int IconSize = 46;
        // Coordinates of the transparent interior on the supplied 46x46 battery PNG.
        // Only this area receives the dynamic battery-level fill.
        private static readonly Rectangle InteriorPixels = new(9, 18, 26, 12);

        private Bitmap? _darkTemplate;
        private Bitmap? _lightTemplate;
        private int _battery;
        private bool _connected;
        private bool _charging;
        private bool _dark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Battery { get => _battery; set { _battery = Math.Clamp(value, 0, 100); Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Connected { get => _connected; set { _connected = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Charging { get => _charging; set { _charging = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        public BatteryIconControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            DisposeTemplate(ref _darkTemplate);
            DisposeTemplate(ref _lightTemplate);
            _darkTemplate = LoadTemplate("Dark", "battery-dark-46x46.png");
            _lightTemplate = LoadTemplate("Light", "battery-light-46x46.png");
        }

        private static Bitmap? LoadTemplate(string themeFolder, string fileName)
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Icons", themeFolder, fileName);
                if (!File.Exists(filePath))
                    return null;

                using Bitmap source = new(filePath);
                Bitmap template = new(IconSize, IconSize, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(template))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.DrawImageUnscaled(source, 0, 0);
                }

                // The supplied PNG already contains the battery frame. Clear only the
                // center area so the dynamic charge level can be painted underneath it.
                for (int y = InteriorPixels.Top; y < InteriorPixels.Bottom; y++)
                {
                    for (int x = InteriorPixels.Left; x < InteriorPixels.Right; x++)
                    {
                        Color pixel = template.GetPixel(x, y);
                        template.SetPixel(x, y, Color.FromArgb(0, pixel.R, pixel.G, pixel.B));
                    }
                }

                return template;
            }
            catch
            {
                return null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            Bitmap? template = _dark ? _darkTemplate : _lightTemplate;
            if (template == null)
                return;

            float scale = Math.Min(Width / (float)IconSize, Height / (float)IconSize);
            if (scale <= 0)
                return;

            float drawWidth = IconSize * scale;
            float drawHeight = IconSize * scale;
            float ox = (Width - drawWidth) / 2f;
            float oy = (Height - drawHeight) / 2f;

            RectangleF interior = new(
                ox + InteriorPixels.X * scale,
                oy + InteriorPixels.Y * scale,
                InteriorPixels.Width * scale,
                InteriorPixels.Height * scale);

            Color cardBackground = _dark
                ? Color.FromArgb(46, 50, 54)
                : Color.FromArgb(248, 249, 251);

            // Restore the background inside the transparent charge cavity.
            using (Brush backgroundBrush = new SolidBrush(cardBackground))
                e.Graphics.FillRectangle(backgroundBrush, interior);

            if (_connected && _battery > 0)
            {
                Color green = Color.FromArgb(52, 211, 85);
                Color yellow = Color.FromArgb(250, 204, 21);
                Color orange = Color.FromArgb(249, 115, 22);
                Color red = Color.FromArgb(239, 68, 68);
                Color active = GetBatteryLevelColor(_battery, green, yellow, orange, red);

                float fillWidth = interior.Width * (_battery / 100f);
                if (fillWidth > 0)
                {
                    RectangleF fillBounds = new(interior.Left, interior.Top, fillWidth, interior.Height);
                    GraphicsState state = e.Graphics.Save();
                    try
                    {
                        e.Graphics.SetClip(interior, CombineMode.Intersect);
                        using LinearGradientBrush gradient = new(
                            new PointF(interior.Left, interior.Top),
                            new PointF(interior.Right, interior.Top),
                            Blend(active, Color.White, 0.08),
                            active);
                        e.Graphics.FillRectangle(gradient, fillBounds);
                    }
                    finally
                    {
                        e.Graphics.Restore(state);
                    }
                }
            }

            // Draw the PNG frame over the dynamic fill.
            e.Graphics.DrawImage(template, new RectangleF(ox, oy, drawWidth, drawHeight));

            if (_connected && _charging)
                DrawChargingBolt(e.Graphics, interior);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeTemplate(ref _darkTemplate);
                DisposeTemplate(ref _lightTemplate);
            }
            base.Dispose(disposing);
        }

        private static void DisposeTemplate(ref Bitmap? bitmap)
        {
            bitmap?.Dispose();
            bitmap = null;
        }

        private static void DrawChargingBolt(Graphics graphics, RectangleF bounds)
        {
            float width = Math.Min(bounds.Width * 0.42f, 15f);
            float height = Math.Min(bounds.Height * 0.72f, 24f);
            if (width <= 2f || height <= 2f) return;

            float left = bounds.Left + (bounds.Width - width) / 2f;
            float top = bounds.Top + (bounds.Height - height) / 2f;
            PointF[] points =
            {
                new(left + width * 0.58f, top),
                new(left + width * 0.08f, top + height * 0.56f),
                new(left + width * 0.48f, top + height * 0.56f),
                new(left + width * 0.30f, top + height),
                new(left + width * 0.92f, top + height * 0.34f),
                new(left + width * 0.52f, top + height * 0.34f)
            };

            using Brush fill = new SolidBrush(Color.WhiteSmoke);
            using Pen outline = new(Color.FromArgb(90, 0, 0, 0), Math.Max(1f, width * 0.08f))
            {
                LineJoin = LineJoin.Round
            };
            graphics.FillPolygon(fill, points);
            graphics.DrawPolygon(outline, points);
        }

        private static Color GetBatteryLevelColor(int battery, Color green, Color yellow, Color orange, Color red)
        {
            battery = Math.Clamp(battery, 0, 100);
            if (battery >= 50)
                return green;
            if (battery >= 30)
                return Blend(yellow, green, (battery - 30) / 20.0);
            if (battery >= 10)
                return Blend(orange, yellow, (battery - 10) / 20.0);
            return Blend(red, orange, battery / 10.0);
        }

        private static Color Blend(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }

    private sealed class DeviceSelector : Control
    {
        private readonly TextBox _searchBox;
        private readonly PictureBox _selectedImage;
        private readonly List<DeviceOption> _options;
        private bool _hover;
        private int _selectedIndex;
        private bool _dark;
        private SearchPopup? _popup;
        private bool _updatingText;
        private string _placeholder = string.Empty;
        private int _editOriginalIndex;
        private bool _editingDeviceSelection;
        private bool _selectionInProgress;
        private readonly ClickOutsideFilter _clickOutsideFilter;

        public event EventHandler? SelectionChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int next = Math.Clamp(value, 0, _options.Count);
                if (_selectedIndex == next && _searchBox.Text == GetDisplayText(next)) return;
                _selectedIndex = next;
                SetSearchText(GetDisplayText(next));
                UpdateSelectedImage();
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedDeviceName
        {
            get => GetDisplayText(_selectedIndex);
            set
            {
                int index = 0;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalizedValue = string.Equals(value.Trim(), "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
                        ? "HyperX Cloud III"
                        : value.Trim();
                    for (int i = 0; i < _options.Count; i++)
                    {
                        if (string.Equals(_options[i].Name, normalizedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            index = i + 1;
                            break;
                        }
                    }
                }
                SelectedIndex = index;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; _searchBox.BackColor = SearchBackColor; _searchBox.ForeColor = SearchTextColor; Invalidate(); } }

        private Color OutsideBackColor => Parent?.BackColor ?? (_dark ? Color.FromArgb(34, 37, 40) : LightBackground);

        public DeviceSelector()
        {
            _clickOutsideFilter = new ClickOutsideFilter(this);
            Application.AddMessageFilter(_clickOutsideFilter);

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            string devicesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Devices");
            _options = new List<DeviceOption>
            {
                new("HyperX Cloud III", Path.Combine(devicesPath, "cloud3.png")),
                new("HyperX Cloud III S", Path.Combine(devicesPath, "cloud3.png")),
                new("HyperX Cloud 2 Core", Path.Combine(devicesPath, "cloud2core.png")),
                new("HyperX Cloud Alpha", Path.Combine(devicesPath, "cloudalpha.png")),
                new("HyperX Cloud Stinger 2", Path.Combine(devicesPath, "cloudstinger2.png"))
            };
            _options.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));

            _selectedImage = new PictureBox
            {
                Location = new Point(12, 9),
                Size = new Size(54, 46),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = false
            };
            Controls.Add(_selectedImage);

            _searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(68, 20),
                Size = new Size(Width - 108, 24),
                Font = new Font("Segoe UI", 12f),
                Multiline = false,
                Padding = new Padding(0),
                TabStop = true,
                ReadOnly = false,
                Cursor = Cursors.IBeam,
                PlaceholderText = _placeholder
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.Enter += SearchBox_Enter;
            _searchBox.Leave += SearchBox_Leave;
            _searchBox.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                bool wasFocused = _searchBox.Focused;
                ShowPopup();
                if (!wasFocused)
                    BeginInvoke((MethodInvoker)(() => _searchBox.SelectAll()));
            };
            _searchBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _popup?.Close();
                    e.Handled = true;
                }
            };
            Controls.Add(_searchBox);

            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
            Click += (_, _) => ShowPopup();
            Resize += (_, _) =>
            {
                _selectedImage.Location = new Point(12, Math.Max(6, (Height - 46) / 2));
                _searchBox.Location = new Point(_selectedImage.Visible ? 68 : 14, Math.Max(0, (Height - _searchBox.Height) / 2));
                _searchBox.Size = new Size(Math.Max(10, Width - (_selectedImage.Visible ? 108 : 52)), _searchBox.Height);
                UpdateSelectedImage();
            };
            ApplySearchTheme();
        }

        public void SetPlaceholder(string placeholder)
        {
            _placeholder = placeholder ?? string.Empty;
            _searchBox.PlaceholderText = _placeholder;
            if (_selectedIndex == 0) SetSearchText(string.Empty);
            Invalidate();
        }

        private string GetDisplayText(int index) => index > 0 && index <= _options.Count ? _options[index - 1].Name : string.Empty;
        private Color SearchBackColor => _dark ? Color.FromArgb(38, 41, 44) : Color.White;
        private Color SearchTextColor => _dark ? Color.WhiteSmoke : LightText;

        private void SetSearchText(string value)
        {
            _updatingText = true;
            _searchBox.Text = value;
            _searchBox.SelectionStart = _searchBox.TextLength;
            _searchBox.SelectionLength = 0;
            _updatingText = false;
        }

        private void UpdateSelectedImage()
        {
            string? path = _selectedIndex > 0 && _selectedIndex <= _options.Count ? _options[_selectedIndex - 1].ImagePath : null;
            _selectedImage.Image?.Dispose();
            _selectedImage.Image = null;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    using Image source = Image.FromFile(path);
                    _selectedImage.Image = new Bitmap(source);
                    _selectedImage.Visible = true;
                }
                catch { _selectedImage.Visible = false; }
            }
            else _selectedImage.Visible = false;
            _searchBox.Location = new Point(_selectedImage.Visible ? 68 : 14, Math.Max(0, (Height - _searchBox.Height) / 2));
            _searchBox.Size = new Size(Math.Max(10, Width - (_selectedImage.Visible ? 108 : 52)), _searchBox.Height);
        }

        private void SearchBox_Enter(object? sender, EventArgs e)
        {
            if (!_editingDeviceSelection)
            {
                _editOriginalIndex = _selectedIndex;
                _editingDeviceSelection = true;
            }

            _searchBox.SelectAll();
            ShowPopup();
        }

        private void SearchBox_Leave(object? sender, EventArgs e)
        {
            if (_selectionInProgress) return;
            CommitOrRestoreDeviceSelection();
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            if (_updatingText) return;

            if (_editingDeviceSelection)
            {
                _selectedImage.Visible = false;
                if (_selectedIndex != 0)
                {
                    _selectedIndex = 0;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            ShowPopup();
            _popup?.RefreshItems(_searchBox.Text);
            Invalidate();
        }

        private void CommitOrRestoreDeviceSelection()
        {
            if (!_editingDeviceSelection) return;

            string typed = _searchBox.Text.Trim();
            int matchingIndex = 0;
            for (int i = 0; i < _options.Count; i++)
            {
                if (string.Equals(_options[i].Name, typed, StringComparison.OrdinalIgnoreCase))
                {
                    matchingIndex = i + 1;
                    break;
                }
            }

            _editingDeviceSelection = false;
            _selectionInProgress = true;
            try
            {
                SelectedIndex = matchingIndex > 0 ? matchingIndex : _editOriginalIndex;
            }
            finally
            {
                _selectionInProgress = false;
            }
        }

        private void ShowPopup()
        {
            if (!IsHandleCreated) return;
            if (_popup == null || _popup.IsDisposed)
            {
                _popup = new SearchPopup(this, _options, _dark, SelectOption);
            }
            _popup.SetTheme(_dark);
            _popup.Width = Width;
            _popup.RefreshItems(_searchBox.Text);
            Point screen = PointToScreen(new Point(0, Height));
            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            int popupHeight = _popup.Height;
            if (screen.Y + popupHeight > workArea.Bottom)
                screen.Y = Math.Max(workArea.Top, PointToScreen(Point.Empty).Y - popupHeight);
            if (screen.X + _popup.Width > workArea.Right)
                screen.X = Math.Max(workArea.Left, workArea.Right - _popup.Width);
            _popup.Location = screen;
            Form? ownerForm = FindForm();
            if (!_popup.Visible)
            {
                if (ownerForm != null)
                    _popup.Show(ownerForm);
                else
                    _popup.Show();
            }
            _popup.BringToFront();
            if (!_searchBox.Focused)
                _searchBox.Focus();
        }

        private void SelectOption(int index)
        {
            _selectionInProgress = true;
            try
            {
                _popup?.Close();
                _editingDeviceSelection = false;
                SelectedIndex = index;
            }
            finally
            {
                _selectionInProgress = false;
            }
            _searchBox.SelectionLength = 0;
            Focus();
        }

        private void ApplySearchTheme()
        {
            _searchBox.BackColor = SearchBackColor;
            _searchBox.ForeColor = SearchTextColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(OutsideBackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Color border = _selectedIndex == 0 ? Color.FromArgb(220, 53, 69) : (_dark ? Color.FromArgb(105, 112, 120) : Color.FromArgb(194, 201, 211));
            Color background = SearchBackColor;
            if (_hover && _selectedIndex != 0) background = _dark ? Color.FromArgb(43, 47, 51) : Color.FromArgb(252, 253, 255);

            float inset = 0.5f;
            RectangleF rect = new(inset, inset, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 7);
            using Brush bg = new SolidBrush(background);
            using Pen pen = new(border, 1f);
            e.Graphics.FillPath(bg, path);
            e.Graphics.DrawPath(pen, path);

            using Pen arrow = new(_dark ? Color.WhiteSmoke : LightText, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int x = Width - 20;
            int y = Height / 2 - 2;
            e.Graphics.DrawLine(arrow, x - 4, y, x, y + 4);
            e.Graphics.DrawLine(arrow, x, y + 4, x + 4, y);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.RemoveMessageFilter(_clickOutsideFilter);
                _popup?.Close();
                _popup?.Dispose();
                _selectedImage?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class ClickOutsideFilter : IMessageFilter
        {
            private readonly DeviceSelector _owner;

            public ClickOutsideFilter(DeviceSelector owner) => _owner = owner;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != 0x0201 || _owner._popup == null || _owner._popup.IsDisposed || !_owner._popup.Visible)
                    return false;

                Control? target = Control.FromHandle(m.HWnd);
                bool insideSelector = target != null && (target == _owner || _owner.Contains(target));
                bool insidePopup = target != null && (_owner._popup == target || _owner._popup.Contains(target));

                if (!insideSelector && !insidePopup)
                {
                    _owner._popup.Close();
                    Control? clicked = Control.FromHandle(m.HWnd);
                    if (clicked != null && clicked != _owner._searchBox && clicked.CanFocus)
                        clicked.Focus();
                    else
                        _owner.FindForm()?.Focus();
                }

                return false;
            }
        }

        private sealed record DeviceOption(string Name, string ImagePath);

        private sealed class SearchPopup : Form
        {
            private readonly List<DeviceOption> _options;
            private readonly Action<int> _select;
            private readonly Panel _list;
            private bool _dark;

            public SearchPopup(DeviceSelector owner, List<DeviceOption> options, bool dark, Action<int> select)
            {
                _options = options; _dark = dark; _select = select;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                ShowIcon = false;
                TopMost = true;
                AutoScaleMode = AutoScaleMode.None;
                Padding = new Padding(1);
                _list = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    HorizontalScroll = { Enabled = false, Visible = false },
                    BackColor = Color.Transparent,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };
                Controls.Add(_list);
                Paint += SearchPopup_Paint;
                SetTheme(dark);
            }

            protected override bool ShowWithoutActivation => true;
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                    cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                    return cp;
                }
            }

            private void SearchPopup_Paint(object? sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.SetClip(ClientRectangle);
                using Pen pen = new(_dark ? Color.FromArgb(86, 92, 98) : Color.FromArgb(194, 201, 211), 1f) { LineJoin = LineJoin.Round };
                RectangleF rect = new(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));
                using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 6);
                e.Graphics.DrawPath(pen, path);
            }

            public void SetTheme(bool dark)
            {
                _dark = dark;
                BackColor = dark ? Color.FromArgb(48, 52, 56) : Color.FromArgb(225, 229, 235);
                _list.BackColor = dark ? Color.FromArgb(38, 41, 44) : Color.White;
                _list.ForeColor = dark ? Color.WhiteSmoke : LightText;
            }

            public void RefreshItems(string query)
            {
                const int itemHeight = 64;
                const int maxVisibleItems = 3;

                _list.SuspendLayout();
                try
                {
                    foreach (Control c in _list.Controls)
                        c.Dispose();
                    _list.Controls.Clear();

                    string normalized = query.Trim();
                    if (string.IsNullOrEmpty(normalized))
                    {
                        for (int i = 0; i < _options.Count; i++)
                            _list.Controls.Add(CreateItem(i + 1, _options[i].Name, _options[i].ImagePath));
                    }
                    else
                    {
                        for (int i = 0; i < _options.Count; i++)
                        {
                            if (_options[i].Name.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                                _list.Controls.Add(CreateItem(i + 1, _options[i].Name, _options[i].ImagePath));
                        }
                    }

                    int itemCount = _list.Controls.Count;
                    int visibleItems = Math.Max(1, Math.Min(itemCount, maxVisibleItems));
                    Height = visibleItems * itemHeight + 2;

                    int availableWidth = Math.Max(1, _list.ClientSize.Width);
                    foreach (Control item in _list.Controls)
                    {
                        item.Width = availableWidth;
                        item.Location = new Point(0, item.Top);
                    }

                    _list.AutoScrollMinSize = new Size(0, itemCount * itemHeight);
                    _list.PerformLayout();
                    foreach (Control item in _list.Controls)
                        item.Width = Math.Max(1, _list.ClientSize.Width);
                }
                finally
                {
                    _list.ResumeLayout(true);
                }
            }

            private Control CreateItem(int index, string name, string? imagePath)
            {
                int y = _list.Controls.Count * 64;
                DeviceListItem item = new(name, imagePath, _dark)
                {
                    Location = new Point(0, y),
                    Width = Math.Max(1, _list.ClientSize.Width),
                    Height = 64
                };
                item.Click += (_, _) => _select(index);
                item.Cursor = Cursors.Hand;
                return item;
            }
        }

        private sealed class DeviceListItem : Control
        {
            private readonly string _name;
            private readonly string? _imagePath;
            private Image? _image;
            private readonly bool _dark;
            private bool _hover;

            public DeviceListItem(string name, string? imagePath, bool dark)
            {
                _name = name; _imagePath = imagePath; _dark = dark;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                LoadImage();
                MouseEnter += (_, _) => { _hover = true; Invalidate(); };
                MouseLeave += (_, _) => { _hover = false; Invalidate(); };
            }

            private void LoadImage()
            {
                if (string.IsNullOrWhiteSpace(_imagePath) || !File.Exists(_imagePath)) return;
                try
                {
                    using Image source = Image.FromFile(_imagePath);
                    _image = new Bitmap(source);
                }
                catch { }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (_hover)
                {
                    using Brush hover = new SolidBrush(_dark ? Color.FromArgb(48, 53, 58) : Color.FromArgb(242, 246, 251));
                    e.Graphics.FillRectangle(hover, ClientRectangle);
                }
                if (_image != null)
                    e.Graphics.DrawImage(_image, new Rectangle(10, 8, 48, 48));
                TextRenderer.DrawText(e.Graphics, _name, Font, new Rectangle(70, 0, Width - 80, Height), _dark ? Color.WhiteSmoke : LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _image?.Dispose();
                base.Dispose(disposing);
            }
        }
    }

    private enum BatteryPreviewKind
    {
        Normal,
        Charging,
        Glow,
        Level,
        Solid
    }

    private sealed class BatteryModeCard : Control
    {
        private bool _selected;
        private bool _dark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                    return;
                _selected = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                if (_dark == value)
                    return;
                _dark = value;
                Invalidate();
            }
        }

        public BatteryModeCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float inset = 2f;
            float diameter = Math.Min(Width, Height) - inset * 2f;
            RectangleF outer = new(inset, inset, diameter, diameter);
            Color border = _selected ? Accent : (_dark ? Color.FromArgb(188, 197, 207) : Color.FromArgb(94, 103, 115));

            using Pen pen = new(border, _selected ? 2.2f : 1.8f);
            e.Graphics.DrawEllipse(pen, outer);

            if (_selected)
            {
                float dot = diameter * 0.43f;
                using Brush brush = new SolidBrush(Accent);
                e.Graphics.FillEllipse(
                    brush,
                    outer.X + (diameter - dot) / 2f,
                    outer.Y + (diameter - dot) / 2f,
                    dot,
                    dot);
            }
        }
    }

    private sealed class BatteryPreviewItem : Control
    {
        private readonly BatteryPreviewKind _kind;
        private readonly Color _accentColor;
        private readonly string _label;
        private readonly bool _showTile;
        private readonly string? _levelIconStem;
        private Bitmap? _darkIcon;
        private Bitmap? _lightIcon;
        private Bitmap? _darkChargingIcon;
        private Bitmap? _lightChargingIcon;
        private Bitmap? _darkLevelIcon;
        private Bitmap? _lightLevelIcon;
        private bool _dark;

        public BatteryPreviewItem(
            BatteryPreviewKind kind,
            Color accentColor,
            string label,
            string? levelIconStem = null,
            bool showTile = false)
        {
            _kind = kind;
            _accentColor = accentColor;
            _label = label;
            _levelIconStem = levelIconStem;
            _showTile = showTile;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            LoadBitmaps();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                if (_dark == value)
                    return;
                _dark = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (_showTile)
            {
                RectangleF tile = new(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
                Color tileBackground = _dark ? Color.FromArgb(23, 27, 30) : Color.FromArgb(245, 247, 250);
                Color tileBorder = _dark ? Color.FromArgb(57, 63, 69) : Color.FromArgb(211, 216, 224);
                using Brush tileBrush = new SolidBrush(tileBackground);
                using Pen tilePen = new(tileBorder, 1f);
                e.Graphics.FillRoundedRectangle(tileBrush, tile, 8);
                e.Graphics.DrawRoundedRectangle(tilePen, tile, 8);
            }

            int labelHeight = Math.Max(16, Math.Min(20, Height / 3));
            int iconAreaHeight = Math.Max(20, Height - labelHeight - 2);
            int iconSize = _showTile
                ? Math.Min(42, Math.Max(28, Math.Min(Width - 12, iconAreaHeight - 2)))
                : Math.Min(58, Math.Max(28, Math.Min(Width - 12, iconAreaHeight - 2)));

            float contentHeight = iconSize + labelHeight;
            float contentTop = _showTile
                ? Math.Max(0f, (Height - contentHeight) / 2f)
                : 1f;

            RectangleF iconRect = new(
                (Width - iconSize) / 2f,
                contentTop,
                iconSize,
                iconSize);

            Bitmap? source = _kind == BatteryPreviewKind.Charging
                ? (_dark ? _darkChargingIcon : _lightChargingIcon)
                : _kind == BatteryPreviewKind.Level
                    ? (_dark ? _darkLevelIcon : _lightLevelIcon)
                    : (_dark ? _darkIcon : _lightIcon);

            if (source != null)
            {
                switch (_kind)
                {
                    case BatteryPreviewKind.Glow:
                        DrawGlow(e.Graphics, source, iconRect, _accentColor);
                        e.Graphics.DrawImage(source, iconRect);
                        break;
                    case BatteryPreviewKind.Level:
                        // Use the exact tray assets already used by the application
                        // (dark_green/light_green, etc.). Do not alter tray rendering.
                        e.Graphics.DrawImage(source, iconRect);
                        break;
                    case BatteryPreviewKind.Solid:
                        using (Bitmap solid = Colorize(source, _accentColor))
                            e.Graphics.DrawImage(solid, iconRect);
                        break;
                    default:
                        e.Graphics.DrawImage(source, iconRect);
                        break;
                }
            }

            using Font labelFont = new("Segoe UI", 8.2f);
            Color text = _dark ? Color.WhiteSmoke : LightText;
            Rectangle labelRect = _showTile
                ? new Rectangle(0, (int)Math.Round(contentTop + iconSize), Width, labelHeight)
                : new Rectangle(0, Height - labelHeight, Width, labelHeight);
            TextRenderer.DrawText(
                e.Graphics,
                _label,
                labelFont,
                labelRect,
                text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);
        }

        private void LoadBitmaps()
        {
            _darkIcon = LoadIconBitmap("Dark", "dark.ico");
            _lightIcon = LoadIconBitmap("Light", "light.ico");
            _darkChargingIcon = LoadIconBitmap("Dark", "dark_charging.ico");
            _lightChargingIcon = LoadIconBitmap("Light", "light_charging.ico");

            if (!string.IsNullOrWhiteSpace(_levelIconStem))
            {
                _darkLevelIcon = LoadIconBitmap("Dark", $"dark_{_levelIconStem}.ico");
                _lightLevelIcon = LoadIconBitmap("Light", $"light_{_levelIconStem}.ico");
            }
        }

        private static Bitmap? LoadIconBitmap(string themeFolder, string fileName)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Icons", themeFolder, fileName);
                if (!File.Exists(path))
                    return null;

                using Icon icon = new(path);
                using Bitmap source = icon.ToBitmap();
                return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap Colorize(Bitmap source, Color color)
        {
            Bitmap result = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color pixel = source.GetPixel(x, y);
                    result.SetPixel(x, y, Color.FromArgb(pixel.A, color.R, color.G, color.B));
                }
            }
            return result;
        }

        private static void DrawGlow(Graphics graphics, Bitmap source, RectangleF bounds, Color glowColor)
        {
            using Bitmap tinted = Colorize(source, glowColor);
            ColorMatrix alphaMatrix = new();
            alphaMatrix.Matrix33 = 0.12f;
            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(alphaMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            for (int radius = 3; radius >= 1; radius--)
            {
                float alpha = 0.12f + (3 - radius) * 0.05f;
                alphaMatrix.Matrix33 = alpha;
                attributes.SetColorMatrix(alphaMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                graphics.DrawImage(
                    tinted,
                    Rectangle.Round(new RectangleF(bounds.X - radius, bounds.Y - radius, bounds.Width + radius * 2, bounds.Height + radius * 2)),
                    0,
                    0,
                    tinted.Width,
                    tinted.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _darkIcon?.Dispose();
                _lightIcon?.Dispose();
                _darkChargingIcon?.Dispose();
                _lightChargingIcon?.Dispose();
                _darkLevelIcon?.Dispose();
                _lightLevelIcon?.Dispose();
                _darkIcon = null;
                _lightIcon = null;
                _darkChargingIcon = null;
                _lightChargingIcon = null;
            }
            base.Dispose(disposing);
        }
    }

    private sealed class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = LightBorder;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color OutsideBackColor { get; set; } = LightBackground;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.White;
            Padding = new Padding(0);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The rounded corners must reveal the exact background behind the card.
            // Do not rely on WinForms transparent-background emulation here: it can
            // expose the Form's default background at the corners and create dark
            // rectangular remnants around the rounded shape.
            e.Graphics.Clear(OutsideBackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Keep the complete path inside the client area. The border is centered
            // on the path, so a half-pixel inset prevents the stroke from being
            // clipped by the control bounds.
            const float borderInset = 0.75f;
            RectangleF rect = new(
                borderInset,
                borderInset,
                Math.Max(1f, Width - borderInset * 2f),
                Math.Max(1f, Height - borderInset * 2f));

            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 10);
            using Brush fill = new SolidBrush(BackColor);
            using Pen border = new(BorderColor, 1f) { LineJoin = LineJoin.Round };

            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
    }

    private enum AboutActionIcon { GitHub, Support, Documentation, External }


    private sealed class CustomizeDynamicIconColorsDialog : Form
    {
        private readonly PngIconCache _iconCache;
        private readonly bool _dark;
        private readonly AppLanguage _language;
        private readonly List<BatteryColorSettings> _colors;
        private readonly List<ColorSwatchControl> _swatches = new();
        private readonly List<CriticalBatteryNumericControl> _levels = new();
        private ToggleSwitchControl _gradientToggle = null!;
        private CriticalBatteryNumericControl _gradientStepInput = null!;
        private DynamicColorPreviewControl _preview = null!;
        private Label _highDescription = null!;
        private Label _mediumDescription = null!;
        private Label _lowDescription = null!;

        public IReadOnlyList<BatteryColorSettings> BatteryColors =>
            _colors.Select(c => new BatteryColorSettings
            {
                Name = c.Name,
                MinimumPercent = c.MinimumPercent,
                Argb = c.Argb
            }).ToList();

        public bool UseGradient => _gradientToggle.Checked;
        public int GradientPercent => _gradientStepInput.Value;

        public CustomizeDynamicIconColorsDialog(
            PngIconCache iconCache,
            bool dark,
            AppLanguage language,
            IEnumerable<BatteryColorSettings>? colors,
            bool useGradient,
            int gradientPercent)
        {
            _iconCache = iconCache;
            _dark = dark;
            _language = language;
            _colors = (colors ?? Enumerable.Empty<BatteryColorSettings>())
                .OrderByDescending(c => c.MinimumPercent)
                .Select(c => new BatteryColorSettings
                {
                    Name = c.Name,
                    MinimumPercent = Math.Clamp(c.MinimumPercent, 0, 100),
                    Argb = c.Argb
                })
                .ToList();

            if (_colors.Count != 3)
                _colors.Clear();

            if (_colors.Count == 0)
            {
                _colors.Add(new BatteryColorSettings { Name = "Green", MinimumPercent = 60, Color = Color.LimeGreen });
                _colors.Add(new BatteryColorSettings { Name = "Yellow", MinimumPercent = 30, Color = Color.Gold });
                _colors.Add(new BatteryColorSettings { Name = "Red", MinimumPercent = 0, Color = Color.Red });
            }

            Text = L("CustomizeDynamicIconColors");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(460, 522);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            TopMost = true;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = _dark ? DarkBackground : LightBackground;
            ForeColor = _dark ? Color.WhiteSmoke : LightText;
            Font = new Font("Segoe UI", 9f);

            Paint += (_, e) =>
            {
                using Pen separator = new(
                    _dark ? Color.FromArgb(53, 58, 63) : Color.FromArgb(220, 225, 232), 1f);
                e.Graphics.DrawLine(separator, 0, 45, ClientSize.Width, 45);
                e.Graphics.DrawLine(separator, 0, 466, ClientSize.Width, 466);
            };

            BuildHeader();
            BuildContent(useGradient, gradientPercent);
            BuildFooter();
            UpdateDescriptions();
            _preview!.RefreshPreview();
        }

        private string L(string key) => Localization.Get(key, _language);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (Width <= 0 || Height <= 0)
                return;

            Region?.Dispose();
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(
                new RectangleF(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f)),
                10);
            Region = new Region(path);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen border = new(
                _dark ? Color.FromArgb(66, 71, 76) : Color.FromArgb(210, 216, 224),
                1f);
            RectangleF rect = new(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 10);
            e.Graphics.DrawPath(border, path);
        }

        private void BuildHeader()
        {
            PngIconControl icon = new(_iconCache, "theme")
            {
                Location = new Point(16, 9),
                Size = new Size(27, 27),
                DarkMode = _dark
            };
            Controls.Add(icon);

            Label title = new()
            {
                Text = L("CustomizeDynamicIconColors"),
                Location = new Point(54, 11),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12.2f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            Controls.Add(title);

            Button close = new()
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                Font = new Font("Segoe UI", 16f),
                Location = new Point(422, 5),
                Size = new Size(28, 32),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            close.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(close);
        }

        private void BuildContent(bool useGradient, int gradientPercent)
        {
            Label introTitle = new()
            {
                Text = L("CustomizeDynamicIconColorsDescription"),
                Location = new Point(22, 57),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.7f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            Controls.Add(introTitle);

            Label introDescription = new()
            {
                Text = L("CustomizeDynamicIconColorsDescription2"),
                Location = new Point(22, 79),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = _dark ? DarkSecondary : LightSecondary,
                BackColor = Color.Transparent
            };
            Controls.Add(introDescription);

            RoundedPanel settingsCard = new()
            {
                Location = new Point(18, 101),
                Size = new Size(424, 271),
                BorderColor = _dark ? DarkBorder : LightBorder,
                OutsideBackColor = _dark ? Color.FromArgb(34, 37, 40) : Color.White,
                BackColor = _dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251)
            };
            Controls.Add(settingsCard);

            string[] titles =
            {
                L("HighBatteryColor"),
                L("MediumBatteryColor"),
                L("LowBatteryColor")
            };

            for (int i = 0; i < 3; i++)
            {
                int y = 9 + i * 50;
                int index = i;

                ColorSwatchControl swatch = new(_colors[i].Color, _dark)
                {
                    Location = new Point(12, y),
                    Size = new Size(48, 48)
                };
                swatch.ColorChanged += (_, _) =>
                {
                    _colors[index].Color = swatch.Color;
                    _preview.RefreshPreview();
                };
                settingsCard.Controls.Add(swatch);
                _swatches.Add(swatch);

                Label title = new()
                {
                    Text = titles[i],
                    Location = new Point(70, y + 5),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.1f),
                    ForeColor = _dark ? Color.WhiteSmoke : LightText,
                    BackColor = Color.Transparent
                };
                settingsCard.Controls.Add(title);

                Label description = new()
                {
                    Text = i == 2 ? L("UsedBelowThisLevel") : L("UsedFromThisLevelAndAbove"),
                    Location = new Point(70, y + 26),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7.8f),
                    ForeColor = _dark ? DarkSecondary : LightSecondary,
                    BackColor = Color.Transparent
                };
                if (i == 0) _highDescription = description;
                else if (i == 1) _mediumDescription = description;
                else _lowDescription = description;
                settingsCard.Controls.Add(description);

                Label levelTitle = new()
                {
                    Text = L("BatteryLevel"),
                    Location = new Point(274, y),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7.8f),
                    ForeColor = _dark ? Color.WhiteSmoke : LightText,
                    BackColor = Color.Transparent
                };
                settingsCard.Controls.Add(levelTitle);

                CriticalBatteryNumericControl levelInput = new()
                {
                    Location = new Point(273, y + 18),
                    Size = new Size(72, 26),
                    Minimum = 0,
                    Maximum = 100,
                    Value = _colors[i].MinimumPercent,
                    DarkMode = _dark
                };
                levelInput.ValueChanged += (_, _) =>
                {
                    _colors[index].MinimumPercent = levelInput.Value;
                    _preview.RefreshPreview();
                };
                settingsCard.Controls.Add(levelInput);
                _levels.Add(levelInput);

                Label percent = new()
                {
                    Text = "%",
                    Location = new Point(350, y + 23),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.2f),
                    ForeColor = _dark ? Color.WhiteSmoke : LightText,
                    BackColor = Color.Transparent
                };
                settingsCard.Controls.Add(percent);
            }

            Panel colorSectionSeparator = new()
            {
                Location = new Point(12, 164),
                Size = new Size(settingsCard.Width - 24, 1),
                BackColor = _dark ? Color.FromArgb(68, 73, 79) : Color.FromArgb(220, 225, 232)
            };
            settingsCard.Controls.Add(colorSectionSeparator);

            Label gradientLabel = new()
            {
                Text = L("UseGradient"),
                Location = new Point(12, 172),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.9f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            settingsCard.Controls.Add(gradientLabel);

            Label gradientDescription = new()
            {
                Text = L("UseGradientDescription"),
                Location = new Point(12, 191),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 7.8f),
                ForeColor = _dark ? DarkSecondary : LightSecondary,
                BackColor = Color.Transparent
            };
            settingsCard.Controls.Add(gradientDescription);

            _gradientToggle = new ToggleSwitchControl
            {
                Location = new Point(357, 169),
                Size = new Size(54, 28),
                Checked = useGradient,
                DarkMode = _dark
            };
            settingsCard.Controls.Add(_gradientToggle);

            Label transitionLabel = new()
            {
                Text = L("GradientTransitionStep"),
                Location = new Point(12, 225),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.9f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            settingsCard.Controls.Add(transitionLabel);

            Label transitionDescription = new()
            {
                Text = L("GradientTransitionStepDescription"),
                Location = new Point(12, 244),
                Size = new Size(285, 22),
                Font = new Font("Segoe UI", 7.7f),
                ForeColor = _dark ? DarkSecondary : LightSecondary,
                BackColor = Color.Transparent
            };
            settingsCard.Controls.Add(transitionDescription);

            _gradientStepInput = new CriticalBatteryNumericControl
            {
                Location = new Point(309, 223),
                Size = new Size(72, 26),
                Minimum = 0,
                Maximum = 50,
                Value = Math.Clamp(gradientPercent, 0, 50),
                DarkMode = _dark
            };
            settingsCard.Controls.Add(_gradientStepInput);

            Label stepPercent = new()
            {
                Text = "%",
                Location = new Point(386, 229),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            settingsCard.Controls.Add(stepPercent);

            RoundedPanel previewCard = new()
            {
                Location = new Point(18, 381),
                Size = new Size(424, 76),
                BorderColor = _dark ? DarkBorder : LightBorder,
                OutsideBackColor = _dark ? Color.FromArgb(34, 37, 40) : Color.White,
                BackColor = _dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251)
            };
            Controls.Add(previewCard);

            Label previewTitle = new()
            {
                Text = L("Preview"),
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.9f),
                ForeColor = _dark ? Color.WhiteSmoke : LightText,
                BackColor = Color.Transparent
            };
            previewCard.Controls.Add(previewTitle);

            _preview = new DynamicColorPreviewControl(_dark, _language, _colors, _gradientToggle, _gradientStepInput)
            {
                Location = new Point(8, 28),
                Size = new Size(408, 43)
            };
            previewCard.Controls.Add(_preview);

            _gradientToggle.CheckedChanged += (_, _) => _preview.RefreshPreview();
            _gradientStepInput.ValueChanged += (_, _) => _preview.RefreshPreview();
        }

        private void BuildFooter()
        {
            ActionButton reset = new(_iconCache)
            {
                Text = L("ResetToDefaults"),
                Location = new Point(16, 475),
                Size = new Size(178, 36),
                Font = new Font("Segoe UI", 8.7f),
                Primary = false,
                ShowResetIcon = true,
                DarkMode = _dark,
                OutsideBackColor = _dark ? DarkBackground : LightBackground
            };
            reset.Click += (_, _) =>
            {
                AppSettings defaults = AppSettings.CreateDefault();
                for (int i = 0; i < 3; i++)
                {
                    _colors[i].Name = defaults.BatteryColors[i].Name;
                    _colors[i].MinimumPercent = defaults.BatteryColors[i].MinimumPercent;
                    _colors[i].Argb = defaults.BatteryColors[i].Argb;
                    _swatches[i].Color = _colors[i].Color;
                    _levels[i].Value = _colors[i].MinimumPercent;
                }

                _gradientToggle.Checked = defaults.UseGradient;
                _gradientStepInput.Value = defaults.GradientPercent;
                _preview.RefreshPreview();
            };
            Controls.Add(reset);

            ActionButton ok = new(_iconCache)
            {
                Text = L("Ok"),
                Location = new Point(244, 475),
                Size = new Size(92, 36),
                Font = new Font("Segoe UI", 8.7f),
                Primary = true,
                DarkMode = _dark,
                OutsideBackColor = _dark ? DarkBackground : LightBackground
            };
            ok.Click += (_, _) =>
            {
                if (_colors[0].MinimumPercent <= _colors[1].MinimumPercent ||
                    _colors[1].MinimumPercent <= _colors[2].MinimumPercent)
                {
                    MessageBox.Show(
                        this,
                        L("ColorOrderError"),
                        L("InvalidSettings"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            ActionButton cancel = new(_iconCache)
            {
                Text = L("Cancel"),
                Location = new Point(346, 475),
                Size = new Size(98, 36),
                Font = new Font("Segoe UI", 8.7f),
                Primary = false,
                DarkMode = _dark,
                OutsideBackColor = _dark ? DarkBackground : LightBackground
            };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
        }

        private void UpdateDescriptions()
        {
            if (_levels.Count < 3)
                return;

            _highDescription.Text = L("UsedFromThisLevelAndAbove");
            _mediumDescription.Text = L("UsedFromThisLevelAndAbove");
            _lowDescription.Text = L("UsedBelowThisLevel");
        }
    }

    private sealed class ColorSwatchControl : Control
    {
        private Color _color;
        private readonly bool _dark;

        public event EventHandler? ColorChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;

                _color = value;
                Invalidate();
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ColorSwatchControl(Color color, bool dark)
        {
            _color = color;
            _dark = dark;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF outer = new(1, 1, Width - 2, Height - 2);
            using Brush background = new SolidBrush(
                _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251));
            using Pen border = new(
                _dark ? Color.FromArgb(91, 96, 102) : Color.FromArgb(170, 176, 185), 1f);

            e.Graphics.FillRoundedRectangle(background, outer, 7);
            e.Graphics.DrawRoundedRectangle(border, outer, 7);

            RectangleF swatch = new(8, 8, Width - 16, Height - 16);
            using Brush colorBrush = new SolidBrush(_color);
            e.Graphics.FillRoundedRectangle(colorBrush, swatch, 5);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            using ColorDialog dialog = new()
            {
                Color = _color,
                FullOpen = true,
                AnyColor = true
            };

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                Color = dialog.Color;
        }
    }

    private sealed class DynamicColorPreviewControl : Control
    {
        private readonly bool _dark;
        private readonly AppLanguage _language;
        private readonly List<BatteryColorSettings> _colors;
        private readonly ToggleSwitchControl _gradientToggle;
        private readonly CriticalBatteryNumericControl _gradientStep;
        private Bitmap? _darkIcon;
        private Bitmap? _lightIcon;
        private Bitmap? _darkCharging;
        private Bitmap? _lightCharging;

        public DynamicColorPreviewControl(
            bool dark,
            AppLanguage language,
            IEnumerable<BatteryColorSettings> colors,
            ToggleSwitchControl gradientToggle,
            CriticalBatteryNumericControl gradientStep)
        {
            _dark = dark;
            _language = language;
            _colors = colors.ToList();
            _gradientToggle = gradientToggle;
            _gradientStep = gradientStep;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            LoadBitmaps();
        }

        public void RefreshPreview() => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            List<BatteryColorSettings> sorted = _colors
                .OrderByDescending(c => c.MinimumPercent)
                .ToList();

            int high = Math.Clamp(sorted[0].MinimumPercent, 0, 100);
            int medium = Math.Clamp(sorted[1].MinimumPercent, 0, 100);
            int low = Math.Clamp(sorted[2].MinimumPercent, 0, 100);

            int[] samples =
            {
                high,
                medium,
                low,
                -1
            };

            string[] labels =
            {
                $">= {high}%",
                $"{Math.Max(medium, high - 1)} – {medium}%",
                $"{Math.Max(low, medium - 1)} – {low}%",
                Localization.Get("BatteryPreviewCharging", _language)
            };

            Bitmap? charging = _dark ? _darkCharging : _lightCharging;
            Bitmap? normal = _dark ? _darkIcon : _lightIcon;

            float slotWidth = Math.Max(1f, Width / (float)samples.Length);
            for (int i = 0; i < samples.Length; i++)
            {
                float x = i * slotWidth;
                RectangleF iconRect = new(x + (slotWidth - 30f) / 2f, 0, 30, 30);

                if (i == samples.Length - 1)
                {
                    if (charging != null)
                        e.Graphics.DrawImage(charging, iconRect);
                }
                else if (normal != null)
                {
                    Color color = GetBatteryColor(samples[i], sorted);
                    using Bitmap tinted = Colorize(normal, color);
                    e.Graphics.DrawImage(tinted, iconRect);
                }

                Rectangle labelRect = new((int)x, 30, (int)Math.Ceiling(slotWidth), 13);
                using Font font = new("Segoe UI", 7.2f);
                TextRenderer.DrawText(
                    e.Graphics,
                    labels[i],
                    font,
                    labelRect,
                    _dark ? Color.WhiteSmoke : LightText,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.EndEllipsis);
            }
        }

        private Color GetBatteryColor(int battery, List<BatteryColorSettings> colors)
        {
            battery = Math.Clamp(battery, 0, 100);

            int activeIndex = -1;
            for (int i = 0; i < colors.Count; i++)
            {
                if (battery >= colors[i].MinimumPercent)
                {
                    activeIndex = i;
                    break;
                }
            }

            if (activeIndex < 0)
                activeIndex = colors.Count - 1;

            Color active = colors[activeIndex].Color;

            if (!_gradientToggle.Checked ||
                _gradientStep.Value <= 0 ||
                activeIndex >= colors.Count - 1)
                return active;

            Color lower = colors[activeIndex + 1].Color;
            int transition = Math.Clamp(_gradientStep.Value, 0, 50);
            int start = colors[activeIndex].MinimumPercent;
            int end = Math.Max(colors[activeIndex + 1].MinimumPercent, start - transition);

            if (battery >= end && battery < start)
            {
                double t = (start - battery) /
                           (double)Math.Max(1, start - end);
                return Blend(active, lower, t);
            }

            return active;
        }

        private static Color Blend(Color first, Color second, double t)
        {
            t = Math.Clamp(t, 0d, 1d);
            int r = (int)Math.Round(first.R + (second.R - first.R) * t);
            int g = (int)Math.Round(first.G + (second.G - first.G) * t);
            int b = (int)Math.Round(first.B + (second.B - first.B) * t);
            return Color.FromArgb(255, r, g, b);
        }

        private static Bitmap Colorize(Bitmap source, Color color)
        {
            Bitmap result = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color pixel = source.GetPixel(x, y);
                    result.SetPixel(x, y, Color.FromArgb(pixel.A, color.R, color.G, color.B));
                }
            }
            return result;
        }

        private void LoadBitmaps()
        {
            _darkIcon = LoadIcon("Dark", "dark.ico");
            _lightIcon = LoadIcon("Light", "light.ico");
            _darkCharging = LoadIcon("Dark", "dark_charging.ico");
            _lightCharging = LoadIcon("Light", "light_charging.ico");
        }

        private static Bitmap? LoadIcon(string folder, string file)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Icons", folder, file);
                if (!File.Exists(path))
                    return null;

                using Icon icon = new(path);
                using Bitmap bitmap = icon.ToBitmap();
                return new Bitmap(bitmap);
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _darkIcon?.Dispose();
                _lightIcon?.Dispose();
                _darkCharging?.Dispose();
                _lightCharging?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class AboutActionButton : Button
    {
        private readonly PngIconCache _iconCache;
        private bool _hover;
        private bool _pressed;
        private bool _dark;
        private AboutActionIcon _icon;
        private string? _customIconPath;
        private Bitmap? _customIcon;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                if (_dark == value)
                    return;

                _dark = value;
                LoadCustomIcon();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color OutsideBackColor { get; set; } = LightBackground;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AboutActionIcon Icon { get => _icon; set { _icon = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CustomIconPath
        {
            get => _customIconPath;
            set
            {
                if (string.Equals(_customIconPath, value, StringComparison.Ordinal))
                    return;

                _customIconPath = value;
                LoadCustomIcon();
                Invalidate();
            }
        }

        public AboutActionButton(PngIconCache iconCache)
        {
            _iconCache = iconCache;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = false;
            SetStyle(ControlStyles.Selectable, false);
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };
            MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } };
            MouseUp += (_, _) => { _pressed = false; Invalidate(); };
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(OutsideBackColor);
        }

        protected override bool ShowFocusCues => false;

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (Width <= 0 || Height <= 0)
                return;

            Region?.Dispose();
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(
                new RectangleF(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f)), 7);
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.SetClip(ClientRectangle);
            e.Graphics.Clear(OutsideBackColor);

            Color fill = _dark ? Color.FromArgb(39, 43, 47) : Color.White;
            if (_hover)
                fill = _dark ? Color.FromArgb(48, 53, 58) : Color.FromArgb(247, 249, 252);
            if (_pressed)
                fill = _dark ? Color.FromArgb(32, 36, 40) : Color.FromArgb(239, 243, 248);

            Color border = _dark ? Color.FromArgb(82, 88, 95) : Color.FromArgb(198, 205, 214);
            RectangleF rect = new(1f, 1f, Math.Max(1f, Width - 2f), Math.Max(1f, Height - 2f));
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 7);
            using Brush brush = new SolidBrush(fill);
            using Pen pen = new(border, 1f)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);

            Rectangle iconRect = new((int)rect.X + 8, (Height - 25) / 2, 25, 25);
            if (_customIcon != null)
            {
                e.Graphics.DrawImage(_customIcon, iconRect);
            }
            else
            {
                string iconKey = _icon switch
                {
                    AboutActionIcon.GitHub => "git",
                    AboutActionIcon.Support => "support",
                    AboutActionIcon.Documentation => "documentation",
                    AboutActionIcon.External => "external",
                    _ => "git"
                };

                _iconCache.Draw(e.Graphics, iconKey, iconRect, _dark, DeviceDpi);
            }

            Rectangle textRect = Rectangle.Round(rect);
            textRect.X += 32;
            textRect.Width -= 32;
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
                _dark ? Color.WhiteSmoke : LightText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void LoadCustomIcon()
        {
            _customIcon?.Dispose();
            _customIcon = null;

            if (string.IsNullOrWhiteSpace(_customIconPath))
                return;

            try
            {
                if (!File.Exists(_customIconPath))
                    return;

                using Bitmap source = new(_customIconPath);
                _customIcon = new Bitmap(source);
            }
            catch
            {
                _customIcon = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _customIcon?.Dispose();
                _customIcon = null;
            }

            base.Dispose(disposing);
        }
    }
    private sealed class ActionButton : Button
    {
        private readonly PngIconCache _iconCache;
        private bool _hover;
        private bool _pressed;
        private bool _dark;
        private bool _primary;
        private bool _showResetIcon;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowResetIcon { get => _showResetIcon; set { _showResetIcon = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Primary { get => _primary; set { _primary = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color OutsideBackColor { get; set; } = LightBackground;

        public ActionButton(PngIconCache iconCache)
        {
            _iconCache = iconCache;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };
            MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } };
            MouseUp += (_, _) => { _pressed = false; Invalidate(); };
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(OutsideBackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.SetClip(ClientRectangle);

            e.Graphics.Clear(OutsideBackColor);
            Color fill = _primary ? Accent : (_dark ? Color.FromArgb(39, 43, 47) : Color.White);
            if (_hover && !_primary)
                fill = _dark ? Color.FromArgb(48, 53, 58) : Color.FromArgb(247, 249, 252);
            if (_pressed)
                fill = _primary ? Color.FromArgb(0, 105, 220) : (_dark ? Color.FromArgb(32, 36, 40) : Color.FromArgb(239, 243, 248));

            Color border = _dark ? Color.FromArgb(82, 88, 95) : Color.FromArgb(198, 205, 214);
            RectangleF rect = new(1f, 1f, Math.Max(1f, Width - 2f), Math.Max(1f, Height - 2f));
            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 7);
            using Brush brush = new SolidBrush(fill);
            using Pen pen = new(border, 1f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.FillPath(brush, path);
            if (!_primary)
                e.Graphics.DrawPath(pen, path);

            Rectangle textRect = Rectangle.Round(rect);
            if (_showResetIcon)
            {
                Color iconColor = _primary ? Color.White : (_dark ? Color.WhiteSmoke : LightText);
                _iconCache.Draw(e.Graphics, "reset", new RectangleF(rect.X + 10, rect.Y + 8, 20, 20), _dark, DeviceDpi);
                textRect.X += 30;
                textRect.Width -= 30;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
                _primary ? Color.White : (_dark ? Color.WhiteSmoke : LightText),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private static void DrawGlyph(Graphics graphics, Glyph glyph, Rectangle r, Color color, float width)
    {
        using Pen pen = new(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float x = r.X, y = r.Y, w = r.Width, h = r.Height;
        switch (glyph)
        {
            case Glyph.Headphones:
                graphics.DrawArc(pen, r, 180, 180);
                graphics.DrawLine(pen, x, y + h * .48f, x, y + h * .80f);
                graphics.DrawLine(pen, x + w, y + h * .48f, x + w, y + h * .80f);
                graphics.DrawRoundedRectangle(pen, new RectangleF(x - 1, y + h * .67f, w * .22f, h * .25f), 2);
                graphics.DrawRoundedRectangle(pen, new RectangleF(x + w - w * .22f + 1, y + h * .67f, w * .22f, h * .25f), 2);
                break;
            case Glyph.Monitor:
                graphics.DrawRoundedRectangle(pen, new RectangleF(x, y, w, h * .70f), 2);
                graphics.DrawLine(pen, x + w / 2, y + h * .70f, x + w / 2, y + h * .90f);
                graphics.DrawLine(pen, x + w * .30f, y + h * .90f, x + w * .70f, y + h * .90f);
                break;
            case Glyph.Battery:
                graphics.DrawRoundedRectangle(pen, new RectangleF(x + 1, y + 2, w * .78f, h - 4), 2);
                graphics.DrawLine(pen, x + w * .82f, y + h * .35f, x + w * .94f, y + h * .35f);
                graphics.DrawLine(pen, x + w * .94f, y + h * .35f, x + w * .94f, y + h * .65f);
                break;
            case Glyph.Bell:
                graphics.DrawArc(pen, new RectangleF(x + 2, y + 1, w - 4, h - 4), 205, 130);
                graphics.DrawLine(pen, x + 2, y + h * .78f, x + w - 2, y + h * .78f);
                graphics.DrawLine(pen, x + w * .43f, y + h * .90f, x + w * .57f, y + h * .90f);
                break;
            case Glyph.Gear:
                graphics.DrawEllipse(pen, new RectangleF(x + 3, y + 3, w - 6, h - 6));
                graphics.DrawEllipse(pen, new RectangleF(x + w * .36f, y + h * .36f, w * .28f, h * .28f));
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    float cx = x + w / 2 + (w * .36f) * (float)Math.Cos(a);
                    float cy = y + h / 2 + (h * .36f) * (float)Math.Sin(a);
                    graphics.DrawLine(pen, x + w / 2 + (w * .29f) * (float)Math.Cos(a), y + h / 2 + (h * .29f) * (float)Math.Sin(a), cx, cy);
                }
                break;
            case Glyph.Info:
                graphics.DrawEllipse(pen, r);
                using (Brush b = new SolidBrush(color))
                {
                    graphics.FillEllipse(b, new RectangleF(x + w / 2 - 1.5f, y + 5, 3, 3));
                    graphics.FillRoundedRectangle(b, new RectangleF(x + w / 2 - 1.5f, y + 10, 3, h * .35f), 1);
                }
                break;
            case Glyph.Globe:
                graphics.DrawEllipse(pen, r);
                graphics.DrawEllipse(pen, new RectangleF(x + w * .28f, y, w * .44f, h));
                graphics.DrawLine(pen, x + 1, y + h / 2, x + w - 1, y + h / 2);
                break;
            case Glyph.Palette:
                graphics.DrawEllipse(pen, r);
                using (Brush b = new SolidBrush(color))
                {
                    graphics.FillEllipse(b, new RectangleF(x + w * .28f, y + h * .27f, 3, 3));
                    graphics.FillEllipse(b, new RectangleF(x + w * .53f, y + h * .20f, 3, 3));
                    graphics.FillEllipse(b, new RectangleF(x + w * .69f, y + h * .39f, 3, 3));
                }
                break;
            case Glyph.Document:
                graphics.DrawRectangle(pen, new RectangleF(x + 2, y + 1, w * .72f, h - 3));
                graphics.DrawLine(pen, x + w * .55f, y + 1, x + w * .75f, y + h * .20f);
                graphics.DrawLine(pen, x + w * .55f, y + 1, x + w * .55f, y + h * .20f);
                graphics.DrawLine(pen, x + w * .55f, y + h * .20f, x + w * .75f, y + h * .20f);
                graphics.DrawLine(pen, x + 5, y + h * .48f, x + w * .60f, y + h * .48f);
                graphics.DrawLine(pen, x + 5, y + h * .68f, x + w * .60f, y + h * .68f);
                break;
            case Glyph.Windows:
                graphics.DrawLine(pen, x + w * .48f, y, x + w * .46f, y + h);
                graphics.DrawLine(pen, x, y + h * .48f, x + w, y + h * .46f);
                break;
        }
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rectangle, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(rectangle, radius);
        graphics.DrawPath(pen, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF rectangle, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(rectangle, radius);
        graphics.DrawPath(pen, path);
    }

    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rectangle, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(rectangle, radius);
        graphics.FillPath(brush, path);
    }

    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rectangle, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(rectangle, radius);
        graphics.FillPath(brush, path);
    }

    internal static GraphicsPath CreateRoundedPath(RectangleF rectangle, int radius)
    {
        float d = Math.Min(radius * 2f, Math.Min(rectangle.Width, rectangle.Height));
        GraphicsPath path = new();
        path.AddArc(rectangle.X, rectangle.Y, d, d, 180, 90);
        path.AddArc(rectangle.Right - d, rectangle.Y, d, d, 270, 90);
        path.AddArc(rectangle.Right - d, rectangle.Bottom - d, d, d, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    internal static GraphicsPath CreateRoundedPath(Rectangle rectangle, int radius)
    {
        int d = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
        GraphicsPath path = new();
        path.AddArc(rectangle.X, rectangle.Y, d, d, 180, 90);
        path.AddArc(rectangle.Right - d, rectangle.Y, d, d, 270, 90);
        path.AddArc(rectangle.Right - d, rectangle.Bottom - d, d, d, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
