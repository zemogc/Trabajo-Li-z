using System.ComponentModel;
using System.Globalization;
using Supermarket.Core.Domain;
using Supermarket.Core.Presentation;
using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>
/// Layout shared by every maintenance screen: a searchable grid on the left and an editor panel on the right.
/// </summary>
/// <remarks>
/// The UI is built in code rather than with the designer because the screens are generic and
/// differ only in their columns and editor fields, which subclasses add from their constructors.
/// </remarks>
internal abstract class CrudForm<TEntity> : Form, ICrudView<TEntity>
    where TEntity : class, IEntity
{
    private readonly BindingSource _bindingSource = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly FlowLayoutPanel _toolbar = new();
    private readonly Panel _editorPanel = new();
    private readonly Label _editorTitle = new();
    private readonly TableLayoutPanel _fields = new();
    private readonly Button _saveButton;
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripStatusLabel _countLabel = new();
    private const string LoadingText = "Cargando...";

    protected CrudForm(string title)
    {
        Text = title;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        KeyPreview = true;

        _saveButton = Ui.Button("Guardar", Glyph.Save, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty), primary: true);

        BuildEditorPanel();
        BuildGrid();
        Controls.Add(_grid);
        Controls.Add(_editorPanel);
        Controls.Add(BuildToolbar());
        Controls.Add(Ui.Header(title));
        Controls.Add(BuildStatusBar());
    }

    public event EventHandler? LoadRequested;

    public event EventHandler? SearchRequested;

    public event EventHandler? CreateRequested;

    public event EventHandler? EditRequested;

    public event EventHandler? DeleteRequested;

    public event EventHandler? SaveRequested;

    public event EventHandler? CancelRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchTerm => _searchBox.Text;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TEntity? SelectedEntity => _bindingSource.Current as TEntity;

    public void DisplayEntities(IReadOnlyList<TEntity> entities)
    {
        var selectedId = SelectedEntity?.Id;
        var list = new SortableBindingList<TEntity>(entities);

        // Keep the order the user chose by clicking a column header.
        if (_bindingSource.DataSource is SortableBindingList<TEntity> previous)
        {
            list.SortLike(previous);
        }

        _bindingSource.DataSource = list;

        var index = list.ToList().FindIndex(e => e.Id == selectedId);
        if (index >= 0)
        {
            _bindingSource.Position = index;
        }

        _countLabel.Text = DescribeEntities(entities);
    }

    public void OpenEditor(TEntity entity)
    {
        _editorTitle.Text = entity.IsNew ? "Nuevo registro" : $"Editar registro #{entity.Id}";
        FillEditor(entity);
        _editorPanel.Visible = true;
        AcceptButton = _saveButton;
        FocusFirstField();
    }

    public TEntity ReadEditor() => ReadEditorValues();

    public void CloseEditor()
    {
        _editorPanel.Visible = false;
        AcceptButton = null;
        _grid.Focus();
    }

    public void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;
        _toolbar.Enabled = !isBusy;
        _editorPanel.Enabled = !isBusy;
        _grid.Enabled = !isBusy;
        if (isBusy)
        {
            _statusLabel.Text = LoadingText;
        }
        else if (_statusLabel.Text == LoadingText)
        {
            _statusLabel.Text = string.Empty;
        }
    }

    public void ShowMessage(string message) => _statusLabel.Text = message;

    public void ShowErrors(IReadOnlyList<string> errors)
    {
        _statusLabel.Text = string.Empty;
        Ui.ShowErrors(this, errors);
    }

    public bool Confirm(string question) => Ui.Confirm(this, question);

    protected abstract void FillEditor(TEntity entity);

    protected abstract TEntity ReadEditorValues();

    /// <summary>Text for the record counter in the status bar.</summary>
    protected virtual string DescribeEntities(IReadOnlyList<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Count == 1 ? "1 registro" : $"{entities.Count} registros";
    }

    /// <summary>Highlights the rows whose entity matches <paramref name="predicate"/> as warnings.</summary>
    protected void HighlightRowsWhen(Func<TEntity, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.CellStyle is not null
                && _grid.Rows[e.RowIndex].DataBoundItem is TEntity entity && predicate(entity))
            {
                e.CellStyle.BackColor = Theme.WarningBackground;
                e.CellStyle.ForeColor = Theme.Warning;
            }
        };
    }

    protected void AddColumn(string property, string header, float fillWeight = 100, string? format = null, bool alignRight = false, int minimumWidth = 70) =>
        _grid.Columns.Add(Ui.Column(property, header, fillWeight, format, alignRight, minimumWidth));

    protected void AddField(string label, Control input)
    {
        ArgumentNullException.ThrowIfNull(input);

        input.Dock = DockStyle.Top;
        input.Margin = new Padding(0, 2, 0, 12);

        _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _fields.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.MutedText, Margin = new Padding(0) });
        _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _fields.Controls.Add(input);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        // Reload every time the screen is shown so changes made in other screens are picked up.
        if (Visible)
        {
            LoadRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.N:
                CreateRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.F:
                _searchBox.Focus();
                return true;
            case Keys.Escape when _editorPanel.Visible:
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private FlowLayoutPanel BuildToolbar()
    {
        _toolbar.Dock = DockStyle.Top;
        _toolbar.AutoSize = true;
        _toolbar.Padding = new Padding(20, 0, 20, 12);
        _toolbar.WrapContents = false;

        _searchBox.PlaceholderText = "Buscar por código o nombre (Ctrl+F)";
        _searchBox.Width = 300;
        _searchBox.Margin = new Padding(0, 4, 8, 0);
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        _toolbar.Controls.Add(_searchBox);
        _toolbar.Controls.Add(Ui.Button("Buscar", Glyph.Search, (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty)));
        _toolbar.Controls.Add(Ui.Button("Nuevo", Glyph.Add, (_, _) => CreateRequested?.Invoke(this, EventArgs.Empty)));
        _toolbar.Controls.Add(Ui.Button("Editar", Glyph.Edit, (_, _) => EditRequested?.Invoke(this, EventArgs.Empty)));

        var deleteButton = Ui.Button("Eliminar", Glyph.Delete, (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty));
        deleteButton.ForeColor = Theme.Danger;
        deleteButton.Image = Glyph.Render(Glyph.Delete, Theme.Danger);
        _toolbar.Controls.Add(deleteButton);
        _toolbar.Controls.Add(Ui.Button("Exportar", Glyph.Export, (_, _) => ExportToCsv()));

        return _toolbar;
    }

    private void BuildGrid()
    {
        Ui.Grid(_grid);
        _grid.DataSource = _bindingSource;

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                EditRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                EditRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    private void BuildEditorPanel()
    {
        _editorPanel.Dock = DockStyle.Right;
        _editorPanel.Width = 320;
        _editorPanel.Padding = new Padding(20, 4, 20, 20);
        _editorPanel.BackColor = Color.White;
        _editorPanel.Visible = false;

        _editorTitle.Dock = DockStyle.Top;
        _editorTitle.Height = 36;
        _editorTitle.Font = Theme.SectionFont;

        _fields.Dock = DockStyle.Top;
        _fields.AutoSize = true;
        _fields.ColumnCount = 1;
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
        };
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(Ui.Button("Cancelar", Glyph.Cancel, (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty)));

        // Docked controls are laid out in reverse order of insertion.
        _editorPanel.Controls.Add(buttons);
        _editorPanel.Controls.Add(_fields);
        _editorPanel.Controls.Add(_editorTitle);
    }

    private StatusStrip BuildStatusBar()
    {
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        var strip = new StatusStrip { SizingGrip = false, BackColor = Theme.Surface };
        strip.Items.Add(_statusLabel);
        strip.Items.Add(_countLabel);
        return strip;
    }

    /// <summary>Exports exactly what the grid shows: visible columns, current order, formatted values.</summary>
    private void ExportToCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar a CSV",
            Filter = "Archivo CSV (*.csv)|*.csv",
            FileName = $"{Text} {DateTime.Now:yyyy-MM-dd HHmm}.csv",
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var columns = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();
        var rows = _grid.Rows.Cast<DataGridViewRow>()
            .Select(row => (IReadOnlyList<string>)columns
                .Select(c => row.Cells[c.Index].FormattedValue?.ToString() ?? string.Empty)
                .ToList());

        try
        {
            using var writer = new StreamWriter(dialog.FileName, append: false, CsvExporter.Encoding);
            CsvExporter.Write(writer, [.. columns.Select(c => c.HeaderText)], rows, CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            ShowMessage($"Exportado: {dialog.FileName}");
        }
        catch (IOException ex)
        {
            ShowErrors([$"No se pudo guardar el archivo. {ex.Message}"]);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowErrors([$"No tiene permisos para guardar en esa ubicación. {ex.Message}"]);
        }
    }

    private void FocusFirstField()
    {
        var first = _fields.Controls.OfType<Control>().FirstOrDefault(c => c is not Label);
        first?.Focus();
        if (first is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bindingSource.Dispose();
        }

        base.Dispose(disposing);
    }
}
