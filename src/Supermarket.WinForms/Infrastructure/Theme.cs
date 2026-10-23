namespace Supermarket.WinForms.Infrastructure;

/// <summary>Colors and fonts shared by every screen.</summary>
internal static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(28, 37, 54);
    public static readonly Color SidebarHover = Color.FromArgb(44, 56, 79);
    public static readonly Color SidebarSelected = Color.FromArgb(37, 99, 235);
    public static readonly Color SidebarText = Color.FromArgb(226, 232, 240);
    public static readonly Color Surface = Color.FromArgb(248, 250, 252);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color Text = Color.FromArgb(15, 23, 42);
    public static readonly Color MutedText = Color.FromArgb(100, 116, 139);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);
    public static readonly Color WarningBackground = Color.FromArgb(254, 243, 199);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 16f);
    public static readonly Font SectionFont = new("Segoe UI Semibold", 11f);
    public static readonly Font KpiFont = new("Segoe UI Semibold", 22f);
}
