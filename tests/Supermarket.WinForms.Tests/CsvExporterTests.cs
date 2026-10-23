using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Tests;

public class CsvExporterTests
{
    [Theory]
    [InlineData("Arroz", "Arroz")]
    [InlineData("Arroz; 1 kg", "\"Arroz; 1 kg\"")]
    [InlineData("Queso \"campesino\"", "\"Queso \"\"campesino\"\"\"")]
    [InlineData("línea 1\nlínea 2", "\"línea 1\nlínea 2\"")]
    [InlineData("", "")]
    public void Quotes_only_when_needed(string value, string expected) =>
        Assert.Equal(expected, CsvExporter.Escape(value, ";"));

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [InlineData("+57 300", "'+57 300")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("-cmd", "'-cmd")]
    public void Neutralizes_spreadsheet_formulas(string value, string expected) =>
        Assert.Equal(expected, CsvExporter.Escape(value, ";"));

    [Theory]
    [InlineData("-5")]
    [InlineData("-1250")]
    public void Negative_numbers_are_not_treated_as_formulas(string value) =>
        Assert.Equal(value, CsvExporter.Escape(value, ";"));

    [Fact]
    public void Writes_header_and_rows_with_the_given_separator()
    {
        using var writer = new StringWriter { NewLine = "\n" };

        CsvExporter.Write(writer, ["Código", "Nombre"], [["1", "Arroz"], ["2", "Sal, marina"]], ",");

        Assert.Equal("Código,Nombre\n1,Arroz\n2,\"Sal, marina\"\n", writer.ToString());
    }

    [Fact]
    public void Files_start_with_a_byte_order_mark_so_excel_detects_utf8() =>
        Assert.Equal([0xEF, 0xBB, 0xBF], CsvExporter.Encoding.GetPreamble());
}
