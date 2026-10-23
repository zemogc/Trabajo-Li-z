using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public sealed partial class SalesPresenter : PresenterBase
{
    internal const int RecentSalesCount = 50;
    internal const string ProductRequiredMessage = "Seleccione un producto.";
    internal const string LineRequiredMessage = "Seleccione en el carrito el producto que desea quitar.";
    internal const string EmptyCartMessage = "Agregue al menos un producto a la venta.";
    internal const string PaymentMethodRequiredMessage = "Seleccione el medio de pago.";
    internal const string ClearCartQuestion = "¿Desea vaciar la venta en curso?";

    private readonly ISalesView _view;
    private readonly ISaleRepository _sales;
    private readonly IRepository<Product> _products;
    private readonly IRepository<PaymentMethod> _paymentMethods;
    private readonly ILogger _logger;

    public SalesPresenter(
        ISalesView view,
        ISaleRepository sales,
        IRepository<Product> products,
        IRepository<PaymentMethod> paymentMethods,
        ILogger<SalesPresenter> logger)
        : base(view, logger)
    {
        ArgumentNullException.ThrowIfNull(sales);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(paymentMethods);

        _view = view;
        _sales = sales;
        _products = products;
        _paymentMethods = paymentMethods;
        _logger = logger;

        _view.LoadRequested += async (_, _) => await LoadAsync();
        _view.AddItemRequested += (_, _) => AddItem();
        _view.RemoveItemRequested += (_, _) => RemoveItem();
        _view.ClearRequested += (_, _) => ClearCart();
        _view.RegisterRequested += async (_, _) => await RegisterAsync();
        _view.SaleSelected += async (_, _) => await ShowSelectedSaleAsync();
    }

    public SaleDraft Draft { get; } = new();

    internal static string RegisteredMessage(int saleId, decimal total) =>
        $"Venta #{saleId} registrada. Total: {total:N2}";

    public Task LoadAsync() => RunAsync(async () =>
    {
        _view.DisplayPaymentMethods(await _paymentMethods.GetAllAsync());
        await RefreshProductsAndHistoryAsync();
        DisplayCart();
    });

    public void AddItem()
    {
        var product = _view.SelectedProduct;
        if (product is null)
        {
            _view.ShowMessage(ProductRequiredMessage);
            return;
        }

        try
        {
            Draft.Add(product, _view.Quantity);
        }
        catch (BusinessRuleException ex)
        {
            _view.ShowErrors([ex.Message]);
            return;
        }

        DisplayCart();
        _view.ResetItemInput();
    }

    public void RemoveItem()
    {
        if (_view.SelectedCartProductId is not { } productId)
        {
            _view.ShowMessage(LineRequiredMessage);
            return;
        }

        Draft.Remove(productId);
        DisplayCart();
    }

    public void ClearCart()
    {
        if (Draft.IsEmpty || !_view.Confirm(ClearCartQuestion))
        {
            return;
        }

        Draft.Clear();
        DisplayCart();
    }

    public Task RegisterAsync()
    {
        var errors = new List<string>();
        if (Draft.IsEmpty)
        {
            errors.Add(EmptyCartMessage);
        }

        if (_view.SelectedPaymentMethodId is not { } paymentMethodId)
        {
            errors.Add(PaymentMethodRequiredMessage);
            paymentMethodId = 0;
        }

        if (errors.Count > 0)
        {
            _view.ShowErrors(errors);
            return Task.CompletedTask;
        }

        return RunAsync(
            async () =>
            {
                var receipt = await _sales.RegisterAsync(paymentMethodId, Draft.ToItems());
                LogSaleRegistered(receipt.SaleId, receipt.Total);

                Draft.Clear();
                DisplayCart();
                await RefreshProductsAndHistoryAsync();
                _view.ShowMessage(RegisteredMessage(receipt.SaleId, receipt.Total));
            },
            onNotFound: RefreshProductsAndHistoryAsync);
    }

    public Task ShowSelectedSaleAsync()
    {
        if (_view.SelectedSaleId is not { } saleId)
        {
            _view.DisplaySaleLines([]);
            return Task.CompletedTask;
        }

        return RunAsync(async () => _view.DisplaySaleLines(await _sales.GetLinesAsync(saleId)));
    }

    private async Task RefreshProductsAndHistoryAsync()
    {
        var products = await _products.GetAllAsync();
        _view.DisplayProducts(products.Where(p => p.Stock > 0).ToList());
        _view.DisplayRecentSales(await _sales.GetRecentAsync(RecentSalesCount));
    }

    private void DisplayCart() => _view.DisplayCart(Draft.Lines, Draft.Total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sale {SaleId} registered for {Total}")]
    private partial void LogSaleRegistered(int saleId, decimal total);
}
