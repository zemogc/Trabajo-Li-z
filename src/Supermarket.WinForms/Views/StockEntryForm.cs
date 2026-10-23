using System.ComponentModel;
using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>Receiving screen: the entry in progress on the left and the latest entries on the right.</summary>
internal sealed class StockEntryForm : Form, IStockEntryView
{
    // Id 0 stands for "no provider" in the selector and is reported as null.
    private const int NoProviderId = 0;

    private readonly ComboBox _product = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FormattingEnabled = true,
        Width = 460,
        Margin = new Padding(0, 4, 8, 0),
    };

    private readonly NumericUpDown _quantity = new()
    {
        Minimum = 1,
        Maximum = 100_000,
        Width = 80,
        TextAlign = HorizontalAlignment.Right,
        Margin = new Padding(0, 4, 8, 0),
    };

    private readonly NumericUpDown _unitCost = new()
    {
        DecimalPlaces = 2,
        Maximum = (decimal)Product.MaxPrice,
        ThousandsSeparator = true,
        Width = 120,
        TextAlign = HorizontalAlignment.Right,
        Margin = new Padding(0, 4, 8, 0),
    };

    private readonly ComboBox _provider = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(Provider.Name),
        ValueMember = nameof(Provider.Id),
        Width = 240,
        Margin = new Padding(0, 6, 16, 0),
    };

    private readonly TextBox _notes = new()
    {
        MaxLength = StockEntry.NotesMaxLength,
        PlaceholderText = "Observaciones (factura, remisión...)",
        Width = 300,
        Margin = new Padding(0, 6, 0, 0),
    };

    private readonly DataGridView _lines = Ui.Grid();
    private readonly DataGridView _history = Ui.Grid();
    private readonly DataGridView _historyLines = Ui.Grid();
    private readonly Label _total = new() { AutoSize = true, Font = Theme.TitleFont, Margin = new Padding(0, 0, 16, 0) };
    private readonly Panel _draft = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 10, 16) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Right, Width = 480, Padding = new Padding(10, 0, 20, 16) };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private bool _bindingHistory;

    public StockEntryForm()
    {
        Text = "Entradas de mercancía";
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;

        BuildDraft();
        BuildSidebar();

        Controls.Add(_draft);
        Controls.Add(_sidebar);
        Controls.Add(Ui.Header("Entradas de mercancía"));

        var statusBar = new StatusStrip { SizingGrip = false, BackColor = Theme.Surface };
        statusBar.Items.Add(_status);
        Controls.Add(statusBar);

        DisplayDraft([], 0m);
    }

    public event EventHandler? LoadRequested;

    public event EventHandler? AddItemRequested;

    public event EventHandler? RemoveItemRequested;

    public event EventHandler? ClearRequested;

    public event EventHandler? RegisterRequested;

    public event EventHandler? EntrySelected;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Product? SelectedProduct => _product.SelectedItem as Product;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Quantity => (int)_quantity.Value;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal UnitCost => _unitCost.Value;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedLineProductId => (_lines.CurrentRow?.DataBoundItem as StockEntryLine)?.ProductId;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedProviderId => _provider.SelectedValue is int id and not NoProviderId ? id : null;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Notes => _notes.Text;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedEntryId =>
        _history.CurrentRow is { Selected: true, DataBoundItem: StockEntry entry } ? entry.Id : null;

    public void DisplayProducts(IReadOnlyList<Product> products)
    {
        var selectedId = SelectedProduct?.Id;
        _product.DataSource = products.ToList();

        var index = products.ToList().FindIndex(p => p.Id == selectedId);
        _product.SelectedIndex = index >= 0 ? index : -1;
    }

    public void DisplayProviders(IReadOnlyList<Provider> providers)
    {
        var selected = SelectedProviderId;
        _provider.DataSource = providers
            .Prepend(new Provider { Id = NoProviderId, Name = "(Sin proveedor)" })
            .ToList();
        _provider.SelectedValue = selected ?? NoProviderId;
    }

    public void DisplayDraft(IReadOnlyList<StockEntryLine> lines, decimal totalCost)
    {
        _lines.DataSource = new BindingList<StockEntryLine>([.. lines]);
        _total.Text = $"Costo total {totalCost.ToString("C2", CultureInfo.CurrentCulture)}";
    }

    public void ResetItemInput(bool clearNotes)
    {
        _quantity.Value = 1;
        _unitCost.Value = 0;
        if (clearNotes)
        {
            _notes.Clear();
        }

        _product.Focus();
    }

    public void DisplayRecentEntries(IReadOnlyList<StockEntry> entries)
    {
        _bindingHistory = true;
        try
        {
            _history.DataSource = new BindingList<StockEntry>([.. entries]);
            _history.ClearSelection();
        }
        finally
        {
            _bindingHistory = false;
        }

        DisplayEntryLines([]);
    }

    public void DisplayEntryLines(IReadOnlyList<StockEntryLine> lines) =>
        _historyLines.DataSource = new BindingList<StockEntryLine>([.. lines]);

    public void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;
        _draft.Enabled = !isBusy;
        _sidebar.Enabled = !isBusy;
    }

    public void ShowMessage(string message) => _status.Text = message;

    public void ShowErrors(IReadOnlyList<string> errors)
    {
        _status.Text = string.Empty;
        Ui.ShowErrors(this, errors);
    }

    public bool Confirm(string question) => Ui.Confirm(this, question);

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible)
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.F9 or (Keys.Control | Keys.Enter))
        {
            RegisterRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildDraft()
    {
        _product.Format += (_, e) =>
        {
            if (e.ListItem is Product product)
            {
                e.Value = string.Create(CultureInfo.CurrentCulture, $"{product.Name}  ·  {product.Stock:N0} en stock");
            }
        };

        foreach (var input in new Control[] { _quantity, _unitCost })
        {
            input.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddItemRequested?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        // Product on its own row; quantity, cost and the add button below it, so both fit the default width.
        var productRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(0, 0, 0, 6) };
        productRow.Controls.Add(Hint("Producto"));
        productRow.Controls.Add(_product);

        var itemRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(0, 0, 0, 12) };
        itemRow.Controls.Add(Hint("Cantidad"));
        itemRow.Controls.Add(_quantity);
        itemRow.Controls.Add(Hint("Costo unitario"));
        itemRow.Controls.Add(_unitCost);
        itemRow.Controls.Add(Ui.Button("Agregar", Glyph.Add, (_, _) => AddItemRequested?.Invoke(this, EventArgs.Empty)));

        _lines.Columns.Add(Ui.Column(nameof(StockEntryLine.ProductName), "Producto", fillWeight: 46, minimumWidth: 140, sortable: false));
        _lines.Columns.Add(Ui.Column(nameof(StockEntryLine.Quantity), "Cantidad", fillWeight: 14, format: "N0", alignRight: true, minimumWidth: 85, sortable: false));
        _lines.Columns.Add(Ui.Column(nameof(StockEntryLine.UnitCost), "Costo unit.", fillWeight: 20, format: "N2", alignRight: true, minimumWidth: 100, sortable: false));
        _lines.Columns.Add(Ui.Column(nameof(StockEntryLine.Subtotal), "Subtotal", fillWeight: 20, format: "N2", alignRight: true, minimumWidth: 105, sortable: false));
        _lines.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                RemoveItemRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        var lineActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        lineActions.Controls.Add(Ui.Button("Quitar", Glyph.Delete, (_, _) => RemoveItemRequested?.Invoke(this, EventArgs.Empty)));
        lineActions.Controls.Add(Ui.Button("Vaciar", Glyph.Cancel, (_, _) => ClearRequested?.Invoke(this, EventArgs.Empty)));

        var detailsRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = false, Padding = new Padding(0, 12, 0, 0) };
        detailsRow.Controls.Add(new Label { Text = "Proveedor", AutoSize = true, ForeColor = Theme.MutedText, Margin = new Padding(0, 10, 8, 0) });
        detailsRow.Controls.Add(_provider);
        detailsRow.Controls.Add(_notes);

        var registerRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        var register = Ui.Button("Registrar entrada (F9)", Glyph.Save, (_, _) => RegisterRequested?.Invoke(this, EventArgs.Empty), primary: true);
        register.Margin = new Padding(0);
        registerRow.Controls.Add(register);
        registerRow.Controls.Add(_total);

        _draft.Controls.Add(_lines);
        _draft.Controls.Add(lineActions);
        _draft.Controls.Add(detailsRow);
        _draft.Controls.Add(registerRow);
        _draft.Controls.Add(itemRow);
        _draft.Controls.Add(productRow);
        _draft.Controls.Add(Ui.Section("Nueva entrada"));
    }

    private void BuildSidebar()
    {
        _history.Columns.Add(Ui.Column(nameof(StockEntry.Id), "N.º", fillWeight: 10, minimumWidth: 55));
        _history.Columns.Add(Ui.Column(nameof(StockEntry.CreatedAtLocal), "Fecha", fillWeight: 34, format: "dd/MM/yyyy HH:mm", minimumWidth: 140));
        _history.Columns.Add(Ui.Column(nameof(StockEntry.ProviderName), "Proveedor", fillWeight: 30, minimumWidth: 100));
        _history.Columns.Add(Ui.Column(nameof(StockEntry.TotalCost), "Costo", fillWeight: 26, format: "N2", alignRight: true, minimumWidth: 100));
        _history.SelectionChanged += (_, _) =>
        {
            if (!_bindingHistory)
            {
                EntrySelected?.Invoke(this, EventArgs.Empty);
            }
        };

        _historyLines.Columns.Add(Ui.Column(nameof(StockEntryLine.ProductName), "Producto", fillWeight: 50, minimumWidth: 120, sortable: false));
        _historyLines.Columns.Add(Ui.Column(nameof(StockEntryLine.Quantity), "Cant.", fillWeight: 16, format: "N0", alignRight: true, minimumWidth: 60, sortable: false));
        _historyLines.Columns.Add(Ui.Column(nameof(StockEntryLine.Subtotal), "Subtotal", fillWeight: 34, format: "N2", alignRight: true, minimumWidth: 100, sortable: false));

        var details = new Panel { Dock = DockStyle.Bottom, Height = 220, Padding = new Padding(0, 12, 0, 0) };
        details.Controls.Add(_historyLines);
        details.Controls.Add(Ui.Section("Detalle de la entrada seleccionada"));

        _sidebar.Controls.Add(_history);
        _sidebar.Controls.Add(details);
        _sidebar.Controls.Add(Ui.Section("Últimas entradas"));
    }

    private static Label Hint(string text) =>
        new() { Text = text, AutoSize = true, ForeColor = Theme.MutedText, Margin = new Padding(0, 8, 4, 0) };
}
