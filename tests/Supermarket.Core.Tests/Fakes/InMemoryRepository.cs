using Supermarket.Core.Abstractions;
using Supermarket.Core.Domain;

namespace Supermarket.Core.Tests.Fakes;

internal sealed class InMemoryRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly List<TEntity> _items = [];
    private int _nextId = 1;

    /// <summary>When set, the next repository call throws this exception.</summary>
    public Exception? NextFailure { get; set; }

    public IReadOnlyList<TEntity> Items => _items;

    public InMemoryRepository<TEntity> With(params TEntity[] entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _nextId++;
            _items.Add(entity);
        }

        return this;
    }

    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        return Task.FromResult<IReadOnlyList<TEntity>>([.. _items]);
    }

    public Task<IReadOnlyList<TEntity>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        var matches = _items
            .Where(e => e.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == term
                || e.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<TEntity>>(matches);
    }

    public Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        entity.Id = _nextId++;
        _items.Add(entity);
        return Task.FromResult(entity.Id);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        var index = _items.FindIndex(e => e.Id == entity.Id);
        if (index < 0)
        {
            throw EntityNotFoundException.For<TEntity>(entity.Id);
        }

        _items[index] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        if (_items.RemoveAll(e => e.Id == id) == 0)
        {
            throw EntityNotFoundException.For<TEntity>(id);
        }

        return Task.CompletedTask;
    }

    private void ThrowIfFailing()
    {
        if (NextFailure is { } failure)
        {
            NextFailure = null;
            throw failure;
        }
    }
}
