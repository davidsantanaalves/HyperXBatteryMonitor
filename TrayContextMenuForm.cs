using System.ComponentModel;
using System.Drawing.Drawing2D;
using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

internal sealed class TrayContextMenuForm : Form
{
    private readonly AppSettings _settings;
    private readonly PngIconCache _iconCache;
    private readonly Action<object?, EventArgs> _settingsAction;
    private readonly Action<object?, EventArgs> _exitAction;
    private readonly PictureBox _deviceImage;
    private readonly Label _deviceName;
    private readonly Label _statusTitle;
    private readonly Label _statusText;
    private readonly Label _batteryTitle;
    private readonly Label _batteryText;
    private readonly TrayStatusDotControl _statusDot;
    private readonly TrayBatteryIconControl _batteryIcon;
    private readonly Label _chargingText;
    private readonly TrayMenuButton _settingsButton;
    private readonly TrayMenuButton _exitButton;
    private readonly Panel _buttonBar;
    private IHyperXDevice? _device;
    private bool _charging;
    private bool _dark;
    private Bitmap? _deviceBitmap;
    private string? _deviceImageFileName;
    private ToolTip? _toolTip;

    public TrayContextMenuForm(
        AppSettings settings,
        IHyperXDevice? device,
        bool charging,
        AppTheme theme,
        Action<object?, EventArgs> settingsAction,
        Action<object?, EventArgs> exitAction)
    {
        _settings = settings;
        _device = device;
        _charging = charging;
        _settingsAction = settingsAction;
        _exitAction = exitAction;
        _iconCache = new PngIconCache();
        _dark = theme == AppTheme.Dark;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(168, 225);
        TopMost = true;
        DoubleBuffered = true;
        Padding = new Padding(0);
        BackColor = Color.FromArgb(45, 48, 51);
        Deactivate += (_, _) => Close();
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        SetRoundedRegion();

        _deviceImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(58, 58),
            Location = new Point(55, 7),
            BackColor = Color.Transparent
        };

        _deviceName = CreateLabel(8.5f, FontStyle.Bold, ContentAlignment.MiddleCenter, new Rectangle(8, 69, 152, 21));
        _statusTitle = CreateLabel(8.5f, FontStyle.Regular, ContentAlignment.MiddleLeft, new Rectangle(12, 97, 44, 20));
        _statusDot = new TrayStatusDotControl { Size = new Size(18, 18), Location = new Point(65, 98) };
        _statusText = CreateLabel(8.5f, FontStyle.Regular, ContentAlignment.MiddleLeft, new Rectangle(88, 97, 68, 20));
        _batteryTitle = CreateLabel(8.5f, FontStyle.Regular, ContentAlignment.MiddleLeft, new Rectangle(12, 123, 50, 20));
        _batteryIcon = new TrayBatteryIconControl { Size = new Size(30, 30), Location = new Point(60, 118) };
        _batteryText = CreateLabel(8.5f, FontStyle.Regular, ContentAlignment.MiddleLeft, new Rectangle(98, 123, 58, 20));
        _chargingText = CreateLabel(7.8f, FontStyle.Regular, ContentAlignment.MiddleCenter, new Rectangle(52, 146, 64, 18));
        _chargingText.Visible = false;

        Controls.AddRange(new Control[]
        {
            _deviceImage, _deviceName, _statusTitle, _statusDot, _statusText,
            _batteryTitle, _batteryIcon, _batteryText, _chargingText
        });

        _buttonBar = new Panel
        {
            Location = new Point(0, 170),
            Size = new Size(168, 55),
            BackColor = Color.Transparent
        };
        _settingsButton = new TrayMenuButton(_iconCache, "config")
        {
            Location = new Point(0, 0),
            Size = new Size(84, 55),
            OutsideBackColor = Color.Transparent,
            DrawTopSeparator = true,
            DrawRightSeparator = true
        };
        _exitButton = new TrayMenuButton(_iconCache, "exit")
        {
            Location = new Point(84, 0),
            Size = new Size(84, 55),
            OutsideBackColor = Color.Transparent,
            DrawTopSeparator = true
        };
        _settingsButton.Click += (_, _) => { Close(); _settingsAction(null, EventArgs.Empty); };
        _exitButton.Click += (_, _) => { Close(); _exitAction(null, EventArgs.Empty); };
        _buttonBar.Controls.Add(_settingsButton);
        _buttonBar.Controls.Add(_exitButton);
        Controls.Add(_buttonBar);

        _toolTip = new ToolTip { AutomaticDelay = 350, InitialDelay = 350, ReshowDelay = 100, AutoPopDelay = 5000 };
        _settingsButton.MouseEnter += (_, _) => _toolTip?.SetToolTip(_settingsButton, Localization.Get("ContextMenuSettings", _settings.Language));
        _exitButton.MouseEnter += (_, _) => _toolTip?.SetToolTip(_exitButton, Localization.Get("ContextMenuExit", _settings.Language));

