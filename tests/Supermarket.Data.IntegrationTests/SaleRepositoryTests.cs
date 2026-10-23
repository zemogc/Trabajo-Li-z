using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class SaleRepositoryTests(SqlServerFixture database) : DatabaseTest(database)
{
    private int _cash;
    private int _rice;
    private int _milk;

    [Fact]
    public async Task Registering_a_sale_discounts_stock_and_stores_database_prices()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);

        var receipt = await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 3), new SaleItem(_milk, 2)]);

        Assert.True(receipt.SaleId > 0);
        Assert.Equal((3 * 4800m) + (2 * 4200m), receipt.Total);
        Assert.Equal(7, await StockOfAsync(_rice));
        Assert.Equal(3, await StockOfAsync(_milk));

        var lines = await Database.Sales.GetLinesAsync(receipt.SaleId);
        Assert.Equal(["Arroz", "Leche"], lines.Select(l => l.ProductName));
        Assert.Equal(4800m, lines[0].UnitPrice);
    }

    [Fact]
    public async Task Repeated_products_are_merged_into_one_line()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);

        var receipt = await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1), new SaleItem(_rice, 2)]);

        var line = Assert.Single(await Database.Sales.GetLinesAsync(receipt.SaleId));
        Assert.Equal(3, line.Quantity);
        Assert.Equal(7, await StockOfAsync(_rice));
    }

    [Fact]
    public async Task Insufficient_stock_rolls_back_the_whole_sale()
    {
        await SeedAsync(riceStock: 10, milkStock: 1);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 3), new SaleItem(_milk, 2)]));

        Assert.Contains("Leche", error.Message, StringComparison.Ordinal);
        Assert.Contains("disponible 1", error.Message, StringComparison.Ordinal);
        Assert.Equal(10, await StockOfAsync(_rice));
        Assert.Equal(1, await StockOfAsync(_milk));
        Assert.Empty(await Database.Sales.GetRecentAsync(10));
    }

    [Fact]
    public async Task Concurrent_sales_cannot_oversell_the_last_units()
    {
        await SeedAsync(riceStock: 1, milkStock: 1);

        var attempts = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            try
            {
                await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1)]);
                return true;
            }
            catch (BusinessRuleException)
            {
                return false;
            }
        }));

        Assert.Equal(1, attempts.Count(succeeded => succeeded));
        Assert.Equal(0, await StockOfAsync(_rice));
        Assert.Single(await Database.Sales.GetRecentAsync(10));
    }

    [Fact]
    public async Task Unknown_product_or_payment_method_saves_nothing()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1), new SaleItem(99_999, 1)]));
        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.Sales.RegisterAsync(99_999, [new SaleItem(_rice, 1)]));

        Assert.Equal(10, await StockOfAsync(_rice));
        Assert.Empty(await Database.Sales.GetRecentAsync(10));
    }

    [Fact]
    public async Task Empty_sales_and_non_positive_quantities_are_rejected()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Database.Sales.RegisterAsync(_cash, []));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 0)]));
    }

    [Fact]
    public async Task Recent_sales_come_newest_first_with_their_totals()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);
        var first = await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1)]);
        var second = await Database.Sales.RegisterAsync(_cash, [new SaleItem(_milk, 2), new SaleItem(_rice, 1)]);

        var sales = await Database.Sales.GetRecentAsync(10);

        Assert.Equal([second.SaleId, first.SaleId], sales.Select(s => s.Id));
        Assert.Equal(3, sales[0].ItemCount);
        Assert.Equal(second.Total, sales[0].Total);
        Assert.Equal("Efectivo", sales[0].PaymentMethodName);
        Assert.Single(await Database.Sales.GetRecentAsync(1));
    }

    [Fact]
    public async Task Sold_products_and_used_payment_methods_cannot_be_deleted()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);
        await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1)]);

        await Assert.ThrowsAsync<DataConflictException>(() => Database.Products.DeleteAsync(_rice));
        await Assert.ThrowsAsync<DataConflictException>(() => Database.PaymentMethods.DeleteAsync(_cash));
        await Database.Products.DeleteAsync(_milk);
    }

    [Fact]
    public async Task Past_sales_keep_their_price_after_the_product_price_changes()
    {
        await SeedAsync(riceStock: 10, milkStock: 5);
        var receipt = await Database.Sales.RegisterAsync(_cash, [new SaleItem(_rice, 1)]);

        var rice = (await Database.Products.GetAllAsync()).Single(p => p.Id == _rice);
        rice.Price = 9999m;
        await Database.Products.UpdateAsync(rice);

        Assert.Equal(4800m, Assert.Single(await Database.Sales.GetLinesAsync(receipt.SaleId)).UnitPrice);
    }

    private async Task SeedAsync(int riceStock, int milkStock)
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        _cash = await Database.PaymentMethods.AddAsync(new PaymentMethod { Name = "Efectivo" });
        _rice = await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = riceStock, CategoryId = grains });
        _milk = await Database.Products.AddAsync(new Product { Name = "Leche", Price = 4200m, Stock = milkStock, CategoryId = grains });
    }

    private async Task<int> StockOfAsync(int productId) =>
        (await Database.Products.GetAllAsync()).Single(p => p.Id == productId).Stock;
}
