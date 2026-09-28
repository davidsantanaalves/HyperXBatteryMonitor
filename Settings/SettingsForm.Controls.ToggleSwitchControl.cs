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
    private sealed class ToggleSwitchControl : Control
    {
        private bool _checked;
        private bool _dark;

        public event EventHandler? CheckedChanged;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get => _dark; set { _dark = value; Invalidate(); } }

        public ToggleSwitchControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new(0, 2, Width - 1, Height - 5);
            Color trackColor = _checked ? Accent : (_dark ? Color.FromArgb(76, 81, 87) : Color.FromArgb(205, 211, 220));
            using (Brush b = new SolidBrush(trackColor))
                e.Graphics.FillRoundedRectangle(b, track, track.Height / 2);

            int knobSize = Math.Max(10, track.Height - 4);
            int knobX = _checked ? track.Right - knobSize - 2 : track.Left + 2;
            using Brush knob = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(knob, knobX, track.Top + 2, knobSize, knobSize);
        }
    }
}
