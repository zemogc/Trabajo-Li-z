using System.ComponentModel;
using Supermarket.Core.Domain;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Tests;

public class SortableBindingListTests
{
    private static readonly PropertyDescriptorCollection Properties = TypeDescriptor.GetProperties(typeof(Product));

    private static SortableBindingList<Product> CreateList() => new(
    [
        new Product { Id = 1, Name = "lenteja", Price = 3900m, ProviderName = "Valle" },
        new Product { Id = 2, Name = "Arroz", Price = 4800m, ProviderName = null },
        new Product { Id = 3, Name = "Leche", Price = 4200m, ProviderName = "Granja" },
        new Product { Id = 4, Name = "Agua", Price = 1800m, ProviderName = "Valle" },
    ]);

    private static void Sort(SortableBindingList<Product> list, string property, ListSortDirection direction) =>
        ((IBindingList)list).ApplySort(Properties[property]!, direction);

    [Fact]
    public void Supports_sorting() => Assert.True(((IBindingList)CreateList()).SupportsSorting);

    [Fact]
    public void Sorts_text_ignoring_case()
    {
        var list = CreateList();

        Sort(list, nameof(Product.Name), ListSortDirection.Ascending);

        Assert.Equal(["Agua", "Arroz", "Leche", "lenteja"], list.Select(p => p.Name));
    }

    [Fact]
    public void Sorts_numbers_by_value_descending()
    {
        var list = CreateList();

        Sort(list, nameof(Product.Price), ListSortDirection.Descending);

        Assert.Equal([4800m, 4200m, 3900m, 1800m], list.Select(p => p.Price));
    }

    [Fact]
    public void Nulls_come_first_and_the_sort_is_stable()
    {
        var list = CreateList();

        Sort(list, nameof(Product.ProviderName), ListSortDirection.Ascending);

        // Both "Valle" products keep their original relative order (1 before 4).
        Assert.Equal([2, 3, 1, 4], list.Select(p => p.Id));
    }

    [Fact]
    public void Sorting_notifies_bound_controls()
    {
        var list = CreateList();
        var resets = 0;
        list.ListChanged += (_, e) => resets += e.ListChangedType == ListChangedType.Reset ? 1 : 0;

        Sort(list, nameof(Product.Name), ListSortDirection.Ascending);

        Assert.Equal(1, resets);
        Assert.True(((IBindingList)list).IsSorted);
    }

    [Fact]
    public void A_refreshed_list_keeps_the_previous_order()
    {
        var previous = CreateList();
        Sort(previous, nameof(Product.Price), ListSortDirection.Ascending);
        var refreshed = CreateList();

        refreshed.SortLike(previous);

        Assert.Equal([4, 1, 3, 2], refreshed.Select(p => p.Id));
        Assert.Equal(ListSortDirection.Ascending, ((IBindingList)refreshed).SortDirection);
    }

    [Fact]
    public void Unsorted_source_leaves_the_order_untouched()
    {
        var refreshed = CreateList();

        refreshed.SortLike(CreateList());

        Assert.Equal([1, 2, 3, 4], refreshed.Select(p => p.Id));
        Assert.False(((IBindingList)refreshed).IsSorted);
    }
}
