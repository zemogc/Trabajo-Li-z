using Dapper;
using Microsoft.Data.SqlClient;
using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Data.Repositories;

internal sealed class ProductRepository(IDbConnectionFactory connections) : IRepository<Product>
{
    private const string SelectProducts = """
        SELECT p.Id, p.Name, p.Price, p.Stock, p.MinimumStock, p.CategoryId, c.Name AS CategoryName
        FROM dbo.Products AS p
        INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
        """;

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var products = await connection.QueryAsync<Product>(new CommandDefinition(
            $"{SelectProducts} ORDER BY p.Name;",
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return products.AsList();
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        var search = SearchTerm.Parse(term);

        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var products = await connection.QueryAsync<Product>(new CommandDefinition(
            $"""
            {SelectProducts}
            WHERE p.Id = @Id OR p.Name LIKE @Pattern OR c.Name LIKE @Pattern
            ORDER BY p.Name;
            """,
            new { search.Id, search.Pattern },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return products.AsList();
    }

    public async Task<int> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                """
                INSERT INTO dbo.Products (Name, Price, Stock, MinimumStock, CategoryId)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Price, @Stock, @MinimumStock, @CategoryId);
                """,
                new { entity.Name, entity.Price, entity.Stock, entity.MinimumStock, entity.CategoryId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw SqlErrors.TranslateSave(ex, entity.Name);
        }
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int affected;
        try
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            affected = await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE dbo.Products
                SET Name = @Name, Price = @Price, Stock = @Stock, MinimumStock = @MinimumStock, CategoryId = @CategoryId
                WHERE Id = @Id;
                """,
                new { entity.Id, entity.Name, entity.Price, entity.Stock, entity.MinimumStock, entity.CategoryId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw SqlErrors.TranslateSave(ex, entity.Name);
        }

        if (affected == 0)
        {
            throw EntityNotFoundException.For<Product>(entity.Id);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Products WHERE Id = @id;",
            new { id },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (affected == 0)
        {
            throw EntityNotFoundException.For<Product>(id);
        }
    }
}
