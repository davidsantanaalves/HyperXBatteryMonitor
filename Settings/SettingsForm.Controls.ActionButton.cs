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
                float iconLeft = rect.X + PngIconCache.ScaleLogical(10f, DeviceDpi);
                float iconTop = rect.Y + PngIconCache.ScaleLogical(8f, DeviceDpi);
                float iconSize = PngIconCache.ScaleLogical(20f, DeviceDpi);
                _iconCache.Draw(e.Graphics, "reset", new RectangleF(iconLeft, iconTop, iconSize, iconSize), _dark, DeviceDpi);
                int textInset = PngIconCache.ScaleLogicalToInt(30, DeviceDpi);
                textRect.X += textInset;
                textRect.Width -= textInset;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
                _primary ? Color.White : (_dark ? Color.WhiteSmoke : LightText),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
