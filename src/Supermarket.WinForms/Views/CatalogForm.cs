using Supermarket.Core.Domain;

namespace Supermarket.WinForms.Views;

/// <summary>Screen for categories, providers and payment methods.</summary>
internal sealed class CatalogForm<TItem> : CrudForm<TItem>
    where TItem : CatalogItem, new()
{
    private readonly TextBox _name = new() { MaxLength = CatalogItem.NameMaxLength };
    private readonly TextBox _observation = new()
    {
        MaxLength = CatalogItem.ObservationMaxLength,
        Multiline = true,
        Height = 90,
        ScrollBars = ScrollBars.Vertical,
    };

    public CatalogForm(string title)
        : base(title)
    {
        AddColumn(nameof(CatalogItem.Id), "Código", fillWeight: 15, minimumWidth: 90);
        AddColumn(nameof(CatalogItem.Name), "Nombre", fillWeight: 35);
        AddColumn(nameof(CatalogItem.Observation), "Observación", fillWeight: 50);

        AddField("Nombre", _name);
        AddField("Observación (opcional)", _observation);
    }

    protected override void FillEditor(TItem entity)
    {
        _name.Text = entity.Name;
        _observation.Text = entity.Observation;
    }

    protected override TItem ReadEditorValues() => new()
    {
        Name = _name.Text,
        Observation = _observation.Text,
    };
}
