using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class StockEntryRepositoryTests(SqlServerFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Registering_an_entry_adds_stock_and_keeps_costs()
    {
        var ids = await SeedAsync();

        var entryId = await Database.StockEntries.RegisterAsync(
            ids.Provider, "Factura 123", [new StockEntryItem(ids.Rice, 20, 3500m), new StockEntryItem(ids.Milk, 6, 2900.50m)]);

        Assert.Equal(25, await StockOfAsync(ids.Rice));
        Assert.Equal(6, await StockOfAsync(ids.Milk));

        var entry = Assert.Single(await Database.StockEntries.GetRecentAsync(10));
        Assert.Equal(entryId, entry.Id);
        Assert.Equal("Distribuidora del Valle", entry.ProviderName);
        Assert.Equal("Factura 123", entry.Notes);
        Assert.Equal(26, entry.ItemCount);
        Assert.Equal((20 * 3500m) + (6 * 2900.50m), entry.TotalCost);

        var lines = await Database.StockEntries.GetLinesAsync(entryId);
        Assert.Equal(["Arroz", "Leche"], lines.Select(l => l.ProductName));
        Assert.Equal(2900.50m, lines[1].UnitCost);
    }

    [Fact]
    public async Task Provider_and_notes_are_optional()
    {
        var ids = await SeedAsync();

        await Database.StockEntries.RegisterAsync(null, null, [new StockEntryItem(ids.Rice, 1, 0m)]);

        var entry = Assert.Single(await Database.StockEntries.GetRecentAsync(10));
        Assert.Null(entry.ProviderName);
        Assert.Null(entry.Notes);
        Assert.Equal(0m, entry.TotalCost);
    }

    [Fact]
    public async Task Unknown_products_or_providers_save_nothing()
    {
        var ids = await SeedAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            Database.StockEntries.RegisterAsync(null, null, [new StockEntryItem(ids.Rice, 5, 1m), new StockEntryItem(99_999, 1, 1m)]));
        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.StockEntries.RegisterAsync(99_999, null, [new StockEntryItem(ids.Rice, 5, 1m)]));

        Assert.Equal(5, await StockOfAsync(ids.Rice));
        Assert.Empty(await Database.StockEntries.GetRecentAsync(10));
    }

    [Fact]
    public async Task Invalid_requests_are_rejected()
    {
        var ids = await SeedAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => Database.StockEntries.RegisterAsync(null, null, []));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Database.StockEntries.RegisterAsync(null, null, [new StockEntryItem(ids.Rice, 1, -1m)]));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Database.StockEntries.RegisterAsync(null, null, [new StockEntryItem(ids.Rice, 1, 10m), new StockEntryItem(ids.Rice, 1, 12m)]));
    }

    [Fact]
    public async Task Received_products_and_providers_with_entries_cannot_be_deleted()
    {
        var ids = await SeedAsync();
        await Database.StockEntries.RegisterAsync(ids.Provider, null, [new StockEntryItem(ids.Rice, 1, 1m)]);

        await Assert.ThrowsAsync<DataConflictException>(() => Database.Products.DeleteAsync(ids.Rice));
        await Assert.ThrowsAsync<DataConflictException>(() => Database.Providers.DeleteAsync(ids.Provider));
    }

    private async Task<(int Provider, int Rice, int Milk)> SeedAsync()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var provider = await Database.Providers.AddAsync(new Provider { Name = "Distribuidora del Valle" });
        var rice = await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = 5, CategoryId = grains });
        var milk = await Database.Products.AddAsync(new Product { Name = "Leche", Price = 4200m, Stock = 0, CategoryId = grains });
        return (provider, rice, milk);
    }

    private async Task<int> StockOfAsync(int productId) =>
        (await Database.Products.GetAllAsync()).Single(p => p.Id == productId).Stock;
}
