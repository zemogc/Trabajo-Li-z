using System.Globalization;
using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Receipts;

namespace Supermarket.Core.Presentation;

public sealed partial class SalesPresenter : PresenterBase
{
    internal const int RecentSalesCount = 50;
    internal const string ProductRequiredMessage = "Seleccione un producto.";
    internal const string LineRequiredMessage = "Seleccione en el carrito el producto que desea quitar.";
    internal const string EmptyCartMessage = "Agregue al menos un producto a la venta.";
    internal const string PaymentMethodRequiredMessage = "Seleccione el medio de pago.";
    internal const string ClearCartQuestion = "¿Desea vaciar la venta en curso?";
    internal const string SaleRequiredMessage = "Seleccione en el historial la venta que desea anular.";
    internal const string ReceiptSaleRequiredMessage = "Seleccione en el historial la venta cuyo recibo desea ver.";

    private readonly ISalesView _view;
    private readonly ISaleRepository _sales;
    private readonly IRepository<Product> _products;
    private readonly IRepository<PaymentMethod> _paymentMethods;
    private readonly StoreInfo _store;
    private readonly ILogger _logger;
    private IReadOnlyList<Sale> _recentSales = [];

    public SalesPresenter(
        ISalesView view,
        ISaleRepository sales,
        IRepository<Product> products,
        IRepository<PaymentMethod> paymentMethods,
        StoreInfo store,
        ILogger<SalesPresenter> logger)
        : base(view, logger)
    {
        ArgumentNullException.ThrowIfNull(sales);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(paymentMethods);
        ArgumentNullException.ThrowIfNull(store);

        _view = view;
        _sales = sales;
        _products = products;
        _paymentMethods = paymentMethods;
        _store = store;
        _logger = logger;

        _view.LoadRequested += async (_, _) => await LoadAsync();
        _view.AddItemRequested += (_, _) => AddItem();
        _view.RemoveItemRequested += (_, _) => RemoveItem();
        _view.ClearRequested += (_, _) => ClearCart();
        _view.RegisterRequested += async (_, _) => await RegisterAsync();
        _view.SaleSelected += async (_, _) => await ShowSelectedSaleAsync();
        _view.CancelSaleRequested += async (_, _) => await CancelSelectedSaleAsync();
        _view.PrintReceiptRequested += async (_, _) => await ShowSelectedReceiptAsync();
    }

    public SaleDraft Draft { get; } = new();

    internal static string RegisteredMessage(int saleId, decimal total) =>
        $"Venta #{saleId} registrada. Total: {total:N2}";

    internal static string AlreadyCancelledMessage(int saleId) => $"La venta #{saleId} ya está anulada.";

    internal static string CancelQuestion(Sale sale) =>
        $"¿Anular la venta #{sale.Id} por {sale.Total:N2}? Las unidades vendidas volverán al inventario.";

    internal static string CancelledMessage(int saleId) => $"Venta #{saleId} anulada. El stock fue restablecido.";

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

    public Task CancelSelectedSaleAsync()
    {
        var sale = _view.SelectedSaleId is { } saleId ? _recentSales.FirstOrDefault(s => s.Id == saleId) : null;
        if (sale is null)
        {
            _view.ShowMessage(SaleRequiredMessage);
            return Task.CompletedTask;
        }

        if (sale.IsCancelled)
        {
            _view.ShowMessage(AlreadyCancelledMessage(sale.Id));
            return Task.CompletedTask;
        }

        if (!_view.Confirm(CancelQuestion(sale)))
        {
            return Task.CompletedTask;
        }

        return RunAsync(
            async () =>
            {
                await _sales.CancelAsync(sale.Id);
                LogSaleCancelled(sale.Id);
                await RefreshProductsAndHistoryAsync();
                _view.ShowMessage(CancelledMessage(sale.Id));
            },
            onNotFound: RefreshProductsAndHistoryAsync);
    }

    public Task ShowSelectedReceiptAsync()
    {
        var sale = _view.SelectedSaleId is { } saleId ? _recentSales.FirstOrDefault(s => s.Id == saleId) : null;
        if (sale is null)
        {
            _view.ShowMessage(ReceiptSaleRequiredMessage);
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            var lines = await _sales.GetLinesAsync(sale.Id);
            var receipt = ReceiptFormatter.Format(sale, lines, _store, CultureInfo.CurrentCulture);
            _view.ShowReceipt($"Recibo de la venta #{sale.Id}", receipt);
        });
    }

    private async Task RefreshProductsAndHistoryAsync()
    {
        var products = await _products.GetAllAsync();
        _view.DisplayProducts(products.Where(p => p.Stock > 0).ToList());

        _recentSales = await _sales.GetRecentAsync(RecentSalesCount);
        _view.DisplayRecentSales(_recentSales);
    }

    private void DisplayCart() => _view.DisplayCart(Draft.Lines, Draft.Total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sale {SaleId} registered for {Total}")]
    private partial void LogSaleRegistered(int saleId, decimal total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sale {SaleId} cancelled")]
    private partial void LogSaleCancelled(int saleId);
}
