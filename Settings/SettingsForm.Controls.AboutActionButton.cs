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

            int iconSize = PngIconCache.ScaleLogicalToInt(25, DeviceDpi);
            int iconLeft = (int)rect.X + PngIconCache.ScaleLogicalToInt(8, DeviceDpi);
            Rectangle iconRect = new(iconLeft, (Height - iconSize) / 2, iconSize, iconSize);
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
            int textInset = PngIconCache.ScaleLogicalToInt(32, DeviceDpi);
            textRect.X += textInset;
            textRect.Width -= textInset;
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
}
