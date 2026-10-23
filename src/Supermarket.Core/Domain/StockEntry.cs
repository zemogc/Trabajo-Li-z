namespace Supermarket.Core.Domain;

/// <summary>Merchandise received into the store, usually from a provider.</summary>
public sealed class StockEntry
{
    public const int NotesMaxLength = 200;

    public int Id { get; set; }

    /// <summary>Moment of the entry in UTC, as stored by the database.</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime CreatedAtLocal => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();

    public int? ProviderId { get; set; }

    public string? ProviderName { get; set; }

    public string? Notes { get; set; }

    /// <summary>Units received, adding up every line.</summary>
    public int ItemCount { get; set; }

    public decimal TotalCost { get; set; }
}

/// <summary>One product within a stock entry.</summary>
public sealed class StockEntryLine
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    /// <summary>What the store paid per unit; zero is allowed for promotional or donated goods.</summary>
    public decimal UnitCost { get; set; }

    public decimal Subtotal => Quantity * UnitCost;
}

public readonly record struct StockEntryItem(int ProductId, int Quantity, decimal UnitCost);
