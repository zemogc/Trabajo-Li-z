using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>
/// Icons drawn from the Segoe MDL2 Assets font that ships with Windows 10 and 11,
/// so the app needs no third-party image files.
/// </summary>
internal static class Glyph
{
    public const char Search = '';
    public const char Add = '';
    public const char Edit = '';
    public const char Delete = '';
    public const char Save = '';
    public const char Cancel = '';
    public const char Product = '';
    public const char Category = '';
    public const char Provider = '';
    public const char Payment = '';
    public const char Exit = '';

    private const string FontFamily = "Segoe MDL2 Assets";

    public static Bitmap Render(char glyph, Color color, int size = 16)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        using var font = new Font(FontFamily, size * 0.75f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, size, size), format);

        return bitmap;
    }
}
