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
    private Label _deviceInfoTextLabel = null!;
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
    private PictureBox _logo = null!;
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
        Icon = LoadThemeIcon(EffectiveTheme);

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

        _logo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(128, 47),
            Location = new Point(20, ClientSize.Height - 120),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _applicationNameLabel = new Label
        {
            AutoSize = true,
            Text = Application.ProductName,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Location = new Point(20, ClientSize.Height - 68),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _versionLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(20, ClientSize.Height - 38),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _sidebar.Controls.Add(_logo);
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
        string[] keys = { "Device", "Interface", "BatteryMonitor", "Notifications", "General", "About" };
        Glyph[] glyphs = { Glyph.Headphones, Glyph.Monitor, Glyph.Battery, Glyph.Bell, Glyph.Gear, Glyph.Info };
        string[] iconKeys =
        {
            "device",
            "interface",
            "battery_monitor",
            "notification",
            "general",
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

        RoundedPanel card = CreateDeviceCard(new Point(20, 86), new Size(540, 342));
        _pageHost.Controls.Add(card);

        Label label = CreateDeviceLabel(L("DeviceLabelShort"), true, new Point(20, 0), 9.5f);
        card.Controls.Add(label);

        _deviceSelector = new DeviceSelector
        {
            Location = new Point(122, 18),
            Size = new Size(398, 64),
            SelectedIndex = 0,
            DarkMode = EffectiveTheme == AppTheme.Dark
        };
        _deviceSelector.SetPlaceholder(StatusText("LocateDevice", "Locate your device", "Localize seu dispositivo", "Localiza tu dispositivo"));
        _deviceSelector.SelectedDeviceName = _pendingSelectedDevice;
        _deviceSelector.SelectionChanged += DeviceSelector_SelectionChanged;
        card.Controls.Add(_deviceSelector);
        label.Location = new Point(label.Left, _deviceSelector.Top + (_deviceSelector.Height - label.Height) / 2);

        RoundedPanel statusCard = CreateDeviceCard(new Point(20, 108), new Size(500, 82), true);
        statusCard.BackColor = EffectiveTheme == AppTheme.Dark ? Color.FromArgb(46, 50, 54) : Color.FromArgb(248, 249, 251);
        card.Controls.Add(statusCard);

        _deviceStatusDot = new StatusDotControl
        {
            Size = new Size(24, 24),
            Location = new Point(17, 28)
        };
        statusCard.Controls.Add(_deviceStatusDot);

        _deviceStatusLabel = CreateDeviceLabel(string.Empty, true, new Point(52, 13), 11f);
        _deviceStatusDescriptionLabel = CreateDeviceLabel(string.Empty, false, new Point(52, 39), 9f);
        statusCard.Controls.Add(_deviceStatusLabel);
        statusCard.Controls.Add(_deviceStatusDescriptionLabel);

        _batteryIcon = new BatteryIconControl
        {
            Location = new Point(327, 17),
            Size = new Size(46, 46)
        };
        statusCard.Controls.Add(_batteryIcon);

        _batteryValueLabel = CreateDeviceLabel(string.Empty, true, new Point(380, 21), 15f);
        statusCard.Controls.Add(_batteryValueLabel);

        _chargingLabel = CreateDeviceLabel(string.Empty, false, new Point(380, 46), 8.5f);
        _chargingLabel.Visible = false;
        statusCard.Controls.Add(_chargingLabel);

        RoundedPanel info = CreateDeviceCard(new Point(20, 215), new Size(500, 108), true);
        info.BackColor = EffectiveTheme == AppTheme.Dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251);
        card.Controls.Add(info);
        info.Controls.Add(new GlyphControl(Glyph.Info, Accent) { Location = new Point(17, 25), Size = new Size(28, 28) });
        info.Controls.Add(CreateDeviceLabel(L("DeviceInformation"), true, new Point(56, 14), 9.5f));
        _deviceInfoTextLabel = CreateDeviceLabel(L("DeviceInformationText"), false, new Point(56, 38), 8.8f);
        _deviceInfoTextLabel.MaximumSize = new Size(405, 0);
        info.Controls.Add(_deviceInfoTextLabel);
    }

    private void ShowInterfacePage()
    {
        _pageHost.Controls.Clear();
        AddInterfacePageHeader();

        // Interface uses independent cards so future layout changes stay isolated
        // from the finalized Device page.
        RoundedPanel languageCard = CreateInterfaceCard(new Point(20, 82), new Size(528, 68), true);
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

        RoundedPanel themeCard = CreateInterfaceCard(new Point(20, 158), new Size(528, 202), true);
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

        RoundedPanel startupCard = CreateInterfaceCard(new Point(20, 376), new Size(528, 68), true);
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

        RoundedPanel lowBatteryCard = CreateCard(new Point(20, 82), new Size(528, 117), true);
        _pageHost.Controls.Add(lowBatteryCard);
        lowBatteryCard.Controls.Add(new PngIconControl(_iconCache, "battery_critical")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        lowBatteryCard.Controls.Add(CreateLabel(L("NotifyOnLowBattery"), true, new Point(68, 12), 9.5f));
        Label lowBatteryDescription = CreateLabel(L("NotifyOnLowBatteryDescription"), false, new Point(68, 34), 8.5f);
        lowBatteryDescription.MaximumSize = new Size(280, 0);
        lowBatteryCard.Controls.Add(lowBatteryDescription);
        lowBatteryCard.Controls.Add(CreateLabel(L("CriticalBatteryLevel"), true, new Point(68, 70), 8.8f));
        Label criticalBatteryDescription = CreateLabel(L("CriticalBatteryLevelDescription"), false, new Point(68, 91), 7.8f);
        criticalBatteryDescription.MaximumSize = new Size(280, 0);
        lowBatteryCard.Controls.Add(criticalBatteryDescription);

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
            Location = new Point(418, 68),
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
        lowBatteryCard.Controls.Add(CreateLabel("%", false, new Point(496, 77), 9f));

        RoundedPanel fullCard = CreateCard(new Point(20, 211), new Size(528, 82), true);
        _pageHost.Controls.Add(fullCard);
        fullCard.Controls.Add(new PngIconControl(_iconCache, "battery_full")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        fullCard.Controls.Add(CreateLabel(L("NotifyWhenFullyCharged"), true, new Point(68, 18), 9.5f));
        Label fullyChargedDescription = CreateLabel(L("NotifyWhenFullyChargedDescription"), false, new Point(68, 40), 8.5f);
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

        RoundedPanel blinkCard = CreateCard(new Point(20, 305), new Size(528, 82), true);
        _pageHost.Controls.Add(blinkCard);
        blinkCard.Controls.Add(new PngIconControl(_iconCache, "blink")
        {
            Location = new Point(18, 18),
            Size = new Size(36, 36),
            DarkMode = EffectiveTheme == AppTheme.Dark
        });
        blinkCard.Controls.Add(CreateLabel(L("FlashSystrayIcon"), true, new Point(68, 18), 9.5f));
        Label blinkDescription = CreateLabel(L("FlashSystrayIconDescription"), false, new Point(68, 40), 8.5f);
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
            if (key == "Notifications")
            {
                ShowNotificationsPage();
                return;
            }

            _pageHost.Controls.Clear();
            AddPageHeader(key switch
            {
                "BatteryMonitor" => "battery_monitor",
                "Notifications" => "notification",
                "General" => "general",
                "About" => "about",
                _ => "about"
            }, Glyph.Info, key, key switch
            {
                "BatteryMonitor" => "ComingSoonBatteryMonitor",
                "Notifications" => "ComingSoonNotifications",
                "General" => "ComingSoonGeneral",
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

    private void DeviceSelector_SelectionChanged(object? sender, EventArgs e)
    {
        _pendingSelectedDevice = _deviceSelector.SelectedDeviceName;
        RefreshDeviceStatus();
    }

    private void RefreshDeviceStatus()
    {
        bool selected = _deviceSelector.SelectedIndex > 0;
        bool connected = selected && _device?.IsConnected == true && _device.Battery >= 0 && _device.Battery <= 100;
        _deviceStatusLabel.Text = connected ? StatusText("Connected", "Connected", "Conectado", "Conectado") : selected ? StatusText("Disconnected", "Disconnected", "Desconectado", "Desconectado") : StatusText("UnknownHeadphones", "Unknown headphones", "Fone desconhecido", "Auriculares desconocidos");
        _deviceStatusDescriptionLabel.Text = connected ? StatusText("ConnectedDescription", "Your device is ready to use.", "Seu dispositivo está pronto para uso.", "Tu dispositivo está listo para usar.") : selected ? StatusText("DisconnectedDescription", "Your device is not currently connected.", "Seu dispositivo não está conectado no momento.", "Tu dispositivo no está conectado actualmente.") : L("UnknownDeviceDescription");
        _deviceStatusLabel.Visible = true;
        _deviceStatusDescriptionLabel.Visible = true;
        _batteryValueLabel.Text = connected ? $"{Math.Clamp(_device!.Battery, 0, 100)}%" : "N/A";
        _chargingLabel.Text = _isCharging ? L("ChargingStatus") : string.Empty;
        _chargingLabel.Visible = connected && _isCharging;
        Color primaryText = EffectiveTheme == AppTheme.Dark ? Color.WhiteSmoke : LightText;
        Color secondaryText = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
        _deviceStatusLabel.ForeColor = primaryText;
        _deviceStatusDescriptionLabel.ForeColor = secondaryText;
        _batteryValueLabel.ForeColor = primaryText;
        _chargingLabel.ForeColor = secondaryText;
        if (_batteryIcon != null)
        {
            _batteryIcon.Connected = connected;
            _batteryIcon.Battery = connected && _device != null ? Math.Clamp(_device.Battery, 0, 100) : 0;
            _batteryIcon.DarkMode = EffectiveTheme == AppTheme.Dark;
            _batteryIcon.Charging = connected && _isCharging;
            _batteryIcon.Invalidate();
        }
        _deviceStatusDot.Connected = connected;
        _deviceStatusDot.Invalidate();
        UpdateDeviceInformation();
    }

    private void UpdateDeviceInformation()
    {
        if (_deviceInfoTextLabel == null) return;

        string device = _deviceSelector?.SelectedDeviceName ?? string.Empty;
        string normalized = string.Equals(device, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III"
            : device;

        string[] keys = normalized switch
        {
            "HyperX Cloud III" => new[] { "Cloud3Wireless_Connectivity", "Cloud3Wireless_Range", "Cloud3Wireless_Battery", "Cloud3Wireless_ChargeTime" },
            "HyperX Cloud III S" => new[] { "Cloud3S_Connectivity", "Cloud3S_Range", "Cloud3S_Battery", "Cloud3S_ChargeTime" },
            "HyperX Cloud 2 Core" => new[] { "Cloud2Core_Connectivity", "Cloud2Core_Range", "Cloud2Core_Battery", "Cloud2Core_ChargeTime" },
            "HyperX Cloud Alpha" => new[] { "CloudAlpha_Connectivity", "CloudAlpha_Range", "CloudAlpha_Battery", "CloudAlpha_ChargeTime" },
            "HyperX Cloud Stinger 2" => new[] { "CloudStinger2_Connectivity", "CloudStinger2_Range", "CloudStinger2_Battery", "CloudStinger2_ChargeTime" },
            _ => Array.Empty<string>()
        };

        _deviceInfoTextLabel.Text = keys.Length == 0
            ? L("DeviceInformationText")
            : string.Join(Environment.NewLine, keys.Select(L));
        _deviceInfoTextLabel.ForeColor = EffectiveTheme == AppTheme.Dark ? DarkSecondary : LightSecondary;
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
        if (_deviceSelector != null)
            _deviceSelector.SetPlaceholder(StatusText("LocateDevice", "Locate your device", "Localize seu dispositivo", "Localiza tu dispositivo"));
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

        try
        {
            string logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "HyperXLogo.png");
            if (File.Exists(logoPath))
            {
                using Image source = Image.FromFile(logoPath);
                _logo.Image?.Dispose();
                _logo.Image = new Bitmap(source);
            }
        }
        catch { }

        Icon = LoadThemeIcon(dark ? AppTheme.Dark : AppTheme.Light);
        WarmUpIcons();
        ApplyThemeRecursive(this, foreground, dark);

        if (_lightThemeOption != null) _lightThemeOption.DarkMode = dark;
        if (_darkThemeOption != null) _darkThemeOption.DarkMode = dark;
        if (_systemThemeOption != null) _systemThemeOption.DarkMode = dark;
        if (_startupToggle != null) _startupToggle.DarkMode = dark;
        if (_notifyOnLowBatteryToggle != null) _notifyOnLowBatteryToggle.DarkMode = dark;
        if (_notifyWhenFullyChargedToggle != null) _notifyWhenFullyChargedToggle.DarkMode = dark;
        if (_blinkOnCriticalBatteryToggle != null) _blinkOnCriticalBatteryToggle.DarkMode = dark;
        if (_criticalBatteryPercentInput != null)
            _criticalBatteryPercentInput.DarkMode = dark;

        foreach (SidebarItem item in _navButtons.Values)
        {
            item.DarkMode = dark;
            item.Invalidate();
        }

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

    private void ApplyThemeRecursive(Control parent, Color foreground, bool dark)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is PngIconControl pngIcon)
            {
                pngIcon.DarkMode = dark;
                continue;
            }

            if (c is SidebarItem || c is Button || c is DeviceSelector || c is StatusDotControl || c is BatteryIconControl || c == _footer)
                continue;

            c.ForeColor = foreground;

            if (c is RoundedPanel panel)
            {
                panel.BackColor = panel.Tag is string s && s == "outer"
                    ? (dark ? Color.FromArgb(34, 37, 40) : Color.White)
                    : (dark ? Color.FromArgb(42, 45, 48) : Color.FromArgb(248, 249, 251));
                panel.BorderColor = dark ? DarkBorder : LightBorder;
                panel.OutsideBackColor = panel.Tag is string innerTag && innerTag == "inner"
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

    private bool ApplySettings()
    {
        _settings.SelectedDevice = _pendingSelectedDevice;
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
        _logo.Image?.Dispose();
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
            new PngIconRequest("general", 25),
            new PngIconRequest("about", 25),
            new PngIconRequest("device", 36),
            new PngIconRequest("interface", 36),
            new PngIconRequest("battery_monitor", 36),
            new PngIconRequest("notification", 36),
            new PngIconRequest("general", 36),
            new PngIconRequest("about", 36),
            new PngIconRequest("language", 25),
            new PngIconRequest("theme", 25),
            new PngIconRequest("windows", 25),
            new PngIconRequest("light", 36),
            new PngIconRequest("dark", 36),
            new PngIconRequest("interface", 36),
            new PngIconRequest("reset", 20)
        }, EffectiveTheme == AppTheme.Dark, DeviceDpi);
    }

    private string StatusText(string key, string english, string portuguese, string spanish)
    {
        try
        {
            string value = L(key);
            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, key, StringComparison.Ordinal))
                return value;
        }
        catch (KeyNotFoundException) { }

        return _selectedLanguage switch
        {
            AppLanguage.PortugueseBrazil => portuguese,
            AppLanguage.Spanish => spanish,
            _ => english
        };
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
    }

    private enum Glyph { Headphones, Monitor, Battery, Bell, Gear, Info, Globe, Palette, Windows }

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
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Width - 54, Height), _selected ? Color.FromArgb(0, 105, 220) : text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
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
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

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

        public RestoreDefaultsDialog(string title, string question, bool dark)
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

            Button yesButton = CreateButton("Yes", DialogResult.Yes, true);
            Button noButton = CreateButton("No", DialogResult.No, false);

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
            _textBox.Focus();
            _textBox.SelectAll();
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

            Point[] up =
            {
                new(centerX - 3, 16),
                new(centerX, 13),
                new(centerX + 3, 16)
            };
            Point[] down =
            {
                new(centerX - 3, 25),
                new(centerX, 28),
                new(centerX + 3, 25)
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
                    _textBox.Focus();
                    _textBox.SelectAll();
                }
            }));
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
            e.Graphics.FillEllipse(brush, 3, 3, 18, 18);
            using Pen glow = new(Color.FromArgb(90, statusColor.R, statusColor.G, statusColor.B), 2f);
            e.Graphics.DrawEllipse(glow, 1, 1, 22, 22);
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
        private string _placeholder = "Locate your device";
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
            _placeholder = string.IsNullOrWhiteSpace(placeholder) ? "Locate your device" : placeholder;
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
