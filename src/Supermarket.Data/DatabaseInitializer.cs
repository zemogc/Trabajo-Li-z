using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Supermarket.Data;

/// <summary>
/// Creates the database when it does not exist and applies the embedded schema (and optionally seed) scripts.
/// </summary>
/// <remarks>
/// The scripts are idempotent, so running the initializer against an existing database is safe.
/// </remarks>
public sealed partial class DatabaseInitializer(string connectionString, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(bool seedSampleData, CancellationToken cancellationToken = default)
    {
        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("The connection string must specify the database (Initial Catalog).");
        }

        await EnsureDatabaseExistsAsync(database, cancellationToken).ConfigureAwait(false);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await RunScriptAsync(connection, "schema.sql", cancellationToken).ConfigureAwait(false);
        if (seedSampleData)
        {
            await RunScriptAsync(connection, "seed.sql", cancellationToken).ConfigureAwait(false);
        }

        LogInitialized(database, seedSampleData);
    }

    /// <summary>
    /// Splits a script on its <c>GO</c> batch separators, which are a client side convention
    /// understood by SSMS and sqlcmd but not by SQL Server itself.
    /// </summary>
    internal static IReadOnlyList<string> SplitBatches(string script) =>
        BatchSeparator().Split(script)
            .Select(batch => batch.Trim())
            .Where(batch => batch.Length > 0)
            .ToList();

    internal static string ReadScript(string name)
    {
        var resource = $"Supermarket.Data.Scripts.{name}";
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded script '{resource}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task EnsureDatabaseExistsAsync(string database, CancellationToken cancellationToken)
    {
        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // QUOTENAME protects against injection through the configured database name.
        var created = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            IF DB_ID(@database) IS NOT NULL
                SELECT CAST(0 AS BIT);
            ELSE
            BEGIN
                DECLARE @sql NVARCHAR(300) = N'CREATE DATABASE ' + QUOTENAME(@database) + N';';
                EXEC (@sql);
                SELECT CAST(1 AS BIT);
            END;
            """,
            new { database },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (created)
        {
            LogDatabaseCreated(database);
        }
    }

    private static async Task RunScriptAsync(SqlConnection connection, string name, CancellationToken cancellationToken)
    {
        foreach (var batch in SplitBatches(ReadScript(name)))
        {
            await connection.ExecuteAsync(new CommandDefinition(batch, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex BatchSeparator();

    [LoggerMessage(Level = LogLevel.Information, Message = "Database {Database} created")]
    private partial void LogDatabaseCreated(string database);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database {Database} is up to date (sample data: {Seeded})")]
    private partial void LogInitialized(string database, bool seeded);
}
