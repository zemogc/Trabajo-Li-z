namespace Supermarket.Core.Abstractions;

/// <summary>
/// Raised when an operation violates a data rule, such as a duplicated name or
/// deleting a record that is still referenced by others. The message is safe to show to users.
/// </summary>
public sealed class DataConflictException : Exception
{
    public DataConflictException()
    {
    }

    public DataConflictException(string message)
        : base(message)
    {
    }

    public DataConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
