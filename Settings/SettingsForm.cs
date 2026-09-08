using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using HyperXBatteryTray;

namespace HyperXBatteryTray.Settings;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly StartupManager _startupManager;

    private readonly ComboBox _deviceComboBox;
    private readonly ComboBox _languageComboBox;
    private readonly CheckBox _startupCheckBox;
    private readonly Label _startupLabel;
    private readonly RadioButton _lightThemeRadioButton;
    private readonly RadioButton _darkThemeRadioButton;
    private readonly RadioButton _systemThemeRadioButton;

    private readonly GroupBox _deviceGroup;
    private readonly GroupBox _interfaceGroup;
    private readonly GroupBox _displayGroup;
    private readonly GroupBox _colorsGroup;
    private GroupBox _batteryColorsGroup = null!;
    private readonly GroupBox _criticalGroup;

    private readonly Label _deviceLabel;
    private readonly Label _languageLabel;
    private readonly Label _themeLabel;
    private readonly Label _colorHeaderLabel;
    private readonly Label _startingAtHeaderLabel;
    private readonly Label _transitionLabel;
    private readonly Label _criticalLimitLabel;

    private readonly RadioButton _staticIconRadioButton;
    private readonly RadioButton _batteryIndicatorRadioButton;
    private readonly RadioButton _advancedRadioButton;
    private readonly RadioButton _gradientAdvancedRadioButton;
    private readonly RadioButton _advancedBatteryIndicatorRadioButton;
    private readonly RadioButton _percentageAdvancedRadioButton;

    private readonly CheckBox _gradientCheckBox;
    private readonly NumericUpDown _gradientPercentNumeric;
    private readonly CheckBox _blinkCheckBox;
    private readonly NumericUpDown _criticalBatteryNumeric;

    private readonly List<NumericUpDown> _minimumPercentControls = new();
    private readonly List<Panel> _colorPreviewPanels = new();
    private readonly List<Label> _percentLabels = new();
    private readonly List<PictureBox> _previewBoxes = new();
    private readonly List<PictureBox> _advancedPreviewBoxes = new();

    private readonly List<BatteryColorSettings> _workingColors;

    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private readonly Button _applyButton;
    private readonly Button _restoreButton;

    private TableLayoutPanel _mainPanel = null!;
    private bool _saved;
    private bool _updatingLanguage;
    private AppLanguage _selectedLanguage;
    private AppTheme _selectedTheme;

    private const int WindowWidth = 620;
    private const int WindowHeightWithColors = 835;
    private const int WindowHeightWithoutColors = 530;

    public event EventHandler? SettingsApplied;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        _selectedLanguage = settings.Language;
        _selectedTheme = settings.Theme;
        _startupManager = new StartupManager();

        _workingColors = CreateWorkingColors(settings);

        Text = L("WindowTitle");
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(WindowWidth, WindowHeightWithColors);
        Icon = LoadThemeIcon(_selectedTheme);

        _deviceLabel = new Label { Text = L("DeviceLabel"), AutoSize = true };
        _languageLabel = new Label { Text = L("Language"), AutoSize = true };
        _themeLabel = new Label { Text = L("Theme"), AutoSize = true };
        _startupLabel = new Label { Text = L("Startup"), AutoSize = true, Cursor = Cursors.Hand };
        _startupCheckBox = new CheckBox { AutoSize = true, Checked = _startupManager.IsEnabled() };
        _startupLabel.Click += StartupLabel_Click;
        _colorHeaderLabel = new Label { Text = L("Color"), AutoSize = true };
        _startingAtHeaderLabel = new Label { Text = L("StartingAt"), AutoSize = true };
        _transitionLabel = new Label { Text = L("Transition"), AutoSize = true };
        _criticalLimitLabel = new Label { Text = L("BatteryLimit"), AutoSize = true };

        _deviceComboBox = new DeviceSelectionComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 400
        };
        _deviceComboBox.Items.Add(string.Empty);
        _deviceComboBox.Items.Add("HyperX Cloud III Wireless");
        _deviceComboBox.SelectedIndex =
            settings.SelectedDevice == "HyperX Cloud III Wireless" ? 1 : 0;
        _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;

        _languageComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 235
        };
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.English));
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.PortugueseBrazil));
        _languageComboBox.Items.Add(Localization.LanguageDisplay(AppLanguage.Spanish));
        _languageComboBox.SelectedIndex = (int)_selectedLanguage;
        _languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;

        _lightThemeRadioButton = new RadioButton
        {
            Text = ThemeText(AppTheme.Light),
            AutoSize = true,
            Checked = _selectedTheme == AppTheme.Light
        };
        _darkThemeRadioButton = new RadioButton
        {
            Text = ThemeText(AppTheme.Dark),
            AutoSize = true,
            Checked = _selectedTheme == AppTheme.Dark
        };
        _systemThemeRadioButton = new RadioButton
        {
            Text = ThemeText(AppTheme.System),
            AutoSize = true,
            Checked = _selectedTheme == AppTheme.System
        };
        _lightThemeRadioButton.CheckedChanged += ThemeRadioButton_CheckedChanged;
        _darkThemeRadioButton.CheckedChanged += ThemeRadioButton_CheckedChanged;
        _systemThemeRadioButton.CheckedChanged += ThemeRadioButton_CheckedChanged;

        _staticIconRadioButton = CreateDisplayRadioButton(L("StaticIcon"), BatteryDisplayMode.StaticIcon);
        _batteryIndicatorRadioButton = CreateDisplayRadioButton(L("BatteryIndicatorMode"), BatteryDisplayMode.BatteryIndicator);
        _advancedRadioButton = CreateDisplayRadioButton(L("AdvancedDynamic"), BatteryDisplayMode.Advanced);

        _gradientAdvancedRadioButton = CreateAdvancedDisplayRadioButton(
            L("BatteryGradientMode"),
            AdvancedDisplayMode.BatteryGradient);
        _advancedBatteryIndicatorRadioButton = CreateAdvancedDisplayRadioButton(
            L("AdvancedBatteryIndicatorMode"),
            AdvancedDisplayMode.BatteryIndicator);
        _percentageAdvancedRadioButton = CreateAdvancedDisplayRadioButton(
            L("PercentageTextMode"),
            AdvancedDisplayMode.PercentageText);

        _gradientCheckBox = new CheckBox
        {
            Text = L("UseGradient"),
            AutoSize = true,
            Checked = settings.UseGradient
        };

        _gradientPercentNumeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 50,
            Value = Math.Clamp(settings.GradientPercent, 0, 50),
            Width = 70
        };

        _blinkCheckBox = new CheckBox
        {
            Text = L("BlinkCritical"),
            AutoSize = true,
            Checked = settings.BlinkOnCriticalBattery
        };

        _criticalBatteryNumeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100,
            Value = Math.Clamp(settings.CriticalBatteryPercent, 0, 100),
            Width = 70
        };

        _deviceGroup = CreateDeviceSection();
        _interfaceGroup = CreateInterfaceSection();
        _displayGroup = CreateDisplaySection();
        _colorsGroup = CreateColorsSection();
        _criticalGroup = CreateAlertSection();

        _okButton = new Button { Text = L("Ok"), AutoSize = true, DialogResult = DialogResult.None };
        _cancelButton = new Button { Text = L("Cancel"), AutoSize = true, DialogResult = DialogResult.Cancel };
        _applyButton = new Button { Text = L("Apply"), AutoSize = true };
        _restoreButton = new Button { Text = L("RestoreDefaults"), AutoSize = true };

        _okButton.Click += OkButton_Click;
        _cancelButton.Click += CancelButton_Click;
        _applyButton.Click += ApplyButton_Click;
        _restoreButton.Click += ResetButton_Click;

        BuildInterface();
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        UpdateAdvancedOptionsVisibility();
        ApplyTheme(_selectedTheme);
        PositionWindowAtTop();
    }

    private static List<BatteryColorSettings> CreateWorkingColors(AppSettings settings)
    {
        List<BatteryColorSettings> configured = settings.BatteryColors;

        IEnumerable<BatteryColorSettings> source =
            configured.Count == 3
                ? configured
                : AppSettings.CreateDefault().BatteryColors;

        return source
            .Select(color => new BatteryColorSettings
            {
                Name = color.Name,
                MinimumPercent = color.MinimumPercent,
                Argb = color.Argb
            })
            .OrderByDescending(color => color.MinimumPercent)
            .ToList();
    }

    private void BuildInterface()
    {
        _mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 6
        };

        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 315));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        _mainPanel.Controls.Add(_deviceGroup, 0, 0);
        _mainPanel.Controls.Add(_interfaceGroup, 0, 1);
        _mainPanel.Controls.Add(_displayGroup, 0, 2);
        _mainPanel.Controls.Add(_colorsGroup, 0, 3);
        _mainPanel.Controls.Add(_criticalGroup, 0, 4);
        _mainPanel.Controls.Add(CreateButtonPanel(), 0, 5);

        Controls.Add(_mainPanel);
    }

    private GroupBox CreateDeviceSection()
    {
        var group = new GroupBox { Text = L("Device"), Dock = DockStyle.Fill };
        var panel = new Panel { Dock = DockStyle.Fill };

        _deviceLabel.Location = new Point(12, 16);
        _deviceComboBox.Location = new Point(105, 12);

        panel.Controls.Add(_deviceLabel);
        panel.Controls.Add(_deviceComboBox);
        group.Controls.Add(panel);
        return group;
    }

    private GroupBox CreateInterfaceSection()
    {
        var group = new GroupBox { Text = L("Interface"), Dock = DockStyle.Fill };
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8, 3, 8, 3)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _languageLabel.Anchor = AnchorStyles.Left;
        _themeLabel.Anchor = AnchorStyles.Left;
        panel.Controls.Add(_languageLabel, 0, 0);
        panel.Controls.Add(_languageComboBox, 1, 0);
        panel.Controls.Add(_themeLabel, 0, 1);

        var themePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 3, 0, 0)
        };
        themePanel.Controls.Add(_lightThemeRadioButton);
        themePanel.Controls.Add(_darkThemeRadioButton);
        themePanel.Controls.Add(_systemThemeRadioButton);
        panel.Controls.Add(themePanel, 1, 1);

        _startupLabel.Anchor = AnchorStyles.Left;
        panel.Controls.Add(_startupLabel, 0, 2);

        var startupPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 3, 0, 0),
            Margin = new Padding(0)
        };

        startupPanel.Controls.Add(_startupCheckBox);
        panel.Controls.Add(startupPanel, 1, 2);

        group.Controls.Add(panel);
        return group;
    }

    private GroupBox CreateDisplaySection()
    {
        var group = new GroupBox { Text = L("BatteryDisplay"), Dock = DockStyle.Fill };
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(0)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        for (int i = 0; i < 3; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        AddPrimaryDisplayOption(panel, 0, _staticIconRadioButton);
        AddPrimaryDisplayOption(panel, 1, _batteryIndicatorRadioButton);
        AddPrimaryDisplayOption(panel, 2, _advancedRadioButton);

        AddPrimaryDisplayPreview(panel, 0, BatteryDisplayMode.StaticIcon);
        AddPrimaryDisplayPreview(panel, 1, BatteryDisplayMode.BatteryIndicator);

        _staticIconRadioButton.CheckedChanged += DisplayModeRadioButton_CheckedChanged;
        _batteryIndicatorRadioButton.CheckedChanged += DisplayModeRadioButton_CheckedChanged;
        _advancedRadioButton.CheckedChanged += DisplayModeRadioButton_CheckedChanged;

        group.Controls.Add(panel);
        return group;
    }

    private void AddPrimaryDisplayPreview(
        TableLayoutPanel panel,
        int row,
        BatteryDisplayMode mode)
    {
        var preview = new PictureBox
        {
            Image = CreateModePreview(mode),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Size = new Size(170, 30),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 0, 2),
            BackColor = Color.Transparent
        };

        _previewBoxes.Add(preview);
        panel.Controls.Add(preview, 1, row);
    }

    private static void AddPrimaryDisplayOption(
        TableLayoutPanel panel,
        int row,
        RadioButton radioButton)
    {
        radioButton.Anchor = AnchorStyles.Left;
        radioButton.Margin = new Padding(3, 2, 3, 2);
        panel.Controls.Add(radioButton, 0, row);
    }

    private RadioButton CreateDisplayRadioButton(string text, BatteryDisplayMode mode)
    {
        return new RadioButton
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Checked = _settings.DisplayMode == mode,
            Tag = mode
        };
    }

    private RadioButton CreateAdvancedDisplayRadioButton(string text, AdvancedDisplayMode mode)
    {
        return new RadioButton
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Checked = _settings.AdvancedDisplayMode == mode,
            Tag = mode
        };
    }

    private Bitmap CreateModePreview(BatteryDisplayMode mode)
    {
        const int width = 170;
        const int height = 30;
        const int iconSize = 24;
        const int spacing = 10;
        const int firstX = 8;
        const int iconY = 3;

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        if (mode == BatteryDisplayMode.StaticIcon)
        {
            DrawApplicationIcon(graphics, firstX, iconY, iconSize, null);
            DrawChargingIcon(graphics, firstX + iconSize + spacing, iconY, iconSize);
            return bitmap;
        }

        // The primary Battery Indicator mode demonstrates the four battery
        // states used by the tray indicator, followed by the charging icon.
        string[] suffixes = { "green", "yellow", "orange", "red" };
        for (int i = 0; i < suffixes.Length; i++)
        {
            int x = firstX + i * (iconSize + spacing);
            DrawBatteryStateIcon(graphics, x, iconY, iconSize, suffixes[i]);
        }

        // Charging state is shown as the final icon in the Battery Indicator
        // preview, using the same theme-specific charging asset as the tray.
        int chargingX = firstX + suffixes.Length * (iconSize + spacing);
        DrawChargingIcon(graphics, chargingX, iconY, iconSize);

        return bitmap;
    }

    private Bitmap CreateAdvancedModePreview(AdvancedDisplayMode mode)
    {
        const int width = 170;
        const int height = 30;
        const int iconSize = 24;
        const int spacing = 10;
        const int firstX = 8;
        const int iconY = 3;

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        switch (mode)
        {
            case AdvancedDisplayMode.BatteryGradient:
                for (int i = 0; i < _workingColors.Count && i < 3; i++)
                {
                    int x = firstX + i * (iconSize + spacing);
                    DrawApplicationIcon(graphics, x, iconY, iconSize, _workingColors[i].Color);
                }
                break;

            case AdvancedDisplayMode.BatteryIndicator:
                DrawAdvancedBatteryIndicatorPreview(graphics, firstX, iconY, iconSize, spacing);
                break;

            case AdvancedDisplayMode.PercentageText:
                int[] percentages = { 100, 50, 10 };
                for (int i = 0; i < _workingColors.Count && i < 3; i++)
                {
                    int x = firstX + i * (iconSize + spacing);
                    // Text percentage keeps the standard static headset icon for the
                    // selected theme. Only the percentage text changes color.
                    DrawApplicationIcon(graphics, x, iconY, iconSize, null);
                    DrawCenteredPercentageOverIcon(
                        graphics,
                        $"{percentages[i]}%",
                        x,
                        iconY,
                        iconSize,
                        _workingColors[i].Color);
                }
                break;
        }

        return bitmap;
    }

    private void DrawApplicationIcon(
        Graphics graphics,
        int x,
        int y,
        int size,
        Color? replacementColor)
    {
        using Icon? sourceIcon = TryGetApplicationIcon(EffectiveSelectedTheme);
        if (sourceIcon != null)
        {
            using Bitmap iconBitmap = RenderIconToBitmap(sourceIcon, size, size, replacementColor);
            graphics.DrawImage(iconBitmap, x, y, size, size);
        }
        else
        {
            DrawHeadsetFallback(
                graphics,
                x,
                y,
                size,
                replacementColor ?? PreviewForeColor);
        }
    }

    private void DrawBatteryStateIcon(
        Graphics graphics,
        int x,
        int y,
        int size,
        string suffix)
    {
        AppTheme theme = EffectiveSelectedTheme;
        string prefix = theme == AppTheme.Dark ? "dark" : "light";
        string themeFolder = theme == AppTheme.Dark ? "Dark" : "Light";
        string path = Path.Combine(
            Application.StartupPath,
            "Icons",
            themeFolder,
            $"{prefix}_{suffix}.ico");

        using Icon? icon = TryLoadIcon(path);
        if (icon != null)
        {
            using Bitmap bitmap = RenderIconToBitmap(icon, size, size, null);
            graphics.DrawImage(bitmap, x, y, size, size);
        }
        else
        {
            DrawApplicationIcon(graphics, x, y, size, GetPreviewStateColor(suffix));
        }
    }

    private void DrawChargingIcon(Graphics graphics, int x, int y, int size)
    {
        AppTheme theme = EffectiveSelectedTheme;
        string prefix = theme == AppTheme.Dark ? "dark" : "light";
        string themeFolder = theme == AppTheme.Dark ? "Dark" : "Light";
        string path = Path.Combine(
            Application.StartupPath,
            "Icons",
            themeFolder,
            $"{prefix}_charging.ico");

        using Icon? icon = TryLoadIcon(path);
        if (icon != null)
        {
            using Bitmap bitmap = RenderIconToBitmap(icon, size, size, null);
            graphics.DrawImage(bitmap, x, y, size, size);
        }
        else
        {
            DrawApplicationIcon(graphics, x, y, size, null);
        }
    }

    private void DrawAdvancedBatteryIndicatorPreview(
        Graphics graphics,
        int firstX,
        int y,
        int size,
        int spacing)
    {
        int[] percentages = { 100, 50, 10 };

        for (int i = 0; i < 3 && i < _workingColors.Count; i++)
        {
            int x = firstX + i * (size + spacing);
            Color color = _workingColors[i].Color;

            DrawApplicationIcon(graphics, x, y, size, null);
            DrawVerticalBattery(
                graphics,
                x + 15,
                y + 3,
                8,
                18,
                percentages[i],
                color,
                EffectiveSelectedTheme);
        }
    }

    private Color GetPreviewStateColor(string suffix) => suffix switch
    {
        "green" => Color.LimeGreen,
        "yellow" => Color.Gold,
        "orange" => Color.Orange,
        "red" => Color.Red,
        _ => PreviewForeColor
    };

    private Color PreviewBackColor => EffectiveSelectedTheme == AppTheme.Dark
        ? Color.FromArgb(45, 45, 48)
        : SystemColors.Window;

    private Color PreviewForeColor => EffectiveSelectedTheme == AppTheme.Dark
        ? Color.WhiteSmoke
        : SystemColors.ControlText;

    private static Icon? TryGetApplicationIcon(AppTheme theme)
    {
        string prefix = theme == AppTheme.Dark ? "dark" : "light";
        return TryLoadIcon(Path.Combine(
            Application.StartupPath,
            "Icons",
            theme == AppTheme.Dark ? "Dark" : "Light",
            $"{prefix}.ico"));
    }

    private static Icon? TryGetBatteryIndicatorIcon(AppTheme theme, int battery)
    {
        string prefix = theme == AppTheme.Dark ? "dark" : "light";
        string suffix = battery switch
        {
            >= 50 => "green",
            >= 30 => "yellow",
            >= 15 => "orange",
            _ => "red"
        };

        return TryLoadIcon(Path.Combine(
            Application.StartupPath,
            "Icons",
            theme == AppTheme.Dark ? "Dark" : "Light",
            $"{prefix}_{suffix}.ico"));
    }

    private static Icon? TryLoadIcon(string path)
    {
        try
        {
            if (File.Exists(path))
                return new Icon(path);
        }
        catch
        {
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    }

    private static Bitmap RenderIconToBitmap(Icon sourceIcon, int width, int height, Color? replacementColor)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.DrawIcon(sourceIcon, new Rectangle(0, 0, width, height));

        if (replacementColor.HasValue)
            ColorizeBitmap(bitmap, replacementColor.Value);

        return bitmap;
    }

    private static void ColorizeBitmap(Bitmap bitmap, Color color)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.A == 0)
                    continue;

                bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, color.R, color.G, color.B));
            }
        }
    }

    private static void DrawHeadsetFallback(Graphics graphics, int x, int y, int size, Color color)
    {
        using var pen = new Pen(color, 1.8f);
        graphics.DrawArc(pen, x + 4, y + 2, size - 8, size - 8, 180, 180);
        graphics.DrawLine(pen, x + 4, y + 13, x + 4, y + 18);
        graphics.DrawLine(pen, x + size - 4, y + 13, x + size - 4, y + 18);
        graphics.DrawRectangle(pen, x + 1, y + 14, 4, 7);
        graphics.DrawRectangle(pen, x + size - 5, y + 14, 4, 7);
    }

    private static void DrawVerticalBattery(
        Graphics graphics,
        int x,
        int y,
        int width,
        int height,
        int percentage,
        Color fillColor,
        AppTheme theme)
    {
        Color outlineColor = theme == AppTheme.Dark
            ? Color.FromArgb(205, 205, 210)
            : Color.FromArgb(75, 75, 75);

        using var outlinePen =
            new Pen(
                outlineColor,
                1.8f);

        graphics.DrawRectangle(
            outlinePen,
            x,
            y + 2,
            width,
            height - 2);

        using var terminalBrush =
            new SolidBrush(outlineColor);

        graphics.FillRectangle(
            terminalBrush,
            x + 2,
            y,
            width - 4,
            3);

        Color emptyColor =
            theme == AppTheme.Dark
                ? Color.FromArgb(38, 38, 40)
                : Color.FromArgb(55, 55, 58);

        using var emptyBrush =
            new SolidBrush(emptyColor);

        graphics.FillRectangle(
            emptyBrush,
            x + 2,
            y + 4,
            width - 3,
            height - 5);

        int innerHeight =
            Math.Max(
                0,
                (height - 7) *
                Math.Clamp(percentage, 0, 100) / 100);

        if (innerHeight <= 0)
            return;

        using var fillBrush = new SolidBrush(fillColor);
        graphics.FillRectangle(
            fillBrush,
            x + 2,
            y + height - 2 - innerHeight,
            width - 3,
            innerHeight);
    }

    private static void DrawCenteredPercentageOverIcon(
        Graphics graphics,
        string text,
        int iconX,
        int iconY,
        int iconSize,
        Color color)
    {
        using var font =
            new Font(
                "Segoe UI",
                7.5f,
                FontStyle.Bold,
                GraphicsUnit.Point);

        SizeF textSize = graphics.MeasureString(text, font);

        float x = iconX + (iconSize - textSize.Width) / 2f;
        float y = iconY + (iconSize - textSize.Height) / 2f;

        // Draw a compact dark background directly behind the percentage so
        // the text remains legible over the headset artwork.
        const float horizontalPadding = 1.5f;
        const float verticalPadding = 0.5f;
        RectangleF backgroundRect = new RectangleF(
            x - horizontalPadding,
            y - verticalPadding,
            textSize.Width + horizontalPadding * 2,
            textSize.Height + verticalPadding * 2);

        using var backgroundBrush =
            new SolidBrush(Color.FromArgb(205, Color.Black));
        graphics.FillRectangle(backgroundBrush, backgroundRect);

        using var brush = new SolidBrush(color);

        graphics.DrawString(
            text,
            font,
            brush,
            x,
            y);
    }

    private static void DrawSmallPercentage(Graphics graphics, string text, int x, int y, Color color)
    {
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, x, y);
    }

    private GroupBox CreateColorsSection()
    {
        var group = new GroupBox { Text = L("AdvancedDynamic"), Dock = DockStyle.Fill };
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0)
        };

        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var modesTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        modesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        modesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        for (int i = 0; i < 3; i++)
            modesTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        AddAdvancedDisplayOption(modesTable, 0, _gradientAdvancedRadioButton, AdvancedDisplayMode.BatteryGradient);
        AddAdvancedDisplayOption(modesTable, 1, _advancedBatteryIndicatorRadioButton, AdvancedDisplayMode.BatteryIndicator);
        AddAdvancedDisplayOption(modesTable, 2, _percentageAdvancedRadioButton, AdvancedDisplayMode.PercentageText);

        _gradientAdvancedRadioButton.CheckedChanged += AdvancedDisplayModeRadioButton_CheckedChanged;
        _advancedBatteryIndicatorRadioButton.CheckedChanged += AdvancedDisplayModeRadioButton_CheckedChanged;
        _percentageAdvancedRadioButton.CheckedChanged += AdvancedDisplayModeRadioButton_CheckedChanged;

        outer.Controls.Add(modesTable, 0, 0);

        _batteryColorsGroup = new GroupBox
        {
            Text = L("BatteryColors"),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 0),
            Padding = new Padding(8, 4, 8, 4)
        };

        var colorsOuter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        colorsOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        colorsOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        colorsOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var colorsContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        colorsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 53));
        colorsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        colorsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        for (int i = 1; i < 4; i++)
            colorsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        _colorHeaderLabel.Anchor = AnchorStyles.Left;
        _startingAtHeaderLabel.Anchor = AnchorStyles.Left;
        colorsContainer.Controls.Add(_colorHeaderLabel, 0, 0);
        colorsContainer.Controls.Add(_startingAtHeaderLabel, 1, 0);

        for (int i = 0; i < _workingColors.Count; i++)
        {
            BatteryColorSettings color = _workingColors[i];
            int row = i + 1;

            var colorPanel = new Panel
            {
                Width = 38,
                Height = 22,
                BackColor = color.Color,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = i,
                Anchor = AnchorStyles.Left
            };
            colorPanel.Click += ColorPanel_Click;
            colorPanel.MouseEnter += ColorPanel_MouseEnter;
            colorPanel.MouseLeave += ColorPanel_MouseLeave;
            _colorPreviewPanels.Add(colorPanel);

            var colorContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 3, 0, 0)
            };
            colorContainer.Controls.Add(colorPanel);
            colorsContainer.Controls.Add(colorContainer, 0, row);

            var minimumPercent = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Clamp(color.MinimumPercent, 0, 100),
                Width = 60,
                Anchor = AnchorStyles.Left
            };
            minimumPercent.ValueChanged += (_, _) => UpdateColorRanges();
            _minimumPercentControls.Add(minimumPercent);
            colorsContainer.Controls.Add(minimumPercent, 1, row);
        }

        colorsOuter.Controls.Add(colorsContainer, 0, 0);

        var gradientPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 3, 0, 0),
            Margin = new Padding(0)
        };

        gradientPanel.Controls.Add(_gradientCheckBox);
        _transitionLabel.Margin = new Padding(20, 4, 4, 0);
        gradientPanel.Controls.Add(_transitionLabel);
        gradientPanel.Controls.Add(_gradientPercentNumeric);
        gradientPanel.Controls.Add(new Label
        {
            Text = "%",
            AutoSize = true,
            Margin = new Padding(4, 4, 0, 0)
        });

        colorsOuter.Controls.Add(gradientPanel, 0, 1);
        _batteryColorsGroup.Controls.Add(colorsOuter);
        outer.Controls.Add(_batteryColorsGroup, 0, 1);
        group.Controls.Add(outer);

        UpdateColorRanges();
        return group;
    }

    private void AddAdvancedDisplayOption(
        TableLayoutPanel panel,
        int row,
        RadioButton radioButton,
        AdvancedDisplayMode mode)
    {
        radioButton.Anchor = AnchorStyles.Left;
        radioButton.Margin = new Padding(3, 0, 3, 0);
        panel.Controls.Add(radioButton, 0, row);

        var preview = new PictureBox
        {
            Image = CreateAdvancedModePreview(mode),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Size = new Size(170, 30),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 0, 2),
            BackColor = Color.Transparent
        };

        _advancedPreviewBoxes.Add(preview);
        panel.Controls.Add(preview, 1, row);
    }

    private GroupBox CreateAlertSection()
    {
        var group = new GroupBox { Text = L("CriticalBattery"), Dock = DockStyle.Fill };
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 3, 8, 3)
        };

        panel.Controls.Add(_blinkCheckBox);

        var thresholdPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 0)
        };
        _criticalLimitLabel.Margin = new Padding(0, 4, 5, 0);
        thresholdPanel.Controls.Add(_criticalLimitLabel);
        thresholdPanel.Controls.Add(_criticalBatteryNumeric);
        thresholdPanel.Controls.Add(new Label
        {
            Text = "%",
            AutoSize = true,
            Margin = new Padding(4, 4, 0, 0)
        });
        panel.Controls.Add(thresholdPanel);
        group.Controls.Add(panel);
        return group;
    }

    private Control CreateButtonPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        left.Controls.Add(_restoreButton);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        right.Controls.Add(_applyButton);
        right.Controls.Add(_cancelButton);
        right.Controls.Add(_okButton);

        panel.Controls.Add(left, 0, 0);
        panel.Controls.Add(right, 1, 0);
        return panel;
    }

    private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingLanguage || _languageComboBox.SelectedIndex < 0)
            return;

        _selectedLanguage = (AppLanguage)_languageComboBox.SelectedIndex;
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        _updatingLanguage = true;
        try
        {
            Text = L("WindowTitle");
            _deviceGroup.Text = L("Device");
            _interfaceGroup.Text = L("Interface");
            _displayGroup.Text = L("BatteryDisplay");
            _colorsGroup.Text = L("AdvancedDynamic");
            _batteryColorsGroup.Text = L("BatteryColors");
            _criticalGroup.Text = L("CriticalBattery");

            _deviceLabel.Text = L("DeviceLabel");
            _languageLabel.Text = L("Language");
            _themeLabel.Text = L("Theme");
            _startupLabel.Text = L("Startup");
            _colorHeaderLabel.Text = L("Color");
            _startingAtHeaderLabel.Text = L("StartingAt");
            _transitionLabel.Text = L("Transition");
            _criticalLimitLabel.Text = L("BatteryLimit");

            _lightThemeRadioButton.Text = ThemeText(AppTheme.Light);
            _darkThemeRadioButton.Text = ThemeText(AppTheme.Dark);
            _systemThemeRadioButton.Text = ThemeText(AppTheme.System);
            _staticIconRadioButton.Text = L("StaticIcon");
            _batteryIndicatorRadioButton.Text = L("BatteryIndicatorMode");
            _advancedRadioButton.Text = L("AdvancedDynamic");
            _gradientAdvancedRadioButton.Text = L("BatteryGradientMode");
            _advancedBatteryIndicatorRadioButton.Text = L("AdvancedBatteryIndicatorMode");
            _percentageAdvancedRadioButton.Text = L("PercentageTextMode");
            _gradientCheckBox.Text = L("UseGradient");
            _blinkCheckBox.Text = L("BlinkCritical");
            _okButton.Text = L("Ok");
            _cancelButton.Text = L("Cancel");
            _applyButton.Text = L("Apply");
            _restoreButton.Text = L("RestoreDefaults");

            RefreshPreviewImages();
            ApplyTheme(_selectedTheme);
        }
        finally
        {
            _updatingLanguage = false;
        }
    }

    private string ThemeText(AppTheme theme) =>
        theme switch
        {
            AppTheme.Dark => "🌙 " + Localization.Get("ThemeDark", _selectedLanguage),
            AppTheme.System => "🖥 " + Localization.Get("ThemeSystem", _selectedLanguage),
            _ => "☀ " + Localization.Get("ThemeLight", _selectedLanguage)
        };

    private string L(string key) => Localization.Get(key, _selectedLanguage);

    private void StartupLabel_Click(object? sender, EventArgs e)
    {
        _startupCheckBox.Checked = !_startupCheckBox.Checked;
    }

    private void ThemeRadioButton_CheckedChanged(object? sender, EventArgs e)
    {
        if (_lightThemeRadioButton.Checked)
            _selectedTheme = AppTheme.Light;
        else if (_darkThemeRadioButton.Checked)
            _selectedTheme = AppTheme.Dark;
        else if (_systemThemeRadioButton.Checked)
            _selectedTheme = AppTheme.System;
        else
            return;

        ApplyTheme(_selectedTheme);
    }

    private void ApplyTheme(AppTheme theme)
    {
        theme = ResolveTheme(theme);
        bool dark = theme == AppTheme.Dark;
        Color back = dark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        Color fore = dark ? Color.WhiteSmoke : SystemColors.ControlText;
        Color inputBack = dark ? Color.FromArgb(45, 45, 48) : SystemColors.Window;

        BackColor = back;
        ForeColor = fore;
        Icon?.Dispose();
        Icon = LoadThemeIcon(theme);

        ApplyThemeToControls(this, back, fore, inputBack, dark);

        for (int i = 0; i < _colorPreviewPanels.Count && i < _workingColors.Count; i++)
            _colorPreviewPanels[i].BackColor = _workingColors[i].Color;

        ApplyTitleBarTheme(dark);
        RefreshPreviewImages();
    }

    private void ApplyThemeToControls(Control control, Color back, Color fore, Color inputBack, bool dark)
    {
        foreach (Control child in control.Controls)
        {
            child.ForeColor = fore;

            // Color selector panels represent user-defined battery colors.
            // Never overwrite their BackColor when applying the application theme.
            if (child is Panel panel && _colorPreviewPanels.Contains(panel))
            {
                panel.BackColor = ((BatteryColorSettings) _workingColors[(int)panel.Tag!]).Color;
            }
            else if (child is TextBox || child is ComboBox || child is NumericUpDown)
                child.BackColor = inputBack;
            else if (child is Button)
                child.BackColor = dark ? Color.FromArgb(55, 55, 58) : SystemColors.Control;
            else if (child is PictureBox)
                child.BackColor = Color.Transparent;
            else
                child.BackColor = back;

            ApplyThemeToControls(child, back, fore, inputBack, dark);
        }
    }

    private void RefreshPreviewImages()
    {
        BatteryDisplayMode[] modes =
        {
            BatteryDisplayMode.StaticIcon,
            BatteryDisplayMode.BatteryIndicator
        };

        for (int i = 0; i < _previewBoxes.Count && i < modes.Length; i++)
        {
            Image? oldImage = _previewBoxes[i].Image;
            _previewBoxes[i].Image = CreateModePreview(modes[i]);
            oldImage?.Dispose();
        }

        AdvancedDisplayMode[] advancedModes =
        {
            AdvancedDisplayMode.BatteryGradient,
            AdvancedDisplayMode.BatteryIndicator,
            AdvancedDisplayMode.PercentageText
        };

        for (int i = 0; i < _advancedPreviewBoxes.Count && i < advancedModes.Length; i++)
        {
            Image? oldImage = _advancedPreviewBoxes[i].Image;
            _advancedPreviewBoxes[i].Image = CreateAdvancedModePreview(advancedModes[i]);
            oldImage?.Dispose();
        }
    }

    private Color GetBatteryColor(int battery)
    {
        List<BatteryColorSettings> colors = _workingColors
            .OrderByDescending(c => c.MinimumPercent)
            .ToList();

        BatteryColorSettings upper = colors.First(c => battery >= c.MinimumPercent);
        int upperIndex = colors.IndexOf(upper);

        if (!_settings.UseGradient || upperIndex >= colors.Count - 1)
            return upper.Color;

        BatteryColorSettings lower = colors[upperIndex + 1];
        int transition = Math.Clamp(_gradientPercentNumeric.Value is decimal v ? (int)v : 0, 0, 50);
        if (transition <= 0)
            return upper.Color;

        int start = upper.MinimumPercent;
        int end = Math.Max(lower.MinimumPercent, start - transition);
        if (battery >= end && battery < start)
        {
            double t = (start - battery) / (double)Math.Max(1, start - end);
            return Blend(upper.Color, lower.Color, t);
        }

        return lower.Color;
    }

    private static Color Blend(Color first, Color second, double t)
    {
        t = Math.Clamp(t, 0, 1);
        int r = (int)Math.Round(first.R + (second.R - first.R) * t);
        int g = (int)Math.Round(first.G + (second.G - first.G) * t);
        int b = (int)Math.Round(first.B + (second.B - first.B) * t);
        return Color.FromArgb(r, g, b);
    }

    private void ColorPanel_Click(object? sender, EventArgs e)
    {
        if (sender is not Panel panel || panel.Tag is not int index)
            return;

        BatteryColorSettings color = _workingColors[index];
        using var dialog = new ColorDialog { Color = color.Color, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        color.Color = dialog.Color;
        panel.BackColor = dialog.Color;
        RefreshPreviewImages();
    }

    private static void ColorPanel_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Panel panel)
            panel.BorderStyle = BorderStyle.Fixed3D;
    }

    private static void ColorPanel_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Panel panel)
            panel.BorderStyle = BorderStyle.FixedSingle;
    }

    private void UpdateColorRanges()
    {
        // Keep the three thresholds strictly descending while the user edits
        // them. The first color may be 0..100; each following color must be
        // strictly below the value directly above it.
        for (int i = 0; i < _minimumPercentControls.Count; i++)
        {
            NumericUpDown control = _minimumPercentControls[i];

            decimal maximum = i == 0
                ? 100
                : Math.Max(0, _minimumPercentControls[i - 1].Value - 1);

            if (control.Maximum != maximum)
                control.Maximum = maximum;

            if (control.Value > maximum)
                control.Value = maximum;

            _workingColors[i].MinimumPercent = (int)control.Value;
        }

        RefreshPreviewImages();
    }

    private BatteryDisplayMode GetSelectedDisplayMode()
    {
        if (_advancedRadioButton.Checked)
            return BatteryDisplayMode.Advanced;
        if (_batteryIndicatorRadioButton.Checked)
            return BatteryDisplayMode.BatteryIndicator;
        return BatteryDisplayMode.StaticIcon;
    }

    private AdvancedDisplayMode GetSelectedAdvancedDisplayMode()
    {
        if (_advancedBatteryIndicatorRadioButton.Checked)
            return AdvancedDisplayMode.BatteryIndicator;
        if (_percentageAdvancedRadioButton.Checked)
            return AdvancedDisplayMode.PercentageText;
        return AdvancedDisplayMode.BatteryGradient;
    }

    private void UpdateAdvancedOptionsVisibility()
    {
        bool showAdvanced = _advancedRadioButton.Checked;
        _colorsGroup.Visible = showAdvanced;

        _mainPanel.RowStyles[3] =
            new RowStyle(
                SizeType.Absolute,
                showAdvanced ? 340 : 0);

        ClientSize = new Size(
            WindowWidth,
            showAdvanced ? WindowHeightWithColors : WindowHeightWithoutColors);

        PositionWindowAtTop();
    }

    private void AdvancedDisplayModeRadioButton_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Checked)
            UpdateAdvancedOptionsVisibility();
    }

    private void DisplayModeRadioButton_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Checked)
            UpdateAdvancedOptionsVisibility();
    }

    private bool ValidateSettings()
    {
        UpdateColorRanges();
        for (int i = 0; i < _workingColors.Count - 1; i++)
        {
            if (_workingColors[i].MinimumPercent <= _workingColors[i + 1].MinimumPercent)
            {
                MessageBox.Show(this, L("ColorOrderError"), L("InvalidSettings"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        return true;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (!ApplyCurrentSettings())
            return;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void ApplyButton_Click(object? sender, EventArgs e)
    {
        _ = ApplyCurrentSettings();
    }

    private void DeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _deviceComboBox.Invalidate();
        if (_deviceComboBox.IsHandleCreated)
            _deviceComboBox.Update();
    }

    private bool ApplyCurrentSettings()
    {
        if (!ValidateSettings())
            return false;

        bool startupEnabled = _startupManager.IsEnabled();

        if (_startupCheckBox.Checked != startupEnabled)
        {
            try
            {
                if (_startupCheckBox.Checked)
                    _startupManager.Enable();
                else
                    _startupManager.Disable();
            }
            catch (Exception ex)
            {
                _startupCheckBox.Checked = _startupManager.IsEnabled();

                MessageBox.Show(
                    this,
                    string.Format(L("StartupError"), ex.Message),
                    L("InvalidSettings"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        _settings.Language = _selectedLanguage;
        _settings.Theme = _selectedTheme;
        _settings.ThemeConfigured = true;
        _settings.SelectedDevice = _deviceComboBox.SelectedItem?.ToString() ?? string.Empty;
        _settings.DisplayMode = GetSelectedDisplayMode();
        _settings.AdvancedDisplayMode = GetSelectedAdvancedDisplayMode();
        _settings.UseGradient = _gradientCheckBox.Checked;
        _settings.GradientPercent = (int)_gradientPercentNumeric.Value;
        _settings.BlinkOnCriticalBattery = _blinkCheckBox.Checked;
        _settings.CriticalBatteryPercent = (int)_criticalBatteryNumeric.Value;
        _settings.BatteryColors = _workingColors
            .Select(color => new BatteryColorSettings
            {
                Name = color.Name,
                MinimumPercent = color.MinimumPercent,
                Argb = color.Argb
            })
            .OrderByDescending(color => color.MinimumPercent)
            .ToList();

        _saved = true;
        SettingsApplied?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            this,
            L("RestoreDefaultsQuestion"),
            L("RestoreDefaults"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        AppSettings defaults = AppSettings.CreateDefault();
        _selectedLanguage = defaults.Language;
        _selectedTheme = defaults.Theme;
        _languageComboBox.SelectedIndex = (int)_selectedLanguage;
        _lightThemeRadioButton.Checked = false;
        _darkThemeRadioButton.Checked = false;
        _systemThemeRadioButton.Checked = true;
        _deviceComboBox.SelectedIndex = 0;
        _startupCheckBox.Checked = false;
        SetSelectedDisplayMode(defaults.DisplayMode);
        SetSelectedAdvancedDisplayMode(defaults.AdvancedDisplayMode);
        _gradientCheckBox.Checked = defaults.UseGradient;
        _gradientPercentNumeric.Value = defaults.GradientPercent;
        _blinkCheckBox.Checked = defaults.BlinkOnCriticalBattery;
        _criticalBatteryNumeric.Value = defaults.CriticalBatteryPercent;

        _workingColors.Clear();
        foreach (BatteryColorSettings color in defaults.BatteryColors)
        {
            _workingColors.Add(new BatteryColorSettings
            {
                Name = color.Name,
                MinimumPercent = color.MinimumPercent,
                Argb = color.Argb
            });
        }

        for (int i = 0; i < _minimumPercentControls.Count; i++)
        {
            _minimumPercentControls[i].Value = _workingColors[i].MinimumPercent;
            _colorPreviewPanels[i].BackColor = _workingColors[i].Color;
        }

        UpdateLocalizedText();
        UpdateAdvancedOptionsVisibility();
    }

    private void SetSelectedDisplayMode(BatteryDisplayMode mode)
    {
        _staticIconRadioButton.Checked = mode == BatteryDisplayMode.StaticIcon;
        _batteryIndicatorRadioButton.Checked = mode == BatteryDisplayMode.BatteryIndicator;
        _advancedRadioButton.Checked = mode == BatteryDisplayMode.Advanced;
    }

    private void SetSelectedAdvancedDisplayMode(AdvancedDisplayMode mode)
    {
        _gradientAdvancedRadioButton.Checked = mode == AdvancedDisplayMode.BatteryGradient;
        _advancedBatteryIndicatorRadioButton.Checked = mode == AdvancedDisplayMode.BatteryIndicator;
        _percentageAdvancedRadioButton.Checked = mode == AdvancedDisplayMode.PercentageText;
    }

    private AppTheme EffectiveSelectedTheme => ResolveTheme(_selectedTheme);

    private static AppTheme ResolveTheme(AppTheme theme)
    {
        if (theme != AppTheme.System)
            return theme;

        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            object? value = key?.GetValue("AppsUseLightTheme");

            if (value is int intValue)
                return intValue == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
        }

        return AppTheme.Light;
    }

    private static Icon LoadThemeIcon(AppTheme theme)
    {
        theme = ResolveTheme(theme);
        string prefix = theme == AppTheme.Dark ? "dark" : "light";
        string path = Path.Combine(
            Application.StartupPath,
            "Icons",
            theme == AppTheme.Dark ? "Dark" : "Light",
            $"{prefix}.ico");

        return TryLoadIcon(path)
            ?? new Icon(SystemIcons.Application, SystemIcons.Application.Size);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    private void ApplyTitleBarTheme(bool dark)
    {
        try
        {
            int value = dark ? 1 : 0;
            _ = DwmSetWindowAttribute(Handle, 20, ref value, sizeof(int));
        }
        catch
        {
        }
    }

    private void PositionWindowAtTop()
    {
        Screen screen = Screen.FromPoint(Cursor.Position);
        Rectangle workingArea = screen.WorkingArea;
        int x = workingArea.Left + (workingArea.Width - Width) / 2;
        int y = workingArea.Top + 5;
        Location = new Point(x, y);
    }

    public bool WasSaved => _saved;
}


internal sealed class DeviceSelectionComboBox : ComboBox
{
    private const int WmPaint = 0x000F;

    public DeviceSelectionComboBox()
    {
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    private bool HasNoSelection => SelectedIndex == 0;

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
        if (IsHandleCreated)
            Update();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WmPaint && HasNoSelection && !IsDisposed)
        {
            using Graphics graphics = Graphics.FromHwnd(Handle);
            using Pen pen = new Pen(Color.Red, 2);
            Rectangle border = new Rectangle(1, 1, Width - 3, Height - 3);
            graphics.DrawRectangle(pen, border);
        }
    }
}
