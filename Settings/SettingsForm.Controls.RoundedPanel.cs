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
    private sealed class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = LightBorder;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color OutsideBackColor { get; set; } = LightBackground;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.White;
            Padding = new Padding(0);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The rounded corners must reveal the exact background behind the card.
            // Do not rely on WinForms transparent-background emulation here: it can
            // expose the Form's default background at the corners and create dark
            // rectangular remnants around the rounded shape.
            e.Graphics.Clear(OutsideBackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Keep the complete path inside the client area. The border is centered
            // on the path, so a half-pixel inset prevents the stroke from being
            // clipped by the control bounds.
            const float borderInset = 0.75f;
            RectangleF rect = new(
                borderInset,
                borderInset,
                Math.Max(1f, Width - borderInset * 2f),
                Math.Max(1f, Height - borderInset * 2f));

            using GraphicsPath path = GraphicsExtensions.CreateRoundedPath(rect, 10);
            using Brush fill = new SolidBrush(BackColor);
            using Pen border = new(BorderColor, 1f) { LineJoin = LineJoin.Round };

            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
    }
}
