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
    private sealed class LanguagePopupControl : Control
    {
        private string[] _items = Array.Empty<string>();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _darkMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<string>();
                Height = _items.Length * ItemHeight;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value >= 0 && value < _items.Length ? value : -1;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                _darkMode = value;
                Invalidate();
            }
        }

        public event Action<int>? ItemClicked;
        public event EventHandler? Dismissed;

        private const int LogicalItemHeight = 30;
        private const int LogicalBorderWidth = 1;

        private int ItemHeight => PngIconCache.ScaleLogicalToInt(LogicalItemHeight, DeviceDpi);
        private int BorderWidth => PngIconCache.ScaleLogicalToInt(LogicalBorderWidth, DeviceDpi);

        public LanguagePopupControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(38, 41, 44);
        }

        public void ApplyTheme(bool dark)
        {
            DarkMode = dark;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = _darkMode ? Color.FromArgb(38, 41, 44) : Color.White;
            Color border = _darkMode ? Color.FromArgb(105, 112, 120) : Color.FromArgb(194, 201, 211);
            Color hover = _darkMode ? Color.FromArgb(30, 66, 99) : Color.FromArgb(224, 238, 255);
            Color foreground = _darkMode ? Color.WhiteSmoke : LightText;

            using SolidBrush backgroundBrush = new(background);
            using Pen borderPen = new(border, 1f);
            e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            for (int i = 0; i < _items.Length; i++)
            {
                Rectangle itemBounds = new(BorderWidth, BorderWidth + i * ItemHeight,
                    Math.Max(1, Width - BorderWidth * 2), ItemHeight);
                bool highlighted = i == _hoverIndex || (i == _selectedIndex && _hoverIndex < 0);

                if (highlighted)
                {
                    using SolidBrush hoverBrush = new(hover);
                    e.Graphics.FillRectangle(hoverBrush, itemBounds);
                }

                TextRenderer.DrawText(e.Graphics, _items[i], Font,
                    new Rectangle(itemBounds.X + ScaleLogical(10), itemBounds.Y, itemBounds.Width - ScaleLogical(20), itemBounds.Height),
                    foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private int ScaleLogical(int logicalValue) =>
            PngIconCache.ScaleLogicalToInt(logicalValue, DeviceDpi);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexFromPoint(e.Location);
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;

            int index = IndexFromPoint(e.Location);
            if (index >= 0)
                ItemClicked?.Invoke(index);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Down)
            {
                int next = Math.Min(_items.Length - 1, Math.Max(0, (_hoverIndex >= 0 ? _hoverIndex : _selectedIndex) + 1));
                _hoverIndex = next;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                int next = Math.Max(0, (_hoverIndex >= 0 ? _hoverIndex : _selectedIndex) - 1);
                _hoverIndex = next;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                int index = _hoverIndex >= 0 ? _hoverIndex : _selectedIndex;
                if (index >= 0)
                    ItemClicked?.Invoke(index);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private int IndexFromPoint(Point point)
        {
            int index = (point.Y - BorderWidth) / ItemHeight;
            return point.X >= BorderWidth && point.X < Width - BorderWidth &&
                   index >= 0 && index < _items.Length ? index : -1;
        }
    }
}
