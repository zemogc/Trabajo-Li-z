using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;

namespace Supermarket.Core.Presentation;

/// <summary>
/// Runs presenter operations with a consistent busy state and error handling.
/// </summary>
/// <remarks>
/// Expected failures (rule violations, data conflicts, concurrent deletions) are shown with their own
/// message. Anything else is logged and reported with a generic message, so no exception ever
/// reaches the UI thread from an async event handler.
/// </remarks>
public abstract partial class PresenterBase
{
    internal const string NotFoundMessage = "El registro ya no existe; es posible que otro usuario lo haya eliminado. La lista se actualizó.";
    internal const string UnexpectedErrorMessage = "Ocurrió un error inesperado. Revise la conexión con la base de datos e intente de nuevo.";

    private readonly IOperationView _view;
    private readonly ILogger _logger;
    private bool _isBusy;

    protected PresenterBase(IOperationView view, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(logger);

        _view = view;
        _logger = logger;
    }

    /// <summary>Name used in log entries to identify what the presenter works with.</summary>
    protected virtual string Subject => GetType().Name;

    /// <param name="operation">The work to run.</param>
    /// <param name="onNotFound">Recovery when the record no longer exists, such as closing an editor and refreshing.</param>
    protected async Task RunAsync(Func<Task> operation, Func<Task>? onNotFound = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Ignore double clicks and repeated events while a database call is still running.
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        _view.SetBusy(true);
        try
        {
            await operation();
        }
        catch (BusinessRuleException ex)
        {
            _view.ShowErrors([ex.Message]);
        }
        catch (DataConflictException ex)
        {
            _view.ShowErrors([ex.Message]);
        }
        catch (EntityNotFoundException ex)
        {
            LogNotFound(ex, Subject);
            _view.ShowErrors([NotFoundMessage]);
            if (onNotFound is not null)
            {
                await TryAsync(onNotFound);
            }
        }
        catch (Exception ex)
        {
            LogUnexpectedError(ex, Subject);
            _view.ShowErrors([UnexpectedErrorMessage]);
        }
        finally
        {
            _isBusy = false;
            _view.SetBusy(false);
        }
    }

    private async Task TryAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            LogUnexpectedError(ex, Subject);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrent change detected while working with {Subject}")]
    private partial void LogNotFound(Exception exception, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error while working with {Subject}")]
    private partial void LogUnexpectedError(Exception exception, string subject);
}
