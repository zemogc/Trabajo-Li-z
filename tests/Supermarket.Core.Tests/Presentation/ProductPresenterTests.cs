using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class ProductPresenterTests
{
    private readonly FakeProductView _view = new();
    private readonly InMemoryRepository<Category> _categories = new InMemoryRepository<Category>()
        .With(new Category { Name = "Granos" });

    private readonly InMemoryRepository<Product> _products = new();
    private readonly ProductPresenter _presenter;

    public ProductPresenterTests() =>
        _presenter = new ProductPresenter(_view, _products, _categories, NullLogger<ProductPresenter>.Instance);

    [Fact]
    public async Task Load_fills_the_category_selector_before_listing_products()
    {
        await _presenter.LoadAsync();

        Assert.Equal("Granos", Assert.Single(_view.Categories).Name);
        Assert.Empty(_view.Displayed);
    }

    [Fact]
    public async Task Categories_created_later_appear_on_the_next_load()
    {
        await _presenter.LoadAsync();
        await _categories.AddAsync(new Category { Name = "Enlatados" });

        await _presenter.LoadAsync();

        Assert.Equal(2, _view.Categories.Count);
    }

    [Fact]
    public async Task Saving_a_product_without_category_is_rejected()
    {
        _presenter.BeginCreate();
        _view.EditorInput = new Product { Name = "Lentejas", Price = 4500m, Stock = 10, CategoryId = 0 };

        await _presenter.SaveAsync();

        Assert.Equal(["Debe seleccionar una categoría."], _view.Errors);
        Assert.Empty(_products.Items);
    }

    [Fact]
    public async Task Saving_a_valid_product_persists_it()
    {
        _presenter.BeginCreate();
        _view.EditorInput = new Product { Name = " Lentejas ", Price = 4500m, Stock = 10, CategoryId = 1 };

        await _presenter.SaveAsync();

        var product = Assert.Single(_products.Items);
        Assert.Equal("Lentejas", product.Name);
        Assert.Equal(4500m, product.Price);
        Assert.Single(_view.Displayed);
    }
}
