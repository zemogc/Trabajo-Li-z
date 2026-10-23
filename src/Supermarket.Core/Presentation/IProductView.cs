using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public interface IProductView : ICrudView<Product>
{
    /// <summary>Fills the category selector used by the product editor.</summary>
    void DisplayCategories(IReadOnlyList<Category> categories);

    /// <summary>Fills the optional provider selector used by the product editor.</summary>
    void DisplayProviders(IReadOnlyList<Provider> providers);
}
