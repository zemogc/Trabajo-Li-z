using Microsoft.Extensions.Logging.Abstractions;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.Core.Tests.Fakes;

namespace Supermarket.Core.Tests.Presentation;

public class CatalogPresenterTests
{
    private readonly FakeCrudView<Category> _view = new();
    private readonly InMemoryRepository<Category> _repository = new InMemoryRepository<Category>()
        .With(new Category { Name = "Bebidas" }, new Category { Name = "Lácteos" }, new Category { Name = "Aseo" });

    private readonly CatalogPresenter<Category> _presenter;

    public CatalogPresenterTests() =>
        _presenter = new CatalogPresenter<Category>(_view, _repository, NullLogger<CatalogPresenter<Category>>.Instance);

    [Fact]
    public async Task Load_displays_all_entities_and_toggles_busy_state()
    {
        await _presenter.LoadAsync();

        Assert.Equal(3, _view.Displayed.Count);
        Assert.Equal([true, false], _view.BusyChanges);
    }

    [Fact]
    public async Task Search_filters_by_name_or_id()
    {
        _view.SearchTerm = "  lác ";
        await _presenter.SearchAsync();
        Assert.Equal("Lácteos", Assert.Single(_view.Displayed).Name);

        _view.SearchTerm = "3";
        await _presenter.SearchAsync();
        Assert.Equal("Aseo", Assert.Single(_view.Displayed).Name);
    }

    [Fact]
    public async Task Search_without_matches_tells_the_user()
    {
        _view.SearchTerm = " xyz ";

        await _presenter.SearchAsync();

        Assert.Empty(_view.Displayed);
        Assert.Equal([CrudPresenter<Category>.NoResultsMessage("xyz")], _view.Messages);
    }

    [Fact]
    public async Task Blank_search_returns_everything()
    {
        _view.SearchTerm = "   ";

        await _presenter.SearchAsync();

        Assert.Equal(3, _view.Displayed.Count);
        Assert.Empty(_view.Messages);
    }

    [Fact]
    public async Task Creating_a_valid_entity_persists_it_and_closes_the_editor()
    {
        _presenter.BeginCreate();
        Assert.True(_view.IsEditorOpen);

        _view.EditorInput = new Category { Name = "  Panadería  ", Observation = "   " };
        await _presenter.SaveAsync();

        var saved = _repository.Items[^1];
        Assert.Equal(4, saved.Id);
        Assert.Equal("Panadería", saved.Name);
        Assert.Null(saved.Observation);
        Assert.False(_view.IsEditorOpen);
        Assert.Null(_presenter.Editing);
        Assert.Equal(4, _view.Displayed.Count);
        Assert.Contains(CrudPresenter<Category>.SavedMessage, _view.Messages);
    }

    [Fact]
    public async Task Invalid_input_is_reported_and_not_saved()
    {
        _presenter.BeginCreate();
        _view.EditorInput = new Category { Name = "x" };

        await _presenter.SaveAsync();

        Assert.Equal(3, _repository.Items.Count);
        Assert.Single(_view.Errors);
        Assert.True(_view.IsEditorOpen);
        Assert.Empty(_view.BusyChanges);
    }

    [Fact]
    public async Task Editing_updates_the_selected_entity()
    {
        _view.SelectedEntity = _repository.Items[0];
        _presenter.BeginEdit();
        Assert.Same(_repository.Items[0], _view.EditorEntity);

        _view.EditorInput = new Category { Name = "Bebidas frías", Observation = "Refrigerador 2" };
        await _presenter.SaveAsync();

        var updated = _repository.Items[0];
        Assert.Equal(1, updated.Id);
        Assert.Equal("Bebidas frías", updated.Name);
        Assert.Equal(3, _repository.Items.Count);
    }

