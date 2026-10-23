using Supermarket.Core.Abstractions;

namespace Supermarket.Core.Domain;

/// <summary>
/// The sale being prepared at the checkout: the products and quantities chosen so far.
/// </summary>
/// <remarks>
/// Stock is checked here against the last known values to give immediate feedback. The database
/// checks it again atomically when the sale is registered, because another checkout may sell
/// the same units in the meantime.
/// </remarks>
public sealed class SaleDraft
{
    private readonly List<SaleLine> _lines = [];

    public IReadOnlyList<SaleLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.Subtotal);

    public bool IsEmpty => _lines.Count == 0;

    /// <summary>Adds units of a product, merging them with a line that already has it.</summary>
    /// <exception cref="BusinessRuleException">The quantity is not positive or exceeds the available stock.</exception>
    public void Add(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
        {
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
        }

        var line = _lines.Find(l => l.ProductId == product.Id);
        var requested = (line?.Quantity ?? 0) + quantity;
        if (requested > product.Stock)
        {
            throw new BusinessRuleException(
                $"Stock insuficiente para \"{product.Name}\": disponible {product.Stock}, solicitado {requested}.");
        }

        if (line is null)
        {
            _lines.Add(new SaleLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = product.Price,
            });
        }
        else
        {
            line.Quantity = requested;
        }
    }

    public void Remove(int productId) => _lines.RemoveAll(l => l.ProductId == productId);

    public void Clear() => _lines.Clear();

    public IReadOnlyList<SaleItem> ToItems() =>
        _lines.Select(l => new SaleItem(l.ProductId, l.Quantity)).ToList();
}
