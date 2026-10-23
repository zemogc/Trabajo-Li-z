using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.IntegrationTests;

public class DatabaseInitializerTests(SqlServerFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Initialization_is_idempotent_and_seeds_only_empty_tables()
    {
        await Database.Initializer.InitializeAsync(seedSampleData: true);
        var categories = await Database.Categories.GetAllAsync();
        var products = await Database.Products.GetAllAsync();

        await Database.Initializer.InitializeAsync(seedSampleData: true);

        Assert.Equal(4, categories.Count);
        Assert.Equal(6, products.Count);
        Assert.Equal(categories.Count, (await Database.Categories.GetAllAsync()).Count);
        Assert.Equal(products.Count, (await Database.Products.GetAllAsync()).Count);
        Assert.All(products, p => Assert.False(string.IsNullOrEmpty(p.CategoryName)));
        Assert.Equal(2, products.Count(p => p.IsLowStock));
        Assert.Equal(5, products.Count(p => p.ProviderName is not null));
    }

    [Fact]
    public async Task Upgrades_databases_created_with_the_first_schema_version()
    {
        var connectionString = new SqlConnectionStringBuilder(Database.ConnectionString)
        {
            InitialCatalog = $"Supermarket_{Guid.NewGuid():N}",
        }.ConnectionString;
        var initializer = new DatabaseInitializer(connectionString, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync(seedSampleData: true);

        // Recreate the v1 shape of the table, which had neither MinimumStock nor ProviderId.
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.ExecuteAsync(
                """
                DROP INDEX IX_Products_ProviderId ON dbo.Products;
                ALTER TABLE dbo.Products DROP CONSTRAINT CK_Products_MinimumStock, DF_Products_MinimumStock, FK_Products_Providers;
                ALTER TABLE dbo.Products DROP COLUMN MinimumStock, ProviderId;
                """);
        }

        await initializer.InitializeAsync(seedSampleData: false);

        await using var upgraded = new SqlConnection(connectionString);
        var rows = (await upgraded.QueryAsync<(int MinimumStock, int? ProviderId)>(
            "SELECT MinimumStock, ProviderId FROM dbo.Products;")).ToList();
        Assert.Equal(6, rows.Count);
        Assert.All(rows, row => Assert.Equal(Product.DefaultMinimumStock, row.MinimumStock));
        Assert.All(rows, row => Assert.Null(row.ProviderId));
    }

    [Fact]
    public async Task Creates_the_database_when_it_does_not_exist()
    {
        var connectionString = new SqlConnectionStringBuilder(Database.ConnectionString)
        {
            InitialCatalog = $"Supermarket_{Guid.NewGuid():N}",
        }.ConnectionString;

        await new DatabaseInitializer(connectionString, NullLogger<DatabaseInitializer>.Instance)
            .InitializeAsync(seedSampleData: false);

        await using var connection = new SqlConnection(connectionString);
        var tables = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE name IN ('Categories', 'Providers', 'PaymentMethods', 'Products');");
        Assert.Equal(4, tables);
    }

    [Fact]
    public async Task Connection_string_without_database_is_rejected()
    {
        var connectionString = new SqlConnectionStringBuilder(Database.ConnectionString) { InitialCatalog = string.Empty }.ConnectionString;
        var initializer = new DatabaseInitializer(connectionString, NullLogger<DatabaseInitializer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync(seedSampleData: false));
    }
}
