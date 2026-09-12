using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Monitoring;
using HyperXBatteryTray.Notifications;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

public sealed class HyperXBatteryMonitorApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly HyperXDeviceManager _deviceManager;
    private BatteryMonitor? _batteryMonitor;
    private IHyperXDevice? _device;
    private readonly SettingsManager _settingsManager;
    private readonly AppSettings _settings;
    private readonly NotificationService _notificationService;

    private Icon? _currentApplicationIcon;
    private SettingsForm? _settingsForm;
    private TrayContextMenuForm? _trayMenu;
    private bool _blinkState;
	private bool _isCharging;
    private System.Windows.Forms.Timer? _blinkTimer;

    public HyperXBatteryMonitorApplicationContext()
    {
        _settingsManager = new SettingsManager();
        _settings = _settingsManager.Load();
        _notificationService = new NotificationService();

        if (!_settings.ThemeConfigured)
        {
            _settings.Theme = AppTheme.System;
            _settings.ThemeConfigured = true;
            _settingsManager.Save(_settings);
        }

        _deviceManager = new HyperXDeviceManager();

        // Do not enumerate or open HID devices until the user has explicitly
        // selected a supported device in Settings.
        if (!string.IsNullOrWhiteSpace(_settings.SelectedDevice))
            InitializeSelectedDevice();

        _currentApplicationIcon = CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _currentApplicationIcon,
            Visible = true
        };
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        ApplyLocalization();
        ApplyTheme();
        UpdateTrayIcon();
        RestartBlinkTimer();

        UpdateTray();

        if (string.IsNullOrWhiteSpace(_settings.SelectedDevice))
            Application.Idle += Application_Idle;

        if (_batteryMonitor != null)
            _batteryMonitor.Start();
    }

    private void InitializeSelectedDevice()
    {
        DisposeDeviceMonitor();

        if (string.IsNullOrWhiteSpace(_settings.SelectedDevice))
            return;

        _device = _deviceManager.GetFirstAvailableDevice();

        if (_device == null)
            return;

        _batteryMonitor = new BatteryMonitor(_device)
        {
            IntervalMilliseconds = 5000
        };

        _batteryMonitor.BatteryChanged += BatteryMonitor_BatteryChanged;
        _batteryMonitor.ConnectionChanged += BatteryMonitor_ConnectionChanged;
        _batteryMonitor.ChargingChanged += BatteryMonitor_ChargingChanged;
    }

    private void DisposeDeviceMonitor()
    {
        if (_batteryMonitor != null)
        {
            _batteryMonitor.BatteryChanged -= BatteryMonitor_BatteryChanged;
            _batteryMonitor.ConnectionChanged -= BatteryMonitor_ConnectionChanged;
            _batteryMonitor.ChargingChanged -= BatteryMonitor_ChargingChanged;
            _batteryMonitor.Dispose();
            _batteryMonitor = null;
        }

        _device = null;
        _isCharging = false;
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e) => ShowSettings(sender, e);

    private void Application_Idle(object? sender, EventArgs e)
    {
        Application.Idle -= Application_Idle;
        ShowSettings(null, EventArgs.Empty);
    }

    private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (_settings.Theme != AppTheme.System || _trayMenu == null || _trayMenu.IsDisposed)
            return;

        if (_trayMenu.IsHandleCreated)
        {
            try
            {
                _trayMenu.BeginInvoke((MethodInvoker)(() =>
                    _trayMenu?.ApplyTheme(GetEffectiveTheme() == AppTheme.Dark)));
            }
            catch (InvalidOperationException)
            {
                // The application is shutting down or the handle is no longer valid.
            }
        }
    }

    private static AppTheme GetWindowsTheme()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            object? value = key?.GetValue("AppsUseLightTheme");

            if (value is int intValue)
                return intValue == 0
                    ? AppTheme.Dark
                    : AppTheme.Light;
        }
        catch
        {
        }

        return AppTheme.Light;
    }

    private string L(string key) => Localization.Get(key, _settings.Language);
    private AppTheme GetEffectiveTheme() =>
        _settings.Theme == AppTheme.System
            ? GetWindowsTheme()
            : _settings.Theme;



    private void ApplyLocalization()
    {
        _trayMenu?.ApplyLocalization();
        UpdateTray();
    }

    private void ApplyTheme()
    {
        _trayMenu?.ApplyTheme(GetEffectiveTheme() == AppTheme.Dark);
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            ShowTrayContextMenu(Cursor.Position);
    }

    private void ShowTrayContextMenu(Point screenLocation)
    {
        if (_trayMenu == null || _trayMenu.IsDisposed)
        {
            _trayMenu = new TrayContextMenuForm(
                _settings,
                _device,
                _isCharging,
                GetEffectiveTheme(),
                ShowSettings,
                ExitApplication);
            _trayMenu.FormClosed += (_, _) => _trayMenu = null;
        }
        else
        {
            _trayMenu.UpdateDevice(_device, _isCharging);
        }

        _trayMenu.ApplyLocalization();
        _trayMenu.ApplyTheme(GetEffectiveTheme() == AppTheme.Dark);
        _trayMenu.ShowAt(screenLocation);
    }

    private void ShowSettings(object? sender, EventArgs e)
    {
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            if (_settingsForm.WindowState == FormWindowState.Minimized)
                _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.Show();
            _settingsForm.BringToFront();
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_settings, _device, _isCharging);
        _settingsForm.SettingsApplied += SettingsForm_SettingsApplied;
        _settingsForm.FormClosed += SettingsForm_FormClosed;
        _settingsForm.Show();
        _settingsForm.BringToFront();
        _settingsForm.Activate();
    }

    private void SettingsForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is SettingsForm form)
            form.SettingsApplied -= SettingsForm_SettingsApplied;
        _settingsForm = null;
    }

    private void SettingsForm_SettingsApplied(object? sender, EventArgs e)
    {
        try
        {
            _settingsManager.Save(_settings);
            _notificationService.ResetState();
            InitializeSelectedDevice();
            _settingsForm?.SetDevice(_device);
            ApplyLocalization();
            ApplyTheme();
            UpdateTrayIcon();
            RestartBlinkTimer();
            UpdateTray();

            if (_batteryMonitor != null)
                _batteryMonitor.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _settingsForm,
                string.Format(L("SaveError"), ex.Message),
                "HyperX Battery Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BatteryMonitor_BatteryChanged(object? sender, int battery)
    {
        if (_device != null)
        {
            _notificationService.Update(
                _settings.SelectedDevice,
                battery,
                _device.IsConnected,
                _isCharging,
                _settings);
        }

        UpdateTrayIcon();
        UpdateTray();
        _trayMenu?.UpdateDevice(_device, _isCharging);
    }

    private void BatteryMonitor_ConnectionChanged(object? sender, bool connected)
    {
        if (!connected)
        {
            _isCharging = false;
            _notificationService.ResetState();
        }

        if (connected && _device != null)
        {
            _notificationService.Update(
                _settings.SelectedDevice,
                _device.Battery,
                true,
                _isCharging,
                _settings);
        }

        _settingsForm?.SetCharging(_isCharging);
        UpdateTrayIcon();
        UpdateTray();
        _trayMenu?.UpdateDevice(_device, _isCharging);
    }

    private void BatteryMonitor_ChargingChanged(
        object? sender,
        bool charging)
    {
        _isCharging = charging;

        if (_device != null)
        {
            _notificationService.Update(
                _settings.SelectedDevice,
                _device.Battery,
                _device.IsConnected,
                _isCharging,
                _settings);
        }

        _settingsForm?.SetCharging(_isCharging);
        UpdateTrayIcon();
        UpdateTray();
        _trayMenu?.UpdateDevice(_device, _isCharging);
    }

    private void UpdateTray()
    {
        UpdateNotifyIconTooltip();
        _trayMenu?.UpdateDevice(_device, _isCharging);
    }

    private void UpdateNotifyIconTooltip()
    {
        bool selected = !string.IsNullOrWhiteSpace(_settings.SelectedDevice);
        string deviceName = selected ? NormalizeDeviceName(_settings.SelectedDevice) : L("UnknownHeadphones");
        string battery;
        string status;

        if (!selected || _device == null || !_device.IsConnected || _device.Battery < 0)
        {
            battery = L("TrayBatteryNA");
            status = L("TrayDisconnected");
        }
        else
        {
            battery = string.Format(L("TrayBattery"), Math.Clamp(_device.Battery, 0, 100));
            if (_isCharging)
                battery += $" {L("TrayCharging")}";
            status = L("TrayConnected");
        }

        _notifyIcon.Text = $"{deviceName}\n{battery}\n{status}";
    }

    private static string NormalizeDeviceName(string value) =>
        string.Equals(value, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III" : value;

    private void RestartBlinkTimer()
    {
        _blinkTimer?.Stop();
        _blinkTimer?.Dispose();
        _blinkTimer = null;

        if (!_settings.BlinkOnCriticalBattery)
            return;

        _blinkTimer = new System.Windows.Forms.Timer { Interval = 600 };
        _blinkTimer.Tick += (_, _) =>
        {
            if (_device == null || !_device.IsConnected || _device.Battery < 0 ||
                _device.Battery > _settings.CriticalBatteryPercent)
            {
                _blinkState = false;
                UpdateTrayIcon();
                return;
            }

            _blinkState = !_blinkState;
            UpdateTrayIcon();
        };
        _blinkTimer.Start();
    }

    private void UpdateTrayIcon()
	{
		Icon? baseIcon = null;
		Icon? newIcon = null;

		try
		{
			if (_blinkState && IsCriticalBattery())
			{
				baseIcon = CreateThemeIcon();
			}
			else
			{
				baseIcon = CreateTrayIcon();
			}

			if (_isCharging &&
				_device?.IsConnected == true &&
				_device.Battery >= 0)
			{
				if (_settings.DisplayMode == BatteryDisplayMode.BatteryIndicator)
				{
					baseIcon.Dispose();
					baseIcon = null;
					newIcon = CreateChargingIcon();
				}
				else
				{
					newIcon = CreateChargingOverlayIcon(baseIcon);

					baseIcon.Dispose();
					baseIcon = null;
				}
			}
			else
			{
				newIcon = baseIcon;
				baseIcon = null;
			}

			Icon? oldIcon = _currentApplicationIcon;

			_currentApplicationIcon = newIcon;
			_notifyIcon.Icon = newIcon;

			oldIcon?.Dispose();
		}
		catch
		{
			newIcon?.Dispose();
		}
		finally
		{
			baseIcon?.Dispose();
		}
	}

    private bool IsCriticalBattery() =>
        _device != null &&
        _device.IsConnected &&
        _device.Battery >= 0 &&
        _device.Battery <= _settings.CriticalBatteryPercent;

    private Icon CreateTrayIcon()
    {
        bool connected =
            !string.IsNullOrWhiteSpace(_settings.SelectedDevice) &&
            _device?.IsConnected == true &&
            _device.Battery >= 0 &&
            _device.Battery <= 100;

        if (!connected)
            return CreateDisconnectedIcon();

        int battery = _device!.Battery;
        Color batteryColor = GetBatteryColor(battery);

        if (_settings.DisplayMode == BatteryDisplayMode.StaticIcon)
            return CreateThemeIcon();

        if (_settings.DisplayMode == BatteryDisplayMode.BatteryIndicator)
            return CreateBatteryIndicatorIcon(battery);

        return _settings.AdvancedDisplayMode switch
        {
            AdvancedDisplayMode.BatteryGradient =>
                CreateRenderedIcon(
                    batteryColor,
                    null,
                    colorizeHeadset: true),

            AdvancedDisplayMode.BatteryIndicator =>
                CreateRenderedIcon(
                    batteryColor,
                    battery,
                    colorizeHeadset: false),

            AdvancedDisplayMode.PercentageText =>
                CreatePercentageOverlayIcon(
                    battery,
                    connected),

            _ =>
                CreateThemeIcon()
        };
    }

    private Icon CreateBatteryIndicatorIcon(int battery)
    {
        string suffix = battery switch
        {
            >= 50 => "green",
            >= 30 => "yellow",
            >= 15 => "orange",
            _ => "red"
        };

        return LoadIconFile($"{GetThemePrefix()}_{suffix}.ico");
    }

    private Icon CreateChargingIcon() =>
        LoadIconFile($"{GetThemePrefix()}_charging.ico");

    private string GetThemePrefix() =>
        GetEffectiveTheme() == AppTheme.Dark ? "dark" : "light";

    private Icon LoadIconFile(string fileName)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Icons",
            GetEffectiveTheme() == AppTheme.Dark ? "Dark" : "Light",
            fileName);

        try
        {
            if (File.Exists(path))
                return new Icon(path);
        }
        catch
        {
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            ?? new Icon(SystemIcons.Application, SystemIcons.Application.Size);
    }

	private Icon CreateChargingOverlayIcon(Icon baseIcon)
	{
		using Bitmap bitmap =
			new Bitmap(
				16,
				16,
				PixelFormat.Format32bppArgb);

		using Graphics graphics =
			Graphics.FromImage(bitmap);

		graphics.SmoothingMode =
			SmoothingMode.AntiAlias;

		graphics.InterpolationMode =
			InterpolationMode.HighQualityBicubic;

		graphics.PixelOffsetMode =
			PixelOffsetMode.HighQuality;

		graphics.Clear(Color.Transparent);

		using Bitmap iconBitmap =
			RenderIcon(
				baseIcon,
				16,
				16,
				replacementColor: null);

		graphics.DrawImage(
			iconBitmap,
			0,
			0,
			16,
			16);

		if (_settings.DisplayMode == BatteryDisplayMode.Advanced &&
			_settings.AdvancedDisplayMode == AdvancedDisplayMode.BatteryIndicator)
		{
			DrawChargingBoltInsideBattery(graphics);
		}
		else if (_settings.DisplayMode == BatteryDisplayMode.Advanced &&
			_settings.AdvancedDisplayMode == AdvancedDisplayMode.PercentageText)
		{
			DrawChargingBoltAfterPercentage(graphics);
		}
		else
		{
			DrawChargingBoltOnRight(graphics);
		}

		return BitmapToIcon(bitmap);
	}

	private void DrawChargingBoltOnRight(Graphics graphics)
	{
		using var path = new GraphicsPath();

		path.AddPolygon(new[]
		{
			new PointF(15.5f, 0.5f),
			new PointF(8.8f, 8.0f),
			new PointF(11.9f, 8.0f),
			new PointF(9.5f, 15.5f),
			new PointF(17.0f, 6.0f),
			new PointF(13.6f, 6.0f)
		});

		using var outlineBrush = new SolidBrush(
			Color.FromArgb(235, 0, 0, 0));

		graphics.FillPath(outlineBrush, path);

		using var innerPath = new GraphicsPath();

		innerPath.AddPolygon(new[]
		{
			new PointF(14.8f, 2.2f),
			new PointF(10.5f, 7.2f),
			new PointF(13.0f, 7.2f),
			new PointF(11.2f, 12.8f),
			new PointF(15.5f, 6.8f),
			new PointF(13.2f, 6.8f)
		});

		using var chargingBrush = new SolidBrush(Color.LimeGreen);

		graphics.FillPath(chargingBrush, innerPath);
	}

	private void DrawChargingBoltInsideBattery(Graphics graphics)
	{
		using var path = new GraphicsPath();

		path.AddPolygon(new[]
		{
			new PointF(15.5f, 2.0f),
			new PointF(10.5f, 8.0f),
			new PointF(12.9f, 8.0f),
			new PointF(11.0f, 14.5f),
			new PointF(16.8f, 6.0f),
			new PointF(13.9f, 6.0f)
		});

		using var outlineBrush = new SolidBrush(
			Color.FromArgb(235, 0, 0, 0));

		graphics.FillPath(outlineBrush, path);

		using var innerPath = new GraphicsPath();

		innerPath.AddPolygon(new[]
		{
			new PointF(14.8f, 3.0f),
			new PointF(11.5f, 7.5f),
			new PointF(13.5f, 7.5f),
			new PointF(12.2f, 12.0f),
			new PointF(15.3f, 6.8f),
			new PointF(13.5f, 6.8f)
		});

		using var chargingBrush = new SolidBrush(Color.LimeGreen);

		graphics.FillPath(chargingBrush, innerPath);
	}

	private void DrawChargingBoltAfterPercentage(Graphics graphics)
	{
		using var path = new GraphicsPath();

		path.AddPolygon(new[]
		{
			new PointF(15.8f, 2.5f),
			new PointF(12.8f, 7.0f),
			new PointF(14.3f, 7.0f),
			new PointF(13.0f, 12.5f),
			new PointF(16.5f, 7.0f),
			new PointF(14.9f, 7.0f)
		});

		using var outlineBrush = new SolidBrush(
			Color.FromArgb(235, 0, 0, 0));

		graphics.FillPath(outlineBrush, path);

		using var innerPath = new GraphicsPath();

		innerPath.AddPolygon(new[]
		{
			new PointF(15.3f, 3.5f),
			new PointF(13.5f, 6.5f),
			new PointF(14.8f, 6.5f),
			new PointF(13.9f, 10.3f),
			new PointF(15.7f, 6.7f),
			new PointF(14.5f, 6.7f)
		});

		using var chargingBrush = new SolidBrush(Color.LimeGreen);

		graphics.FillPath(chargingBrush, innerPath);
	}

    private Icon CreateDisconnectedIcon()
    {
        using Bitmap bitmap =
            new Bitmap(
                16,
                16,
                PixelFormat.Format32bppArgb);

        using Graphics graphics =
            Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        using Icon source = CreateThemeIcon();

        using Bitmap iconBitmap =
            RenderIcon(
                source,
                18,
                18,
                replacementColor: null);

        graphics.DrawImage(
            iconBitmap,
            -1,
            -1,
            18,
            18);

        using var pen =
            new Pen(
                Color.Red,
                1.8f);

        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

        graphics.DrawLine(
            pen,
            10,
            10,
            15,
            15);

        graphics.DrawLine(
            pen,
            15,
            10,
            10,
            15);

        return BitmapToIcon(bitmap);
    }

    private Icon CreateThemeIcon() =>
        LoadIconFile($"{GetThemePrefix()}.ico");

    private Icon CreateRenderedIcon(
        Color batteryColor,
        int? battery,
        bool colorizeHeadset)
    {
        // Battery-gradient mode preserves the exact 16x16 scale of the
        // static tray icon and changes only its color.
        if (colorizeHeadset && !battery.HasValue)
        {
            using Bitmap bitmap =
                new Bitmap(
                    16,
                    16,
                    PixelFormat.Format32bppArgb);

            using Graphics graphics =
                Graphics.FromImage(bitmap);

            graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            graphics.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            graphics.PixelOffsetMode =
                PixelOffsetMode.HighQuality;

            graphics.Clear(Color.Transparent);

            using Icon source = CreateThemeIcon();

            using Bitmap iconBitmap =
                RenderIcon(
                    source,
                    16,
                    16,
                    batteryColor);

            graphics.DrawImage(
                iconBitmap,
                0,
                0,
                16,
                16);

            return BitmapToIcon(bitmap);
        }

        using Bitmap compositeBitmap =
            new Bitmap(
                16,
                16,
                PixelFormat.Format32bppArgb);

        using Graphics compositeGraphics =
            Graphics.FromImage(compositeBitmap);

        compositeGraphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        compositeGraphics.InterpolationMode =
            InterpolationMode.HighQualityBicubic;

        compositeGraphics.PixelOffsetMode =
            PixelOffsetMode.HighQuality;

        compositeGraphics.Clear(Color.Transparent);

        using Icon compositeSource = CreateThemeIcon();

        // This layout is exclusively for the advanced battery-indicator
        // mode: the headset is on the left and the battery is beside it.
        const int headsetWidth = 11;
        const int headsetHeight = 16;

        using Bitmap headsetBitmap =
            RenderIcon(
                compositeSource,
                headsetWidth,
                headsetHeight,
                null);

        compositeGraphics.DrawImage(
            headsetBitmap,
            0,
            0,
            headsetWidth,
            headsetHeight);

        if (battery.HasValue)
        {
            DrawBattery(
                compositeGraphics,
                9,
                1,
                7,
                14,
                battery.Value,
                batteryColor);
        }

        return BitmapToIcon(compositeBitmap);
    }

    private Icon CreatePercentageOverlayIcon(
        int battery,
        bool connected)
    {
        using Bitmap bitmap =
            new Bitmap(
                16,
                16,
                PixelFormat.Format32bppArgb);

        using Graphics graphics =
            Graphics.FromImage(bitmap);

        graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        graphics.InterpolationMode =
            InterpolationMode.HighQualityBicubic;

        graphics.PixelOffsetMode =
            PixelOffsetMode.HighQuality;

        graphics.Clear(Color.Transparent);

        using Icon source = CreateThemeIcon();

        using Bitmap iconBitmap =
            RenderIcon(
                source,
                18,
                18,
                replacementColor: null);

        graphics.DrawImage(
            iconBitmap,
            -1,
            -1,
            18,
            18);

        Color textColor =
            connected
                ? GetBatteryColor(battery)
                : Color.Red;

        using var font =
            new Font(
                "Segoe UI",
                5.5f,
                FontStyle.Bold,
                GraphicsUnit.Point);

        string text = $"{battery}%";

        SizeF size =
            graphics.MeasureString(
                text,
                font);

        // Center the percentage over the headset and place a compact,
        // high-contrast background behind it so it remains readable.
        float backgroundWidth =
            Math.Min(15f, size.Width + 4f);

        const float backgroundHeight = 8f;

        float backgroundX =
            (16f - backgroundWidth) / 2f;

        float backgroundY =
            (16f - backgroundHeight) / 2f;

        Color backgroundColor =
            GetEffectiveTheme() == AppTheme.Dark
                ? Color.FromArgb(205, 20, 20, 22)
                : Color.FromArgb(215, 245, 245, 245);

        using var backgroundBrush =
            new SolidBrush(backgroundColor);

        using var backgroundPath =
            new GraphicsPath();

        const float radius = 2f;
        float diameter = radius * 2f;

        backgroundPath.AddArc(
            backgroundX,
            backgroundY,
            diameter,
            diameter,
            180,
            90);

        backgroundPath.AddArc(
            backgroundX + backgroundWidth - diameter,
            backgroundY,
            diameter,
            diameter,
            270,
            90);

        backgroundPath.AddArc(
            backgroundX + backgroundWidth - diameter,
            backgroundY + backgroundHeight - diameter,
            diameter,
            diameter,
            0,
            90);

        backgroundPath.AddArc(
            backgroundX,
            backgroundY + backgroundHeight - diameter,
            diameter,
            diameter,
            90,
            90);

        backgroundPath.CloseFigure();
        graphics.FillPath(backgroundBrush, backgroundPath);

        float x =
            backgroundX +
            (backgroundWidth - size.Width) / 2f;

        float y =
            backgroundY +
            (backgroundHeight - size.Height) / 2f -
            0.8f;

        using var outlineBrush =
            new SolidBrush(
                GetEffectiveTheme() == AppTheme.Dark
                    ? Color.Black
                    : Color.White);

        // Stronger outline around the percentage.
        graphics.DrawString(text, font, outlineBrush, x - 0.7f, y);
        graphics.DrawString(text, font, outlineBrush, x + 0.7f, y);
        graphics.DrawString(text, font, outlineBrush, x, y - 0.7f);
        graphics.DrawString(text, font, outlineBrush, x, y + 0.7f);

        using var brush =
            new SolidBrush(textColor);

        graphics.DrawString(
            text,
            font,
            brush,
            x,
            y);

        return BitmapToIcon(bitmap);
    }

    private static Bitmap RenderIcon(Icon source, int width, int height, Color? replacementColor)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.DrawIcon(source, new Rectangle(0, 0, width, height));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!replacementColor.HasValue)
                    continue;

                Color pixel = bitmap.GetPixel(x, y);

                if (pixel.A > 0)
                {
                    Color replacement = replacementColor.Value;

                    bitmap.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            pixel.A,
                            replacement.R,
                            replacement.G,
                            replacement.B));
                }
            }
        }
        return bitmap;
    }

    private void DrawBattery(
        Graphics graphics,
        int x,
        int y,
        int width,
        int height,
        int percentage,
        Color fillColor)
    {
        Color outlineColor =
            GetEffectiveTheme() == AppTheme.Dark
                ? Color.FromArgb(248, 248, 248)
                : Color.FromArgb(70, 70, 70);

        // Strong, compact outline comparable to the reference tray glyph.
        const float outlineWidth = 1.55f;

        using var pen =
            new Pen(
                outlineColor,
                outlineWidth);

        pen.Alignment = PenAlignment.Center;

        DrawRoundedRectangle(
            graphics,
            pen,
            x,
            y + 2,
            width,
            height - 2,
            1.2f);

        using var terminal =
            new SolidBrush(outlineColor);

        FillRoundedRectangle(
            graphics,
            terminal,
            x + 1,
            y,
            width - 2,
            2.5f,
            0.8f);

        // Opaque dark interior for the unfilled portion of the battery.
        // This is deliberately not transparent: the tray background must
        // never show through the empty charge area.
        Color emptyColor =
            GetEffectiveTheme() == AppTheme.Dark
                ? Color.FromArgb(38, 38, 40)
                : Color.FromArgb(55, 55, 58);

        FillRoundedRectangle(
            graphics,
            new SolidBrush(emptyColor),
            x + 1.2f,
            y + 3.2f,
            width - 2.4f,
            height - 4.2f,
            0.7f);

        int innerHeight =
            Math.Max(
                0,
                (height - 6) *
                Math.Clamp(percentage, 0, 100) /
                100);

        if (innerHeight > 0)
        {
            using var fill =
                new SolidBrush(fillColor);

            graphics.FillRectangle(
                fill,
                x + 1.2f,
                y + height - 1.1f - innerHeight,
                width - 2.4f,
                innerHeight);
        }
    }

    private static void DrawRoundedRectangle(
        Graphics graphics,
        Pen pen,
        float x,
        float y,
        float width,
        float height,
        float radius)
    {
        using var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }

    private static void FillRoundedRectangle(
        Graphics graphics,
        Brush brush,
        float x,
        float y,
        float width,
        float height,
        float radius)
    {
        using var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }

    private static Icon BitmapToIcon(Bitmap bitmap)
    {
        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using Icon temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIconNative(hIcon);
        }
    }

    [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIconNative(IntPtr hIcon);

    private Color GetBatteryColor(int battery)
    {
        battery = Math.Clamp(battery, 0, 100);

        List<BatteryColorSettings> colors = _settings.BatteryColors
            .Where(c => c != null)
            .OrderByDescending(c => c.MinimumPercent)
            .ToList();

        if (colors.Count == 0)
            return Color.LimeGreen;

        // The first configured range whose threshold is at or below the
        // current battery level is the active color.
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

        Color activeColor = colors[activeIndex].Color;

        if (!_settings.UseGradient ||
            _settings.GradientPercent <= 0 ||
            activeIndex >= colors.Count - 1)
        {
            return activeColor;
        }

        BatteryColorSettings lower = colors[activeIndex + 1];
        int transition = Math.Clamp(_settings.GradientPercent, 0, 50);
        int start = colors[activeIndex].MinimumPercent;
        int end = Math.Max(lower.MinimumPercent, start - transition);

        if (battery >= end && battery < start)
        {
            double t = (start - battery) / (double)Math.Max(1, start - end);
            return Blend(activeColor, lower.Color, t);
        }

        return activeColor;
    }

    private static Color Blend(Color first, Color second, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(first.R + (second.R - first.R) * t),
            (int)Math.Round(first.G + (second.G - first.G) * t),
            (int)Math.Round(first.B + (second.B - first.B) * t));
    }

    private void ShowAbout(object? sender, EventArgs e)
	{
		using var aboutForm =
			new AboutForm(
				_settings.Language,
				GetEffectiveTheme());

		aboutForm.ShowDialog();
	}

    private void ExitApplication(object? sender, EventArgs e) => ExitThread();

    protected override void ExitThreadCore()
    {
        _settingsForm?.Close();
        _blinkTimer?.Stop();
        _blinkTimer?.Dispose();
        _blinkTimer = null;

        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _notifyIcon.MouseUp -= NotifyIcon_MouseUp;
        _trayMenu?.Close();
        _trayMenu?.Dispose();
        _trayMenu = null;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentApplicationIcon?.Dispose();
        _currentApplicationIcon = null;

        DisposeDeviceMonitor();
        _notificationService.ResetState();
        _deviceManager.Dispose();
        base.ExitThreadCore();
    }
}

