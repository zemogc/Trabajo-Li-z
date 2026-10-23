using Dapper;
using Microsoft.Data.SqlClient;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.Repositories;

internal sealed class SaleRepository(IDbConnectionFactory connections) : ISaleRepository
{
    public async Task<SaleReceipt> RegisterAsync(int paymentMethodId, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var requested = Merge(items);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        // Leaving this block without committing (any exception) rolls the whole sale back.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var lines = new List<(int ProductId, int Quantity, decimal UnitPrice)>(requested.Count);

            // Products are updated in id order so concurrent sales lock rows in the same order
            // and cannot deadlock each other.
            foreach (var item in requested)
            {
                // The WHERE clause makes the stock check and the discount a single atomic step:
                // two checkouts can never sell the same last unit.
                var unitPrice = await connection.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
                    """
                    UPDATE dbo.Products
                    SET Stock = Stock - @Quantity
                    OUTPUT INSERTED.Price
                    WHERE Id = @ProductId AND Stock >= @Quantity;
                    """,
                    new { item.ProductId, item.Quantity },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

                if (unitPrice is null)
                {
                    throw await ExplainRejectedItemAsync(connection, transaction, item, cancellationToken).ConfigureAwait(false);
                }

                lines.Add((item.ProductId, item.Quantity, unitPrice.Value));
            }

            var total = lines.Sum(l => l.Quantity * l.UnitPrice);
            var saleId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "INSERT INTO dbo.Sales (PaymentMethodId, Total) OUTPUT INSERTED.Id VALUES (@paymentMethodId, @total);",
                new { paymentMethodId, total },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO dbo.SaleLines (SaleId, ProductId, Quantity, UnitPrice) VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice);",
                lines.Select(l => new { SaleId = saleId, l.ProductId, l.Quantity, l.UnitPrice }),
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new SaleReceipt(saleId, total);
        }
        catch (SqlException ex) when (ex.Number == SqlErrors.ConstraintViolation)
        {
            // Only foreign key left unchecked above: the payment method was deleted meanwhile.
            throw new DataConflictException("El medio de pago seleccionado ya no existe. Elija otro e intente de nuevo.", ex);
        }
    }

    public async Task<IReadOnlyList<Sale>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sales = await connection.QueryAsync<Sale>(new CommandDefinition(
            """
            SELECT TOP (@count)
                   s.Id, s.CreatedAt, s.PaymentMethodId, pm.Name AS PaymentMethodName, s.Total,
                   (SELECT SUM(l.Quantity) FROM dbo.SaleLines AS l WHERE l.SaleId = s.Id) AS ItemCount
            FROM dbo.Sales AS s
            INNER JOIN dbo.PaymentMethods AS pm ON pm.Id = s.PaymentMethodId
            ORDER BY s.CreatedAt DESC, s.Id DESC;
            """,
            new { count },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return sales.AsList();
    }

    public async Task<IReadOnlyList<SaleLine>> GetLinesAsync(int saleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var lines = await connection.QueryAsync<SaleLine>(new CommandDefinition(
            """
            SELECT l.ProductId, p.Name AS ProductName, l.Quantity, l.UnitPrice
            FROM dbo.SaleLines AS l
            INNER JOIN dbo.Products AS p ON p.Id = l.ProductId
            WHERE l.SaleId = @saleId
            ORDER BY p.Name;
            """,
            new { saleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return lines.AsList();
    }

    /// <summary>Validates the request and combines repeated products into a single line.</summary>
    private static List<SaleItem> Merge(IReadOnlyList<SaleItem> items)
    {
        if (items.Count == 0)
        {
            throw new BusinessRuleException("La venta debe tener al menos un producto.");
        }

        if (items.Any(i => i.Quantity <= 0))
        {
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
        }

        return items
            .GroupBy(i => i.ProductId)
            .Select(g => new SaleItem(g.Key, g.Sum(i => i.Quantity)))
            .OrderBy(i => i.ProductId)
            .ToList();
    }

    private static async Task<Exception> ExplainRejectedItemAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        SaleItem item,
        CancellationToken cancellationToken)
    {
        var product = await connection.QuerySingleOrDefaultAsync<StockRow>(new CommandDefinition(
            "SELECT Name, Stock FROM dbo.Products WHERE Id = @ProductId;",
            new { item.ProductId },
            transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return product is null
            ? EntityNotFoundException.For<Product>(item.ProductId)
            : new BusinessRuleException(
                $"Stock insuficiente para \"{product.Name}\": disponible {product.Stock}, solicitado {item.Quantity}.");
    }

    private sealed class StockRow
    {
        public string Name { get; set; } = string.Empty;

        public int Stock { get; set; }
    }
}