        UpdateDevice(device, charging);
        ApplyLocalization();
        ApplyTheme(_dark);
    }

    private Label CreateLabel(float size, FontStyle style, ContentAlignment align, Rectangle bounds) => new()
    {
        AutoSize = false,
        Bounds = bounds,
        Font = new Font("Segoe UI", size, style),
        TextAlign = align,
        BackColor = Color.Transparent
    };

    public void UpdateDevice(IHyperXDevice? device, bool charging)
    {
        _device = device;
        _charging = charging;
        bool selected = !string.IsNullOrWhiteSpace(_settings.SelectedDevice);
        bool connected = selected && device?.IsConnected == true && device.Battery >= 0 && device.Battery <= 100;
        string normalized = NormalizeDeviceName(_settings.SelectedDevice);
        string imageFile = GetDeviceImage(normalized, selected);
        SetDeviceImage(imageFile);

        _deviceName.Text = selected ? normalized : Localization.Get("UnknownHeadphones", _settings.Language);
        _statusText.Text = connected ? Localization.Get("Connected", _settings.Language) : selected ? Localization.Get("Disconnected", _settings.Language) : Localization.Get("SidebarUnknown", _settings.Language);
        _statusDot.Connected = connected;
        _batteryIcon.Connected = connected;
        _batteryIcon.Battery = connected && device != null ? Math.Clamp(device.Battery, 0, 100) : 0;
        _batteryIcon.Charging = connected && charging;
        _batteryText.Text = connected ? $"{Math.Clamp(device!.Battery, 0, 100)}%" : Localization.Get("BatteryNA", _settings.Language);
        _chargingText.Text = charging && connected ? Localization.Get("ChargingStatus", _settings.Language) : string.Empty;
        _chargingText.Visible = charging && connected;
        Invalidate(true);
    }

    public void ApplyLocalization()
    {
        _statusTitle.Text = Localization.Get("SidebarStatus", _settings.Language);
        _batteryTitle.Text = Localization.Get("SidebarBattery", _settings.Language);
        _toolTip?.SetToolTip(_settingsButton, Localization.Get("ContextMenuSettings", _settings.Language));
        _toolTip?.SetToolTip(_exitButton, Localization.Get("ContextMenuExit", _settings.Language));
        UpdateDevice(_device, _charging);
    }

    public void ApplyTheme(bool dark)
    {
        _dark = dark;
        BackColor = dark ? Color.FromArgb(45, 48, 51) : Color.White;
        Color foreground = dark ? Color.WhiteSmoke : Color.FromArgb(24, 32, 45);
        Color secondary = dark ? Color.FromArgb(196, 201, 207) : Color.FromArgb(82, 95, 115);
        _deviceName.ForeColor = foreground;
        _statusTitle.ForeColor = secondary;
        _statusText.ForeColor = secondary;
        _batteryTitle.ForeColor = secondary;
        _batteryText.ForeColor = foreground;
        _chargingText.ForeColor = secondary;
        _statusDot.Invalidate();
        _batteryIcon.DarkMode = dark;
        _settingsButton.DarkMode = dark;
        _settingsButton.OutsideBackColor = dark
            ? Color.FromArgb(45, 48, 51)
            : Color.FromArgb(247, 248, 250);
        _exitButton.DarkMode = dark;
        _exitButton.OutsideBackColor = dark
            ? Color.FromArgb(45, 48, 51)
            : Color.FromArgb(247, 248, 250);
        _buttonBar.Invalidate();
        Invalidate(true);
    }

    public void ShowAt(Point requested)
    {
        int x = requested.X - Width / 2;
        int y = requested.Y - Height - 8;
        Rectangle work = Screen.FromPoint(requested).WorkingArea;
        x = Math.Clamp(x, work.Left + 4, work.Right - Width - 4);
        y = Math.Clamp(y, work.Top + 4, work.Bottom - Height - 4);
        Location = new Point(x, y);
        if (!Visible) Show();
        BringToFront();
        Activate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        SetRoundedRegion();
    }

    private void SetRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        using GraphicsPath path = CreateRoundedPath(
            new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height)), 10);
        Region = new Region(path);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(_dark ? Color.FromArgb(45, 48, 51) : Color.White);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using GraphicsPath path = CreateRoundedPath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 10);
        using Pen border = new(_dark ? Color.FromArgb(70, 74, 78) : Color.FromArgb(205, 211, 218), 1f);
        e.Graphics.DrawPath(border, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip?.Dispose();
            _iconCache.Dispose();
            _deviceImage.Image = null;
            _deviceBitmap?.Dispose();
            _deviceBitmap = null;
        }
        base.Dispose(disposing);
    }

    private void SetDeviceImage(string fileName)
    {
        if (string.Equals(_deviceImageFileName, fileName, StringComparison.OrdinalIgnoreCase) &&
            _deviceImage.Image != null)
        {
            return;
        }

        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Devices", fileName);
        // Detach the current image before disposing its bitmap. PictureBox can
        // access Image during layout/paint, and keeping a disposed bitmap
        // assigned to Image can result in "Parameter is not valid" exceptions.
        _deviceImage.Image = null;
        _deviceBitmap?.Dispose();
        _deviceBitmap = null;
        if (File.Exists(path))
        {
            try
            {
                using Bitmap source = new(path);
                _deviceBitmap = new Bitmap(source);
            }
            catch { }
        }
        _deviceImage.Image = _deviceBitmap;
        _deviceImageFileName = fileName;
    }

    private static string NormalizeDeviceName(string value) =>
        string.Equals(value, "HyperX Cloud III Wireless", StringComparison.OrdinalIgnoreCase)
            ? "HyperX Cloud III" : value;

    private static string GetDeviceImage(string name, bool selected) => name switch
    {
        "HyperX Cloud III" => "cloud3.png",
        "HyperX Cloud III S" => "cloud3.png",
        "HyperX Cloud 2 Core" => "cloud2core.png",
        "HyperX Cloud Alpha" => "cloudalpha.png",
        "HyperX Cloud Stinger 2" => "cloudstinger2.png",
        _ => "unknown-device.png"
    };

    private static GraphicsPath CreateRoundedPath(RectangleF r, float radius)
    {
        float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
        GraphicsPath p = new();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

internal sealed class TrayMenuButton : Control
{
    private readonly PngIconCache _iconCache;
    private readonly string _iconKey;
    private bool _hover;
    private bool _pressed;
    private bool _dark;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color OutsideBackColor { get; set; } = Color.Transparent;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawTopSeparator { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawRightSeparator { get; set; }

    public TrayMenuButton(PngIconCache iconCache, string iconKey)
    {
        _iconCache = iconCache;
        _iconKey = iconKey;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } };
        MouseUp += (_, _) => { _pressed = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(OutsideBackColor);
        Color fill = _pressed
            ? (_dark ? Color.FromArgb(35, 39, 42) : Color.FromArgb(225, 229, 235))
            : _hover
                ? (_dark ? Color.FromArgb(17, 23, 34) : Color.FromArgb(208, 208, 208))
                : (_dark ? Color.FromArgb(35, 47, 70) : Color.FromArgb(238, 238, 238));
        using (Brush brush = new SolidBrush(fill))
            e.Graphics.FillRectangle(brush, ClientRectangle);
        using Pen separatorPen = new(_dark ? Color.FromArgb(70, 74, 78) : Color.FromArgb(215, 220, 226), 1f);
        if (DrawTopSeparator)
            e.Graphics.DrawLine(separatorPen, 0, 0, Width - 1, 0);
        if (DrawRightSeparator)
            e.Graphics.DrawLine(separatorPen, Width - 1, 0, Width - 1, Height);

        _iconCache.Draw(e.Graphics, _iconKey, new RectangleF((Width - 25) / 2f, (Height - 25) / 2f, 25, 25), _dark, DeviceDpi);
    }
}

internal sealed class TrayStatusDotControl : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Connected { get; set; }
    public TrayStatusDotControl() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color c = Connected ? Color.FromArgb(52, 211, 85) : Color.FromArgb(239, 68, 68);
        using Brush b = new SolidBrush(c);
        using Pen p = new(Color.FromArgb(90, c.R, c.G, c.B), 1.5f);
        e.Graphics.FillEllipse(b, 2.5f, 2.5f, 13f, 13f);
        e.Graphics.DrawEllipse(p, 1f, 1f, 16f, 16f);
    }
}

