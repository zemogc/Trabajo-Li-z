using Microsoft.Extensions.Logging;
using Supermarket.WinForms.Infrastructure.Logging;

namespace Supermarket.WinForms.Tests;

public sealed class FileLoggerProviderTests : IDisposable
{
    private static readonly TimeZoneInfo Bogota =
        TimeZoneInfo.CreateCustomTimeZone("Test/Bogota", TimeSpan.FromHours(-5), "Bogotá", "Bogotá");

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "supermarket-log-tests", Guid.NewGuid().ToString("N"));
    private readonly MutableClock _clock = new(new DateTimeOffset(2026, 10, 23, 15, 30, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Entries_include_local_time_level_category_and_message()
    {
        using (var provider = new FileLoggerProvider(_directory, _clock))
        {
            provider.CreateLogger("Supermarket.Sales").LogInformation("Sale {SaleId} registered", 42);
        }

        var line = Assert.Single(File.ReadAllLines(Path.Combine(_directory, "supermarket-20261023.log")));
        Assert.Equal("2026-10-23 10:30:00.000 -05:00 [INF] Supermarket.Sales: Sale 42 registered", line);
    }

    [Fact]
    public void Exceptions_are_written_with_their_stack_trace()
    {
        using (var provider = new FileLoggerProvider(_directory, _clock))
        {
            provider.CreateLogger("App").LogError(new InvalidOperationException("boom"), "Unexpected error");
        }

        var content = File.ReadAllText(Path.Combine(_directory, "supermarket-20261023.log"));
        Assert.Contains("[ERR] App: Unexpected error", content, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException: boom", content, StringComparison.Ordinal);
    }

    [Fact]
    public void A_new_file_is_started_each_local_day()
    {
        using (var provider = new FileLoggerProvider(_directory, _clock))
        {
            var logger = provider.CreateLogger("App");
            logger.LogInformation("first day");

            // 04:59 UTC on the 24th is still the 23rd in Bogotá; 05:00 is the 24th.
            _clock.UtcNow = new DateTimeOffset(2026, 10, 24, 4, 59, 0, TimeSpan.Zero);
            logger.LogInformation("still first day");
            _clock.UtcNow = new DateTimeOffset(2026, 10, 24, 5, 0, 0, TimeSpan.Zero);
            logger.LogInformation("second day");
        }

        Assert.Equal(2, File.ReadAllLines(Path.Combine(_directory, "supermarket-20261023.log")).Length);
        Assert.Single(File.ReadAllLines(Path.Combine(_directory, "supermarket-20261024.log")));
    }

    [Fact]
    public void Files_older_than_the_retention_period_are_deleted()
    {
        Directory.CreateDirectory(_directory);
        foreach (var name in new[] { "supermarket-20261020.log", "supermarket-20261021.log", "supermarket-20261022.log", "notes.log" })
        {
            File.WriteAllText(Path.Combine(_directory, name), "old");
        }

        using (var provider = new FileLoggerProvider(_directory, _clock, retainedDays: 3))
        {
            provider.CreateLogger("App").LogInformation("today");
        }

        var remaining = Directory.GetFiles(_directory).Select(Path.GetFileName).Order(StringComparer.Ordinal);
        Assert.Equal(["notes.log", "supermarket-20261021.log", "supermarket-20261022.log", "supermarket-20261023.log"], remaining);
    }

    [Fact]
    public void Loggers_are_reused_per_category_and_ignore_level_none()
    {
        using var provider = new FileLoggerProvider(_directory, _clock);

        Assert.Same(provider.CreateLogger("A"), provider.CreateLogger("A"));
        Assert.False(provider.CreateLogger("A").IsEnabled(LogLevel.None));
        Assert.True(provider.CreateLogger("A").IsEnabled(LogLevel.Trace));
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override TimeZoneInfo LocalTimeZone => Bogota;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
