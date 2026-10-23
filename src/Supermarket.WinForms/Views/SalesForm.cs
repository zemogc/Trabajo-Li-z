using System.ComponentModel;
using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>
/// Checkout screen: the sale in progress on the left and the latest sales on the right.
/// </summary>
internal sealed class SalesForm : Form, ISalesView
{
    private readonly ComboBox _product = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FormattingEnabled = true,
        Width = 380,
        Margin = new Padding(0, 4, 8, 0),
    };

    private readonly NumericUpDown _quantity = new()
    {
        Minimum = 1,
        Maximum = 10_000,
        Width = 80,
        TextAlign = HorizontalAlignment.Right,
        Margin = new Padding(0, 4, 8, 0),
    };

    private readonly ComboBox _paymentMethod = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(PaymentMethod.Name),
        ValueMember = nameof(PaymentMethod.Id),
        Width = 220,
        Margin = new Padding(0, 6, 16, 0),
    };

    private readonly DataGridView _cart = Ui.Grid();
    private readonly DataGridView _history = Ui.Grid();
    private readonly DataGridView _historyLines = Ui.Grid();
    private readonly Label _total = new()
    {
        AutoSize = true,
        Font = Theme.TitleFont,
        Margin = new Padding(0, 0, 16, 0),
    };

    private readonly Panel _checkout = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 10, 16) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Right, Width = 480, Padding = new Padding(10, 0, 20, 16) };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    // Rebinding the history grid moves its selection; that must not look like a user choice.
    private bool _bindingHistory;
    private Font? _strikeout;

    public SalesForm()
    {
        Text = "Ventas";
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        KeyPreview = true;

        BuildCheckout();
        BuildSidebar();

        Controls.Add(_checkout);
        Controls.Add(_sidebar);
        Controls.Add(Ui.Header("Ventas"));

        var statusBar = new StatusStrip { SizingGrip = false, BackColor = Theme.Surface };
        statusBar.Items.Add(_status);
        Controls.Add(statusBar);

        DisplayCart([], 0m);
    }

    public event EventHandler? LoadRequested;

    public event EventHandler? AddItemRequested;

    public event EventHandler? RemoveItemRequested;

    public event EventHandler? ClearRequested;

    public event EventHandler? RegisterRequested;

    public event EventHandler? SaleSelected;

    public event EventHandler? CancelSaleRequested;

    public event EventHandler? PrintReceiptRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Product? SelectedProduct => _product.SelectedItem as Product;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Quantity => (int)_quantity.Value;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedCartProductId => (_cart.CurrentRow?.DataBoundItem as SaleLine)?.ProductId;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedPaymentMethodId => _paymentMethod.SelectedValue as int?;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? SelectedSaleId =>
        _history.CurrentRow is { Selected: true, DataBoundItem: Sale sale } ? sale.Id : null;

    public void DisplayProducts(IReadOnlyList<Product> products)
    {
        var selectedId = SelectedProduct?.Id;
        _product.DataSource = products.ToList();

        var index = products.ToList().FindIndex(p => p.Id == selectedId);
        _product.SelectedIndex = index >= 0 ? index : -1;
    }

    public void DisplayPaymentMethods(IReadOnlyList<PaymentMethod> paymentMethods)
    {
        var selected = SelectedPaymentMethodId;
        _paymentMethod.DataSource = paymentMethods.ToList();

        // Cash-first stores expect the first method to be ready; the user can still change it.
        if (selected is not null && paymentMethods.Any(p => p.Id == selected))
        {
            _paymentMethod.SelectedValue = selected;
        }
        else
        {
            _paymentMethod.SelectedIndex = paymentMethods.Count > 0 ? 0 : -1;
        }
    }

    public void DisplayCart(IReadOnlyList<SaleLine> lines, decimal total)
    {
        _cart.DataSource = new BindingList<SaleLine>([.. lines]);
        _total.Text = $"Total {total.ToString("C2", CultureInfo.CurrentCulture)}";
    }

    public void ResetItemInput()
    {
        _quantity.Value = 1;
        _product.Focus();
    }

    public void DisplayRecentSales(IReadOnlyList<Sale> sales)
    {
        _bindingHistory = true;
        try
        {
            _history.DataSource = new BindingList<Sale>([.. sales]);
            _history.ClearSelection();
        }
        finally
        {
            _bindingHistory = false;
        }

        DisplaySaleLines([]);
    }

    public void DisplaySaleLines(IReadOnlyList<SaleLine> lines) =>
        _historyLines.DataSource = new BindingList<SaleLine>([.. lines]);

    public void ShowReceipt(string title, IReadOnlyList<string> lines)
    {
        using var document = new ReceiptDocument(title, lines);
        using var preview = new PrintPreviewDialog
        {
            Document = document,
            Text = title,
            Width = 560,
            Height = 780,
            StartPosition = FormStartPosition.CenterParent,
            UseAntiAlias = true,
        };

        try
        {
            preview.ShowDialog(this);
        }
        catch (System.Drawing.Printing.InvalidPrinterException)
        {
            ShowErrors(["No hay una impresora instalada. Instale una (o \"Microsoft Print to PDF\") para ver e imprimir recibos."]);
        }
    }

    public void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;
        _checkout.Enabled = !isBusy;
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

        // Prices and stock may have changed in the products screen.
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

    private void BuildCheckout()
    {
        _product.Format += (_, e) =>
        {
            if (e.ListItem is Product product)
            {
                e.Value = string.Create(
                    CultureInfo.CurrentCulture,
                    $"{product.Name}  ·  {product.Price:C2}  ·  {product.Stock:N0} disp.");
            }
        };

        _quantity.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddItemRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        var itemRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(0, 0, 0, 12) };
        itemRow.Controls.Add(_product);
        itemRow.Controls.Add(_quantity);
        itemRow.Controls.Add(Ui.Button("Agregar", Glyph.Add, (_, _) => AddItemRequested?.Invoke(this, EventArgs.Empty)));

        _cart.Columns.Add(Ui.Column(nameof(SaleLine.ProductName), "Producto", fillWeight: 46, minimumWidth: 140, sortable: false));
        _cart.Columns.Add(Ui.Column(nameof(SaleLine.Quantity), "Cantidad", fillWeight: 14, format: "N0", alignRight: true, minimumWidth: 85, sortable: false));
        _cart.Columns.Add(Ui.Column(nameof(SaleLine.UnitPrice), "Precio", fillWeight: 20, format: "N2", alignRight: true, minimumWidth: 95, sortable: false));
        _cart.Columns.Add(Ui.Column(nameof(SaleLine.Subtotal), "Subtotal", fillWeight: 20, format: "N2", alignRight: true, minimumWidth: 105, sortable: false));
        _cart.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                RemoveItemRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        var cartActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        cartActions.Controls.Add(Ui.Button("Quitar", Glyph.Delete, (_, _) => RemoveItemRequested?.Invoke(this, EventArgs.Empty)));
        cartActions.Controls.Add(Ui.Button("Vaciar", Glyph.Cancel, (_, _) => ClearRequested?.Invoke(this, EventArgs.Empty)));

        var paymentLabel = new Label { Text = "Medio de pago", AutoSize = true, ForeColor = Theme.MutedText, Margin = new Padding(0, 10, 8, 0) };
        var register = Ui.Button("Registrar venta (F9)", Glyph.Save, (_, _) => RegisterRequested?.Invoke(this, EventArgs.Empty), primary: true);

        var checkoutRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = false, Padding = new Padding(0, 12, 0, 0) };
        checkoutRow.Controls.Add(paymentLabel);
        checkoutRow.Controls.Add(_paymentMethod);
        checkoutRow.Controls.Add(_total);

        var registerRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        register.Margin = new Padding(0);
        registerRow.Controls.Add(register);

        // Docked controls are laid out in reverse order of insertion: fill first, edges last.
        _checkout.Controls.Add(_cart);
        _checkout.Controls.Add(cartActions);
        _checkout.Controls.Add(checkoutRow);
        _checkout.Controls.Add(registerRow);
        _checkout.Controls.Add(itemRow);
        _checkout.Controls.Add(Ui.Section("Nueva venta"));
    }

    private void BuildSidebar()
    {
        _history.Columns.Add(Ui.Column(nameof(Sale.Id), "N.º", fillWeight: 10, minimumWidth: 55));
        _history.Columns.Add(Ui.Column(nameof(Sale.CreatedAtLocal), "Fecha", fillWeight: 38, format: "dd/MM/yyyy HH:mm", minimumWidth: 140));
        _history.Columns.Add(Ui.Column(nameof(Sale.PaymentMethodName), "Pago", fillWeight: 26, minimumWidth: 90));
        _history.Columns.Add(Ui.Column(nameof(Sale.Status), "Estado", fillWeight: 20, minimumWidth: 85));
        _history.Columns.Add(Ui.Column(nameof(Sale.Total), "Total", fillWeight: 26, format: "N2", alignRight: true, minimumWidth: 100));

        // Voided sales stay visible but clearly out of the totals.
        _history.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.CellStyle is not null
                && _history.Rows[e.RowIndex].DataBoundItem is Sale { IsCancelled: true })
            {
                e.CellStyle.ForeColor = Theme.MutedText;
                e.CellStyle.Font = _strikeout ??= new Font(_history.Font, FontStyle.Strikeout);
            }
        };
        _history.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                PrintReceiptRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        _history.SelectionChanged += (_, _) =>
        {
            if (!_bindingHistory)
            {
                SaleSelected?.Invoke(this, EventArgs.Empty);
            }
        };

        _historyLines.Columns.Add(Ui.Column(nameof(SaleLine.ProductName), "Producto", fillWeight: 50, minimumWidth: 120, sortable: false));
        _historyLines.Columns.Add(Ui.Column(nameof(SaleLine.Quantity), "Cant.", fillWeight: 16, format: "N0", alignRight: true, minimumWidth: 60, sortable: false));
        _historyLines.Columns.Add(Ui.Column(nameof(SaleLine.Subtotal), "Subtotal", fillWeight: 34, format: "N2", alignRight: true, minimumWidth: 100, sortable: false));

        var historyActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var cancel = Ui.Button("Anular venta", Glyph.Cancel, (_, _) => CancelSaleRequested?.Invoke(this, EventArgs.Empty));
        cancel.ForeColor = Theme.Danger;
        cancel.Image = Glyph.Render(Glyph.Cancel, Theme.Danger);
        historyActions.Controls.Add(Ui.Button("Recibo", Glyph.Print, (_, _) => PrintReceiptRequested?.Invoke(this, EventArgs.Empty)));
        historyActions.Controls.Add(cancel);

        var details = new Panel { Dock = DockStyle.Bottom, Height = 220, Padding = new Padding(0, 12, 0, 0) };
        details.Controls.Add(_historyLines);
        details.Controls.Add(Ui.Section("Detalle de la venta seleccionada"));

        _sidebar.Controls.Add(_history);
        _sidebar.Controls.Add(historyActions);
        _sidebar.Controls.Add(details);
        _sidebar.Controls.Add(Ui.Section("Últimas ventas"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _strikeout?.Dispose();
        }

        base.Dispose(disposing);
    }
}
