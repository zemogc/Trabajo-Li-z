using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>
/// Icons drawn from the Segoe MDL2 Assets font that ships with Windows 10 and 11,
/// so the app needs no third-party image files.
/// </summary>
internal static class Glyph
{
    public const char Search = '\uE721';
    public const char Add = '\uE710';
    public const char Edit = '\uE70F';
    public const char Delete = '\uE74D';
    public const char Save = '\uE74E';
    public const char Cancel = '\uE711';
    public const char Product = '\uE719';
    public const char Category = '\uE8EC';
    public const char Provider = '\uE716';
    public const char Payment = '\uE8C7';
    public const char Exit = '\uE7E8';
    public const char Export = '\uE896';
    public const char Sale = '\uE7BF';

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
