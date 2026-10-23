namespace Supermarket.Core.Tests.Fakes;

/// <summary>A fixed clock in a fixed time zone, so date calculations are deterministic.</summary>
internal sealed class FakeClock(DateTimeOffset utcNow, TimeZoneInfo zone) : TimeProvider
{
    /// <summary>UTC-5 without daylight saving, like Bogotá.</summary>
    public static readonly TimeZoneInfo Bogota =
        TimeZoneInfo.CreateCustomTimeZone("Test/Bogota", TimeSpan.FromHours(-5), "Bogotá", "Bogotá");

    public override TimeZoneInfo LocalTimeZone => zone;

    public override DateTimeOffset GetUtcNow() => utcNow;
}
