namespace Supermarket.Core.Abstractions;

/// <summary>
/// Raised when an operation would break a business rule, for example selling more units than
/// are in stock. The message is safe to show to users.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException()
    {
    }

    public BusinessRuleException(string message)
        : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
