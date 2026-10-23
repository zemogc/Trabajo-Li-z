using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Supermarket.Core.Domain;

/// <summary>
/// Base type for simple lookup entities that only carry a name and an optional observation.
/// </summary>
public abstract class CatalogItem : IEntity
{
    public const int NameMaxLength = 50;
    public const int ObservationMaxLength = 200;

    [DisplayName("Código")]
    public int Id { get; set; }

    [DisplayName("Nombre")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(NameMaxLength, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("Observación")]
    [StringLength(ObservationMaxLength, ErrorMessage = "La observación no puede superar {1} caracteres.")]
    public string? Observation { get; set; }

    public bool IsNew => Id == 0;

    public override string ToString() => Name;
}
