using System.Drawing;
using System.Drawing.Drawing2D;

namespace HyperXBatteryTray.Settings;

public sealed partial class SettingsForm : Form
{
    private static void DrawGlyph(Graphics graphics, Glyph glyph, Rectangle r, Color color, float width)
    {
        using Pen pen = new(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float x = r.X, y = r.Y, w = r.Width, h = r.Height;
        switch (glyph)
        {
            case Glyph.Headphones:
                graphics.DrawArc(pen, r, 180, 180);
                graphics.DrawLine(pen, x, y + h * .48f, x, y + h * .80f);
                graphics.DrawLine(pen, x + w, y + h * .48f, x + w, y + h * .80f);
                graphics.DrawRoundedRectangle(pen, new RectangleF(x - 1, y + h * .67f, w * .22f, h * .25f), 2);
                graphics.DrawRoundedRectangle(pen, new RectangleF(x + w - w * .22f + 1, y + h * .67f, w * .22f, h * .25f), 2);
                break;
            case Glyph.Monitor:
                graphics.DrawRoundedRectangle(pen, new RectangleF(x, y, w, h * .70f), 2);
                graphics.DrawLine(pen, x + w / 2, y + h * .70f, x + w / 2, y + h * .90f);
                graphics.DrawLine(pen, x + w * .30f, y + h * .90f, x + w * .70f, y + h * .90f);
                break;
            case Glyph.Battery:
                graphics.DrawRoundedRectangle(pen, new RectangleF(x + 1, y + 2, w * .78f, h - 4), 2);
                graphics.DrawLine(pen, x + w * .82f, y + h * .35f, x + w * .94f, y + h * .35f);
                graphics.DrawLine(pen, x + w * .94f, y + h * .35f, x + w * .94f, y + h * .65f);
                break;
            case Glyph.Bell:
                graphics.DrawArc(pen, new RectangleF(x + 2, y + 1, w - 4, h - 4), 205, 130);
                graphics.DrawLine(pen, x + 2, y + h * .78f, x + w - 2, y + h * .78f);
                graphics.DrawLine(pen, x + w * .43f, y + h * .90f, x + w * .57f, y + h * .90f);
                break;
            case Glyph.Gear:
                graphics.DrawEllipse(pen, new RectangleF(x + 3, y + 3, w - 6, h - 6));
                graphics.DrawEllipse(pen, new RectangleF(x + w * .36f, y + h * .36f, w * .28f, h * .28f));
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    float cx = x + w / 2 + (w * .36f) * (float)Math.Cos(a);
                    float cy = y + h / 2 + (h * .36f) * (float)Math.Sin(a);
                    graphics.DrawLine(pen, x + w / 2 + (w * .29f) * (float)Math.Cos(a), y + h / 2 + (h * .29f) * (float)Math.Sin(a), cx, cy);
                }
                break;
            case Glyph.Info:
                graphics.DrawEllipse(pen, r);
                using (Brush b = new SolidBrush(color))
                {
                    graphics.FillEllipse(b, new RectangleF(x + w / 2 - 1.5f, y + 5, 3, 3));
                    graphics.FillRoundedRectangle(b, new RectangleF(x + w / 2 - 1.5f, y + 10, 3, h * .35f), 1);
                }
                break;
            case Glyph.Globe:
                graphics.DrawEllipse(pen, r);
                graphics.DrawEllipse(pen, new RectangleF(x + w * .28f, y, w * .44f, h));
                graphics.DrawLine(pen, x + 1, y + h / 2, x + w - 1, y + h / 2);
                break;
            case Glyph.Palette:
                graphics.DrawEllipse(pen, r);
                using (Brush b = new SolidBrush(color))
                {
                    graphics.FillEllipse(b, new RectangleF(x + w * .28f, y + h * .27f, 3, 3));
                    graphics.FillEllipse(b, new RectangleF(x + w * .53f, y + h * .20f, 3, 3));
                    graphics.FillEllipse(b, new RectangleF(x + w * .69f, y + h * .39f, 3, 3));
                }
                break;
            case Glyph.Document:
                graphics.DrawRectangle(pen, new RectangleF(x + 2, y + 1, w * .72f, h - 3));
                graphics.DrawLine(pen, x + w * .55f, y + 1, x + w * .75f, y + h * .20f);
                graphics.DrawLine(pen, x + w * .55f, y + 1, x + w * .55f, y + h * .20f);
                graphics.DrawLine(pen, x + w * .55f, y + h * .20f, x + w * .75f, y + h * .20f);
                graphics.DrawLine(pen, x + 5, y + h * .48f, x + w * .60f, y + h * .48f);
                graphics.DrawLine(pen, x + 5, y + h * .68f, x + w * .60f, y + h * .68f);
                break;
            case Glyph.Windows:
                graphics.DrawLine(pen, x + w * .48f, y, x + w * .46f, y + h);
                graphics.DrawLine(pen, x, y + h * .48f, x + w, y + h * .46f);
                break;
        }
    }

}
