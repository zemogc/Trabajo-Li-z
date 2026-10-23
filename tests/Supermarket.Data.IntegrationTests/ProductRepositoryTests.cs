using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class ProductRepositoryTests(SqlServerFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Products_are_returned_with_their_category_name()
    {
        var dairy = await Database.Categories.AddAsync(new Category { Name = "Lácteos" });
        var id = await Database.Products.AddAsync(new Product { Name = "Leche 1 L", Price = 4200.50m, Stock = 48, MinimumStock = 12, CategoryId = dairy });

        var product = Assert.Single(await Database.Products.GetAllAsync());

        Assert.Equal(id, product.Id);
        Assert.Equal(4200.50m, product.Price);
        Assert.Equal(48, product.Stock);
        Assert.Equal(12, product.MinimumStock);
        Assert.Equal(dairy, product.CategoryId);
        Assert.Equal("Lácteos", product.CategoryName);
    }

    [Fact]
    public async Task Update_changes_every_column()
    {
        var dairy = await Database.Categories.AddAsync(new Category { Name = "Lácteos" });
        var drinks = await Database.Categories.AddAsync(new Category { Name = "Bebidas" });
        var id = await Database.Products.AddAsync(new Product { Name = "Kumis", Price = 3000m, Stock = 5, CategoryId = dairy });

        await Database.Products.UpdateAsync(new Product { Id = id, Name = "Kumis 1 L", Price = 6500m, Stock = 12, MinimumStock = 3, CategoryId = drinks });

        var product = Assert.Single(await Database.Products.GetAllAsync());
        Assert.Equal("Kumis 1 L", product.Name);
        Assert.Equal(6500m, product.Price);
        Assert.Equal(12, product.Stock);
        Assert.Equal(3, product.MinimumStock);
        Assert.Equal("Bebidas", product.CategoryName);
    }

    [Fact]
    public async Task Search_also_matches_the_category_name()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var cleaning = await Database.Categories.AddAsync(new Category { Name = "Aseo" });
        await Database.Products.AddAsync(new Product { Name = "Lenteja", Price = 3900m, Stock = 1, CategoryId = grains });
        await Database.Products.AddAsync(new Product { Name = "Jabón", Price = 5600m, Stock = 1, CategoryId = cleaning });

        Assert.Equal("Lenteja", Assert.Single(await Database.Products.SearchAsync("granos")).Name);
    }

    [Fact]
    public async Task Unknown_category_is_rejected_as_a_conflict()
    {
        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.Products.AddAsync(new Product { Name = "Huérfano", Price = 1m, Stock = 0, CategoryId = 999 }));
    }

    [Fact]
    public async Task Database_checks_guard_price_and_stock()
    {
        var category = await Database.Categories.AddAsync(new Category { Name = "Granos" });

        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.Products.AddAsync(new Product { Name = "Gratis", Price = 0m, Stock = 1, CategoryId = category }));
        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.Products.AddAsync(new Product { Name = "Negativo", Price = 1m, Stock = -1, CategoryId = category }));
        await Assert.ThrowsAsync<DataConflictException>(() =>
            Database.Products.AddAsync(new Product { Name = "Mínimo negativo", Price = 1m, Stock = 1, MinimumStock = -1, CategoryId = category }));
    }

    [Fact]
    public async Task Deleting_a_missing_product_throws_not_found() =>
        await Assert.ThrowsAsync<EntityNotFoundException>(() => Database.Products.DeleteAsync(4242));

    [Fact]
    public async Task Provider_is_optional_and_returned_by_name()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var valle = await Database.Providers.AddAsync(new Provider { Name = "Distribuidora del Valle" });
        await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = 10, CategoryId = grains, ProviderId = valle });
        await Database.Products.AddAsync(new Product { Name = "Sal", Price = 1500m, Stock = 10, CategoryId = grains });

        var products = await Database.Products.GetAllAsync();

        Assert.Equal("Distribuidora del Valle", products.Single(p => p.Name == "Arroz").ProviderName);
        Assert.Null(products.Single(p => p.Name == "Sal").ProviderId);
        Assert.Equal("Arroz", Assert.Single(await Database.Products.SearchAsync("valle")).Name);
    }

    [Fact]
    public async Task Providers_in_use_cannot_be_deleted()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var valle = await Database.Providers.AddAsync(new Provider { Name = "Distribuidora del Valle" });
        await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = 10, CategoryId = grains, ProviderId = valle });

        await Assert.ThrowsAsync<DataConflictException>(() => Database.Providers.DeleteAsync(valle));
    }

    [Fact]
    public async Task Removing_the_provider_from_a_product_releases_it()
    {
        var grains = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        var valle = await Database.Providers.AddAsync(new Provider { Name = "Distribuidora del Valle" });
        var id = await Database.Products.AddAsync(new Product { Name = "Arroz", Price = 4800m, Stock = 10, CategoryId = grains, ProviderId = valle });

        await Database.Products.UpdateAsync(new Product { Id = id, Name = "Arroz", Price = 4800m, Stock = 10, CategoryId = grains, ProviderId = null });

        var product = Assert.Single(await Database.Products.GetAllAsync());
        Assert.Null(product.ProviderId);
        Assert.Null(product.ProviderName);
        await Database.Providers.DeleteAsync(valle);
    }
}
