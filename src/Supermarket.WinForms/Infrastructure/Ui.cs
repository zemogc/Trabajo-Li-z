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

    public static Label Header(string title) => new()
    {
        Text = title,
        Font = Theme.TitleFont,
        Dock = DockStyle.Top,
        Height = 56,
        Padding = new Padding(20, 0, 0, 0),
        TextAlign = ContentAlignment.MiddleLeft,
    };

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
        bool sortable = true)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            FillWeight = fillWeight,
            MinimumWidth = minimumWidth,
            SortMode = sortable ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable,
        };

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
