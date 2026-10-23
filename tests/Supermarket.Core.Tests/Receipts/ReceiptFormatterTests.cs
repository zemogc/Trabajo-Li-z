using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Receipts;

namespace Supermarket.Core.Tests.Receipts;

public class ReceiptFormatterTests
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    private static readonly StoreInfo Store = new()
    {
        Name = "Supermercado El Ahorro",
        TaxId = "900.123.456-7",
        Address = "Calle 10 # 5-20, Cali",
        Phone = "602 555 0101",
    };

    private static Sale CreateSale(DateTime? cancelledAt = null) => new()
    {
        Id = 3,
        CreatedAt = new DateTime(2026, 10, 23, 17, 54, 0, DateTimeKind.Utc),
        PaymentMethodName = "Efectivo",
        Total = 13200m,
        CancelledAt = cancelledAt,
    };

    private static readonly SaleLine[] Lines =
    [
        new() { ProductName = "Leche entera 1 L", Quantity = 2, UnitPrice = 4200m },
        new() { ProductName = "Arroz 1 kg", Quantity = 1, UnitPrice = 4800m },
    ];

    [Fact]
    public void Lays_out_header_lines_and_totals()
    {
        var receipt = ReceiptFormatter.Format(CreateSale(), Lines, Store, Colombia);

        Assert.Equal("         SUPERMERCADO EL AHORRO", receipt[0]);
        Assert.Equal("           NIT 900.123.456-7", receipt[1]);
        Assert.Contains("Leche entera 1 L", receipt);
        Assert.Contains("  2 x 4.200,00                  8.400,00", receipt);
        Assert.Contains("TOTAL                          13.200,00", receipt);
        Assert.Contains("Medio de pago                   Efectivo", receipt);
        Assert.Contains("Artículos                              3", receipt);
        Assert.Equal("        ¡Gracias por su compra!", receipt[^1]);
    }

    [Fact]
    public void No_line_exceeds_the_width()
    {
        var longName = new[] { new SaleLine { ProductName = "Detergente líquido concentrado para ropa blanca y de color 3 L", Quantity = 1, UnitPrice = 39900m } };

        var receipt = ReceiptFormatter.Format(CreateSale(), longName, Store, Colombia, width: 32);

        Assert.All(receipt, line => Assert.True(line.Length <= 32, $"'{line}' is longer than 32"));
        Assert.Contains(receipt, line => line.StartsWith("Detergente", StringComparison.Ordinal));
    }

    [Fact]
    public void Voided_sales_are_marked()
    {
        var receipt = ReceiptFormatter.Format(CreateSale(DateTime.UtcNow), Lines, Store, Colombia);

        Assert.Contains("         *** VENTA ANULADA ***", receipt);
    }

    [Fact]
    public void Optional_store_details_are_skipped()
    {
        var receipt = ReceiptFormatter.Format(CreateSale(), Lines, new StoreInfo { Footer = "" }, Colombia);

        Assert.Equal("              SUPERMERCADO", receipt[0]);
        Assert.StartsWith("-----", receipt[1], StringComparison.Ordinal);
        Assert.DoesNotContain(receipt, line => line.Contains("NIT", StringComparison.Ordinal));
        Assert.StartsWith("-----", receipt[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void Too_narrow_widths_are_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ReceiptFormatter.Format(CreateSale(), Lines, Store, Colombia, width: 10));
}
