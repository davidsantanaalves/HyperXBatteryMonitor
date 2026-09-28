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
            float iconSize = PngIconCache.ScaleLogical(36f, DeviceDpi);
            float contentHeight = PngIconCache.ScaleLogical(73f, DeviceDpi);
            float iconY = (Height - contentHeight) / 2f;
            if (!string.IsNullOrWhiteSpace(_iconKey))
                _iconCache.Draw(e.Graphics, _iconKey, new RectangleF((Width - iconSize) / 2f, iconY, iconSize, iconSize), _dark, DeviceDpi);

            string label = LabelText;
            using Font labelFont = new("Segoe UI", 9f);
            Size textSize = TextRenderer.MeasureText(label, labelFont);
            float labelY = iconY + PngIconCache.ScaleLogical(53f, DeviceDpi);
            float groupWidth = textSize.Width + PngIconCache.ScaleLogical(18f, DeviceDpi);
            float groupX = (Width - groupWidth) / 2f;
            float radioX = groupX;
            float textX = radioX + PngIconCache.ScaleLogical(16f, DeviceDpi);
            float radioOffsetY = PngIconCache.ScaleLogical(3f, DeviceDpi);
            float radioSize = PngIconCache.ScaleLogical(10f, DeviceDpi);
            using (Brush radio = new SolidBrush(_selected ? Accent : secondary))
                e.Graphics.FillEllipse(radio, radioX, labelY + radioOffsetY, radioSize, radioSize);
            if (_selected)
            {
                using Brush dot = new SolidBrush(Color.White);
                float dotOffsetX = PngIconCache.ScaleLogical(3f, DeviceDpi);
                float dotOffsetY = PngIconCache.ScaleLogical(6f, DeviceDpi);
                float dotSize = PngIconCache.ScaleLogical(4f, DeviceDpi);
                e.Graphics.FillEllipse(dot, radioX + dotOffsetX, labelY + dotOffsetY, dotSize, dotSize);
            }
            TextRenderer.DrawText(e.Graphics, label, labelFont, new Point((int)textX, (int)labelY), text, TextFormatFlags.NoPrefix);
        }
    }
}
