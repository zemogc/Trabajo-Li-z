using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Supermarket.WinForms.Infrastructure.Logging;

/// <summary>
/// Writes log entries to one file per day (supermarket-yyyyMMdd.log) and deletes files older than
/// the retention period, so support can ask for a file instead of reproducing a problem.
/// </summary>
/// <remarks>
/// Writes are synchronous and serialized with a lock: a desktop application logs little, and a
/// background queue would risk losing the last entries when the process crashes.
/// </remarks>
[ProviderAlias("File")]
internal sealed class FileLoggerProvider : ILoggerProvider
{
    public const string FilePrefix = "supermarket-";

    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private readonly TimeProvider _clock;
    private readonly int _retainedDays;
    private StreamWriter? _writer;
    private DateOnly _writerDay;

    public FileLoggerProvider(string directory, TimeProvider clock, int retainedDays = 14)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retainedDays);

        Directory = directory;
        _clock = clock;
        _retainedDays = retainedDays;
    }

    public string Directory { get; }

    /// <summary>The default location: %LOCALAPPDATA%\Supermarket\logs.</summary>
    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Supermarket", "logs");

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    internal static string FileNameFor(DateOnly day) =>
        $"{FilePrefix}{day.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log";

    internal void Write(string category, LogLevel level, string message, Exception? exception)
    {
        var now = _clock.GetLocalNow();
        var entry = new StringBuilder()
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
            .Append(" [").Append(Abbreviate(level)).Append("] ")
            .Append(category).Append(": ")
            .Append(message);

        if (exception is not null)
        {
            entry.AppendLine().Append(exception);
        }

        lock (_gate)
        {
            try
            {
                WriterFor(DateOnly.FromDateTime(now.DateTime)).WriteLine(entry);
            }
            catch (IOException)
            {
                // Logging must never take the application down (full disk, file locked by an editor...).
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private StreamWriter WriterFor(DateOnly day)
    {
        if (_writer is not null && day == _writerDay)
        {
            return _writer;
        }

        _writer?.Dispose();
        System.IO.Directory.CreateDirectory(Directory);
        DeleteExpiredFiles(day);

        var stream = new FileStream(Path.Combine(Directory, FileNameFor(day)), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
        _writerDay = day;
        return _writer;
    }

    private void DeleteExpiredFiles(DateOnly today)
    {
        var oldestKept = FileNameFor(today.AddDays(-(_retainedDays - 1)));
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, $"{FilePrefix}*.log"))
        {
            // File names sort chronologically, so a plain comparison finds the expired ones.
            if (string.CompareOrdinal(Path.GetFileName(file), oldestKept) < 0)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private static string Abbreviate(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "---",
    };

    private sealed class FileLogger(string category, FileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (IsEnabled(logLevel))
            {
                provider.Write(category, logLevel, formatter(state, exception), exception);
            }
        }
    }
}
