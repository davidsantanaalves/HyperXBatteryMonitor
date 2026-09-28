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
}
