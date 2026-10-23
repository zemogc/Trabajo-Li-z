using Supermarket.Core.Abstractions;

namespace Supermarket.Core.Domain;

/// <summary>The stock entry being prepared: products received, quantities and unit costs.</summary>
public sealed class StockEntryDraft
{
    private readonly List<StockEntryLine> _lines = [];

    public IReadOnlyList<StockEntryLine> Lines => _lines;

    public decimal TotalCost => _lines.Sum(line => line.Subtotal);

    public bool IsEmpty => _lines.Count == 0;

    /// <exception cref="BusinessRuleException">
    /// The quantity is not positive, the cost is negative, or the product is already listed with another cost.
    /// </exception>
    public void Add(Product product, int quantity, decimal unitCost)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
        {
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
        }

        if (unitCost < 0)
        {
            throw new BusinessRuleException("El costo unitario no puede ser negativo.");
        }

        var line = _lines.Find(l => l.ProductId == product.Id);
        if (line is null)
        {
            _lines.Add(new StockEntryLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitCost = unitCost,
            });
            return;
        }

        // One line per product keeps the cost of each unit unambiguous.
        if (line.UnitCost != unitCost)
        {
            throw new BusinessRuleException(
                $"\"{product.Name}\" ya está en la entrada con un costo de {line.UnitCost:N2}. Quítelo para cambiar el costo.");
        }

        line.Quantity += quantity;
    }

    public void Remove(int productId) => _lines.RemoveAll(l => l.ProductId == productId);

    public void Clear() => _lines.Clear();

    public IReadOnlyList<StockEntryItem> ToItems() =>
        _lines.Select(l => new StockEntryItem(l.ProductId, l.Quantity, l.UnitCost)).ToList();
}
