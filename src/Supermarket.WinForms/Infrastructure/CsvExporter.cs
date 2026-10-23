using System.Globalization;
using System.Text;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>
/// Writes tabular data as CSV that Excel opens correctly in any locale.
/// </summary>
internal static class CsvExporter
{
    // Excel reads UTF-8 only when the file starts with a byte order mark.
    public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>Writes the header line followed by one line per row.</summary>
    /// <remarks>
    /// The separator is usually the culture's list separator: ";" in Spanish locales, where "," is the decimal separator.
    /// </remarks>
    public static void Write(TextWriter writer, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, string separator)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentException.ThrowIfNullOrEmpty(separator);

        WriteLine(writer, headers, separator);
        foreach (var row in rows)
        {
            WriteLine(writer, row, separator);
        }
    }

    /// <summary>
    /// Quotes the value when needed and neutralizes spreadsheet formulas (CSV injection):
    /// a product named "=HYPERLINK(...)" must be shown as text, not executed.
    /// </summary>
    internal static string Escape(string value, string separator)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > 0 && FormulaPrefixes.Contains(value[0]) && !IsNumber(value))
        {
            value = "'" + value;
        }

        var mustQuote = value.Contains(separator, StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal);

        return mustQuote ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
    }

    private static bool IsNumber(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _);

    private static void WriteLine(TextWriter writer, IReadOnlyList<string> values, string separator) =>
        writer.WriteLine(string.Join(separator, values.Select(v => Escape(v, separator))));
}
