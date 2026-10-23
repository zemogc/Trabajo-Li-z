using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class StockEntryPresenterTests
{
    private readonly FakeStockEntryView _view = new();
    private readonly InMemoryRepository<Product> _products = new InMemoryRepository<Product>().With(
        new Product { Name = "Arroz", Stock = 0 },
        new Product { Name = "Leche", Stock = 5 });

    private readonly InMemoryRepository<Provider> _providers = new InMemoryRepository<Provider>()
        .With(new Provider { Name = "Distribuidora del Valle" });

    private readonly FakeStockEntryRepository _entries;
    private readonly StockEntryPresenter _presenter;

    public StockEntryPresenterTests()
    {
        _entries = new FakeStockEntryRepository(_products);
        _presenter = new StockEntryPresenter(_view, _entries, _products, _providers, NullLogger<StockEntryPresenter>.Instance);
    }

    [Fact]
    public async Task Load_offers_every_product_even_without_stock()
    {
        await _presenter.LoadAsync();

        Assert.Equal(2, _view.Products.Count);
        Assert.Single(_view.Providers);
    }

    [Fact]
    public void Adding_items_updates_the_draft_and_keeps_the_notes()
    {
        _view.SelectedProduct = _products.Items[0];
        _view.Quantity = 12;
        _view.UnitCost = 3500m;

        _presenter.AddItem();

        Assert.Equal(12, Assert.Single(_view.Lines).Quantity);
        Assert.Equal(42000m, _view.TotalCost);
        Assert.Equal([false], _view.Resets);
    }

    [Fact]
    public void Invalid_items_show_the_rule()
    {
        _view.SelectedProduct = _products.Items[0];
        _view.UnitCost = -5m;

        _presenter.AddItem();

        Assert.Equal(["El costo unitario no puede ser negativo."], _view.Errors);
    }

    [Fact]
    public async Task Registering_adds_stock_and_clears_the_form()
    {
        _view.SelectedProduct = _products.Items[0];
        _view.Quantity = 12;
        _view.UnitCost = 3500m;
        _presenter.AddItem();
        _view.SelectedProviderId = 1;
        _view.Notes = "  Factura 123  ";

        await _presenter.RegisterAsync();

        Assert.Equal(12, _products.Items[0].Stock);
        Assert.Equal((1, "Factura 123"), (_entries.LastProviderId, _entries.LastNotes));
        Assert.True(_presenter.Draft.IsEmpty);
        Assert.Equal([false, true], _view.Resets);
        Assert.Single(_view.Entries);
        Assert.Equal(StockEntryPresenter.RegisteredMessage(1, 12), _view.Messages[^1]);
    }

    [Fact]
    public async Task Empty_drafts_and_long_notes_are_rejected()
    {
        _view.Notes = new string('x', StockEntry.NotesMaxLength + 1);

        await _presenter.RegisterAsync();

        Assert.Equal([StockEntryPresenter.EmptyDraftMessage, StockEntryPresenter.NotesTooLongMessage], _view.Errors);
    }

    [Fact]
    public async Task Blank_notes_and_no_provider_are_sent_as_null()
    {
        _view.SelectedProduct = _products.Items[1];
        _presenter.AddItem();
        _view.Notes = "   ";

        await _presenter.RegisterAsync();

        Assert.Null(_entries.LastProviderId);
        Assert.Null(_entries.LastNotes);
    }

    [Fact]
    public void Remove_and_clear_need_a_selection_or_confirmation()
    {
        _view.SelectedProduct = _products.Items[0];
        _presenter.AddItem();

        _presenter.RemoveItem();
        Assert.Equal([StockEntryPresenter.LineRequiredMessage], _view.Messages);

        _view.ConfirmAnswer = false;
        _presenter.Clear();
        Assert.Single(_view.Lines);

        _view.ConfirmAnswer = true;
        _presenter.Clear();
        Assert.Empty(_view.Lines);
    }

    [Fact]
    public async Task Selecting_an_entry_shows_its_lines()
    {
        _view.SelectedProduct = _products.Items[0];
        _presenter.AddItem();
        await _presenter.RegisterAsync();

        _view.SelectedEntryId = 1;
        await _presenter.ShowSelectedEntryAsync();

        Assert.Equal("Arroz", Assert.Single(_view.EntryLines).ProductName);
    }

    private sealed class FakeStockEntryView : IStockEntryView
    {
        public event EventHandler? LoadRequested;
        public event EventHandler? AddItemRequested;
        public event EventHandler? RemoveItemRequested;
        public event EventHandler? ClearRequested;
        public event EventHandler? RegisterRequested;
        public event EventHandler? EntrySelected;

        public Product? SelectedProduct { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal UnitCost { get; set; }

        public int? SelectedLineProductId { get; set; }

        public int? SelectedProviderId { get; set; }

        public string Notes { get; set; } = string.Empty;

        public int? SelectedEntryId { get; set; }

        public bool ConfirmAnswer { get; set; } = true;

        public IReadOnlyList<Product> Products { get; private set; } = [];

        public IReadOnlyList<Provider> Providers { get; private set; } = [];

        public IReadOnlyList<StockEntryLine> Lines { get; private set; } = [];

        public decimal TotalCost { get; private set; }

        public IReadOnlyList<StockEntry> Entries { get; private set; } = [];

        public IReadOnlyList<StockEntryLine> EntryLines { get; private set; } = [];

        public List<bool> Resets { get; } = [];

        public List<string> Messages { get; } = [];

        public List<string> Errors { get; } = [];

        public void DisplayProducts(IReadOnlyList<Product> products) => Products = products;

        public void DisplayProviders(IReadOnlyList<Provider> providers) => Providers = providers;

        public void DisplayDraft(IReadOnlyList<StockEntryLine> lines, decimal totalCost)
        {
            Lines = [.. lines];
            TotalCost = totalCost;
        }

        public void ResetItemInput(bool clearNotes) => Resets.Add(clearNotes);

        public void DisplayRecentEntries(IReadOnlyList<StockEntry> entries) => Entries = entries;

        public void DisplayEntryLines(IReadOnlyList<StockEntryLine> lines) => EntryLines = lines;

        public void SetBusy(bool isBusy)
        {
        }

        public void ShowMessage(string message) => Messages.Add(message);

        public void ShowErrors(IReadOnlyList<string> errors) => Errors.AddRange(errors);

        public bool Confirm(string question) => ConfirmAnswer;

        public void RaiseAll()
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
            AddItemRequested?.Invoke(this, EventArgs.Empty);
            RemoveItemRequested?.Invoke(this, EventArgs.Empty);
            ClearRequested?.Invoke(this, EventArgs.Empty);
            RegisterRequested?.Invoke(this, EventArgs.Empty);
            EntrySelected?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FakeStockEntryRepository(InMemoryRepository<Product> products) : IStockEntryRepository
    {
        private readonly List<(StockEntry Entry, List<StockEntryLine> Lines)> _entries = [];

        public int? LastProviderId { get; private set; }

        public string? LastNotes { get; private set; }

        public Task<int> RegisterAsync(int? providerId, string? notes, IReadOnlyList<StockEntryItem> items, CancellationToken cancellationToken = default)
        {
            LastProviderId = providerId;
            LastNotes = notes;

            var lines = items.Select(item =>
            {
                var product = products.Items.Single(p => p.Id == item.ProductId);
                product.Stock += item.Quantity;
                return new StockEntryLine { ProductId = product.Id, ProductName = product.Name, Quantity = item.Quantity, UnitCost = item.UnitCost };
            }).ToList();

            var entry = new StockEntry { Id = _entries.Count + 1, ProviderId = providerId, Notes = notes, ItemCount = lines.Sum(l => l.Quantity) };
            _entries.Add((entry, lines));
            return Task.FromResult(entry.Id);
        }

        public Task<IReadOnlyList<StockEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StockEntry>>(_entries.Select(e => e.Entry).Reverse().Take(count).ToList());

        public Task<IReadOnlyList<StockEntryLine>> GetLinesAsync(int entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StockEntryLine>>(_entries.Single(e => e.Entry.Id == entryId).Lines);
    }
}
