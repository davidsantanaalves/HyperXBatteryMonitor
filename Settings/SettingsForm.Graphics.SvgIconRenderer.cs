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
    private static class SvgIconRenderer
    {
        private static readonly System.Globalization.CultureInfo Invariant = System.Globalization.CultureInfo.InvariantCulture;
        private static readonly System.Text.RegularExpressions.Regex TokenRegex = new(@"[AaCcHhLlMmQqSsTtVvZz]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex TransformRegex = new(@"(?<name>matrix|translate|scale|rotate|skewX|skewY)\s*\((?<args>[^)]*)\)", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        public static void Draw(Graphics graphics, string? filePath, RectangleF bounds, Color color)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            try
            {
                XDocument document = XDocument.Load(filePath);
                XElement? root = document.Root;
                if (root == null) return;

                XNamespace ns = root.Name.Namespace;
                List<(GraphicsPath Path, bool HasFill, bool HasStroke, float StrokeWidth)> paths = new();
                RectangleF sourceBounds = RectangleF.Empty;

                // Normalize the artwork using the actual transformed path bounds rather
                // than the SVG viewport. The supplied Potrace files have no viewBox and
                // their artwork does not necessarily occupy the complete canvas.
                foreach (XElement element in document.Descendants(ns + "path"))
                {
                    string? rawData = element.Attribute("d")?.Value;
                    if (string.IsNullOrWhiteSpace(rawData)) continue;

                    // Parse the full, untouched path data first so that relative
                    // commands following the Potrace canvas sub-path keep the correct
                    // current point (see ParseFigures for details), then drop the
                    // canvas figure afterwards instead of slicing the raw text.
                    List<GraphicsPath> figures = ParseFigures(rawData);
                    if (figures.Count == 0) continue;
                    if (HasLeadingCanvasFigure(rawData))
                    {
                        figures[0].Dispose();
                        figures.RemoveAt(0);
                    }
                    if (figures.Count == 0) continue;

                    string fillRule = GetInheritedStyle(element, "fill-rule") ?? "evenodd";
                    FillMode fillMode = string.Equals(fillRule, "nonzero", StringComparison.OrdinalIgnoreCase)
                        ? FillMode.Winding
                        : FillMode.Alternate;

                    GraphicsPath path = new(fillMode);
                    foreach (GraphicsPath figure in figures)
                    {
                        path.AddPath(figure, false);
                        figure.Dispose();
                    }
                    if (path.PointCount == 0) { path.Dispose(); continue; }

                    using Matrix sourceTransform = GetCumulativeTransform(element, root);
                    if (!sourceTransform.IsIdentity)
                        path.Transform(sourceTransform);

                    RectangleF pathBounds = path.GetBounds();
                    if (pathBounds.Width <= 0 || pathBounds.Height <= 0)
                    {
                        path.Dispose();
                        continue;
                    }

                    sourceBounds = sourceBounds.IsEmpty
                        ? pathBounds
                        : RectangleF.Union(sourceBounds, pathBounds);

                    string fill = GetInheritedStyle(element, "fill") ?? "black";
                    string stroke = GetInheritedStyle(element, "stroke") ?? "none";
                    string strokeWidthText = GetInheritedStyle(element, "stroke-width") ?? "1";

                    bool hasFill = !string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(fill, "transparent", StringComparison.OrdinalIgnoreCase);
                    bool hasStroke = !string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase) &&
                                     !string.Equals(stroke, "transparent", StringComparison.OrdinalIgnoreCase);

                    float strokeWidth = 1.8f;
                    if (float.TryParse(strokeWidthText, System.Globalization.NumberStyles.Float, Invariant, out float parsed) && parsed > 0)
                        strokeWidth = parsed;

                    paths.Add((path, hasFill, hasStroke, strokeWidth));
                }

                if (paths.Count == 0 || sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
                {
                    foreach (var item in paths) item.Path.Dispose();
                    return;
                }

                float scale = Math.Min(
                    bounds.Width / sourceBounds.Width,
                    bounds.Height / sourceBounds.Height);
                float ox = bounds.X + (bounds.Width - sourceBounds.Width * scale) / 2f - sourceBounds.X * scale;
                float oy = bounds.Y + (bounds.Height - sourceBounds.Height * scale) / 2f - sourceBounds.Y * scale;

                using Matrix matrix = new(scale, 0, 0, scale, ox, oy);
                using Brush brush = new SolidBrush(color);

                foreach (var item in paths)
                {
                    using GraphicsPath path = item.Path;
                    path.Transform(matrix);

                    if (item.HasFill)
                        graphics.FillPath(brush, path);

                    if (item.HasStroke)
                    {
                        using Pen pen = new(color, Math.Max(1f, item.StrokeWidth * scale))
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        };
                        graphics.DrawPath(pen, path);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Detects whether <paramref name="data"/> begins with the Potrace full-canvas
        /// sub-path (a rectangle covering the whole document) that these icon files use
        /// as a base for their nonzero-winding cutouts. This only inspects the raw text
        /// to decide whether the first parsed figure should be discarded - the actual
        /// removal happens on the parsed geometry in <see cref="ParseFigures"/> callers,
        /// never by slicing the string itself (doing so would desynchronize the current
        /// point for any relative command that follows, shifting the real artwork).
        /// </summary>
        public static bool HasLeadingCanvasFigure(string data)
        {
            int firstMove = data.IndexOf('M');
            if (firstMove < 0) firstMove = data.IndexOf('m');
            if (firstMove < 0) return false;

            // The Potrace canvas sub-path always closes before the actual artwork starts.
            int closeUpper = data.IndexOf('Z', firstMove + 1);
            int closeLower = data.IndexOf('z', firstMove + 1);
            int close;
            if (closeUpper < 0) close = closeLower;
            else if (closeLower < 0) close = closeUpper;
            else close = Math.Min(closeUpper, closeLower);

            if (close < 0) return false;

            string prefix = data[firstMove..(close + 1)];
            return LooksLikeCanvasSubpath(prefix);
        }

        private static bool LooksLikeCanvasSubpath(string data)
        {
            // Every supplied Potrace icon starts with the 1254px canvas rectangle
            // represented in source coordinates by the 6270/12540 contour.
            return data.Contains("M0 6270", StringComparison.Ordinal) ||
                   data.Contains("M0 6270", StringComparison.OrdinalIgnoreCase);
        }

        private static Matrix GetCumulativeTransform(XElement element, XElement root)
        {
            Matrix result = new();
            List<XElement> chain = new();
            for (XElement? current = element; current != null; current = current.Parent)
            {
                chain.Add(current);
                if (current == root) break;
            }
            chain.Reverse();

            foreach (XElement current in chain)
            {
                string? transform = current.Attribute("transform")?.Value;
                if (string.IsNullOrWhiteSpace(transform)) continue;
                using Matrix next = ParseTransform(transform);
                result.Multiply(next, MatrixOrder.Prepend);
            }
            return result;
        }

        private static Matrix ParseTransform(string value)
        {
            Matrix result = new();
            foreach (System.Text.RegularExpressions.Match match in TransformRegex.Matches(value))
            {
                string name = match.Groups["name"].Value.ToLowerInvariant();
                float[] a = match.Groups["args"].Value
                    .Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => float.Parse(v, System.Globalization.NumberStyles.Float, Invariant))
                    .ToArray();
                using Matrix next = name switch
                {
                    "matrix" when a.Length >= 6 => new Matrix(a[0], a[1], a[2], a[3], a[4], a[5]),
                    "translate" when a.Length >= 1 => new Matrix(1, 0, 0, 1, a[0], a.Length > 1 ? a[1] : 0),
                    "scale" when a.Length >= 1 => new Matrix(a[0], 0, 0, a.Length > 1 ? a[1] : a[0], 0, 0),
                    "rotate" when a.Length >= 1 && a.Length < 3 => CreateRotation(a[0]),
                    "rotate" when a.Length >= 3 => CreateRotation(a[0], a[1], a[2]),
                    "skewx" when a.Length >= 1 => CreateSkewX(a[0]),
                    "skewy" when a.Length >= 1 => CreateSkewY(a[0]),
                    _ => new Matrix()
                };
                result.Multiply(next, MatrixOrder.Prepend);
            }
            return result;
        }

        private static Matrix CreateRotation(float degrees) { Matrix m = new(); m.Rotate(degrees, MatrixOrder.Append); return m; }
        private static Matrix CreateRotation(float degrees, float cx, float cy) { Matrix m = new(); m.Translate(cx, cy, MatrixOrder.Append); m.Rotate(degrees, MatrixOrder.Append); m.Translate(-cx, -cy, MatrixOrder.Append); return m; }
        private static Matrix CreateSkewX(float degrees) => new(1, 0, (float)Math.Tan(degrees * Math.PI / 180.0), 1, 0, 0);
        private static Matrix CreateSkewY(float degrees) => new(1, (float)Math.Tan(degrees * Math.PI / 180.0), 0, 1, 0, 0);

        private static string? GetInheritedStyle(XElement element, string name)
        {
            for (XElement? current = element; current != null; current = current.Parent)
            {
                string? direct = current.Attribute(name)?.Value;
                if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();

                string? style = current.Attribute("style")?.Value;
                if (!string.IsNullOrWhiteSpace(style))
                {
                    foreach (string declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] pair = declaration.Split(':', 2);
                        if (pair.Length == 2 && string.Equals(pair[0].Trim(), name, StringComparison.OrdinalIgnoreCase))
                            return pair[1].Trim();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Parses raw SVG path data into one <see cref="GraphicsPath"/> per sub-figure
        /// (i.e. one per M/m command), returned in document order.
        ///
        /// Crucially, this always parses the *entire* original data string in a single
        /// continuous pass, exactly like a real SVG renderer would. The current point
        /// (x, y) and current sub-path start point (sx, sy) are tracked across figure
        /// boundaries, so a relative command that immediately follows a closed figure
        /// (e.g. "...z m9855 2532...") still resolves to the correct absolute
        /// coordinates. Splitting figures out this way - rather than slicing the figure
        /// we don't want out of the *text* beforehand - is what lets callers safely
        /// discard the leading Potrace canvas rectangle (see
        /// <see cref="HasLeadingCanvasFigure"/>) without shifting every subsequent
        /// sub-path by that rectangle's own extent.
        /// </summary>
        public static List<GraphicsPath> ParseFigures(string data)
        {
            List<GraphicsPath> figures = new();
            GraphicsPath? current = null;
            string[] tokens = TokenRegex.Matches(data).Select(m => m.Value).ToArray();
            int i = 0;
            char command = 'M';
            float x = 0, y = 0, sx = 0, sy = 0;
            float lastCubicX = 0, lastCubicY = 0, lastQuadX = 0, lastQuadY = 0;
            char previousCommand = '\0';

            bool IsCommandToken() => i < tokens.Length && tokens[i].Length == 1 && char.IsLetter(tokens[i][0]);
            bool Has(int count) => i + count <= tokens.Length;
            float Number() => float.Parse(tokens[i++], Invariant);
            GraphicsPath Figure() => current ??= new GraphicsPath(FillMode.Winding);

            while (i < tokens.Length)
            {
                if (IsCommandToken()) command = tokens[i++][0];
                char upper = char.ToUpperInvariant(command);
                bool rel = char.IsLower(command);

                try
                {
                    switch (upper)
                    {
                        case 'M':
                            if (!Has(2)) return figures;
                            float mx = Number(), my = Number();
                            if (rel) { mx += x; my += y; }
                            current = new GraphicsPath(FillMode.Winding);
                            figures.Add(current);
                            current.StartFigure();
                            x = mx; y = my; sx = x; sy = y;
                            command = rel ? 'l' : 'L';
                            previousCommand = 'M';
                            break;

                        case 'L':
                            if (!Has(2)) return figures;
                            float lx = Number(), ly = Number();
                            if (rel) { lx += x; ly += y; }
                            Figure().AddLine(x, y, lx, ly); x = lx; y = ly;
                            previousCommand = 'L';
                            break;

                        case 'H':
                            if (!Has(1)) return figures;
                            float hx = Number(); if (rel) hx += x;
                            Figure().AddLine(x, y, hx, y); x = hx;
                            previousCommand = 'H';
                            break;

                        case 'V':
                            if (!Has(1)) return figures;
                            float vy = Number(); if (rel) vy += y;
                            Figure().AddLine(x, y, x, vy); y = vy;
                            previousCommand = 'V';
                            break;

                        case 'C':
                            if (!Has(6)) return figures;
                            float c1x = Number(), c1y = Number(), c2x = Number(), c2y = Number(), cx = Number(), cy = Number();
                            if (rel) { c1x += x; c1y += y; c2x += x; c2y += y; cx += x; cy += y; }
                            Figure().AddBezier(x, y, c1x, c1y, c2x, c2y, cx, cy);
                            x = cx; y = cy; lastCubicX = c2x; lastCubicY = c2y; previousCommand = 'C';
                            break;

                        case 'S':
                            if (!Has(4)) return figures;
                            float sc2x = Number(), sc2y = Number(), sx2 = Number(), sy2 = Number();
                            if (rel) { sc2x += x; sc2y += y; sx2 += x; sy2 += y; }
                            float sc1x = (previousCommand is 'C' or 'S') ? 2 * x - lastCubicX : x;
                            float sc1y = (previousCommand is 'C' or 'S') ? 2 * y - lastCubicY : y;
                            Figure().AddBezier(x, y, sc1x, sc1y, sc2x, sc2y, sx2, sy2);
                            x = sx2; y = sy2; lastCubicX = sc2x; lastCubicY = sc2y; previousCommand = 'S';
                            break;

                        case 'Q':
                            if (!Has(4)) return figures;
                            float qx = Number(), qy = Number(), qex = Number(), qey = Number();
                            if (rel) { qx += x; qy += y; qex += x; qey += y; }
                            float q1x = x + 2f * (qx - x) / 3f;
                            float q1y = y + 2f * (qy - y) / 3f;
                            float q2x = qex + 2f * (qx - qex) / 3f;
                            float q2y = qey + 2f * (qy - qey) / 3f;
                            Figure().AddBezier(x, y, q1x, q1y, q2x, q2y, qex, qey);
                            x = qex; y = qey; lastQuadX = qx; lastQuadY = qy; previousCommand = 'Q';
                            break;

                        case 'T':
                            if (!Has(2)) return figures;
                            float tex = Number(), tey = Number(); if (rel) { tex += x; tey += y; }
                            float tqx = (previousCommand is 'Q' or 'T') ? 2 * x - lastQuadX : x;
                            float tqy = (previousCommand is 'Q' or 'T') ? 2 * y - lastQuadY : y;
                            float tq1x = x + 2f * (tqx - x) / 3f;
                            float tq1y = y + 2f * (tqy - y) / 3f;
                            float tq2x = tex + 2f * (tqx - tex) / 3f;
                            float tq2y = tey + 2f * (tqy - tey) / 3f;
                            Figure().AddBezier(x, y, tq1x, tq1y, tq2x, tq2y, tex, tey);
                            x = tex; y = tey; lastQuadX = tqx; lastQuadY = tqy; previousCommand = 'T';
                            break;

                        case 'A':
                            if (!Has(7)) return figures;
                            float rx = Math.Abs(Number()), ry = Math.Abs(Number()), rotation = Number();
                            bool largeArc = Number() != 0, sweep = Number() != 0;
                            float ax = Number(), ay = Number(); if (rel) { ax += x; ay += y; }
                            AddArc(Figure(), x, y, rx, ry, rotation, largeArc, sweep, ax, ay);
                            x = ax; y = ay; previousCommand = 'A';
                            break;

                        case 'Z':
                            Figure().CloseFigure(); x = sx; y = sy; previousCommand = 'Z';
                            break;

                        default:
                            return figures;
                    }
                }
                catch { return figures; }
            }
            return figures;
        }

        private static void AddArc(GraphicsPath path, float x1, float y1, float rx, float ry, float rotation, bool largeArc, bool sweep, float x2, float y2)
        {
            if (Math.Abs(x1 - x2) < 0.0001f && Math.Abs(y1 - y2) < 0.0001f) return;
            if (rx < 0.0001f || ry < 0.0001f) { path.AddLine(x1, y1, x2, y2); return; }

            double phi = rotation * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);
            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double xp = cosPhi * dx + sinPhi * dy;
            double yp = -sinPhi * dx + cosPhi * dy;
            double rxd = rx, ryd = ry;
            double lambda = (xp * xp) / (rxd * rxd) + (yp * yp) / (ryd * ryd);
            if (lambda > 1) { double k = Math.Sqrt(lambda); rxd *= k; ryd *= k; }

            double sign = largeArc == sweep ? -1 : 1;
            double numerator = Math.Max(0, (rxd * rxd * ryd * ryd - rxd * rxd * yp * yp - ryd * ryd * xp * xp));
            double denominator = rxd * rxd * yp * yp + ryd * ryd * xp * xp;
            double coef = denominator < 1e-12 ? 0 : sign * Math.Sqrt(numerator / denominator);
            double cxp = coef * (rxd * yp / ryd);
            double cyp = coef * (-ryd * xp / rxd);
            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

            double ux = (xp - cxp) / rxd, uy = (yp - cyp) / ryd;
            double vx = (-xp - cxp) / rxd, vy = (-yp - cyp) / ryd;
            double theta1 = Math.Atan2(uy, ux);
            double delta = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            if (!sweep && delta > 0) delta -= 2 * Math.PI;
            if (sweep && delta < 0) delta += 2 * Math.PI;

            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2)));
            double step = delta / segments;
            double theta = theta1;
            for (int segment = 0; segment < segments; segment++)
            {
                double next = theta + step;
                double alpha = 4.0 / 3.0 * Math.Tan((next - theta) / 4.0);
                PointF p1 = ArcPoint(cx, cy, rxd, ryd, cosPhi, sinPhi, theta);
                PointF p2 = ArcPoint(cx, cy, rxd, ryd, cosPhi, sinPhi, next);
                double d1x = -rxd * cosPhi * Math.Sin(theta) - ryd * sinPhi * Math.Cos(theta);
                double d1y = -rxd * sinPhi * Math.Sin(theta) + ryd * cosPhi * Math.Cos(theta);
                double d2x = -rxd * cosPhi * Math.Sin(next) - ryd * sinPhi * Math.Cos(next);
                double d2y = -rxd * sinPhi * Math.Sin(next) + ryd * cosPhi * Math.Cos(next);
                PointF c1 = new((float)(p1.X + alpha * d1x), (float)(p1.Y + alpha * d1y));
                PointF c2 = new((float)(p2.X - alpha * d2x), (float)(p2.Y - alpha * d2y));
                path.AddBezier(p1, c1, c2, p2);
                theta = next;
            }
        }

        private static PointF ArcPoint(double cx, double cy, double rx, double ry, double cosPhi, double sinPhi, double theta)
        {
            double ct = Math.Cos(theta), st = Math.Sin(theta);
            return new PointF((float)(cx + rx * cosPhi * ct - ry * sinPhi * st),
                              (float)(cy + rx * sinPhi * ct + ry * cosPhi * st));
        }
    }
}
