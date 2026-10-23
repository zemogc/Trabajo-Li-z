using Supermarket.Core.Domain;

namespace Supermarket.Core.Presentation;

/// <summary>
/// Passive view for screens that list, search, create, edit and delete one kind of entity.
/// </summary>
/// <remarks>
/// The view only raises user intents and renders what the presenter tells it to. It must not
/// talk to repositories or contain business rules, which keeps presenters testable without a UI.
/// </remarks>
public interface ICrudView<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>Raised when the screen becomes visible and its data should be (re)loaded.</summary>
    event EventHandler? LoadRequested;

    event EventHandler? SearchRequested;

    event EventHandler? CreateRequested;

    event EventHandler? EditRequested;

    event EventHandler? DeleteRequested;

    event EventHandler? SaveRequested;

    event EventHandler? CancelRequested;

    string SearchTerm { get; }

    TEntity? SelectedEntity { get; }

    void DisplayEntities(IReadOnlyList<TEntity> entities);

    /// <summary>Shows the editor populated with <paramref name="entity"/>.</summary>
    void OpenEditor(TEntity entity);

    /// <summary>Builds an entity from the values currently typed in the editor.</summary>
    TEntity ReadEditor();

    void CloseEditor();

    void SetBusy(bool isBusy);

    void ShowMessage(string message);

    void ShowErrors(IReadOnlyList<string> errors);

    bool Confirm(string question);
}
