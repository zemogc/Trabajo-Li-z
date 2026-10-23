using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Tests.Domain;

public class StockEntryDraftTests
{
    private readonly Product _rice = new() { Id = 1, Name = "Arroz" };
    private readonly StockEntryDraft _draft = new();

    [Fact]
    public void Lines_and_total_cost_follow_what_is_added()
    {
        _draft.Add(_rice, 10, 3500m);
        _draft.Add(new Product { Id = 2, Name = "Leche" }, 4, 2900.50m);

        Assert.Equal(2, _draft.Lines.Count);
        Assert.Equal((10 * 3500m) + (4 * 2900.50m), _draft.TotalCost);
    }

    [Fact]
    public void The_same_product_and_cost_are_merged()
    {
        _draft.Add(_rice, 10, 3500m);
        _draft.Add(_rice, 5, 3500m);

        Assert.Equal(15, Assert.Single(_draft.Lines).Quantity);
    }

    [Fact]
    public void The_same_product_with_another_cost_is_rejected()
    {
        _draft.Add(_rice, 10, 3500m);

        var error = Assert.Throws<BusinessRuleException>(() => _draft.Add(_rice, 5, 3600m));

        Assert.Contains("ya está en la entrada", error.Message, StringComparison.Ordinal);
        Assert.Equal(10, Assert.Single(_draft.Lines).Quantity);
    }

    [Fact]
    public void Zero_cost_is_allowed_but_negative_values_are_not()
    {
        _draft.Add(_rice, 1, 0m);

        Assert.Throws<BusinessRuleException>(() => _draft.Add(_rice, 0, 0m));
        Assert.Throws<BusinessRuleException>(() => _draft.Add(new Product { Id = 3, Name = "Sal" }, 1, -1m));
    }

    [Fact]
    public void Items_carry_product_quantity_and_cost()
    {
        _draft.Add(_rice, 10, 3500m);
        _draft.Remove(99);

        Assert.Equal([new StockEntryItem(1, 10, 3500m)], _draft.ToItems());

        _draft.Clear();
        Assert.True(_draft.IsEmpty);
    }
}
