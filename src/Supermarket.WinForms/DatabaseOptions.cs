namespace Supermarket.WinForms;

/// <summary>Settings bound from the <c>Database</c> section of appsettings.json.</summary>
internal sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Creates the database and applies the schema when the application starts.</summary>
    public bool InitializeOnStartup { get; set; } = true;

    /// <summary>Inserts sample data into empty tables. Intended for development only.</summary>
    public bool SeedSampleData { get; set; }
}
