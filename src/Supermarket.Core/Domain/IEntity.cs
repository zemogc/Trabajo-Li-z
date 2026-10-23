namespace Supermarket.Core.Domain;

/// <summary>
/// An entity persisted with a database-generated identifier and a human readable name.
/// </summary>
public interface IEntity
{
    int Id { get; set; }

    string Name { get; set; }

    /// <summary>
    /// <see langword="true"/> while the entity has not been persisted yet.
    /// </summary>
    bool IsNew { get; }
}
