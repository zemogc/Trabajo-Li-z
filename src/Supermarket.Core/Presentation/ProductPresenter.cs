using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public sealed class ProductPresenter(
    IProductView view,
    IRepository<Product> products,
    IRepository<Category> categories,
    ILogger<ProductPresenter> logger)
    : CrudPresenter<Product>(view, products, logger)
{
    // Categories are reloaded every time the screen is shown so new ones are available right away.
    protected override async Task OnLoadingAsync() =>
        view.DisplayCategories(await categories.GetAllAsync());
}
