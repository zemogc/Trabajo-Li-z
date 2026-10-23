using System.Globalization;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Receipts;

/// <summary>
/// Lays out a sale as fixed-width text, the format of thermal receipt printers.
/// </summary>
/// <remarks>
/// Producing plain lines keeps the layout testable and independent of the printing technology:
/// the UI only draws them with a monospaced font.
/// </remarks>
public static class ReceiptFormatter
{
    /// <summary>Characters per line of a typical 80 mm receipt printer.</summary>
    public const int DefaultWidth = 40;

    public static IReadOnlyList<string> Format(
        Sale sale,
        IReadOnlyList<SaleLine> lines,
        StoreInfo store,
        CultureInfo culture,
        int width = DefaultWidth)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 24);

        var separator = new string('-', width);
        var output = new List<string>();

        foreach (var header in new[] { store.Name.ToUpper(culture), Prefixed("NIT ", store.TaxId), store.Address, Prefixed("Tel. ", store.Phone) })
        {
            if (!string.IsNullOrWhiteSpace(header))
            {
                output.AddRange(Wrap(header, width).Select(line => Center(line, width)));
            }
        }

        output.Add(separator);
        output.Add(Columns($"Venta N.º {sale.Id}", sale.CreatedAtLocal.ToString("dd/MM/yyyy HH:mm", culture), width));
        output.Add(separator);

        foreach (var line in lines)
        {
            output.AddRange(Wrap(line.ProductName, width));
            output.Add(Columns(
                $"  {line.Quantity.ToString("N0", culture)} x {line.UnitPrice.ToString("N2", culture)}",
                line.Subtotal.ToString("N2", culture),
                width));
        }

        output.Add(separator);
        output.Add(Columns("TOTAL", sale.Total.ToString("N2", culture), width));
        output.Add(Columns("Medio de pago", sale.PaymentMethodName, width));
        output.Add(Columns("Artículos", lines.Sum(l => l.Quantity).ToString("N0", culture), width));
        output.Add(separator);

        if (sale.IsCancelled)
        {
            output.Add(Center("*** VENTA ANULADA ***", width));
        }

        if (!string.IsNullOrWhiteSpace(store.Footer))
        {
            output.AddRange(Wrap(store.Footer, width).Select(line => Center(line, width)));
        }

        return output;
    }

    private static string? Prefixed(string prefix, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : prefix + value.Trim();

    private static string Center(string text, int width)
    {
        var padding = Math.Max(0, (width - text.Length) / 2);
        return (new string(' ', padding) + text).TrimEnd();
    }

    /// <summary>Left text and right-aligned value on one line; the left text is cut if both do not fit.</summary>
    private static string Columns(string left, string right, int width)
    {
        var room = width - right.Length - 1;
        if (left.Length > room)
        {
            left = room > 1 ? left[..(room - 1)] + "…" : string.Empty;
        }

        return left + new string(' ', width - left.Length - right.Length) + right;
    }

    /// <summary>Breaks text on spaces so no line exceeds the width; long words are split.</summary>
    private static IEnumerable<string> Wrap(string text, int width)
    {
        var current = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var remaining = word;
            while (remaining.Length > width)
            {
                if (current.Length > 0)
                {
                    yield return current;
                    current = string.Empty;
                }

                yield return remaining[..width];
                remaining = remaining[width..];
            }

            if (current.Length == 0)
            {
                current = remaining;
            }
            else if (current.Length + 1 + remaining.Length <= width)
            {
                current += " " + remaining;
            }
            else
            {
                yield return current;
                current = remaining;
            }
        }

        if (current.Length > 0)
        {
            yield return current;
        }
    }
}
