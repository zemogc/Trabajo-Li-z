using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Supermarket.Core.Domain;

/// <summary>
/// An item offered for sale, always classified under a <see cref="Category"/>.
/// </summary>
public sealed class Product : IEntity
{
    public const int NameMaxLength = 100;
    public const double MaxPrice = 999_999_999.99;

    [DisplayName("Código")]
    public int Id { get; set; }

    [DisplayName("Nombre")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(NameMaxLength, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("Precio")]
    [Range(0.01, MaxPrice, ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Price { get; set; }

    [DisplayName("Stock")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [DisplayName("Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría.")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Read-only projection of the category name, populated when products are queried.
    /// </summary>
    [DisplayName("Categoría")]
    public string? CategoryName { get; set; }

    public bool IsNew => Id == 0;

    public override string ToString() => Name;
}
