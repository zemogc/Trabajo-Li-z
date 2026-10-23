using Dapper;
using Microsoft.Data.SqlClient;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class ReportRepositoryTests(SqlServerFixture database) : DatabaseTest(database)
{
    private static DateRange AroundNow => new(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

    [Fact]
    public async Task Empty_period_reports_zeros()
    {
        var summary = await Database.Reports.GetSalesSummaryAsync(AroundNow);

        Assert.Equal(0, summary.SalesCount);
        Assert.Equal(0m, summary.Revenue);
        Assert.Equal(0, summary.UnitsSold);
        Assert.Empty(await Database.Reports.GetTopProductsAsync(AroundNow, 5));
        Assert.Empty(await Database.Reports.GetSalesByPaymentMethodAsync(AroundNow));
    }

    [Fact]
    public async Task Reports_count_valid_sales_inside_the_period_only()
    {
        var ids = await SeedAsync();
        await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Rice, 2), new SaleItem(ids.Milk, 1)]);
        await Database.Sales.RegisterAsync(ids.Card, [new SaleItem(ids.Rice, 1)]);

        var voided = await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Milk, 5)]);
        await Database.Sales.CancelAsync(voided.SaleId);

        var lastWeek = await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Milk, 3)]);
        await MoveSaleAsync(lastWeek.SaleId, DateTime.UtcNow.AddDays(-7));

        var summary = await Database.Reports.GetSalesSummaryAsync(AroundNow);
        Assert.Equal(2, summary.SalesCount);
        Assert.Equal((3 * 4800m) + 4200m, summary.Revenue);
        Assert.Equal(4, summary.UnitsSold);

        var top = await Database.Reports.GetTopProductsAsync(AroundNow, 5);
        Assert.Equal(["Arroz", "Leche"], top.Select(p => p.ProductName));
        Assert.Equal(3, top[0].Units);
        Assert.Equal(14400m, top[0].Revenue);

        var byMethod = await Database.Reports.GetSalesByPaymentMethodAsync(AroundNow);
        Assert.Equal(["Efectivo", "Tarjeta"], byMethod.Select(m => m.PaymentMethodName));
        Assert.Equal(9600m + 4200m, byMethod[0].Total);
        Assert.Equal(1, byMethod[1].SalesCount);
    }

    [Fact]
    public async Task Top_products_respects_the_requested_count()
    {
        var ids = await SeedAsync();
        await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Rice, 1), new SaleItem(ids.Milk, 2)]);

        var top = Assert.Single(await Database.Reports.GetTopProductsAsync(AroundNow, 1));

        Assert.Equal("Leche", top.ProductName);
    }

    [Fact]
    public async Task The_period_end_is_exclusive()
    {
        var ids = await SeedAsync();
        var sale = await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Rice, 1)]);
        var boundary = new DateTime(2026, 1, 1, 5, 0, 0, DateTimeKind.Utc);
        await MoveSaleAsync(sale.SaleId, boundary);

        Assert.Equal(0, (await Database.Reports.GetSalesSummaryAsync(new DateRange(boundary.AddDays(-1), boundary))).SalesCount);
        Assert.Equal(1, (await Database.Reports.GetSalesSummaryAsync(new DateRange(boundary, boundary.AddDays(1)))).SalesCount);
    }

    [Fact]
    public async Task The_period_sales_list_includes_voided_sales_newest_first()
    {
        var ids = await SeedAsync();
        var first = await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Rice, 2)]);
        var voided = await Database.Sales.RegisterAsync(ids.Card, [new SaleItem(ids.Milk, 1)]);
        await Database.Sales.CancelAsync(voided.SaleId);
        var outside = await Database.Sales.RegisterAsync(ids.Cash, [new SaleItem(ids.Milk, 1)]);
        await MoveSaleAsync(outside.SaleId, DateTime.UtcNow.AddDays(-3));

        var sales = await Database.Reports.GetSalesAsync(AroundNow);

        Assert.Equal([voided.SaleId, first.SaleId], sales.Select(s => s.Id));
        Assert.True(sales[0].IsCancelled);
        Assert.Equal(2, sales[1].ItemCount);
        Assert.Equal("Efectivo", sales[1].PaymentMethodName);
    }

    private async Task<(int Cash, int Card, int Rice, int Milk)> SeedAsync()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var cash = await Database.PaymentMethods.AddAsync(new PaymentMethod { Name = "Efectivo" });
        var card = await Database.PaymentMethods.AddAsync(new PaymentMethod { Name = "Tarjeta" });
        var rice = await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = 100, CategoryId = grains });
        var milk = await Database.Products.AddAsync(new Product { Name = "Leche", Price = 4200m, Stock = 100, CategoryId = grains });
        return (cash, card, rice, milk);
    }

    private async Task MoveSaleAsync(int saleId, DateTime createdAtUtc)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Sales SET CreatedAt = @createdAtUtc WHERE Id = @saleId;", new { saleId, createdAtUtc });
    }
}
