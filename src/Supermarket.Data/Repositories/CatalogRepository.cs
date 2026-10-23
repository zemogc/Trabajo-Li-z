using Dapper;
using Microsoft.Data.SqlClient;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.Repositories;

/// <summary>
/// Repository for the catalog tables, which all share the Id/Name/Observation shape.
/// </summary>
/// <remarks>
/// SQL is composed from <see cref="TableName"/>, a compile-time constant defined by each subclass.
/// User supplied values are always sent as parameters.
/// </remarks>
internal abstract class CatalogRepository<TItem>(IDbConnectionFactory connections) : IRepository<TItem>
    where TItem : CatalogItem
{
    protected abstract string TableName { get; }

    public async Task<IReadOnlyList<TItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var items = await connection.QueryAsync<TItem>(new CommandDefinition(
            $"SELECT Id, Name, Observation FROM {TableName} ORDER BY Name;",
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return items.AsList();
    }

    public async Task<IReadOnlyList<TItem>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        var search = SearchTerm.Parse(term);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var items = await connection.QueryAsync<TItem>(new CommandDefinition(
            $"""
            SELECT Id, Name, Observation
            FROM {TableName}
            WHERE Id = @Id OR Name LIKE @Pattern
            ORDER BY Name;
            """,
            new { search.Id, search.Pattern },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return items.AsList();
    }

    public async Task<int> AddAsync(TItem entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                $"INSERT INTO {TableName} (Name, Observation) OUTPUT INSERTED.Id VALUES (@Name, @Observation);",
                new { entity.Name, entity.Observation },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw SqlErrors.TranslateSave(ex, entity.Name);
        }
    }

    public async Task UpdateAsync(TItem entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int affected;
        try
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            affected = await connection.ExecuteAsync(new CommandDefinition(
                $"UPDATE {TableName} SET Name = @Name, Observation = @Observation WHERE Id = @Id;",
                new { entity.Id, entity.Name, entity.Observation },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw SqlErrors.TranslateSave(ex, entity.Name);
        }

        if (affected == 0)
        {
            throw EntityNotFoundException.For<TItem>(entity.Id);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        int affected;
        try
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            affected = await connection.ExecuteAsync(new CommandDefinition(
                $"DELETE FROM {TableName} WHERE Id = @id;",
                new { id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw SqlErrors.TranslateDelete(ex);
        }

        if (affected == 0)
        {
            throw EntityNotFoundException.For<TItem>(id);
        }
    }
}

internal sealed class CategoryRepository(IDbConnectionFactory connections) : CatalogRepository<Category>(connections)
{
    protected override string TableName => "dbo.Categories";
}

internal sealed class ProviderRepository(IDbConnectionFactory connections) : CatalogRepository<Provider>(connections)
{
    protected override string TableName => "dbo.Providers";
}

internal sealed class PaymentMethodRepository(IDbConnectionFactory connections) : CatalogRepository<PaymentMethod>(connections)
{
    protected override string TableName => "dbo.PaymentMethods";
}
