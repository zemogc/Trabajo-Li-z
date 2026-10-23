using System.Drawing.Printing;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>
/// Prints receipt lines with a monospaced font so the fixed-width layout keeps its columns,
/// continuing on new pages when a long receipt does not fit.
/// </summary>
internal sealed class ReceiptDocument : PrintDocument
{
    private readonly IReadOnlyList<string> _lines;
    private readonly Font _font = new("Consolas", 9f);
    private int _nextLine;

    public ReceiptDocument(string title, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        _lines = lines;
        DocumentName = title;
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);

        // The preview and the actual print each run the document from the start.
        _nextLine = 0;
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPrintPage(e);

        var graphics = e.Graphics!;
        var lineHeight = _font.GetHeight(graphics);
        var y = (float)e.MarginBounds.Top;

        while (_nextLine < _lines.Count && y + lineHeight <= e.MarginBounds.Bottom)
        {
            graphics.DrawString(_lines[_nextLine], _font, Brushes.Black, e.MarginBounds.Left, y);
            y += lineHeight;
            _nextLine++;
        }

        e.HasMorePages = _nextLine < _lines.Count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _font.Dispose();
        }

        base.Dispose(disposing);
    }
}
