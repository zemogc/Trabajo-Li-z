using Microsoft.Extensions.DependencyInjection;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Views;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>A screen reachable from the main menu.</summary>
internal sealed record Module(string Title, char Glyph, Func<Form> Create);

/// <summary>
/// Builds each screen together with its presenter. The presenter subscribes to the view's
/// events, so it lives exactly as long as the form that owns those events.
/// </summary>
internal sealed class ModuleFactory(IServiceProvider services)
{
    public IReadOnlyList<Module> Modules { get; } =
    [
        new("Inicio", Infrastructure.Glyph.Home, () => CreateDashboard(services)),
        new("Ventas", Infrastructure.Glyph.Sale, () => CreateSales(services)),
        new("Entradas", Infrastructure.Glyph.StockEntry, () => CreateStockEntries(services)),
        new("Productos", Infrastructure.Glyph.Product, () => CreateProducts(services)),
        new("Categorías", Infrastructure.Glyph.Category, () => CreateCatalog<Category>(services, "Categorías")),
        new("Proveedores", Infrastructure.Glyph.Provider, () => CreateCatalog<Provider>(services, "Proveedores")),
        new("Medios de pago", Infrastructure.Glyph.Payment, () => CreateCatalog<PaymentMethod>(services, "Medios de pago")),
    ];

    private static DashboardForm CreateDashboard(IServiceProvider services)
    {
        var view = new DashboardForm();
        ActivatorUtilities.CreateInstance<DashboardPresenter>(services, view);
        return view;
    }

    private static SalesForm CreateSales(IServiceProvider services)
    {
        var view = new SalesForm();
        ActivatorUtilities.CreateInstance<SalesPresenter>(services, view);
        return view;
    }

    private static StockEntryForm CreateStockEntries(IServiceProvider services)
    {
        var view = new StockEntryForm();
        ActivatorUtilities.CreateInstance<StockEntryPresenter>(services, view);
        return view;
    }

    private static ProductForm CreateProducts(IServiceProvider services)
    {
        var view = new ProductForm();
        ActivatorUtilities.CreateInstance<ProductPresenter>(services, view);
        return view;
    }

    private static CatalogForm<TItem> CreateCatalog<TItem>(IServiceProvider services, string title)
        where TItem : CatalogItem, new()
    {
        var view = new CatalogForm<TItem>(title);
        ActivatorUtilities.CreateInstance<CatalogPresenter<TItem>>(services, view);
        return view;
    }
}
