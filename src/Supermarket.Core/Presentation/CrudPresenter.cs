using Microsoft.Extensions.Logging;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Validation;

namespace Supermarket.Core.Presentation;

/// <summary>
/// Coordinates an <see cref="ICrudView{TEntity}"/> with its <see cref="IRepository{TEntity}"/>.
/// </summary>
public partial class CrudPresenter<TEntity>
    where TEntity : class, IEntity, new()
{
    internal const string SelectionRequiredMessage = "Seleccione un registro de la lista.";
    internal const string SavedMessage = "Registro guardado correctamente.";
    internal const string DeletedMessage = "Registro eliminado correctamente.";
    internal const string NotFoundMessage = "El registro ya no existe; es posible que otro usuario lo haya eliminado. La lista se actualizó.";
    internal const string UnexpectedErrorMessage = "Ocurrió un error inesperado. Revise la conexión con la base de datos e intente de nuevo.";

    internal static string NoResultsMessage(string term) => $"No se encontraron resultados para \"{term}\".";

    private readonly ICrudView<TEntity> _view;
    private readonly IRepository<TEntity> _repository;
    private readonly ILogger _logger;
    private TEntity? _editing;
    private bool _isBusy;

    public CrudPresenter(ICrudView<TEntity> view, IRepository<TEntity> repository, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _view = view;
        _repository = repository;
        _logger = logger;

        // Async event handlers are the one place where async void is appropriate; every
        // operation below handles its own exceptions so nothing escapes to the UI thread.
        _view.LoadRequested += async (_, _) => await LoadAsync();
        _view.SearchRequested += async (_, _) => await SearchAsync();
        _view.CreateRequested += (_, _) => BeginCreate();
        _view.EditRequested += (_, _) => BeginEdit();
        _view.DeleteRequested += async (_, _) => await DeleteAsync();
        _view.SaveRequested += async (_, _) => await SaveAsync();
        _view.CancelRequested += (_, _) => CancelEdit();
    }

    /// <summary>The entity currently open in the editor, or <see langword="null"/> when no edit is in progress.</summary>
    public TEntity? Editing => _editing;

    public Task LoadAsync() => RunAsync(async () =>
    {
        await OnLoadingAsync();
        await RefreshAsync();
    });

    public Task SearchAsync() => RunAsync(async () =>
    {
        var found = await RefreshAsync();
        var term = _view.SearchTerm.Trim();
        if (found == 0 && term.Length > 0)
        {
            _view.ShowMessage(NoResultsMessage(term));
        }
    });

    public void BeginCreate()
    {
        _editing = new TEntity();
        _view.OpenEditor(_editing);
    }

    public void BeginEdit()
    {
        var selected = _view.SelectedEntity;
        if (selected is null)
        {
            _view.ShowMessage(SelectionRequiredMessage);
            return;
        }

        _editing = selected;
        _view.OpenEditor(selected);
    }

    public void CancelEdit()
    {
        _editing = null;
        _view.CloseEditor();
    }

    public Task SaveAsync()
    {
        if (_editing is null)
        {
            return Task.CompletedTask;
        }

        var entity = _view.ReadEditor();
        entity.Id = _editing.Id;
        Normalize(entity);

        var errors = EntityValidator.Validate(entity);
        if (errors.Count > 0)
        {
            _view.ShowErrors(errors);
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            if (entity.IsNew)
            {
                entity.Id = await _repository.AddAsync(entity);
                LogCreated(typeof(TEntity).Name, entity.Id);
            }
            else
            {
                await _repository.UpdateAsync(entity);
                LogUpdated(typeof(TEntity).Name, entity.Id);
            }

            _editing = null;
            _view.CloseEditor();
            await RefreshAsync();
            _view.ShowMessage(SavedMessage);
        });
    }

    public Task DeleteAsync()
    {
        var selected = _view.SelectedEntity;
        if (selected is null)
        {
            _view.ShowMessage(SelectionRequiredMessage);
            return Task.CompletedTask;
        }

        if (!_view.Confirm($"¿Desea eliminar \"{selected.Name}\"? Esta acción no se puede deshacer."))
        {
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            await _repository.DeleteAsync(selected.Id);
            LogDeleted(typeof(TEntity).Name, selected.Id);

            if (_editing?.Id == selected.Id)
            {
                CancelEdit();
            }

            await RefreshAsync();
            _view.ShowMessage(DeletedMessage);
        });
    }

    /// <summary>Loads any additional data the screen needs, such as lookup lists.</summary>
    protected virtual Task OnLoadingAsync() => Task.CompletedTask;

    /// <summary>Cleans user input (trimming, empty to null...) before it is validated and saved.</summary>
    protected virtual void Normalize(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.Name = entity.Name.Trim();
    }

    /// <returns>The number of entities displayed.</returns>
    private async Task<int> RefreshAsync()
    {
        var term = _view.SearchTerm.Trim();
        var entities = term.Length == 0
            ? await _repository.GetAllAsync()
            : await _repository.SearchAsync(term);

        _view.DisplayEntities(entities);
        return entities.Count;
    }

    private async Task RunAsync(Func<Task> operation)
    {
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
        catch (DataConflictException ex)
        {
            _view.ShowErrors([ex.Message]);
        }
        catch (EntityNotFoundException ex)
        {
            LogNotFound(ex);
            CancelEdit();
            _view.ShowErrors([NotFoundMessage]);
            await TryRefreshAsync();
        }
        catch (Exception ex)
        {
            LogUnexpectedError(ex, typeof(TEntity).Name);
            _view.ShowErrors([UnexpectedErrorMessage]);
        }
        finally
        {
            _isBusy = false;
            _view.SetBusy(false);
        }
    }

    private async Task TryRefreshAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            LogUnexpectedError(ex, typeof(TEntity).Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{EntityType} {Id} created")]
    private partial void LogCreated(string entityType, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "{EntityType} {Id} updated")]
    private partial void LogUpdated(string entityType, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "{EntityType} {Id} deleted")]
    private partial void LogDeleted(string entityType, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrent change detected")]
    private partial void LogNotFound(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error while working with {EntityType}")]
    private partial void LogUnexpectedError(Exception exception, string entityType);
}
