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
    private sealed class GlyphControl : Control
    {
        private readonly Glyph _glyph;
        private readonly Color? _forcedColor;
        public GlyphControl(Glyph glyph, Color? color = null)
        {
            _glyph = glyph;
            _forcedColor = color;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawGlyph(e.Graphics, _glyph, new Rectangle(3, 3, Math.Max(8, Width - 6), Math.Max(8, Height - 6)), _forcedColor ?? ForeColor, 1.8f);
        }
    }
}
