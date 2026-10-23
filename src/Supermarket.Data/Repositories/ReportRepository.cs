using Dapper;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.Repositories;

internal sealed class ReportRepository(IDbConnectionFactory connections) : IReportRepository
{
    // Every report only counts valid sales inside the half-open period.
    private const string ValidSalesInPeriod = "s.CreatedAt >= @FromUtc AND s.CreatedAt < @ToUtc AND s.CancelledAt IS NULL";

    public async Task<SalesSummary> GetSalesSummaryAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleAsync<SalesSummary>(new CommandDefinition(
            $"""
            SELECT COUNT(*) AS SalesCount,
                   COALESCE(SUM(s.Total), 0) AS Revenue,
                   COALESCE((SELECT SUM(l.Quantity)
                             FROM dbo.SaleLines AS l
                             INNER JOIN dbo.Sales AS s ON s.Id = l.SaleId
                             WHERE {ValidSalesInPeriod}), 0) AS UnitsSold
            FROM dbo.Sales AS s
            WHERE {ValidSalesInPeriod};
            """,
            new { period.FromUtc, period.ToUtc },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductSales>> GetTopProductsAsync(DateRange period, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var products = await connection.QueryAsync<ProductSales>(new CommandDefinition(
            $"""
            SELECT TOP (@count)
                   p.Id AS ProductId, p.Name AS ProductName,
                   SUM(l.Quantity) AS Units, SUM(l.Quantity * l.UnitPrice) AS Revenue
            FROM dbo.SaleLines AS l
            INNER JOIN dbo.Sales AS s ON s.Id = l.SaleId
            INNER JOIN dbo.Products AS p ON p.Id = l.ProductId
            WHERE {ValidSalesInPeriod}
            GROUP BY p.Id, p.Name
            ORDER BY Units DESC, Revenue DESC, p.Name;
            """,
            new { period.FromUtc, period.ToUtc, count },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return products.AsList();
    }

    public async Task<IReadOnlyList<PaymentMethodSales>> GetSalesByPaymentMethodAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var methods = await connection.QueryAsync<PaymentMethodSales>(new CommandDefinition(
            $"""
            SELECT pm.Name AS PaymentMethodName, COUNT(*) AS SalesCount, SUM(s.Total) AS Total
            FROM dbo.Sales AS s
            INNER JOIN dbo.PaymentMethods AS pm ON pm.Id = s.PaymentMethodId
            WHERE {ValidSalesInPeriod}
            GROUP BY pm.Name
            ORDER BY Total DESC, pm.Name;
            """,
            new { period.FromUtc, period.ToUtc },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return methods.AsList();
    }
}
