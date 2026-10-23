using Supermarket.Core.Domain;

namespace Supermarket.Core.Abstractions;

public interface ISaleRepository
{
    /// <summary>
    /// Registers a sale and discounts the stock of every product, all or nothing.
    /// </summary>
    /// <returns>The number of the new sale and its total, computed with the prices stored in the database.</returns>
    /// <exception cref="BusinessRuleException">A product does not have enough stock; nothing is saved.</exception>
    /// <exception cref="EntityNotFoundException">A product no longer exists; nothing is saved.</exception>
    /// <exception cref="DataConflictException">The payment method no longer exists; nothing is saved.</exception>
    Task<SaleReceipt> RegisterAsync(int paymentMethodId, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken = default);

    /// <summary>Most recent sales first.</summary>
    Task<IReadOnlyList<Sale>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleLine>> GetLinesAsync(int saleId, CancellationToken cancellationToken = default);
}
