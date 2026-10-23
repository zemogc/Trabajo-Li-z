using System.ComponentModel.DataAnnotations;

namespace Supermarket.Core.Validation;

/// <summary>
/// Validates entities against their data annotation rules.
/// </summary>
public static class EntityValidator
{
    /// <returns>The validation messages, or an empty list when the entity is valid.</returns>
    public static IReadOnlyList<string> Validate(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(entity, new ValidationContext(entity), results, validateAllProperties: true);

        return results
            .Select(result => result.ErrorMessage)
            .OfType<string>()
            .ToList();
    }
}
