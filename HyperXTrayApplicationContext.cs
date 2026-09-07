using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Monitoring;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

public sealed class HyperXTrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly HyperXDeviceManager _deviceManager;
    private readonly BatteryMonitor? _batteryMonitor;
    private readonly IHyperXDevice? _device;
    private readonly SettingsManager _settingsManager;
    private readonly AppSettings _settings;

    private readonly ToolStripMenuItem _deviceMenuItem;
    private readonly ToolStripMenuItem _batteryMenuItem;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _aboutMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    private Icon? _currentApplicationIcon;
    private SettingsForm? _settingsForm;
    private bool _blinkState;
    private System.Windows.Forms.Timer? _blinkTimer;

    public HyperXTrayApplicationContext()
    {
        _settingsManager = new SettingsManager();
        _settings = _settingsManager.Load();

        if (!_settings.ThemeConfigured)
        {
            _settings.Theme = GetWindowsTheme();
            _settingsManager.Save(_settings);
        }

        _deviceManager = new HyperXDeviceManager();
        _device = _deviceManager.GetFirstAvailableDevice();

        _deviceMenuItem = new ToolStripMenuItem { Enabled = false };
        _batteryMenuItem = new ToolStripMenuItem { Enabled = false };
        _statusMenuItem = new ToolStripMenuItem { Enabled = false };

        _contextMenu = new ContextMenuStrip { ShowImageMargin = false };
        _settingsMenuItem = new ToolStripMenuItem();
        _aboutMenuItem = new ToolStripMenuItem();
        _exitMenuItem = new ToolStripMenuItem();
        _settingsMenuItem.Click += ShowSettings;
        _aboutMenuItem.Click += ShowAbout;
        _exitMenuItem.Click += ExitApplication;

        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            _deviceMenuItem,
            _batteryMenuItem,
            _statusMenuItem,
            new ToolStripSeparator(),
            _settingsMenuItem,
            _aboutMenuItem,
            _exitMenuItem
        });

        _currentApplicationIcon = CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _currentApplicationIcon,
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

        ApplyLocalization();
        ApplyTheme();
        UpdateTrayIcon();
        RestartBlinkTimer();

        if (_device != null)
        {
            _batteryMonitor = new BatteryMonitor(_device)
            {
                IntervalMilliseconds = 5000
            };
            _batteryMonitor.BatteryChanged += BatteryMonitor_BatteryChanged;
            _batteryMonitor.ConnectionChanged += BatteryMonitor_ConnectionChanged;
            UpdateTray();
            _batteryMonitor.Start();
        }
        else
        {
            UpdateTray();
        }
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e) => ShowSettings(sender, e);

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

    private void ApplyLocalization()
    {
        _settingsMenuItem.Text = L("Settings");
        _aboutMenuItem.Text = L("About");
        _exitMenuItem.Text = L("Exit");
        UpdateTray();
    }

    private void ApplyTheme()
    {
        bool dark = _settings.Theme == AppTheme.Dark;
        _contextMenu.Renderer = new ToolStripProfessionalRenderer(new TrayColorTable(dark));
        Color back = dark ? Color.FromArgb(45, 45, 48) : SystemColors.Menu;
        Color fore = dark ? Color.WhiteSmoke : SystemColors.MenuText;
        _contextMenu.BackColor = back;
        _contextMenu.ForeColor = fore;
        foreach (ToolStripItem item in _contextMenu.Items)
        {
            item.BackColor = back;
            item.ForeColor = fore;
        }
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

        _settingsForm = new SettingsForm(_settings);
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
            ApplyLocalization();
            ApplyTheme();
            UpdateTrayIcon();
            RestartBlinkTimer();
            UpdateTray();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _settingsForm,
                string.Format(L("SaveError"), ex.Message),
                "HyperX Battery Tray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BatteryMonitor_BatteryChanged(object? sender, int battery)
    {
        UpdateTrayIcon();
        UpdateTray();
    }

    private void BatteryMonitor_ConnectionChanged(object? sender, bool connected)
    {
        UpdateTrayIcon();
        UpdateTray();
    }

    private void UpdateTray()
    {
        if (_device == null)
        {
            _deviceMenuItem.Text = "HyperX Cloud III Wireless";
            _batteryMenuItem.Text = L("TrayBatteryNA");
            _statusMenuItem.Text = L("TrayDisconnected");
            _notifyIcon.Text = string.Format(L("TrayTooltip"), "N/A");
            return;
        }

        bool connected = _device.IsConnected && _device.Battery >= 0;
        _deviceMenuItem.Text = "HyperX Cloud III Wireless";

        if (connected)
        {
            _batteryMenuItem.Text = string.Format(L("TrayBattery"), _device.Battery);
            _statusMenuItem.Text = L("TrayConnected");
            _notifyIcon.Text = string.Format(L("TrayTooltip"), $"{_device.Battery}%");
        }
        else
        {
            _batteryMenuItem.Text = L("TrayBatteryNA");
            _statusMenuItem.Text = L("TrayDisconnected");
            _notifyIcon.Text = string.Format(L("TrayTooltip"), "N/A");
        }

        if (_notifyIcon.Text.Length > 63)
            _notifyIcon.Text = _notifyIcon.Text[..63];
    }

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
        Icon? newIcon = null;
        try
        {
            if (_blinkState && IsCriticalBattery())
            {
                newIcon = CreateThemeIcon();
            }
            else
            {
                newIcon = CreateTrayIcon();
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
    }

    private bool IsCriticalBattery() =>
        _device != null &&
        _device.IsConnected &&
        _device.Battery >= 0 &&
        _device.Battery <= _settings.CriticalBatteryPercent;

    private Icon CreateTrayIcon()
    {
        bool connected =
            _device?.IsConnected == true &&
            _device.Battery >= 0 &&
            _device.Battery <= 100;

        if (!connected)
            return CreateDisconnectedIcon();

        int battery = _device!.Battery;
        Color batteryColor = GetBatteryColor(battery);

        return _settings.DisplayMode switch
        {
            BatteryDisplayMode.StaticIcon =>
                CreateThemeIcon(),

            BatteryDisplayMode.ColoredIcon =>
                CreateRenderedIcon(
                    batteryColor,
                    null,
                    colorizeHeadset: true),

            BatteryDisplayMode.IconAndBattery =>
                CreateRenderedIcon(
                    batteryColor,
                    battery,
                    colorizeHeadset: false),

            BatteryDisplayMode.IconAndPercentage =>
                CreatePercentageOverlayIcon(
                    battery,
                    connected),

            _ =>
                CreateThemeIcon()
        };
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

    private Icon CreateThemeIcon()
    {
        string fileName =
            _settings.Theme == AppTheme.Dark
                ? "DarkTheme.ico"
                : "WhiteTheme.ico";

        string path =
            Path.Combine(
                AppContext.BaseDirectory,
                fileName);

        try
        {
            if (File.Exists(path))
                return new Icon(path);
        }
        catch
        {
        }

        return
            Icon.ExtractAssociatedIcon(
                Application.ExecutablePath)
            ?? new Icon(
                SystemIcons.Application,
                SystemIcons.Application.Size);
    }

    private Icon CreateRenderedIcon(
        Color batteryColor,
        int? battery,
        bool colorizeHeadset)
    {
        // ColoredIcon is intentionally independent from the composite
        // IconAndBattery layout. It must preserve the exact 16x16 scale of
        // the static tray icon and change only its color.
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

        // This layout is exclusively for IconAndBattery:
        // the headset is on the left and the battery is beside it.
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
            _settings.Theme == AppTheme.Dark
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
                _settings.Theme == AppTheme.Dark
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
            _settings.Theme == AppTheme.Dark
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
            _settings.Theme == AppTheme.Dark
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
        MessageBox.Show(
            L("AboutText"),
            L("AboutTitle"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApplication(object? sender, EventArgs e) => ExitThread();

    protected override void ExitThreadCore()
    {
        _settingsForm?.Close();
        _blinkTimer?.Stop();
        _blinkTimer?.Dispose();
        _blinkTimer = null;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _currentApplicationIcon?.Dispose();
        _currentApplicationIcon = null;

        if (_batteryMonitor != null)
        {
            _batteryMonitor.BatteryChanged -= BatteryMonitor_BatteryChanged;
            _batteryMonitor.ConnectionChanged -= BatteryMonitor_ConnectionChanged;
            _batteryMonitor.Dispose();
        }
        else
        {
            _device?.Dispose();
        }

        _deviceManager.Dispose();
        base.ExitThreadCore();
    }
}

internal sealed class TrayColorTable : ProfessionalColorTable
{
    private readonly bool _dark;
    public TrayColorTable(bool dark) => _dark = dark;
    private Color Back => _dark ? Color.FromArgb(45, 45, 48) : SystemColors.Menu;
    private Color Border => _dark ? Color.FromArgb(80, 80, 80) : SystemColors.ActiveBorder;
    private Color Highlight => _dark ? Color.FromArgb(62, 62, 66) : SystemColors.Highlight;
    public override Color MenuBorder => Border;
    public override Color MenuItemBorder => Border;
    public override Color MenuItemSelected => Highlight;
    public override Color MenuItemSelectedGradientBegin => Highlight;
    public override Color MenuItemSelectedGradientEnd => Highlight;
    public override Color ToolStripDropDownBackground => Back;
    public override Color ImageMarginGradientBegin => Back;
    public override Color ImageMarginGradientMiddle => Back;
    public override Color ImageMarginGradientEnd => Back;
}
