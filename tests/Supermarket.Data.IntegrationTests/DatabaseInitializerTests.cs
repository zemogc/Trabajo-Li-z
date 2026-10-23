using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

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
