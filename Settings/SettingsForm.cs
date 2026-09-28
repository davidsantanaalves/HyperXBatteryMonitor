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

public sealed partial class SettingsForm : Form
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
    private TableLayoutPanel? _activePageLayout;
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
    private BatteryIconControl _sidebarBatteryIcon = null!;
    private PictureBox _sidebarChargingIcon = null!;
    private bool? _sidebarChargingIconDark;
    private Label _sidebarMicrophoneTitleLabel = null!;
    private PictureBox _sidebarMicrophoneIcon = null!;
    private Label _sidebarMicrophoneLabel = null!;
    private bool? _sidebarMicrophoneIconDark;
    private bool? _sidebarMicrophoneIconMuted;
    private readonly Dictionary<string, SidebarItem> _navButtons = new();
    private IHyperXDevice? _device;
    private AppLanguage _selectedLanguage;
    private AppTheme _selectedTheme;
    private string _pendingSelectedDevice;
    private bool _pendingStartupEnabled;
    private bool _pendingNotifyOnLowBattery;
    private bool _pendingNotifyWhenFullyCharged;
    private bool _pendingBlinkOnCriticalBattery;
    private bool _pendingShowMicrophoneMuteInSystray;
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
    private ToggleSwitchControl _showMicrophoneMuteInSystrayToggle = null!;
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

    private const int LogicalDpi = 96;
    private const int SidebarFirstSeparatorLogicalY = 236;
    private const int SidebarSecondSeparatorLogicalY = 458;
    private const int SidebarDeviceCardLogicalHeight = 190;
    private const int SidebarDeviceCardLogicalTop =
        SidebarFirstSeparatorLogicalY +
        (SidebarSecondSeparatorLogicalY - SidebarFirstSeparatorLogicalY - SidebarDeviceCardLogicalHeight) / 2;
    private const int SidebarTitleLogicalLeft = 8;
    private const int SidebarTitleLogicalWidth = 50;
    private const int SidebarBatteryTitleLogicalWidth = 46;
    private const int SidebarIconColumnLogicalLeft = 50;
    private const int SidebarIconColumnLogicalWidth = 32;
    private const int SidebarValueLogicalLeft = 83;
    private const int SidebarStatusRowLogicalTop = 97;
    private const int SidebarStatusDotLogicalTop = 97;
    private const int SidebarStatusDotLogicalSize = 18;
    private const int SidebarStatusTextVerticalOffsetLogical = 1;
    private const int SidebarBatteryRowLogicalTop = 128;
    private const int SidebarBatteryIconLogicalTop = 122;
    private const int SidebarBatteryIconLogicalSize = 32;
    private const int SidebarChargingIconLogicalTop = 125;
    private const int SidebarChargingIconLogicalSize = 25;
    private const int SidebarMicrophoneRowLogicalTop = 160;
    private const int SidebarMicrophoneIconLogicalTop = 157;
    private const int SidebarMicrophoneIconLogicalSize = 25;
    private const int SidebarContentRightInsetLogical = 0;
    private const int SidebarInlineIconGapLogical = 3;
    private const string MicrophoneOpenIconAssetName = "mic";
    private const string MicrophoneMutedIconAssetName = "mute";
    private const string ChargingIconAssetName = "lightning";
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const string DarkScrollableThemeClass = "DarkMode_Explorer";
    private const string LightScrollableThemeClass = "Explorer";

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme(EffectiveTheme == AppTheme.Dark);
        ApplyCurrentScrollThemes(EffectiveTheme == AppTheme.Dark);
        PositionFooterButtons();
        _iconCache.ClearBitmaps();
        WarmUpIcons();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        _iconCache.ClearBitmaps();
        WarmUpIcons();
        _sidebar.Invalidate();

        if (IsHandleCreated && !IsDisposed && !Disposing)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                if (!IsDisposed && !Disposing)
                    ShowPage(_currentPage);
            }));
        }
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

    private void ApplyCurrentScrollThemes(bool dark)
    {
        if (!IsHandleCreated)
            return;

        ApplyNativeScrollTheme(_pageHost, dark);
        if (_activePageLayout != null)
            ApplyNativeScrollTheme(_activePageLayout, dark);
    }

    private static void ApplyNativeScrollTheme(ScrollableControl control, bool dark)
    {
        if (control.IsDisposed || control.Disposing)
            return;

        try
        {
            if (!control.IsHandleCreated)
                control.CreateControl();

            if (!control.IsHandleCreated)
                return;

            _ = SetWindowTheme(
                control.Handle,
                dark ? DarkScrollableThemeClass : LightScrollableThemeClass,
                null);
            control.Invalidate();
        }
        catch (DllNotFoundException)
        {
            // uxtheme.dll is part of supported Windows versions; keep default styling if unavailable.
        }
        catch (EntryPointNotFoundException)
        {
            // Keep the default scrollbar styling if the API is unavailable.
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
        _pendingStartupEnabled = settings.IsNewSettingsProfile
            ? AppSettings.DefaultStartWithWindows
            : _startupManager.IsEnabled();
        _pendingNotifyOnLowBattery = settings.NotifyOnLowBattery;
        _pendingNotifyWhenFullyCharged = settings.NotifyWhenFullyCharged;
        _pendingBlinkOnCriticalBattery = settings.BlinkOnCriticalBattery;
        _pendingShowMicrophoneMuteInSystray = settings.ShowMicrophoneMuteInSystray;
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
            Padding = new Padding(20, 22, 20, 66),
            BackColor = LightBackground,
            AutoScroll = true
        };

        Controls.Add(_pageHost);
        Controls.Add(_sidebar);

        _sidebarDeviceCard = new RoundedPanel
        {
            Location = new Point(10, SidebarDeviceCardLogicalTop),
            Size = new Size(_sidebar.ClientSize.Width - 20, SidebarDeviceCardLogicalHeight),
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
            Size = new Size(SidebarTitleLogicalWidth, SidebarStatusDotLogicalSize),
            Location = new Point(SidebarTitleLogicalLeft, SidebarStatusRowLogicalTop),
            Text = L("SidebarStatus"),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarStatusDot = new StatusDotControl
        {
            Size = new Size(SidebarStatusDotLogicalSize, SidebarStatusDotLogicalSize),
            Location = new Point(
                SidebarIconColumnLogicalLeft + (SidebarIconColumnLogicalWidth - SidebarStatusDotLogicalSize) / 2,
                SidebarStatusDotLogicalTop)
        };

        _sidebarStatusLabel = new Label
        {
            AutoSize = false,
            Size = new Size(
                _sidebarDeviceCard.Width - SidebarValueLogicalLeft - SidebarContentRightInsetLogical,
                SidebarStatusDotLogicalSize),
            Location = new Point(
                SidebarValueLogicalLeft,
                SidebarStatusRowLogicalTop + SidebarStatusTextVerticalOffsetLogical),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarBatteryTitleLabel = new Label
        {
            AutoSize = false,
            Size = new Size(SidebarBatteryTitleLogicalWidth, 20),
            Location = new Point(SidebarTitleLogicalLeft, SidebarBatteryRowLogicalTop),
            Text = L("SidebarBattery"),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarBatteryIcon = new BatteryIconControl
        {
            Size = new Size(SidebarBatteryIconLogicalSize, SidebarBatteryIconLogicalSize),
            Location = new Point(SidebarIconColumnLogicalLeft, SidebarBatteryIconLogicalTop),
            DarkMode = false
        };

        _sidebarBatteryLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebarDeviceCard.Width - SidebarValueLogicalLeft - SidebarContentRightInsetLogical, 20),
            Location = new Point(SidebarValueLogicalLeft, SidebarBatteryRowLogicalTop),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarChargingIcon = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(SidebarChargingIconLogicalSize, SidebarChargingIconLogicalSize),
            Location = new Point(SidebarValueLogicalLeft, SidebarChargingIconLogicalTop),
            BackColor = Color.Transparent,
            Visible = false
        };

        _sidebarMicrophoneTitleLabel = new Label
        {
            AutoSize = false,
            Size = new Size(_sidebarStatusTitleLabel.Width, _sidebarStatusTitleLabel.Height),
            Location = new Point(_sidebarStatusTitleLabel.Left, SidebarMicrophoneRowLogicalTop),
            Text = L("SidebarMicrophone"),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarMicrophoneIcon = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(SidebarMicrophoneIconLogicalSize, SidebarMicrophoneIconLogicalSize),
            Location = new Point(
                SidebarIconColumnLogicalLeft + (SidebarIconColumnLogicalWidth - SidebarMicrophoneIconLogicalSize + 1) / 2,
                SidebarMicrophoneIconLogicalTop),
            BackColor = Color.Transparent
        };

        _sidebarMicrophoneLabel = new Label
        {
            AutoSize = false,
            Size = new Size(
                _sidebarDeviceCard.Width - SidebarValueLogicalLeft - SidebarContentRightInsetLogical,
                _sidebarStatusLabel.Height),
            Location = new Point(
                SidebarValueLogicalLeft,
                SidebarMicrophoneRowLogicalTop),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _sidebarDeviceCard.Controls.Add(_sidebarDeviceImage);
        _sidebarDeviceCard.Controls.Add(_sidebarDeviceNameLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusTitleLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusDot);
        _sidebarDeviceCard.Controls.Add(_sidebarStatusLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryTitleLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryIcon);
        _sidebarDeviceCard.Controls.Add(_sidebarBatteryLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarChargingIcon);
        _sidebarDeviceCard.Controls.Add(_sidebarMicrophoneTitleLabel);
        _sidebarDeviceCard.Controls.Add(_sidebarMicrophoneIcon);
        _sidebarDeviceCard.Controls.Add(_sidebarMicrophoneLabel);
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

        // Configure WinForms DPI autoscaling only after the complete 96-DPI control tree
        // exists. Runtime-created pages use ScaleUi after the handle is available.
        AutoScaleDimensions = new SizeF(LogicalDpi, LogicalDpi);
        AutoScaleMode = AutoScaleMode.Dpi;

        ApplyTheme(_selectedTheme);
        PositionWindowAtTop();
        RefreshDeviceStatus();

        if (_device != null)
        {
            _device.BatteryChanged += Device_BatteryChanged;
            _device.MicrophoneMuteChanged += Device_MicrophoneMuteChanged;
        }
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
        {
            _device.BatteryChanged -= Device_BatteryChanged;
            _device.MicrophoneMuteChanged -= Device_MicrophoneMuteChanged;
        }

        _device = device;

        if (_device != null)
        {
            _device.BatteryChanged += Device_BatteryChanged;
            _device.MicrophoneMuteChanged += Device_MicrophoneMuteChanged;
        }

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

}
