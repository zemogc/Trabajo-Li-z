namespace Supermarket.Core.Receipts;

/// <summary>Store details printed in the header and footer of receipts.</summary>
public sealed class StoreInfo
{
    public const string SectionName = "Store";

    public string Name { get; set; } = "Supermercado";

    /// <summary>Tax identification number, such as the Colombian NIT.</summary>
    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string Footer { get; set; } = "¡Gracias por su compra!";
}
