using Supermarket.Core.Domain;

namespace Supermarket.Core.Abstractions;

public interface IStockEntryRepository
{
    /// <summary>Registers received merchandise and adds it to stock, all or nothing.</summary>
    /// <returns>The number of the new entry.</returns>
    /// <exception cref="BusinessRuleException">The entry is empty or has invalid quantities or costs; nothing is saved.</exception>
    /// <exception cref="EntityNotFoundException">A product no longer exists; nothing is saved.</exception>
    /// <exception cref="DataConflictException">The provider no longer exists; nothing is saved.</exception>
    Task<int> RegisterAsync(int? providerId, string? notes, IReadOnlyList<StockEntryItem> items, CancellationToken cancellationToken = default);

    /// <summary>Most recent entries first.</summary>
    Task<IReadOnlyList<StockEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockEntryLine>> GetLinesAsync(int entryId, CancellationToken cancellationToken = default);
}