    [Fact]
    public void Edit_without_selection_asks_the_user_to_select()
    {
        _presenter.BeginEdit();

        Assert.False(_view.IsEditorOpen);
        Assert.Equal([CrudPresenter<Category>.SelectionRequiredMessage], _view.Messages);
    }

    [Fact]
    public async Task Save_without_open_editor_does_nothing()
    {
        await _presenter.SaveAsync();

        Assert.Empty(_view.BusyChanges);
        Assert.Equal(3, _repository.Items.Count);
    }

    [Fact]
    public void Cancel_closes_the_editor_and_discards_the_edit()
    {
        _presenter.BeginCreate();

        _presenter.CancelEdit();

        Assert.False(_view.IsEditorOpen);
        Assert.Null(_presenter.Editing);
    }

    [Fact]
    public async Task Delete_removes_the_selected_entity_after_confirmation()
    {
        _view.SelectedEntity = _repository.Items[1];

        await _presenter.DeleteAsync();

        Assert.Contains("Lácteos", Assert.Single(_view.Questions), StringComparison.Ordinal);
        Assert.DoesNotContain(_repository.Items, c => c.Name == "Lácteos");
        Assert.Equal(2, _view.Displayed.Count);
        Assert.Contains(CrudPresenter<Category>.DeletedMessage, _view.Messages);
    }

    [Fact]
    public async Task Delete_is_skipped_when_the_user_declines()
    {
        _view.SelectedEntity = _repository.Items[1];
        _view.ConfirmAnswer = false;

        await _presenter.DeleteAsync();

        Assert.Equal(3, _repository.Items.Count);
        Assert.Empty(_view.BusyChanges);
    }

    [Fact]
    public async Task Delete_without_selection_does_not_ask_for_confirmation()
    {
        await _presenter.DeleteAsync();

        Assert.Empty(_view.Questions);
        Assert.Equal([CrudPresenter<Category>.SelectionRequiredMessage], _view.Messages);
    }

    [Fact]
    public async Task Data_conflicts_are_shown_with_their_own_message()
    {
        _view.SelectedEntity = _repository.Items[0];
        _repository.NextFailure = new DataConflictException("La categoría tiene productos asociados.");

        await _presenter.DeleteAsync();

        Assert.Equal(["La categoría tiene productos asociados."], _view.Errors);
        Assert.Equal(3, _repository.Items.Count);
        Assert.Equal([true, false], _view.BusyChanges);
    }

    [Fact]
    public async Task Concurrent_deletion_closes_the_editor_and_refreshes_the_list()
    {
        var stale = new Category { Name = "Fantasma" };
        stale.Id = 99;
        _view.SelectedEntity = stale;
        _presenter.BeginEdit();
        _view.EditorInput = new Category { Name = "Fantasma editado" };

        await _presenter.SaveAsync();

        Assert.Equal([CrudPresenter<Category>.NotFoundMessage], _view.Errors);
        Assert.False(_view.IsEditorOpen);
        Assert.Equal(3, _view.Displayed.Count);
    }

    [Fact]
    public async Task Unexpected_failures_show_a_generic_message_and_release_the_busy_state()
    {
        _repository.NextFailure = new TimeoutException("SQL Server did not respond");

        await _presenter.LoadAsync();

        Assert.Equal([CrudPresenter<Category>.UnexpectedErrorMessage], _view.Errors);
        Assert.Equal([true, false], _view.BusyChanges);
    }

    [Fact]
    public async Task View_events_are_routed_to_the_presenter()
    {
        _view.RaiseLoad();
        await Task.Yield();
        Assert.Equal(3, _view.Displayed.Count);

        _view.RaiseCreate();
        Assert.True(_view.IsEditorOpen);

        _view.RaiseCancel();
        Assert.False(_view.IsEditorOpen);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CatalogPresenter<Category>(null!, _repository, NullLogger<CatalogPresenter<Category>>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new CatalogPresenter<Category>(_view, null!, NullLogger<CatalogPresenter<Category>>.Instance));
    }
}
