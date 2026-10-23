using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public sealed partial class StockEntryPresenter : PresenterBase
{
    internal const int RecentEntriesCount = 50;
    internal const string ProductRequiredMessage = "Seleccione un producto.";
    internal const string LineRequiredMessage = "Seleccione en la lista el producto que desea quitar.";
    internal const string EmptyDraftMessage = "Agregue al menos un producto a la entrada.";
    internal const string ClearQuestion = "¿Desea vaciar la entrada en curso?";

    private readonly IStockEntryView _view;
    private readonly IStockEntryRepository _entries;
    private readonly IRepository<Product> _products;
    private readonly IRepository<Provider> _providers;
    private readonly ILogger _logger;

    public StockEntryPresenter(
        IStockEntryView view,
        IStockEntryRepository entries,
        IRepository<Product> products,
        IRepository<Provider> providers,
        ILogger<StockEntryPresenter> logger)
        : base(view, logger)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(providers);

        _view = view;
        _entries = entries;
        _products = products;
        _providers = providers;
        _logger = logger;

        _view.LoadRequested += async (_, _) => await LoadAsync();
        _view.AddItemRequested += (_, _) => AddItem();
        _view.RemoveItemRequested += (_, _) => RemoveItem();
        _view.ClearRequested += (_, _) => Clear();
        _view.RegisterRequested += async (_, _) => await RegisterAsync();
        _view.EntrySelected += async (_, _) => await ShowSelectedEntryAsync();
    }

    public StockEntryDraft Draft { get; } = new();

    internal static string RegisteredMessage(int entryId, int units) =>
        $"Entrada #{entryId} registrada: {units:N0} unidades agregadas al inventario.";

    internal static string NotesTooLongMessage =>
        $"Las observaciones no pueden superar {StockEntry.NotesMaxLength} caracteres.";

    public Task LoadAsync() => RunAsync(async () =>
    {
        _view.DisplayProviders(await _providers.GetAllAsync());
        await RefreshAsync();
        DisplayDraft();
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
            Draft.Add(product, _view.Quantity, _view.UnitCost);
        }
        catch (BusinessRuleException ex)
        {
            _view.ShowErrors([ex.Message]);
            return;
        }

        DisplayDraft();
        _view.ResetItemInput(clearNotes: false);
    }

    public void RemoveItem()
    {
        if (_view.SelectedLineProductId is not { } productId)
        {
            _view.ShowMessage(LineRequiredMessage);
            return;
        }

        Draft.Remove(productId);
        DisplayDraft();
    }

    public void Clear()
    {
        if (Draft.IsEmpty || !_view.Confirm(ClearQuestion))
        {
            return;
        }

        Draft.Clear();
        DisplayDraft();
    }

    public Task RegisterAsync()
    {
        var notes = string.IsNullOrWhiteSpace(_view.Notes) ? null : _view.Notes.Trim();

        var errors = new List<string>();
        if (Draft.IsEmpty)
        {
            errors.Add(EmptyDraftMessage);
        }

        if (notes?.Length > StockEntry.NotesMaxLength)
        {
            errors.Add(NotesTooLongMessage);
        }

        if (errors.Count > 0)
        {
            _view.ShowErrors(errors);
            return Task.CompletedTask;
        }

        return RunAsync(
            async () =>
            {
                var units = Draft.Lines.Sum(l => l.Quantity);
                var entryId = await _entries.RegisterAsync(_view.SelectedProviderId, notes, Draft.ToItems());
                LogEntryRegistered(entryId, units);

                Draft.Clear();
                DisplayDraft();
                _view.ResetItemInput(clearNotes: true);
                await RefreshAsync();
                _view.ShowMessage(RegisteredMessage(entryId, units));
            },
            onNotFound: RefreshAsync);
    }

    public Task ShowSelectedEntryAsync()
    {
        if (_view.SelectedEntryId is not { } entryId)
        {
            _view.DisplayEntryLines([]);
            return Task.CompletedTask;
        }

        return RunAsync(async () => _view.DisplayEntryLines(await _entries.GetLinesAsync(entryId)));
    }

    private async Task RefreshAsync()
    {
        _view.DisplayProducts(await _products.GetAllAsync());
        _view.DisplayRecentEntries(await _entries.GetRecentAsync(RecentEntriesCount));
    }

    private void DisplayDraft() => _view.DisplayDraft(Draft.Lines, Draft.TotalCost);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stock entry {EntryId} registered with {Units} units")]
    private partial void LogEntryRegistered(int entryId, int units);
}
