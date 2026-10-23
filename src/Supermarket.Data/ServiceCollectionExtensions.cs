using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Data.Repositories;

namespace Supermarket.Data;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQL Server implementations of the repositories and the database initializer.
    /// </summary>
    public static IServiceCollection AddSqlServerData(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddSingleton(provider =>
            new DatabaseInitializer(connectionString, provider.GetRequiredService<ILogger<DatabaseInitializer>>()));

        services.AddTransient<IRepository<Category>, CategoryRepository>();
        services.AddTransient<IRepository<Provider>, ProviderRepository>();
        services.AddTransient<IRepository<PaymentMethod>, PaymentMethodRepository>();
        services.AddTransient<IRepository<Product>, ProductRepository>();
        services.AddTransient<ISaleRepository, SaleRepository>();
        services.AddTransient<IReportRepository, ReportRepository>();

        return services;
    }
}
