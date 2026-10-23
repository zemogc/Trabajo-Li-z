using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Tests.Domain;

public class SaleDraftTests
{
    private readonly Product _rice = new() { Id = 1, Name = "Arroz", Price = 4800m, Stock = 10 };
    private readonly Product _milk = new() { Id = 2, Name = "Leche", Price = 4200.50m, Stock = 3 };
    private readonly SaleDraft _draft = new();

    [Fact]
    public void Starts_empty() => Assert.True(_draft.IsEmpty);

    [Fact]
    public void Adding_products_builds_lines_and_total()
    {
        _draft.Add(_rice, 2);
        _draft.Add(_milk, 1);

        Assert.Equal(2, _draft.Lines.Count);
        Assert.Equal((2 * 4800m) + 4200.50m, _draft.Total);
        Assert.Equal(9600m, _draft.Lines[0].Subtotal);
    }

    [Fact]
    public void Adding_the_same_product_again_merges_the_quantity()
    {
        _draft.Add(_rice, 2);
        _draft.Add(_rice, 3);

        Assert.Equal(5, Assert.Single(_draft.Lines).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity)
    {
        var error = Assert.Throws<BusinessRuleException>(() => _draft.Add(_rice, quantity));

        Assert.Equal("La cantidad debe ser mayor que cero.", error.Message);
        Assert.True(_draft.IsEmpty);
    }

    [Fact]
    public void Cannot_add_more_than_the_available_stock_across_lines()
    {
        _draft.Add(_milk, 2);

        var error = Assert.Throws<BusinessRuleException>(() => _draft.Add(_milk, 2));

        Assert.Equal("Stock insuficiente para \"Leche\": disponible 3, solicitado 4.", error.Message);
        Assert.Equal(2, Assert.Single(_draft.Lines).Quantity);
    }

    [Fact]
    public void Remove_and_clear()
    {
        _draft.Add(_rice, 1);
        _draft.Add(_milk, 1);

        _draft.Remove(_rice.Id);
        Assert.Equal("Leche", Assert.Single(_draft.Lines).ProductName);

        _draft.Clear();
        Assert.True(_draft.IsEmpty);
        Assert.Equal(0m, _draft.Total);
    }

    [Fact]
    public void Items_carry_only_product_and_quantity()
    {
        _draft.Add(_rice, 2);

        Assert.Equal([new SaleItem(1, 2)], _draft.ToItems());
    }
}
