using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>Home screen with the figures a store manager checks first.</summary>
public interface IDashboardView : IOperationView
{
    event EventHandler? LoadRequested;

    void DisplayToday(SalesSummary today, int productsToRestock);

    void DisplayTopProducts(IReadOnlyList<ProductSales> products);

    void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods);

    /// <summary>Products at or below their minimum stock, most urgent first.</summary>
    void DisplayProductsToRestock(IReadOnlyList<Product> products);
}
