using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>
/// Presenter for name/observation catalogs: categories, providers and payment methods.
/// </summary>
public sealed class CatalogPresenter<TItem>(
    ICrudView<TItem> view,
    IRepository<TItem> repository,
    ILogger<CatalogPresenter<TItem>> logger)
    : CrudPresenter<TItem>(view, repository, logger)
    where TItem : CatalogItem, new()
{
    protected override void Normalize(TItem entity)
    {
        base.Normalize(entity);
        entity.Observation = string.IsNullOrWhiteSpace(entity.Observation) ? null : entity.Observation.Trim();
    }
}
