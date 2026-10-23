using Supermarket.Core.Domain;

namespace Supermarket.Core.Abstractions;

/// <summary>Read-only sales figures. Voided sales are never counted.</summary>
public interface IReportRepository
{
    Task<SalesSummary> GetSalesSummaryAsync(DateRange period, CancellationToken cancellationToken = default);

    /// <summary>Best sellers by units, then by revenue.</summary>
    Task<IReadOnlyList<ProductSales>> GetTopProductsAsync(DateRange period, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every sale of the period, newest first. Unlike the other reports, voided sales are included
    /// (marked as such) so the list can be reconciled with the receipts that were printed.
    /// </summary>
    Task<IReadOnlyList<Sale>> GetSalesAsync(DateRange period, CancellationToken cancellationToken = default);

    /// <summary>Totals per payment method, highest first.</summary>
    Task<IReadOnlyList<PaymentMethodSales>> GetSalesByPaymentMethodAsync(DateRange period, CancellationToken cancellationToken = default);
}
