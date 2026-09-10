using System.Drawing;
using System.Drawing.Drawing2D;

namespace HyperXBatteryTray.Settings;

internal sealed class PngIconCache : IDisposable
{
    private readonly Dictionary<BitmapCacheKey, Bitmap> _bitmaps = new();
    private bool _disposed;

    public Bitmap Get(string iconKey, bool darkMode, int sizePx, int dpi = 96)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int requestedSize = Math.Max(1, sizePx);
        BitmapCacheKey cacheKey = new(iconKey, darkMode, requestedSize, Math.Max(1, dpi));
        if (_bitmaps.TryGetValue(cacheKey, out Bitmap? bitmap))
            return bitmap;

        string filePath = GetIconPath(iconKey, darkMode, requestedSize);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"PNG icon '{Path.GetFileName(filePath)}' was not found.", filePath);

        using Bitmap source = new(filePath);
        bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        _bitmaps.Add(cacheKey, bitmap);
        return bitmap;
    }

    public void Draw(Graphics graphics, string iconKey, RectangleF bounds, bool darkMode, int dpi)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        int logicalSize = Math.Max(1, (int)Math.Round(
            Math.Max(bounds.Width, bounds.Height),
            MidpointRounding.AwayFromZero));
        int assetSize = ResolveAssetSize(iconKey, logicalSize);
        Bitmap bitmap = Get(iconKey, darkMode, assetSize, dpi);

        GraphicsState state = graphics.Save();
        try
        {
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(bitmap, bounds);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    public void Prewarm(IEnumerable<PngIconRequest> requests, bool darkMode, int dpi)
    {
        foreach (PngIconRequest request in requests)
            _ = Get(request.IconKey, darkMode, request.LogicalSize, dpi);
    }

    public void ClearBitmaps()
    {
        foreach (Bitmap bitmap in _bitmaps.Values)
            bitmap.Dispose();
        _bitmaps.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearBitmaps();
        _disposed = true;
    }

    private static string GetIconPath(string iconKey, bool darkMode, int sizePx)
    {
        string theme = darkMode ? "Dark" : "Light";
        string suffix = darkMode ? "dark" : "light";
        return Path.Combine(
            AppContext.BaseDirectory,
            "Icons",
            theme,
            $"{iconKey}-{suffix}-{sizePx}x{sizePx}.png");
    }

    private static int ResolveAssetSize(string iconKey, int requestedSize)
    {
        if (string.Equals(iconKey, "reset", StringComparison.OrdinalIgnoreCase))
            return 20;
        if (requestedSize <= 25)
            return 25;
        return 36;
    }

    private readonly record struct BitmapCacheKey(string IconKey, bool DarkMode, int SizePx, int Dpi);
}

internal readonly record struct PngIconRequest(string IconKey, int LogicalSize);
