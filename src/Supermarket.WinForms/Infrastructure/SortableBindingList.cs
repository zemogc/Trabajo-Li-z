using System.ComponentModel;
using System.Globalization;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>
/// <see cref="BindingList{T}"/> with sorting support, so clicking a grid header orders the rows.
/// </summary>
/// <remarks>The sort is stable: rows with equal values keep their previous relative order.</remarks>
internal sealed class SortableBindingList<T> : BindingList<T>
{
    private bool _isSorted;
    private ListSortDirection _direction;
    private PropertyDescriptor? _property;

    public SortableBindingList(IEnumerable<T> items)
        : base([.. items])
    {
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override ListSortDirection SortDirectionCore => _direction;

    protected override PropertyDescriptor? SortPropertyCore => _property;

    /// <summary>Sorts this list the same way as <paramref name="other"/>, if it is sorted.</summary>
    public void SortLike(SortableBindingList<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other._isSorted && other._property is not null)
        {
            ApplySortCore(other._property, other._direction);
        }
    }

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var items = (List<T>)Items;
        var sorted = direction == ListSortDirection.Ascending
            ? items.OrderBy(item => prop.GetValue(item), ValueComparer.Instance).ToList()
            : items.OrderByDescending(item => prop.GetValue(item), ValueComparer.Instance).ToList();

        items.Clear();
        items.AddRange(sorted);

        _property = prop;
        _direction = direction;
        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _property = null;
    }

    /// <summary>Orders nulls first, text by the current culture ignoring case, and anything else by IComparable.</summary>
    /// <remarks>
    /// Culture-aware on purpose: an ordinal comparison would put "Ñame" and "Ácido" after every name
    /// starting with "z", which is not what Spanish speaking users expect.
    /// </remarks>
    internal sealed class ValueComparer : IComparer<object?>
    {
        public static readonly ValueComparer Instance = new();

        public int Compare(object? x, object? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (string a, string b) => CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase),
            (IComparable a, _) when a.GetType() == y.GetType() => a.CompareTo(y),
            _ => CultureInfo.CurrentCulture.CompareInfo.Compare(x.ToString(), y.ToString(), CompareOptions.IgnoreCase),
        };
    }
}
