using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class DashboardPresenterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeDashboardView _view = new();
    private readonly FakeReportRepository _reports = new();
    private readonly InMemoryRepository<Product> _products = new InMemoryRepository<Product>().With(
        new Product { Name = "Arroz", Stock = 60, MinimumStock = 25 },
        new Product { Name = "Jabón", Stock = 4, MinimumStock = 5 },
        new Product { Name = "Queso", Stock = 2, MinimumStock = 8 });

    private readonly DashboardPresenter _presenter;

    public DashboardPresenterTests() =>
        _presenter = new DashboardPresenter(
            _view, _reports, _products, new FakeClock(Now, FakeClock.Bogota), NullLogger<DashboardPresenter>.Instance);

    [Fact]
    public async Task Shows_todays_figures_for_the_local_day()
    {
        await _presenter.LoadAsync();

        Assert.Same(_reports.Summary, _view.Today);
        Assert.Equal(new DateRange(new DateTime(2026, 10, 15, 5, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 16, 5, 0, 0, DateTimeKind.Utc)), _reports.SummaryPeriod);
        Assert.Equal(_reports.SummaryPeriod, _reports.PaymentPeriod);
    }

    [Fact]
    public async Task Best_sellers_cover_the_last_30_days()
    {
        await _presenter.LoadAsync();

        Assert.Equal(new DateTime(2026, 9, 16, 5, 0, 0, DateTimeKind.Utc), _reports.TopPeriod.FromUtc);
        Assert.Equal(DashboardPresenter.TopProductsCount, _reports.TopCount);
    }

    [Fact]
    public async Task Products_to_restock_are_ordered_by_urgency()
    {
        await _presenter.LoadAsync();

        Assert.Equal(["Queso", "Jabón"], _view.ToRestock.Select(p => p.Name));
        Assert.Equal(2, _view.ToRestockCount);
    }

    [Fact]
    public async Task Report_failures_are_reported_without_crashing()
    {
        _reports.NextFailure = new TimeoutException();

        await _presenter.LoadAsync();

        Assert.Equal([PresenterBase.UnexpectedErrorMessage], _view.Errors);
        Assert.Equal([true, false], _view.BusyChanges);
    }

    private sealed class FakeDashboardView : IDashboardView
    {
        public event EventHandler? LoadRequested;

        public SalesSummary? Today { get; private set; }

        public int ToRestockCount { get; private set; }

        public IReadOnlyList<Product> ToRestock { get; private set; } = [];

        public List<bool> BusyChanges { get; } = [];

        public List<string> Errors { get; } = [];

        public void DisplayToday(SalesSummary today, int productsToRestock)
        {
            Today = today;
            ToRestockCount = productsToRestock;
        }

        public void DisplayTopProducts(IReadOnlyList<ProductSales> products)
        {
        }

        public void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods)
        {
        }

        public void DisplayProductsToRestock(IReadOnlyList<Product> products) => ToRestock = products;

        public void SetBusy(bool isBusy) => BusyChanges.Add(isBusy);

        public void ShowMessage(string message)
        {
        }

        public void ShowErrors(IReadOnlyList<string> errors) => Errors.AddRange(errors);

        public bool Confirm(string question) => true;

        public void RaiseLoad() => LoadRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public SalesSummary Summary { get; } = new() { SalesCount = 3, Revenue = 30000m, UnitsSold = 9 };

        public Exception? NextFailure { get; set; }

        public DateRange SummaryPeriod { get; private set; }

        public DateRange PaymentPeriod { get; private set; }

        public DateRange TopPeriod { get; private set; }

        public int TopCount { get; private set; }

        public Task<SalesSummary> GetSalesSummaryAsync(DateRange period, CancellationToken cancellationToken = default)
        {
            if (NextFailure is { } failure)
            {
                throw failure;
            }

            SummaryPeriod = period;
            return Task.FromResult(Summary);
        }

        public Task<IReadOnlyList<ProductSales>> GetTopProductsAsync(DateRange period, int count, CancellationToken cancellationToken = default)
        {
            TopPeriod = period;
            TopCount = count;
            return Task.FromResult<IReadOnlyList<ProductSales>>([]);
        }

        public Task<IReadOnlyList<PaymentMethodSales>> GetSalesByPaymentMethodAsync(DateRange period, CancellationToken cancellationToken = default)
        {
            PaymentPeriod = period;
            return Task.FromResult<IReadOnlyList<PaymentMethodSales>>([]);
        }
    }
}
