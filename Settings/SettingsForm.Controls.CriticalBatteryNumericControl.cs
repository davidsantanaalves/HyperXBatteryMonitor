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
    private sealed class CriticalBatteryNumericControl : UserControl
    {
        private readonly TextBox _textBox;
        private int _minimum = 1;
        private int _maximum = 100;
        private int _increment = 1;
        private int _value = 10;
        private bool _dark;
        private NumericInputMouseFilter? _mouseFilter;

        public event EventHandler? ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = Math.Min(value, _maximum);
                Value = Math.Max(_minimum, _value);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(value, _minimum);
                Value = Math.Min(_maximum, _value);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Increment
        {
            get => _increment;
            set => _increment = Math.Max(1, value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Clamp(value, _minimum, _maximum);
                if (_value == clamped)
                {
                    UpdateText();
                    return;
                }

                _value = clamped;
                UpdateText();
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DarkMode
        {
            get => _dark;
            set
            {
                _dark = value;
                ApplyTheme();
                Invalidate();
            }
        }

        public CriticalBatteryNumericControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            TabStop = true;
            Padding = new Padding(8, 0, 24, 0);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Dock = DockStyle.None,
                Location = new Point(8, 5),
                Size = new Size(38, 24),
                Margin = Padding.Empty,
                Multiline = false,
                TabStop = true,
                Font = Font,
                BackColor = Color.FromArgb(38, 41, 44),
                ForeColor = Color.WhiteSmoke
            };
            _textBox.KeyPress += TextBox_KeyPress;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.MouseDown += TextBox_MouseDown;
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.Validating += TextBox_Validating;
            _textBox.TextChanged += (_, _) => Invalidate();
            _textBox.SizeChanged += (_, _) => CenterTextBoxVertically();
            Controls.Add(_textBox);
            ApplyTheme();
            UpdateText();
            LayoutTextBox();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_textBox != null)
            {
                _textBox.Font = Font;
                LayoutTextBox();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutTextBox();
            Invalidate();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            LayoutTextBox();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            LayoutTextBox();
        }

        private void LayoutTextBox()
        {
            if (_textBox == null)
                return;

            int leftInset = ScaleLogical(8);
            int rightInset = ScaleLogical(24);
            _textBox.Left = leftInset;
            _textBox.Width = Math.Max(1, Width - leftInset - rightInset);
            CenterTextBoxVertically();
        }

        private void CenterTextBoxVertically()
        {
            if (_textBox == null)
                return;

            _textBox.Top = Math.Max(0, (Height - _textBox.Height) / 2);
        }

        private int ScaleLogical(int logicalValue) =>
            PngIconCache.ScaleLogicalToInt(logicalValue, DeviceDpi);

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            EnsureMouseFilter();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _textBox.DeselectAll();
            RemoveMouseFilter();
            base.OnLostFocus(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            _textBox.Focus();
            _textBox.SelectAll();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta > 0)
                ChangeValue(_increment);
            else if (e.Delta < 0)
                ChangeValue(-_increment);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int borderInset = ScaleLogical(1);
            Rectangle borderRect = new(borderInset, borderInset, Math.Max(1, Width - ScaleLogical(3)), Math.Max(1, Height - ScaleLogical(3)));
            Color background = _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251);
            Color border = _dark ? Color.FromArgb(91, 96, 102) : Color.FromArgb(150, 157, 168);
            Color chevron = _dark ? Color.FromArgb(224, 227, 231) : Color.FromArgb(82, 89, 99);

            using Brush backgroundBrush = new SolidBrush(background);
            using Pen borderPen = new(border, 1f);
            int cornerRadius = ScaleLogical(7);
            e.Graphics.FillRoundedRectangle(backgroundBrush, borderRect, cornerRadius);
            e.Graphics.DrawRoundedRectangle(borderPen, borderRect, cornerRadius);

            int centerX = Width - ScaleLogical(13);
            using Pen chevronPen = new(chevron, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            // Compact 26px control: keep both chevrons fully inside the field.
            // The previous fixed coordinates placed the lower chevron outside the
            // control, making the spinner appear vertically displaced.
            float minimumVerticalInset = PngIconCache.ScaleLogical(5f, DeviceDpi);
            float upY = Math.Max(minimumVerticalInset, Height * 0.31f);
            float downY = Math.Min(Height - minimumVerticalInset, Height * 0.69f);
            float minimumChevronHalfHeight = PngIconCache.ScaleLogical(2f, DeviceDpi);
            float maximumChevronHalfHeight = PngIconCache.ScaleLogical(2.6f, DeviceDpi);
            float chevronHalfHeight = Math.Max(minimumChevronHalfHeight, Math.Min(maximumChevronHalfHeight, Height * 0.10f));
            int chevronHalfWidth = ScaleLogical(3);

            PointF[] up =
            {
                new(centerX - chevronHalfWidth, upY + chevronHalfHeight),
                new(centerX, upY),
                new(centerX + chevronHalfWidth, upY + chevronHalfHeight)
            };
            PointF[] down =
            {
                new(centerX - chevronHalfWidth, downY - chevronHalfHeight),
                new(centerX, downY),
                new(centerX + chevronHalfWidth, downY - chevronHalfHeight)
            };
            e.Graphics.DrawLines(chevronPen, up);
            e.Graphics.DrawLines(chevronPen, down);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            if (e.X >= Width - ScaleLogical(28))
            {
                if (e.Y < Height / 2)
                    ChangeValue(_increment);
                else
                    ChangeValue(-_increment);
            }
            else
            {
                _textBox.Focus();
                _textBox.SelectAll();
            }
        }

        private void ChangeValue(int delta)
        {
            Value = Math.Clamp(_value + delta, _minimum, _maximum);
            _textBox.Focus();
            _textBox.SelectAll();
        }

        private void TextBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            BeginInvoke(new Action(() =>
            {
                if (!_textBox.IsDisposed)
                {
                    EnsureMouseFilter();
                    _textBox.Focus();
                    _textBox.SelectAll();
                }
            }));
        }

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            _textBox.DeselectAll();
        }

        private void EnsureMouseFilter()
        {
            if (_mouseFilter != null)
                return;

            _mouseFilter = new NumericInputMouseFilter(this);
            Application.AddMessageFilter(_mouseFilter);
        }

        private void RemoveMouseFilter()
        {
            if (_mouseFilter == null)
                return;

            Application.RemoveMessageFilter(_mouseFilter);
            _mouseFilter = null;
        }

        private void ClearFocusFromInput()
        {
            _textBox.DeselectAll();
            FindForm()?.Focus();
        }

        private sealed class NumericInputMouseFilter : IMessageFilter
        {
            private readonly CriticalBatteryNumericControl _owner;

            public NumericInputMouseFilter(CriticalBatteryNumericControl owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != 0x0201 || _owner.IsDisposed || !_owner.Visible)
                    return false;

                Point screenPoint = Control.MousePosition;
                Rectangle ownerBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                if (!ownerBounds.Contains(screenPoint))
                    _owner.ClearFocusFromInput();

                return false;
            }
        }

        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                ChangeValue(_increment);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ChangeValue(-_increment);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                CommitText();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                UpdateText();
                e.Handled = true;
            }
        }

        private void TextBox_Validating(object? sender, CancelEventArgs e) => CommitText();

        private void CommitText()
        {
            if (int.TryParse(_textBox.Text, out int parsed))
                Value = parsed;
            else
                UpdateText();
        }

        private void UpdateText()
        {
            if (_textBox == null)
                return;

            string text = _value.ToString();
            if (_textBox.Text != text)
                _textBox.Text = text;
        }

        private void ApplyTheme()
        {
            if (_textBox == null)
                return;

            _textBox.BackColor = _dark ? Color.FromArgb(38, 41, 44) : Color.FromArgb(248, 249, 251);
            _textBox.ForeColor = _dark ? Color.WhiteSmoke : LightText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveMouseFilter();
                _textBox.LostFocus -= TextBox_LostFocus;
            }

            base.Dispose(disposing);
        }
    }
}
