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
    private enum Glyph { Headphones, Monitor, Battery, Bell, Gear, Info, Globe, Palette, Windows, Document }

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
            int accentTop = PngIconCache.ScaleLogicalToInt(6, DeviceDpi);
            int accentWidth = PngIconCache.ScaleLogicalToInt(3, DeviceDpi);
            int accentBottomInset = PngIconCache.ScaleLogicalToInt(12, DeviceDpi);
            if (_selected)
            {
                using Brush accent = new SolidBrush(Accent);
                e.Graphics.FillRoundedRectangle(accent, new Rectangle(0, accentTop, accentWidth, Math.Max(1, Height - accentBottomInset)), PngIconCache.ScaleLogicalToInt(2, DeviceDpi));
            }

            Color iconColor = text;
            if (!string.IsNullOrWhiteSpace(_iconKey))
                _iconCache.DrawLogical(e.Graphics, _iconKey, new RectangleF(14, 7, 25, 25), _dark, DeviceDpi);
            else
            {
                RectangleF glyphBounds = PngIconCache.ScaleLogical(new RectangleF(15, 8, 23, 23), DeviceDpi);
                DrawGlyph(e.Graphics, _glyph, Rectangle.Round(glyphBounds), iconColor, PngIconCache.ScaleLogical(1.65f, DeviceDpi));
            }

            int textLeft = PngIconCache.ScaleLogicalToInt(48, DeviceDpi);
            int textRightInset = PngIconCache.ScaleLogicalToInt(54, DeviceDpi);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(textLeft, 0, Math.Max(1, Width - textRightInset), Height), text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }
}
