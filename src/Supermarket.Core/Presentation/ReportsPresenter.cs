using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

public sealed class ReportsPresenter : PresenterBase
{
    internal const int TopProductsCount = 20;

    /// <summary>A little over a year keeps queries fast and covers annual comparisons.</summary>
    internal const int MaxDays = 366;

    internal const string InvalidPeriodMessage = "La fecha final no puede ser anterior a la inicial.";
    internal static readonly string PeriodTooLongMessage = $"El período no puede superar {MaxDays} días.";

    private readonly IReportsView _view;
    private readonly IReportRepository _reports;
    private readonly TimeProvider _clock;
    private bool _initialized;

    public ReportsPresenter(IReportsView view, IReportRepository reports, TimeProvider clock, ILogger<ReportsPresenter> logger)
        : base(view, logger)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(clock);

        _view = view;
        _reports = reports;
        _clock = clock;

        _view.LoadRequested += async (_, _) => await LoadAsync();
        _view.GenerateRequested += async (_, _) => await GenerateAsync();
    }

    /// <summary>The first time the screen opens it suggests the current month; afterwards it keeps the user's period.</summary>
    public Task LoadAsync()
    {
        if (!_initialized)
        {
            var today = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
            _view.SetPeriod(new DateOnly(today.Year, today.Month, 1), today);
            _initialized = true;
        }

        return GenerateAsync();
    }

    public Task GenerateAsync()
    {
        var first = _view.FirstDay;
        var last = _view.LastDay;

        if (last < first)
        {
            _view.ShowErrors([InvalidPeriodMessage]);
            return Task.CompletedTask;
        }

        if (last.DayNumber - first.DayNumber + 1 > MaxDays)
        {
            _view.ShowErrors([PeriodTooLongMessage]);
            return Task.CompletedTask;
        }

        var period = DateRange.LocalDates(first, last, _clock.LocalTimeZone);
        return RunAsync(async () =>
        {
            _view.DisplaySummary(await _reports.GetSalesSummaryAsync(period));
            _view.DisplayPaymentBreakdown(await _reports.GetSalesByPaymentMethodAsync(period));
            _view.DisplayTopProducts(await _reports.GetTopProductsAsync(period, TopProductsCount));
            _view.DisplaySales(await _reports.GetSalesAsync(period));
        });
    }
}
