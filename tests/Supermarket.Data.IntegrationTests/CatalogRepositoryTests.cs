using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class CatalogRepositoryTests(SqlServerFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Supports_the_full_crud_cycle()
    {
        var repository = Database.Categories;

        var id = await repository.AddAsync(new Category { Name = "Panadería", Observation = "Pan fresco diario" });
        Assert.True(id > 0);

        await repository.UpdateAsync(new Category { Id = id, Name = "Panadería y repostería", Observation = null });
        var updated = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(id, updated.Id);
        Assert.Equal("Panadería y repostería", updated.Name);
        Assert.Null(updated.Observation);

        await repository.DeleteAsync(id);
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task Lists_are_sorted_by_name()
    {
        foreach (var name in new[] { "Transferencia", "Efectivo", "Tarjeta" })
        {
            await Database.PaymentMethods.AddAsync(new PaymentMethod { Name = name });
        }

        var names = (await Database.PaymentMethods.GetAllAsync()).Select(p => p.Name);

        Assert.Equal(["Efectivo", "Tarjeta", "Transferencia"], names);
    }

    [Fact]
    public async Task Search_matches_by_id_or_partial_name()
    {
        var repository = Database.Providers;
        var andinaId = await repository.AddAsync(new Provider { Name = "Comercializadora Andina" });
        await repository.AddAsync(new Provider { Name = "Distribuidora del Valle" });

        Assert.Equal(andinaId, Assert.Single(await repository.SearchAsync("andina")).Id);
        Assert.Equal("Comercializadora Andina", Assert.Single(await repository.SearchAsync($" {andinaId} ")).Name);
        Assert.Equal(2, (await repository.SearchAsync("dora")).Count);
        Assert.Empty(await repository.SearchAsync("inexistente"));
    }

    [Fact]
    public async Task Search_treats_like_wildcards_as_literal_text()
    {
        var repository = Database.Categories;
        await repository.AddAsync(new Category { Name = "Descuento 50%" });
        await repository.AddAsync(new Category { Name = "Descuento 500" });
        await repository.AddAsync(new Category { Name = "Aseo_hogar" });
        await repository.AddAsync(new Category { Name = "Aseo personal" });

        Assert.Equal("Descuento 50%", Assert.Single(await repository.SearchAsync("50%")).Name);
        Assert.Equal("Aseo_hogar", Assert.Single(await repository.SearchAsync("o_h")).Name);
    }

    [Fact]
    public async Task Duplicated_names_are_reported_as_conflicts()
    {
        await Database.Categories.AddAsync(new Category { Name = "Bebidas" });

        var error = await Assert.ThrowsAsync<DataConflictException>(
            () => Database.Categories.AddAsync(new Category { Name = "Bebidas" }));

        Assert.Contains("Bebidas", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Updating_or_deleting_a_missing_row_throws_not_found()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Database.Providers.UpdateAsync(new Provider { Id = 12345, Name = "Nadie" }));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => Database.Providers.DeleteAsync(12345));
    }

    [Fact]
    public async Task Categories_in_use_cannot_be_deleted()
    {
        var categoryId = await Database.Categories.AddAsync(new Category { Name = "Granos" });
        await Database.Products.AddAsync(new Product { Name = "Arroz 1 kg", Price = 4800m, Stock = 10, CategoryId = categoryId });

        await Assert.ThrowsAsync<DataConflictException>(() => Database.Categories.DeleteAsync(categoryId));
        Assert.Single(await Database.Categories.GetAllAsync());
    }
}
