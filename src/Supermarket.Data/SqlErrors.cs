using Microsoft.Data.SqlClient;
using Supermarket.Core.Abstractions;

namespace Supermarket.Data;

/// <summary>
/// Translates SQL Server constraint violations into provider independent exceptions
/// with messages that can be shown to the user.
/// </summary>
internal static class SqlErrors
{
    // https://learn.microsoft.com/sql/relational-databases/errors-events/database-engine-events-and-errors
    internal const int ConstraintViolation = 547;
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInUniqueConstraint = 2627;

    public static Exception TranslateSave(SqlException exception, string name) => exception.Number switch
    {
        DuplicateKeyInUniqueIndex or DuplicateKeyInUniqueConstraint =>
            new DataConflictException($"Ya existe un registro con el nombre \"{name}\".", exception),
        ConstraintViolation =>
            new DataConflictException("Los datos no cumplen las reglas de la base de datos. Verifique los valores e intente de nuevo.", exception),
        _ => exception,
    };

    public static Exception TranslateDelete(SqlException exception) => exception.Number switch
    {
        ConstraintViolation =>
            new DataConflictException("No se puede eliminar porque hay otros registros que lo utilizan.", exception),
        _ => exception,
    };
}
