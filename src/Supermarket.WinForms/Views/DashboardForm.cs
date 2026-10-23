using System.ComponentModel;
using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>Home screen: today's figures, best sellers and the products that need restocking.</summary>
internal sealed class DashboardForm : Form, IDashboardView
{
    private readonly Label _salesCount = Ui.CardValue();
    private readonly Label _revenue = Ui.CardValue();
    private readonly Label _averageTicket = Ui.CardValue();
    private readonly Label _toRestock = Ui.CardValue();
    private readonly DataGridView _topProducts = Ui.Grid();
    private readonly DataGridView _paymentMethods = Ui.Grid();
    private readonly DataGridView _restock = Ui.Grid();
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    public DashboardForm()
    {
        Text = "Inicio";
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10, 0, 10, 10),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.ProductName), "Producto", fillWeight: 50, minimumWidth: 140, sortable: false));
        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.Units), "Unidades", format: "N0", alignRight: true, minimumWidth: 40, sortable: false, fitContent: true));
        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.Revenue), "Ingresos", format: "N2", alignRight: true, minimumWidth: 40, sortable: false, fitContent: true));

        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.PaymentMethodName), "Medio de pago", fillWeight: 45, minimumWidth: 120, sortable: false));
        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.SalesCount), "Ventas", format: "N0", alignRight: true, minimumWidth: 40, sortable: false, fitContent: true));
        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.Total), "Total", format: "N2", alignRight: true, minimumWidth: 40, sortable: false, fitContent: true));

        _restock.Columns.Add(Ui.Column(nameof(Product.Name), "Producto", fillWeight: 34, minimumWidth: 140, sortable: false));
        _restock.Columns.Add(Ui.Column(nameof(Product.CategoryName), "Categoría", fillWeight: 18, minimumWidth: 90, sortable: false));
        _restock.Columns.Add(Ui.Column(nameof(Product.ProviderName), "Proveedor", fillWeight: 24, minimumWidth: 100, sortable: false));
        _restock.Columns.Add(Ui.Column(nameof(Product.Stock), "Stock", fillWeight: 12, format: "N0", alignRight: true, minimumWidth: 70, sortable: false));
        _restock.Columns.Add(Ui.Column(nameof(Product.MinimumStock), "Mínimo", fillWeight: 12, format: "N0", alignRight: true, minimumWidth: 80, sortable: false));

        // These grids are read-only summaries: nothing should look selected.
        foreach (var grid in new[] { _topProducts, _paymentMethods, _restock })
        {
            grid.DataBindingComplete += (_, _) => grid.ClearSelection();
        }

        body.Controls.Add(Ui.Box("Más vendidos (últimos 30 días)", _topProducts), 0, 0);
        body.Controls.Add(Ui.Box("Ventas de hoy por medio de pago", _paymentMethods), 1, 0);
        var restockBox = Ui.Box("Productos por reponer", _restock);
        body.Controls.Add(restockBox, 0, 1);
        body.SetColumnSpan(restockBox, 2);

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 116, ColumnCount = 4, Padding = new Padding(10, 0, 10, 0) };
        for (var i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        cards.Controls.Add(Ui.Card("Ventas de hoy", _salesCount), 0, 0);
        cards.Controls.Add(Ui.Card("Ingresos de hoy", _revenue), 1, 0);
        cards.Controls.Add(Ui.Card("Ticket promedio", _averageTicket), 2, 0);
        cards.Controls.Add(Ui.Card("Productos por reponer", _toRestock), 3, 0);

        var statusBar = new StatusStrip { SizingGrip = false, BackColor = Theme.Surface };
        statusBar.Items.Add(_status);

        Controls.Add(body);
        Controls.Add(cards);
        Controls.Add(Ui.Header("Inicio"));
        Controls.Add(statusBar);
    }

    public event EventHandler? LoadRequested;

    public void DisplayToday(SalesSummary today, int productsToRestock)
    {
        ArgumentNullException.ThrowIfNull(today);

        var culture = CultureInfo.CurrentCulture;
        _salesCount.Text = today.SalesCount.ToString("N0", culture);
        _revenue.Text = today.Revenue.ToString("C0", culture);
        _averageTicket.Text = today.AverageTicket.ToString("C0", culture);
        _toRestock.Text = productsToRestock.ToString("N0", culture);
        _toRestock.ForeColor = productsToRestock > 0 ? Theme.Warning : Theme.Text;
        _status.Text = $"Actualizado {DateTime.Now:HH:mm}";
    }

    public void DisplayTopProducts(IReadOnlyList<ProductSales> products) =>
        _topProducts.DataSource = new BindingList<ProductSales>([.. products]);

    public void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods) =>
        _paymentMethods.DataSource = new BindingList<PaymentMethodSales>([.. paymentMethods]);

    public void DisplayProductsToRestock(IReadOnlyList<Product> products) =>
        _restock.DataSource = new BindingList<Product>([.. products]);

    public void SetBusy(bool isBusy) => UseWaitCursor = isBusy;

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

        // Figures change with every sale, so they are refreshed each time the screen is shown.
        if (Visible)
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
