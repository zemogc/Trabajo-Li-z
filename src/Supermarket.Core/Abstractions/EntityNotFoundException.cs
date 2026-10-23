namespace Supermarket.Core.Abstractions;

/// <summary>
/// Raised when an update or delete targets a record that no longer exists,
/// typically because another user removed it.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException()
    {
    }

    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static EntityNotFoundException For<TEntity>(int id) =>
        new($"{typeof(TEntity).Name} with id {id} was not found.");
}
