using System.Globalization;

namespace Supermarket.WinForms.Infrastructure;

/// <summary>Factory methods so every screen shares the same buttons, headers and grids.</summary>
internal static class Ui
{
    private static readonly Color SelectionBackground = Color.FromArgb(219, 234, 254);

    public static Button Button(string text, char glyph, EventHandler onClick, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Image = Glyph.Render(glyph, primary ? Color.White : Theme.Text),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.Accent : Color.White,
            ForeColor = primary ? Color.White : Theme.Text,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderColor = primary ? Theme.Accent : Theme.Border;
        button.Click += onClick;
        return button;
    }

    public static void ShowErrors(IWin32Window owner, IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var text = errors.Count == 1 ? errors[0] : string.Join(Environment.NewLine, errors.Select(e => $"• {e}"));
        MessageBox.Show(owner, text, "No se pudo completar la operación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>Asks a yes/no question with "No" as the default, so Enter never confirms by accident.</summary>
    public static bool Confirm(IWin32Window owner, string question) =>
        MessageBox.Show(owner, question, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        == DialogResult.Yes;

    /// <summary>
    /// Exports exactly what a grid shows (visible columns, current order, formatted values) to a CSV file
    /// chosen by the user.
    /// </summary>
    public static void ExportToCsv(
        IWin32Window owner,
        DataGridView grid,
        string suggestedName,
        Action<string> showMessage,
        Action<IReadOnlyList<string>> showErrors)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(showMessage);
        ArgumentNullException.ThrowIfNull(showErrors);

        using var dialog = new SaveFileDialog
        {
            Title = "Exportar a CSV",
            Filter = "Archivo CSV (*.csv)|*.csv",
            FileName = $"{suggestedName} {DateTime.Now:yyyy-MM-dd HHmm}.csv",
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            return;
        }

        var columns = grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();
        var rows = grid.Rows.Cast<DataGridViewRow>()
            .Select(row => (IReadOnlyList<string>)columns
                .Select(c => row.Cells[c.Index].FormattedValue?.ToString() ?? string.Empty)
                .ToList());

        try
        {
            using var writer = new StreamWriter(dialog.FileName, append: false, CsvExporter.Encoding);
            CsvExporter.Write(writer, [.. columns.Select(c => c.HeaderText)], rows, CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            showMessage($"Exportado: {dialog.FileName}");
        }
        catch (IOException ex)
        {
            showErrors([$"No se pudo guardar el archivo. {ex.Message}"]);
        }
        catch (UnauthorizedAccessException ex)
        {
            showErrors([$"No tiene permisos para guardar en esa ubicación. {ex.Message}"]);
        }
    }

    public static Label Header(string title) => new()
    {
        Text = title,
        Font = Theme.TitleFont,
        Dock = DockStyle.Top,
        Height = 56,
        Padding = new Padding(20, 0, 0, 0),
        TextAlign = ContentAlignment.MiddleLeft,
    };

    /// <summary>Big number shown inside a <see cref="Card"/>.</summary>
    public static Label CardValue() => new()
    {
        Dock = DockStyle.Fill,
        Font = Theme.KpiFont,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "—",
    };

    /// <summary>White tile with a caption and a key figure.</summary>
    public static Panel Card(string title, Label value)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(10, 0, 10, 10),
            Padding = new Padding(16, 10, 16, 6),
        };
        card.Controls.Add(value);
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 24, ForeColor = Theme.MutedText });
        return card;
    }

    /// <summary>A titled area that hosts a grid.</summary>
    public static Panel Box(string title, DataGridView grid)
    {
        var box = new Panel { Dock = DockStyle.Fill, Margin = new Padding(10, 6, 10, 0) };
        box.Controls.Add(grid);
        box.Controls.Add(Section(title));
        return box;
    }

    public static Label Section(string title) => new()
    {
        Text = title,
        Font = Theme.SectionFont,
        Dock = DockStyle.Top,
        Height = 32,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    /// <summary>Read-only, full-row selection grid with the application look.</summary>
    public static DataGridView Grid(DataGridView? grid = null)
    {
        grid ??= new DataGridView();
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Theme.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Surface;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.MutedText;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Surface;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.MutedText;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.SelectionBackColor = SelectionBackground;
        grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
        grid.RowTemplate.Height = 32;
        return grid;
    }

    public static DataGridViewTextBoxColumn Column(
        string property,
        string header,
        float fillWeight = 100,
        string? format = null,
        bool alignRight = false,
        int minimumWidth = 70,
        bool sortable = true,
        bool fitContent = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            FillWeight = fillWeight,
            MinimumWidth = minimumWidth,
            SortMode = sortable ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable,
        };

        // Short, predictable values (ids, dates, amounts, states) get exactly the width they need,
        // so the remaining Fill columns share what is left instead of truncating them.
        if (fitContent)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        if (format is not null)
        {
            column.DefaultCellStyle.Format = format;
        }

        if (alignRight)
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        return column;
    }
}
