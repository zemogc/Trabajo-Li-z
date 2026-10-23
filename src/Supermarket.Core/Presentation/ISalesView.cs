using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>Checkout screen: builds a sale and shows the latest registered sales.</summary>
public interface ISalesView : IOperationView
{
    event EventHandler? LoadRequested;

    event EventHandler? AddItemRequested;

    event EventHandler? RemoveItemRequested;

    event EventHandler? ClearRequested;

    event EventHandler? RegisterRequested;

    /// <summary>Raised when a sale is selected in the history to see its lines.</summary>
    event EventHandler? SaleSelected;

    Product? SelectedProduct { get; }

    int Quantity { get; }

    int? SelectedCartProductId { get; }

    int? SelectedPaymentMethodId { get; }

    int? SelectedSaleId { get; }

    /// <summary>Products that can be sold, with their current price and stock.</summary>
    void DisplayProducts(IReadOnlyList<Product> products);

    void DisplayPaymentMethods(IReadOnlyList<PaymentMethod> paymentMethods);

    void DisplayCart(IReadOnlyList<SaleLine> lines, decimal total);

    /// <summary>Gets the product and quantity inputs ready for the next item.</summary>
    void ResetItemInput();

    void DisplayRecentSales(IReadOnlyList<Sale> sales);

    void DisplaySaleLines(IReadOnlyList<SaleLine> lines);
}
