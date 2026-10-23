using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;

namespace Supermarket.Core.Tests.Fakes;

internal sealed class FakeSalesView : ISalesView
{
    public event EventHandler? LoadRequested;
    public event EventHandler? AddItemRequested;
    public event EventHandler? RemoveItemRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? RegisterRequested;
    public event EventHandler? SaleSelected;

    public Product? SelectedProduct { get; set; }

    public int Quantity { get; set; } = 1;

    public int? SelectedCartProductId { get; set; }

    public int? SelectedPaymentMethodId { get; set; }

    public int? SelectedSaleId { get; set; }

    public bool ConfirmAnswer { get; set; } = true;

    public IReadOnlyList<Product> Products { get; private set; } = [];

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; private set; } = [];

    public IReadOnlyList<SaleLine> Cart { get; private set; } = [];

    public decimal CartTotal { get; private set; }

    public IReadOnlyList<Sale> RecentSales { get; private set; } = [];

    public IReadOnlyList<SaleLine>? SaleLines { get; private set; }

    public int ItemInputResets { get; private set; }

    public List<bool> BusyChanges { get; } = [];

    public List<string> Messages { get; } = [];

    public List<string> Errors { get; } = [];

    public void DisplayProducts(IReadOnlyList<Product> products) => Products = products;

    public void DisplayPaymentMethods(IReadOnlyList<PaymentMethod> paymentMethods) => PaymentMethods = paymentMethods;

    public void DisplayCart(IReadOnlyList<SaleLine> lines, decimal total)
    {
        Cart = [.. lines];
        CartTotal = total;
    }

    public void ResetItemInput() => ItemInputResets++;

    public void DisplayRecentSales(IReadOnlyList<Sale> sales) => RecentSales = sales;

    public void DisplaySaleLines(IReadOnlyList<SaleLine> lines) => SaleLines = lines;

    public void SetBusy(bool isBusy) => BusyChanges.Add(isBusy);

    public void ShowMessage(string message) => Messages.Add(message);

    public void ShowErrors(IReadOnlyList<string> errors) => Errors.AddRange(errors);

    public bool Confirm(string question) => ConfirmAnswer;

    public void RaiseLoad() => LoadRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseAddItem() => AddItemRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseRemoveItem() => RemoveItemRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseClear() => ClearRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseRegister() => RegisterRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSaleSelected() => SaleSelected?.Invoke(this, EventArgs.Empty);
}

/// <summary>Sale repository backed by an in-memory product list, mimicking the database rules.</summary>
internal sealed class InMemorySaleRepository(InMemoryRepository<Product> products) : ISaleRepository
{
    private readonly List<(Sale Sale, List<SaleLine> Lines)> _sales = [];

    public Exception? NextFailure { get; set; }

    public IReadOnlyList<SaleItem>? LastItems { get; private set; }

    public Task<SaleReceipt> RegisterAsync(int paymentMethodId, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken = default)
    {
        if (NextFailure is { } failure)
        {
            NextFailure = null;
            throw failure;
        }

        LastItems = items;
        var lines = items.Select(item =>
        {
            var product = products.Items.Single(p => p.Id == item.ProductId);
            if (product.Stock < item.Quantity)
            {
                throw new BusinessRuleException($"Stock insuficiente para \"{product.Name}\".");
            }

            return new SaleLine { ProductId = product.Id, ProductName = product.Name, Quantity = item.Quantity, UnitPrice = product.Price };
        }).ToList();

        foreach (var line in lines)
        {
            products.Items.Single(p => p.Id == line.ProductId).Stock -= line.Quantity;
        }

        var sale = new Sale
        {
            Id = _sales.Count + 1,
            CreatedAt = DateTime.UtcNow,
            PaymentMethodId = paymentMethodId,
            Total = lines.Sum(l => l.Subtotal),
            ItemCount = lines.Sum(l => l.Quantity),
        };
        _sales.Add((sale, lines));
        return Task.FromResult(new SaleReceipt(sale.Id, sale.Total));
    }

    public Task<IReadOnlyList<Sale>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Sale>>(_sales.Select(s => s.Sale).Reverse().Take(count).ToList());

    public Task<IReadOnlyList<SaleLine>> GetLinesAsync(int saleId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaleLine>>(_sales.Single(s => s.Sale.Id == saleId).Lines);
}
