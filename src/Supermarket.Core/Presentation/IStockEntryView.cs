using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>Screen to receive merchandise and review previous entries.</summary>
public interface IStockEntryView : IOperationView
{
    event EventHandler? LoadRequested;

    event EventHandler? AddItemRequested;

    event EventHandler? RemoveItemRequested;

    event EventHandler? ClearRequested;

    event EventHandler? RegisterRequested;

    event EventHandler? EntrySelected;

    Product? SelectedProduct { get; }

    int Quantity { get; }

    decimal UnitCost { get; }

    int? SelectedLineProductId { get; }

    /// <summary>The provider of the merchandise, or <see langword="null"/> when it is not recorded.</summary>
    int? SelectedProviderId { get; }

    string Notes { get; }

    int? SelectedEntryId { get; }

    void DisplayProducts(IReadOnlyList<Product> products);

    void DisplayProviders(IReadOnlyList<Provider> providers);

    void DisplayDraft(IReadOnlyList<StockEntryLine> lines, decimal totalCost);

    /// <summary>Gets the inputs ready for the next product, and clears the notes after a registration.</summary>
    void ResetItemInput(bool clearNotes);

    void DisplayRecentEntries(IReadOnlyList<StockEntry> entries);

    void DisplayEntryLines(IReadOnlyList<StockEntryLine> lines);
}
