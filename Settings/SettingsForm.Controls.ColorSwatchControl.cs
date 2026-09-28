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
    private sealed class ColorSwatchControl : Control
    {
        private Color _color;
        private readonly bool _dark;

        public event EventHandler? ColorChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;

                _color = value;
                Invalidate();
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ColorSwatchControl(Color color, bool dark)
        {
            _color = color;
            _dark = dark;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF outer = new(1, 1, Width - 2, Height - 2);
            using Brush background = new SolidBrush(
                _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251));
            using Pen border = new(
                _dark ? Color.FromArgb(91, 96, 102) : Color.FromArgb(170, 176, 185), 1f);

            e.Graphics.FillRoundedRectangle(background, outer, 7);
            e.Graphics.DrawRoundedRectangle(border, outer, 7);

            RectangleF swatch = new(8, 8, Width - 16, Height - 16);
            using Brush colorBrush = new SolidBrush(_color);
            e.Graphics.FillRoundedRectangle(colorBrush, swatch, 5);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            using ColorDialog dialog = new()
            {
                Color = _color,
                FullOpen = true,
                AnyColor = true
            };

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                Color = dialog.Color;
        }
    }
}
