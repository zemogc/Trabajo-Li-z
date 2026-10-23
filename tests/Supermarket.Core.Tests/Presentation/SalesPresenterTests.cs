using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class SalesPresenterTests
{
    private readonly FakeSalesView _view = new();
    private readonly InMemoryRepository<Product> _products = new InMemoryRepository<Product>().With(
        new Product { Name = "Arroz", Price = 4800m, Stock = 10, CategoryId = 1 },
        new Product { Name = "Leche", Price = 4200m, Stock = 2, CategoryId = 1 },
        new Product { Name = "Agotado", Price = 1000m, Stock = 0, CategoryId = 1 });

    private readonly InMemoryRepository<PaymentMethod> _paymentMethods = new InMemoryRepository<PaymentMethod>()
        .With(new PaymentMethod { Name = "Efectivo" });

    private readonly InMemorySaleRepository _sales;
    private readonly SalesPresenter _presenter;

    public SalesPresenterTests()
    {
        _sales = new InMemorySaleRepository(_products);
        _presenter = new SalesPresenter(_view, _sales, _products, _paymentMethods, NullLogger<SalesPresenter>.Instance);
    }

    private Product Rice => _products.Items[0];

    private Product Milk => _products.Items[1];

    [Fact]
    public async Task Load_shows_products_in_stock_payment_methods_and_history()
    {
        await _presenter.LoadAsync();

        Assert.Equal(["Arroz", "Leche"], _view.Products.Select(p => p.Name));
        Assert.Single(_view.PaymentMethods);
        Assert.Empty(_view.RecentSales);
        Assert.Empty(_view.Cart);
    }

    [Fact]
    public void Adding_an_item_updates_the_cart_and_resets_the_input()
    {
        _view.SelectedProduct = Rice;
        _view.Quantity = 3;

        _presenter.AddItem();

        Assert.Equal(3, Assert.Single(_view.Cart).Quantity);
        Assert.Equal(14400m, _view.CartTotal);
        Assert.Equal(1, _view.ItemInputResets);
    }

    [Fact]
    public void Adding_without_product_asks_for_one()
    {
        _presenter.AddItem();

        Assert.Equal([SalesPresenter.ProductRequiredMessage], _view.Messages);
        Assert.Empty(_view.Cart);
    }

    [Fact]
    public void Adding_more_than_the_stock_shows_the_rule()
    {
        _view.SelectedProduct = Milk;
        _view.Quantity = 5;

        _presenter.AddItem();

        Assert.Equal(["Stock insuficiente para \"Leche\": disponible 2, solicitado 5."], _view.Errors);
        Assert.Equal(0, _view.ItemInputResets);
    }

    [Fact]
    public void Removing_needs_a_selected_line()
    {
        _view.SelectedProduct = Rice;
        _presenter.AddItem();

        _presenter.RemoveItem();
        Assert.Equal([SalesPresenter.LineRequiredMessage], _view.Messages);

        _view.SelectedCartProductId = Rice.Id;
        _presenter.RemoveItem();
        Assert.Empty(_view.Cart);
    }

    [Fact]
    public void Clearing_asks_for_confirmation()
    {
        _view.SelectedProduct = Rice;
        _presenter.AddItem();

        _view.ConfirmAnswer = false;
        _presenter.ClearCart();
        Assert.Single(_view.Cart);

        _view.ConfirmAnswer = true;
        _presenter.ClearCart();
        Assert.Empty(_view.Cart);
    }

    [Fact]
    public async Task Registering_requires_items_and_payment_method()
    {
        await _presenter.RegisterAsync();

        Assert.Equal([SalesPresenter.EmptyCartMessage, SalesPresenter.PaymentMethodRequiredMessage], _view.Errors);
        Assert.Empty(_view.BusyChanges);
    }

    [Fact]
    public async Task Registering_a_sale_clears_the_cart_and_refreshes_stock_and_history()
    {
        _view.SelectedProduct = Rice;
        _view.Quantity = 2;
        _presenter.AddItem();
        _view.SelectedPaymentMethodId = 1;

        await _presenter.RegisterAsync();

        Assert.Equal([new SaleItem(Rice.Id, 2)], _sales.LastItems);
        Assert.True(_presenter.Draft.IsEmpty);
        Assert.Empty(_view.Cart);
        Assert.Equal(8, _view.Products.Single(p => p.Name == "Arroz").Stock);
        Assert.Single(_view.RecentSales);
        Assert.Equal(SalesPresenter.RegisteredMessage(1, 9600m), _view.Messages[^1]);
        Assert.Equal([true, false], _view.BusyChanges);
    }

    [Fact]
    public async Task Stock_sold_elsewhere_keeps_the_cart_and_shows_why()
    {
        _view.SelectedProduct = Milk;
        _view.Quantity = 2;
        _presenter.AddItem();
        _view.SelectedPaymentMethodId = 1;
        _sales.NextFailure = new BusinessRuleException("Stock insuficiente para \"Leche\": disponible 1, solicitado 2.");

        await _presenter.RegisterAsync();

        Assert.Equal(["Stock insuficiente para \"Leche\": disponible 1, solicitado 2."], _view.Errors);
        Assert.False(_presenter.Draft.IsEmpty);
        Assert.Single(_view.Cart);
    }

    [Fact]
    public async Task Selecting_a_sale_shows_its_lines()
    {
        _view.SelectedProduct = Rice;
        _presenter.AddItem();
        _view.SelectedPaymentMethodId = 1;
        await _presenter.RegisterAsync();

        _view.SelectedSaleId = 1;
        await _presenter.ShowSelectedSaleAsync();
        Assert.Equal("Arroz", Assert.Single(_view.SaleLines!).ProductName);

        _view.SelectedSaleId = null;
        await _presenter.ShowSelectedSaleAsync();
        Assert.Empty(_view.SaleLines!);
    }

    [Fact]
    public async Task View_events_are_routed_to_the_presenter()
    {
        _view.RaiseLoad();
        await Task.Yield();
        Assert.Equal(2, _view.Products.Count);

        _view.SelectedProduct = Rice;
        _view.RaiseAddItem();
        Assert.Single(_view.Cart);

        _view.SelectedCartProductId = Rice.Id;
        _view.RaiseRemoveItem();
        Assert.Empty(_view.Cart);
    }

    [Fact]
    public async Task Cancelling_a_sale_asks_first_and_restores_stock()
    {
        await RegisterRiceSaleAsync(quantity: 4);
        _view.SelectedSaleId = 1;

        await _presenter.CancelSelectedSaleAsync();

        Assert.Contains("#1", Assert.Single(_view.Questions), StringComparison.Ordinal);
        Assert.Equal(10, Rice.Stock);
        Assert.True(Assert.Single(_view.RecentSales).IsCancelled);
        Assert.Equal(SalesPresenter.CancelledMessage(1), _view.Messages[^1]);
    }

    [Fact]
    public async Task Declining_the_confirmation_keeps_the_sale()
    {
        await RegisterRiceSaleAsync(quantity: 4);
        _view.SelectedSaleId = 1;
        _view.ConfirmAnswer = false;

        await _presenter.CancelSelectedSaleAsync();

        Assert.Equal(6, Rice.Stock);
        Assert.False(Assert.Single(_view.RecentSales).IsCancelled);
    }

    [Fact]
    public async Task Cancelled_sales_are_not_cancelled_again()
    {
        await RegisterRiceSaleAsync(quantity: 4);
        _view.SelectedSaleId = 1;
        await _presenter.CancelSelectedSaleAsync();
        _view.Questions.Clear();

        await _presenter.CancelSelectedSaleAsync();

        Assert.Empty(_view.Questions);
        Assert.Equal(SalesPresenter.AlreadyCancelledMessage(1), _view.Messages[^1]);
        Assert.Equal(10, Rice.Stock);
    }

    [Fact]
    public async Task Cancelling_needs_a_selected_sale()
    {
        await _presenter.LoadAsync();

        await _presenter.CancelSelectedSaleAsync();

        Assert.Equal([SalesPresenter.SaleRequiredMessage], _view.Messages);
    }

    [Fact]
    public async Task A_sale_voided_elsewhere_shows_the_reason_and_refreshes()
    {
        await RegisterRiceSaleAsync(quantity: 1);
        _view.SelectedSaleId = 1;
        _sales.NextFailure = new BusinessRuleException("La venta #1 ya estaba anulada.");

        await _presenter.CancelSelectedSaleAsync();

        Assert.Equal(["La venta #1 ya estaba anulada."], _view.Errors);
    }

    private async Task RegisterRiceSaleAsync(int quantity)
    {
        _view.SelectedProduct = Rice;
        _view.Quantity = quantity;
        _presenter.AddItem();
        _view.SelectedPaymentMethodId = 1;
        await _presenter.RegisterAsync();
    }
}
