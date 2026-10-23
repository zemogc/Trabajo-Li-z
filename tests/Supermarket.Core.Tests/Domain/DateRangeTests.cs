using Supermarket.Core.Domain;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Domain;

public class DateRangeTests
{
    [Fact]
    public void Today_starts_at_local_midnight_expressed_in_utc()
    {
        // 03:30 UTC on the 15th is still 22:30 of the 14th in Bogotá.
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 15, 3, 30, 0, TimeSpan.Zero), FakeClock.Bogota);

        var today = DateRange.LocalDays(clock);

        Assert.Equal(new DateTime(2026, 10, 14, 5, 0, 0, DateTimeKind.Utc), today.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 15, 5, 0, 0, DateTimeKind.Utc), today.ToUtc);
        Assert.Equal(DateTimeKind.Utc, today.FromUtc.Kind);
    }

    [Fact]
    public void Days_back_extend_the_start_and_keep_the_end_of_today()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 15, 18, 0, 0, TimeSpan.Zero), FakeClock.Bogota);

        var lastWeek = DateRange.LocalDays(clock, daysBack: 6);

        Assert.Equal(new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc), lastWeek.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 16, 5, 0, 0, DateTimeKind.Utc), lastWeek.ToUtc);
    }

    [Fact]
    public void Local_dates_include_both_days()
    {
        var september = DateRange.LocalDates(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), FakeClock.Bogota);

        Assert.Equal(new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc), september.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc), september.ToUtc);
    }

    [Fact]
    public void Local_dates_cannot_be_inverted() =>
        Assert.Throws<ArgumentException>(() =>
            DateRange.LocalDates(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1), FakeClock.Bogota));

    [Fact]
    public void Negative_days_are_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DateRange.LocalDays(TimeProvider.System, -1));

    [Fact]
    public void Average_ticket_handles_days_without_sales()
    {
        Assert.Equal(0m, new SalesSummary().AverageTicket);
        Assert.Equal(3333.33m, new SalesSummary { SalesCount = 3, Revenue = 10000m }.AverageTicket);
    }
}
