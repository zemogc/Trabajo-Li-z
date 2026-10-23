using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;

namespace Supermarket.Core.Tests.Fakes;

/// <summary>
/// Records everything the presenter asks the view to do, and lets tests simulate user input.
/// </summary>
internal class FakeCrudView<TEntity> : ICrudView<TEntity>
    where TEntity : class, IEntity
{
    public event EventHandler? LoadRequested;
    public event EventHandler? SearchRequested;
    public event EventHandler? CreateRequested;
    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public string SearchTerm { get; set; } = string.Empty;

    public TEntity? SelectedEntity { get; set; }

    /// <summary>What <see cref="ReadEditor"/> returns, i.e. what the user typed.</summary>
    public TEntity? EditorInput { get; set; }

    public bool ConfirmAnswer { get; set; } = true;

    public IReadOnlyList<TEntity> Displayed { get; private set; } = [];

    public TEntity? EditorEntity { get; private set; }

    public bool IsEditorOpen { get; private set; }

    public List<bool> BusyChanges { get; } = [];

    public List<string> Messages { get; } = [];

    public List<string> Errors { get; } = [];

    public List<string> Questions { get; } = [];

    public void DisplayEntities(IReadOnlyList<TEntity> entities) => Displayed = entities;

    public void OpenEditor(TEntity entity)
    {
        EditorEntity = entity;
        IsEditorOpen = true;
    }

    public TEntity ReadEditor() =>
        EditorInput ?? throw new InvalidOperationException("Set EditorInput before saving.");

    public void CloseEditor()
    {
        EditorEntity = null;
        IsEditorOpen = false;
    }

    public void SetBusy(bool isBusy) => BusyChanges.Add(isBusy);

    public void ShowMessage(string message) => Messages.Add(message);

    public void ShowErrors(IReadOnlyList<string> errors) => Errors.AddRange(errors);

    public bool Confirm(string question)
    {
        Questions.Add(question);
        return ConfirmAnswer;
    }

    public void RaiseLoad() => LoadRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSearch() => SearchRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseCreate() => CreateRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseEdit() => EditRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseDelete() => DeleteRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSave() => SaveRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseCancel() => CancelRequested?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeProductView : FakeCrudView<Product>, IProductView
{
    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public IReadOnlyList<Provider> Providers { get; private set; } = [];

    public void DisplayCategories(IReadOnlyList<Category> categories) => Categories = categories;

    public void DisplayProviders(IReadOnlyList<Provider> providers) => Providers = providers;
}
