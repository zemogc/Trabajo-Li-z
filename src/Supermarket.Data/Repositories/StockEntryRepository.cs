using Dapper;
using Microsoft.Data.SqlClient;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.Repositories;

internal sealed class StockEntryRepository(IDbConnectionFactory connections) : IStockEntryRepository
{
    public async Task<int> RegisterAsync(int? providerId, string? notes, IReadOnlyList<StockEntryItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var lines = Merge(items);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        // Leaving this block without committing (any exception) rolls the whole entry back.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Same lock order as sales (by product id), so entries and sales cannot deadlock each other.
            foreach (var line in lines)
            {
                var updated = await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Products SET Stock = Stock + @Quantity WHERE Id = @ProductId;",
                    new { line.ProductId, line.Quantity },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

                if (updated == 0)
                {
                    throw EntityNotFoundException.For<Product>(line.ProductId);
                }
            }

            var entryId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "INSERT INTO dbo.StockEntries (ProviderId, Notes) OUTPUT INSERTED.Id VALUES (@providerId, @notes);",
                new { providerId, notes },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO dbo.StockEntryLines (EntryId, ProductId, Quantity, UnitCost) VALUES (@EntryId, @ProductId, @Quantity, @UnitCost);",
                lines.Select(l => new { EntryId = entryId, l.ProductId, l.Quantity, l.UnitCost }),
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return entryId;
        }
        catch (SqlException ex) when (ex.Number == SqlErrors.ConstraintViolation)
        {
            // Products were checked above, so the remaining foreign key is the provider.
            throw new DataConflictException("El proveedor seleccionado ya no existe. Elija otro e intente de nuevo.", ex);
        }
    }

    public async Task<IReadOnlyList<StockEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var entries = await connection.QueryAsync<StockEntry>(new CommandDefinition(
            """
            SELECT TOP (@count)
                   e.Id, e.CreatedAt, e.ProviderId, pr.Name AS ProviderName, e.Notes,
                   totals.ItemCount, totals.TotalCost
            FROM dbo.StockEntries AS e
            LEFT JOIN dbo.Providers AS pr ON pr.Id = e.ProviderId
            CROSS APPLY (SELECT SUM(l.Quantity) AS ItemCount, SUM(l.Quantity * l.UnitCost) AS TotalCost
                         FROM dbo.StockEntryLines AS l
                         WHERE l.EntryId = e.Id) AS totals
            ORDER BY e.CreatedAt DESC, e.Id DESC;
            """,
            new { count },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return entries.AsList();
    }

    public async Task<IReadOnlyList<StockEntryLine>> GetLinesAsync(int entryId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var lines = await connection.QueryAsync<StockEntryLine>(new CommandDefinition(
            """
            SELECT l.ProductId, p.Name AS ProductName, l.Quantity, l.UnitCost
            FROM dbo.StockEntryLines AS l
            INNER JOIN dbo.Products AS p ON p.Id = l.ProductId
            WHERE l.EntryId = @entryId
            ORDER BY p.Name;
            """,
            new { entryId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return lines.AsList();
    }

    /// <summary>Validates the request and combines repeated products, which must share the same cost.</summary>
    private static List<StockEntryItem> Merge(IReadOnlyList<StockEntryItem> items)
    {
        if (items.Count == 0)
        {
            throw new BusinessRuleException("La entrada debe tener al menos un producto.");
        }

        if (items.Any(i => i.Quantity <= 0))
        {
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
        }

        if (items.Any(i => i.UnitCost < 0))
        {
            throw new BusinessRuleException("El costo unitario no puede ser negativo.");
        }

        var merged = new List<StockEntryItem>();
        foreach (var group in items.GroupBy(i => i.ProductId).OrderBy(g => g.Key))
        {
            if (group.Select(i => i.UnitCost).Distinct().Count() > 1)
            {
                throw new BusinessRuleException("Un mismo producto no puede ingresar con dos costos distintos en la misma entrada.");
            }

            merged.Add(new StockEntryItem(group.Key, group.Sum(i => i.Quantity), group.First().UnitCost));
        }

        return merged;
    }
}
