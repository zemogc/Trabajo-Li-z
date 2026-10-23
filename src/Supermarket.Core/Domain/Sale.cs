namespace Supermarket.Core.Domain;

/// <summary>A registered sale, as listed in the sales history.</summary>
public sealed class Sale
{
    public int Id { get; set; }

    /// <summary>Moment of the sale in UTC, as stored by the database.</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime CreatedAtLocal => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();

    public int PaymentMethodId { get; set; }

    public string PaymentMethodName { get; set; } = string.Empty;

    public decimal Total { get; set; }

    /// <summary>Number of units sold, adding up every line.</summary>
    public int ItemCount { get; set; }

    /// <summary>Moment the sale was voided, in UTC, or <see langword="null"/> while it is valid.</summary>
    public DateTime? CancelledAt { get; set; }

    public bool IsCancelled => CancelledAt is not null;

    public string Status => IsCancelled ? "Anulada" : "Registrada";
}

/// <summary>One product within a sale or within the sale being prepared.</summary>
public sealed class SaleLine
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    /// <summary>Price at the moment of the sale; later price changes do not alter past sales.</summary>
    public decimal UnitPrice { get; set; }

    public decimal Subtotal => Quantity * UnitPrice;
}

/// <summary>What the client asks to sell. Prices are always taken from the database.</summary>
public readonly record struct SaleItem(int ProductId, int Quantity);

/// <summary>Result of registering a sale, with the total computed from database prices.</summary>
public readonly record struct SaleReceipt(int SaleId, decimal Total);
