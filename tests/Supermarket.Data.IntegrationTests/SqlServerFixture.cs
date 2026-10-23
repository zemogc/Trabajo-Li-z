using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Testcontainers.MsSql;

namespace Supermarket.Data.IntegrationTests;

/// <summary>
/// Starts a disposable SQL Server container once for the whole test run and
/// creates the application database with the same initializer the app uses.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Same major version as production; the tag tracks the latest SQL Server 2022 cumulative update.
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private ServiceProvider? _services;

    public string ConnectionString { get; private set; } = string.Empty;

    public IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public IRepository<Category> Categories => Services.GetRequiredService<IRepository<Category>>();

    public IRepository<Provider> Providers => Services.GetRequiredService<IRepository<Provider>>();

    public IRepository<PaymentMethod> PaymentMethods => Services.GetRequiredService<IRepository<PaymentMethod>>();

    public IRepository<Product> Products => Services.GetRequiredService<IRepository<Product>>();

    public ISaleRepository Sales => Services.GetRequiredService<ISaleRepository>();

    public DatabaseInitializer Initializer => Services.GetRequiredService<DatabaseInitializer>();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "SupermarketTests",
        }.ConnectionString;

        _services = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSqlServerData(ConnectionString)
            .BuildServiceProvider();

        await Initializer.InitializeAsync(seedSampleData: false);
    }

    /// <summary>Empties every table so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            """
            DELETE FROM dbo.SaleLines;
            DELETE FROM dbo.Sales;
            DELETE FROM dbo.Products;
            DELETE FROM dbo.Categories;
            DELETE FROM dbo.Providers;
            DELETE FROM dbo.PaymentMethods;
            """);
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}

/// <summary>Base class that resets the database before every test.</summary>
[Collection(SqlServerTestGroup.Name)]
public abstract class DatabaseTest(SqlServerFixture database) : IAsyncLifetime
{
    protected SqlServerFixture Database { get; } = database;

    public Task InitializeAsync() => Database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
