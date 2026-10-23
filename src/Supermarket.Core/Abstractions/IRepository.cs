using Supermarket.Core.Domain;

namespace Supermarket.Core.Abstractions;

/// <summary>
/// Persistence contract shared by every entity managed by the application.
/// </summary>
/// <remarks>
/// Implementations translate storage specific failures into <see cref="DataConflictException"/>
/// and <see cref="EntityNotFoundException"/> so callers never depend on the database provider.
/// </remarks>
public interface IRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the entities whose identifier equals <paramref name="term"/> or whose name contains it.
    /// </summary>
    Task<IReadOnlyList<TEntity>> SearchAsync(string term, CancellationToken cancellationToken = default);

    /// <returns>The identifier generated for the new entity.</returns>
    Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
