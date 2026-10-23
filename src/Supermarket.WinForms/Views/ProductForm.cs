using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;

namespace Supermarket.WinForms.Views;

internal sealed class ProductForm : CrudForm<Product>, IProductView
{
    private readonly TextBox _name = new() { MaxLength = Product.NameMaxLength };
    private readonly NumericUpDown _price = new()
    {
        DecimalPlaces = 2,
        Maximum = (decimal)Product.MaxPrice,
        ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right,
    };

    private readonly NumericUpDown _stock = new()
    {
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right,
    };

    private readonly NumericUpDown _minimumStock = new()
    {
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right,
    };

    private readonly ComboBox _category = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(Category.Name),
        ValueMember = nameof(Category.Id),
    };

    public ProductForm()
        : base("Productos")
    {
        AddColumn(nameof(Product.Id), "Código", fillWeight: 10, minimumWidth: 90);
        AddColumn(nameof(Product.Name), "Nombre", fillWeight: 34, minimumWidth: 140);
        AddColumn(nameof(Product.CategoryName), "Categoría", fillWeight: 20, minimumWidth: 90);
        AddColumn(nameof(Product.Price), "Precio", fillWeight: 14, format: "N2", alignRight: true, minimumWidth: 100);
        AddColumn(nameof(Product.Stock), "Stock", fillWeight: 10, format: "N0", alignRight: true, minimumWidth: 80);
        AddColumn(nameof(Product.MinimumStock), "Mínimo", fillWeight: 10, format: "N0", alignRight: true, minimumWidth: 90);
        HighlightRowsWhen(product => product.IsLowStock);

        AddField("Nombre", _name);
        AddField("Categoría", _category);
        AddField("Precio", _price);
        AddField("Stock", _stock);
        AddField("Stock mínimo (alerta de reposición)", _minimumStock);
    }

    public void DisplayCategories(IReadOnlyList<Category> categories)
    {
        // Keep the current choice when the list is refreshed while the editor is open.
        var selected = _category.SelectedValue;
        _category.DataSource = categories.ToList();
        _category.SelectedValue = selected ?? -1;
    }

    protected override string DescribeEntities(IReadOnlyList<Product> entities)
    {
        var description = base.DescribeEntities(entities);
        var lowStock = entities.Count(p => p.IsLowStock);
        return lowStock == 0 ? description : $"{description} · {lowStock} con stock bajo";
    }

    protected override void FillEditor(Product entity)
    {
        _name.Text = entity.Name;
        _price.Value = Math.Clamp(entity.Price, _price.Minimum, _price.Maximum);
        _stock.Value = Math.Clamp(entity.Stock, _stock.Minimum, _stock.Maximum);
        _minimumStock.Value = Math.Clamp(entity.MinimumStock, _minimumStock.Minimum, _minimumStock.Maximum);

        if (entity.IsNew)
        {
            _category.SelectedIndex = -1;
        }
        else
        {
            _category.SelectedValue = entity.CategoryId;
        }
    }

    protected override Product ReadEditorValues() => new()
    {
        Name = _name.Text,
        Price = _price.Value,
        Stock = (int)_stock.Value,
        MinimumStock = (int)_minimumStock.Value,
        CategoryId = _category.SelectedValue is int id ? id : 0,
    };
}
