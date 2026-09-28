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
    private sealed class StatusDotControl : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Connected { get; set; }
        public StatusDotControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color statusColor = Connected ? Color.FromArgb(52, 211, 85) : Color.FromArgb(239, 68, 68);
            using Brush brush = new SolidBrush(statusColor);
            e.Graphics.FillEllipse(brush, 2.5f, 2.5f, 13f, 13f);
            using Pen glow = new(Color.FromArgb(90, statusColor.R, statusColor.G, statusColor.B), 1.5f);
            e.Graphics.DrawEllipse(glow, 1f, 1f, 16f, 16f);
        }
    }
}
