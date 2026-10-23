using System.ComponentModel;
using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>Sales of a chosen period: totals, every sale, payment methods and best sellers.</summary>
internal sealed class ReportsForm : Form, IReportsView
{
    private readonly DateTimePicker _firstDay = DatePicker();
    private readonly DateTimePicker _lastDay = DatePicker();
    private readonly Label _salesCount = Ui.CardValue();
    private readonly Label _revenue = Ui.CardValue();
    private readonly Label _averageTicket = Ui.CardValue();
    private readonly Label _unitsSold = Ui.CardValue();
    private readonly DataGridView _sales = Ui.Grid();
    private readonly DataGridView _paymentMethods = Ui.Grid();
    private readonly DataGridView _topProducts = Ui.Grid();
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(20, 0, 20, 12) };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private Font? _strikeout;

    public ReportsForm()
    {
        Text = "Reportes";
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;

        BuildToolbar();
        BuildGrids();

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 116, ColumnCount = 4, Padding = new Padding(10, 0, 10, 0) };
        for (var i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        cards.Controls.Add(Ui.Card("Ventas", _salesCount), 0, 0);
        cards.Controls.Add(Ui.Card("Ingresos", _revenue), 1, 0);
        cards.Controls.Add(Ui.Card("Ticket promedio", _averageTicket), 2, 0);
        cards.Controls.Add(Ui.Card("Unidades vendidas", _unitsSold), 3, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10, 0, 10, 10) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

        var salesBox = Ui.Box("Ventas del período (las anuladas no suman)", _sales);
        body.Controls.Add(salesBox, 0, 0);
        body.SetRowSpan(salesBox, 2);
        body.Controls.Add(Ui.Box("Por medio de pago", _paymentMethods), 1, 0);
        body.Controls.Add(Ui.Box("Más vendidos", _topProducts), 1, 1);

        var statusBar = new StatusStrip { SizingGrip = false, BackColor = Theme.Surface };
        statusBar.Items.Add(_status);

        Controls.Add(body);
        Controls.Add(cards);
        Controls.Add(_toolbar);
        Controls.Add(Ui.Header("Reportes"));
        Controls.Add(statusBar);
    }

    public event EventHandler? LoadRequested;

    public event EventHandler? GenerateRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateOnly FirstDay => DateOnly.FromDateTime(_firstDay.Value);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateOnly LastDay => DateOnly.FromDateTime(_lastDay.Value);

    public void SetPeriod(DateOnly firstDay, DateOnly lastDay)
    {
        _firstDay.Value = firstDay.ToDateTime(TimeOnly.MinValue);
        _lastDay.Value = lastDay.ToDateTime(TimeOnly.MinValue);
    }

    public void DisplaySummary(SalesSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var culture = CultureInfo.CurrentCulture;
        _salesCount.Text = summary.SalesCount.ToString("N0", culture);
        _revenue.Text = summary.Revenue.ToString("C0", culture);
        _averageTicket.Text = summary.AverageTicket.ToString("C0", culture);
        _unitsSold.Text = summary.UnitsSold.ToString("N0", culture);
        _status.Text = string.Create(culture, $"Período del {FirstDay:dd/MM/yyyy} al {LastDay:dd/MM/yyyy}");
    }

    public void DisplaySales(IReadOnlyList<Sale> sales) =>
        _sales.DataSource = new SortableBindingList<Sale>(sales);

    public void DisplayTopProducts(IReadOnlyList<ProductSales> products) =>
        _topProducts.DataSource = new BindingList<ProductSales>([.. products]);

    public void DisplayPaymentBreakdown(IReadOnlyList<PaymentMethodSales> paymentMethods) =>
        _paymentMethods.DataSource = new BindingList<PaymentMethodSales>([.. paymentMethods]);

    public void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;
        _toolbar.Enabled = !isBusy;
    }

    public void ShowMessage(string message) => _status.Text = message;

    public void ShowErrors(IReadOnlyList<string> errors) => Ui.ShowErrors(this, errors);

    public bool Confirm(string question) => Ui.Confirm(this, question);

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible)
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _strikeout?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static DateTimePicker DatePicker() => new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 130,
        Margin = new Padding(0, 6, 12, 0),
    };

    private void BuildToolbar()
    {
        _toolbar.Controls.Add(Hint("Desde"));
        _toolbar.Controls.Add(_firstDay);
        _toolbar.Controls.Add(Hint("Hasta"));
        _toolbar.Controls.Add(_lastDay);
        _toolbar.Controls.Add(Ui.Button("Generar", Glyph.Search, (_, _) => GenerateRequested?.Invoke(this, EventArgs.Empty), primary: true));
        _toolbar.Controls.Add(Ui.Button("Hoy", Glyph.Calendar, (_, _) => Preset(DateTime.Today, DateTime.Today)));
        _toolbar.Controls.Add(Ui.Button("Últimos 7 días", Glyph.Calendar, (_, _) => Preset(DateTime.Today.AddDays(-6), DateTime.Today)));
        _toolbar.Controls.Add(Ui.Button("Este mes", Glyph.Calendar, (_, _) => Preset(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today)));
        _toolbar.Controls.Add(Ui.Button("Exportar ventas", Glyph.Export, (_, _) => Ui.ExportToCsv(this, _sales, "Ventas", ShowMessage, ShowErrors)));
    }

    private void Preset(DateTime first, DateTime last)
    {
        _firstDay.Value = first;
        _lastDay.Value = last;
        GenerateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildGrids()
    {
        _sales.Columns.Add(Ui.Column(nameof(Sale.Id), "N.º", fillWeight: 10, minimumWidth: 55));
        _sales.Columns.Add(Ui.Column(nameof(Sale.CreatedAtLocal), "Fecha", fillWeight: 28, format: "dd/MM/yyyy HH:mm", minimumWidth: 140));
        _sales.Columns.Add(Ui.Column(nameof(Sale.PaymentMethodName), "Pago", fillWeight: 22, minimumWidth: 90));
        _sales.Columns.Add(Ui.Column(nameof(Sale.ItemCount), "Artículos", fillWeight: 14, format: "N0", alignRight: true, minimumWidth: 85));
        _sales.Columns.Add(Ui.Column(nameof(Sale.Status), "Estado", fillWeight: 14, minimumWidth: 85));
        _sales.Columns.Add(Ui.Column(nameof(Sale.Total), "Total", fillWeight: 18, format: "N2", alignRight: true, minimumWidth: 100));
        _sales.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.CellStyle is not null
                && _sales.Rows[e.RowIndex].DataBoundItem is Sale { IsCancelled: true })
            {
                e.CellStyle.ForeColor = Theme.MutedText;
                e.CellStyle.Font = _strikeout ??= new Font(_sales.Font, FontStyle.Strikeout);
            }
        };

        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.PaymentMethodName), "Medio de pago", fillWeight: 45, minimumWidth: 120, sortable: false));
        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.SalesCount), "Ventas", fillWeight: 20, format: "N0", alignRight: true, minimumWidth: 70, sortable: false));
        _paymentMethods.Columns.Add(Ui.Column(nameof(PaymentMethodSales.Total), "Total", fillWeight: 35, format: "N2", alignRight: true, minimumWidth: 105, sortable: false));

        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.ProductName), "Producto", fillWeight: 50, minimumWidth: 130, sortable: false));
        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.Units), "Unidades", fillWeight: 20, format: "N0", alignRight: true, minimumWidth: 85, sortable: false));
        _topProducts.Columns.Add(Ui.Column(nameof(ProductSales.Revenue), "Ingresos", fillWeight: 30, format: "N2", alignRight: true, minimumWidth: 105, sortable: false));

        foreach (var grid in new[] { _paymentMethods, _topProducts })
        {
            grid.DataBindingComplete += (_, _) => grid.ClearSelection();
        }
    }

    private static Label Hint(string text) =>
        new() { Text = text, AutoSize = true, ForeColor = Theme.MutedText, Margin = new Padding(0, 10, 6, 0) };
}
