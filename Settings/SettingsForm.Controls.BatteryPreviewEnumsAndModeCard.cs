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
    private enum BatteryPreviewKind
    {
        Normal,
        Charging,
        Glow,
        Level,
        Solid
    }

    private sealed class BatteryModeCard : Control
    {
        private bool _selected;
        private bool _dark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                    return;
                _selected = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                if (_dark == value)
                    return;
                _dark = value;
                Invalidate();
            }
        }

        public BatteryModeCard()
        {
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

            float inset = 2f;
            float diameter = Math.Min(Width, Height) - inset * 2f;
            RectangleF outer = new(inset, inset, diameter, diameter);
            Color border = _selected ? Accent : (_dark ? Color.FromArgb(188, 197, 207) : Color.FromArgb(94, 103, 115));

            using Pen pen = new(border, _selected ? 2.2f : 1.8f);
            e.Graphics.DrawEllipse(pen, outer);

            if (_selected)
            {
                float dot = diameter * 0.43f;
                using Brush brush = new SolidBrush(Accent);
                e.Graphics.FillEllipse(
                    brush,
                    outer.X + (diameter - dot) / 2f,
                    outer.Y + (diameter - dot) / 2f,
                    dot,
                    dot);
            }
        }
    }
}
