namespace Supermarket.Core.Domain;

/// <summary>Totals of the valid (not voided) sales within a period.</summary>
public sealed class SalesSummary
{
    public int SalesCount { get; set; }

    public decimal Revenue { get; set; }

    public int UnitsSold { get; set; }

    public decimal AverageTicket => SalesCount == 0 ? 0m : Math.Round(Revenue / SalesCount, 2);
}

/// <summary>Units and revenue of one product within a period.</summary>
public sealed class ProductSales
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Units { get; set; }

    public decimal Revenue { get; set; }
}

/// <summary>How much was collected with one payment method within a period.</summary>
public sealed class PaymentMethodSales
{
    public string PaymentMethodName { get; set; } = string.Empty;

    public int SalesCount { get; set; }

    public decimal Total { get; set; }
}

/// <summary>A half-open period [<see cref="FromUtc"/>, <see cref="ToUtc"/>) in UTC.</summary>
public readonly record struct DateRange(DateTime FromUtc, DateTime ToUtc)
{
    /// <summary>
    /// The local calendar days from <paramref name="daysBack"/> days ago until the end of today,
    /// converted to UTC because sales are stored in UTC.
    /// </summary>
    public static DateRange LocalDays(TimeProvider clock, int daysBack = 0)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfNegative(daysBack);

        var zone = clock.LocalTimeZone;
        var today = clock.GetLocalNow().Date;

        return new DateRange(
            StartOfLocalDayInUtc(today.AddDays(-daysBack), zone),
            StartOfLocalDayInUtc(today.AddDays(1), zone));
    }

    private static DateTime StartOfLocalDayInUtc(DateTime localMidnight, TimeZoneInfo zone)
    {
        // Where daylight saving starts at midnight, that midnight does not exist: the day begins an hour later.
        var start = zone.IsInvalidTime(localMidnight) ? localMidnight.AddHours(1) : localMidnight;
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(start, DateTimeKind.Unspecified), zone);
    }
}