internal sealed class TrayBatteryIconControl : Control
{
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
    public TrayBatteryIconControl() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = Math.Min(Width / 46f, Height / 46f);
        float x = (Width - 46f * scale) / 2f;
        float y = (Height - 46f * scale) / 2f;
        RectangleF body = new(x + 9f * scale, y + 17f * scale, 26f * scale, 14f * scale);
        Color frame = _dark ? Color.WhiteSmoke : Color.FromArgb(55, 65, 75);
        using Pen pen = new(frame, Math.Max(1f, 2f * scale));
        e.Graphics.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);
        using Brush terminal = new SolidBrush(frame);
        e.Graphics.FillRectangle(terminal, x + 36f * scale, y + 21f * scale, 3f * scale, 6f * scale);
        if (_connected)
        {
            Color level = _battery >= 50 ? Color.LimeGreen : _battery >= 30 ? Color.Gold : _battery >= 15 ? Color.Orange : Color.Red;
            float fillWidth = Math.Max(0, 22f * _battery / 100f) * scale;
            using Brush b = new SolidBrush(level);
            e.Graphics.FillRectangle(b, x + 11f * scale, y + 19f * scale, fillWidth, 10f * scale);
        }
        if (_charging && _connected)
        {
            using Pen bolt = new(Color.LimeGreen, Math.Max(1f, 1.2f * scale)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            PointF[] pts = { new(x + 25f * scale, y + 18f * scale), new(x + 20f * scale, y + 24f * scale), new(x + 23f * scale, y + 24f * scale), new(x + 20f * scale, y + 30f * scale), new(x + 27f * scale, y + 22f * scale), new(x + 24f * scale, y + 22f * scale) };
            e.Graphics.DrawLines(bolt, pts);
        }
    }
}
