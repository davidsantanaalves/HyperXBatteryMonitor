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
    private sealed class PngIconControl : Control
    {
        private readonly PngIconCache _iconCache;
        private readonly string _iconKey;
        private bool _dark;

        public PngIconControl(PngIconCache iconCache, string iconKey)
        {
            _iconCache = iconCache;
            _iconKey = iconKey;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            int size = Math.Max(1, Math.Min(Width, Height));
            _iconCache.Draw(e.Graphics, _iconKey, new RectangleF((Width - size) / 2f, (Height - size) / 2f, size, size), _dark, DeviceDpi);
        }
    }
}
