using Supermarket.WinForms.Infrastructure;

namespace Supermarket.WinForms.Views;

/// <summary>
/// Application shell: a navigation sidebar and a content area that hosts one screen at a time.
/// </summary>
internal sealed class MainForm : Form
{
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.Surface };
    private readonly Dictionary<Module, Form> _openScreens = [];
    private readonly Dictionary<Module, Button> _menuButtons = [];
    private Form? _current;

    public MainForm(ModuleFactory modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        Text = "Supermercado";
        MinimumSize = new Size(1000, 620);
        Size = new Size(1200, 720);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(_content);
        Controls.Add(BuildSidebar(modules.Modules));

        Shown += (_, _) => Open(modules.Modules[0]);
    }

    private Panel BuildSidebar(IReadOnlyList<Module> modules)
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Theme.Sidebar };

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
        };

        foreach (var module in modules)
        {
            var button = CreateMenuButton(module.Title, module.Glyph);
            button.Click += (_, _) => Open(module);
            _menuButtons[module] = button;
            menu.Controls.Add(button);
        }

        var exit = CreateMenuButton("Salir", Glyph.Exit);
        exit.Dock = DockStyle.Bottom;
        exit.Click += (_, _) => Close();

        var brand = new Label
        {
            Text = "Supermercado",
            Dock = DockStyle.Top,
            Height = 64,
            Font = Theme.SectionFont,
            ForeColor = Color.White,
            Padding = new Padding(20, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        sidebar.Controls.Add(menu);
        sidebar.Controls.Add(exit);
        sidebar.Controls.Add(brand);
        return sidebar;
    }

    private static Button CreateMenuButton(string text, char glyph)
    {
        var button = new Button
        {
            Text = "  " + text,
            Image = Glyph.Render(glyph, Theme.SidebarText, 18),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            TextAlign = ContentAlignment.MiddleLeft,
            Width = 220,
            Height = 44,
            Margin = new Padding(0),
            Padding = new Padding(20, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Theme.SidebarText,
            BackColor = Theme.Sidebar,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
        return button;
    }

    /// <summary>Shows a screen, creating it the first time and reusing it afterwards.</summary>
    private void Open(Module module)
    {
        if (!_openScreens.TryGetValue(module, out var screen))
        {
            screen = module.Create();
            screen.TopLevel = false;
            screen.FormBorderStyle = FormBorderStyle.None;
            screen.Dock = DockStyle.Fill;
            _content.Controls.Add(screen);
            _openScreens[module] = screen;
        }

        if (ReferenceEquals(screen, _current))
        {
            return;
        }

        _current?.Hide();
        _current = screen;
        screen.Show();
        screen.BringToFront();

        foreach (var (item, button) in _menuButtons)
        {
            button.BackColor = item == module ? Theme.SidebarSelected : Theme.Sidebar;
        }
    }
}
