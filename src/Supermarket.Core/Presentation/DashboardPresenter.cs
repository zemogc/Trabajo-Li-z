using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public sealed class DashboardPresenter : PresenterBase
{
    internal const int TopProductsCount = 5;

    /// <summary>Best sellers are measured over the last 30 days, today included.</summary>
    internal const int TopProductsDaysBack = 29;

    private readonly IDashboardView _view;
    private readonly IReportRepository _reports;
    private readonly IRepository<Product> _products;
    private readonly TimeProvider _clock;

    public DashboardPresenter(
        IDashboardView view,
        IReportRepository reports,
        IRepository<Product> products,
        TimeProvider clock,
        ILogger<DashboardPresenter> logger)
        : base(view, logger)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(clock);

        _view = view;
        _reports = reports;
        _products = products;
        _clock = clock;

        _view.LoadRequested += async (_, _) => await LoadAsync();
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var today = DateRange.LocalDays(_clock);
        var lastDays = DateRange.LocalDays(_clock, TopProductsDaysBack);

        var toRestock = (await _products.GetAllAsync())
            .Where(p => p.IsLowStock)
            .OrderBy(p => p.Stock - p.MinimumStock)
            .ThenBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();

        _view.DisplayToday(await _reports.GetSalesSummaryAsync(today), toRestock.Count);
        _view.DisplayPaymentBreakdown(await _reports.GetSalesByPaymentMethodAsync(today));
        _view.DisplayTopProducts(await _reports.GetTopProductsAsync(lastDays, TopProductsCount));
        _view.DisplayProductsToRestock(toRestock);
    });
}
