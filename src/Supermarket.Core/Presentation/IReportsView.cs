using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>Sales figures for a period chosen by the user.</summary>
public interface IReportsView : IOperationView
{
    event EventHandler? LoadRequested;

    event EventHandler? GenerateRequested;

    DateOnly FirstDay { get; }

    DateOnly LastDay { get; }

    /// <summary>Sets the period inputs, used to suggest a default period.</summary>
    void SetPeriod(DateOnly firstDay, DateOnly lastDay);

    void DisplaySummary(SalesSummary summary);

    void DisplaySales(IReadOnlyList<Sale> sales);

    void DisplayTopProducts(IReadOnlyList<ProductSales> products);

    void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods);
}
