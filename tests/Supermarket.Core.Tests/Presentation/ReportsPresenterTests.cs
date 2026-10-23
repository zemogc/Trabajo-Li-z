using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class ReportsPresenterTests
{
    // 18:00 UTC on October 23rd is 13:00 in Bogotá.
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeReportsView _view = new();
    private readonly RecordingReports _reports = new();
    private readonly ReportsPresenter _presenter;

    public ReportsPresenterTests() =>
        _presenter = new ReportsPresenter(_view, _reports, new FakeClock(Now, FakeClock.Bogota), NullLogger<ReportsPresenter>.Instance);

    [Fact]
    public async Task First_load_suggests_the_current_month()
    {
        await _presenter.LoadAsync();

        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 23)), (_view.FirstDay, _view.LastDay));
        Assert.Equal(new DateRange(new DateTime(2026, 10, 1, 5, 0, 0), new DateTime(2026, 10, 24, 5, 0, 0)), _reports.LastPeriod);
        Assert.Equal(1, _view.SummaryCount);
    }

    [Fact]
    public async Task Later_loads_keep_the_period_chosen_by_the_user()
    {
        await _presenter.LoadAsync();
        _view.SetPeriod(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        await _presenter.LoadAsync();

        Assert.Equal(new DateOnly(2026, 9, 1), _view.FirstDay);
        Assert.Equal(new DateTime(2026, 10, 1, 5, 0, 0), _reports.LastPeriod.ToUtc);
    }

    [Fact]
    public async Task Inverted_or_too_long_periods_are_rejected()
    {
        _view.SetPeriod(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 9));
        await _presenter.GenerateAsync();

        _view.SetPeriod(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 2));
        await _presenter.GenerateAsync();

        Assert.Equal([ReportsPresenter.InvalidPeriodMessage, ReportsPresenter.PeriodTooLongMessage], _view.Errors);
        Assert.Equal(0, _view.SummaryCount);
    }

    [Fact]
    public async Task A_full_year_is_allowed()
    {
        _view.SetPeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        await _presenter.GenerateAsync();

        Assert.Empty(_view.Errors);
        Assert.Equal(ReportsPresenter.TopProductsCount, _reports.TopCount);
    }

    private sealed class FakeReportsView : IReportsView
    {
        public event EventHandler? LoadRequested;
        public event EventHandler? GenerateRequested;

        public DateOnly FirstDay { get; private set; } = new(2026, 1, 1);

        public DateOnly LastDay { get; private set; } = new(2026, 1, 31);

        public int SummaryCount { get; private set; }

        public List<string> Errors { get; } = [];

        public void SetPeriod(DateOnly firstDay, DateOnly lastDay) => (FirstDay, LastDay) = (firstDay, lastDay);

        public void DisplaySummary(SalesSummary summary) => SummaryCount++;

        public void DisplaySales(IReadOnlyList<Sale> sales)
        {
        }

        public void DisplayTopProducts(IReadOnlyList<ProductSales> products)
        {
        }

        public void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods)
        {
        }

        public void SetBusy(bool isBusy)
        {
        }

        public void ShowMessage(string message)
        {
        }

        public void ShowErrors(IReadOnlyList<string> errors) => Errors.AddRange(errors);

        public bool Confirm(string question) => true;

        public void RaiseAll()
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
            GenerateRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class RecordingReports : IReportRepository
    {
        public DateRange LastPeriod { get; private set; }

        public int TopCount { get; private set; }

        public Task<SalesSummary> GetSalesSummaryAsync(DateRange period, CancellationToken cancellationToken = default)
        {
            LastPeriod = period;
            return Task.FromResult(new SalesSummary());
        }

        public Task<IReadOnlyList<ProductSales>> GetTopProductsAsync(DateRange period, int count, CancellationToken cancellationToken = default)
        {
            TopCount = count;
            return Task.FromResult<IReadOnlyList<ProductSales>>([]);
        }

        public Task<IReadOnlyList<Sale>> GetSalesAsync(DateRange period, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Sale>>([]);

        public Task<IReadOnlyList<PaymentMethodSales>> GetSalesByPaymentMethodAsync(DateRange period, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentMethodSales>>([]);
    }
}
