using Microsoft.Extensions.Logging;

namespace Supermarket.WinForms.Infrastructure;

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled UI exception")]
    public static partial void UnhandledUiException(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled exception, the application will close")]
    public static partial void UnhandledException(this ILogger logger, Exception? exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Database initialization failed")]
    public static partial void DatabaseInitializationFailed(this ILogger logger, Exception exception);
}
