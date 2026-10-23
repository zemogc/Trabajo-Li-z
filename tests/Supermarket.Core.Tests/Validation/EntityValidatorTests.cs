using Supermarket.Core.Domain;
using Supermarket.Core.Validation;

namespace Supermarket.Core.Tests.Validation;

public class EntityValidatorTests
{
    [Fact]
    public void Valid_category_has_no_errors()
    {
        var category = new Category { Name = "Lácteos", Observation = "Leche, queso y yogur" };

        Assert.Empty(EntityValidator.Validate(category));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    public void Catalog_name_must_have_at_least_three_characters(string name)
    {
        var errors = EntityValidator.Validate(new Provider { Name = name });

        Assert.Single(errors);
    }

    [Fact]
    public void Catalog_name_cannot_exceed_maximum_length()
    {
        var method = new PaymentMethod { Name = new string('x', CatalogItem.NameMaxLength + 1) };

        var error = Assert.Single(EntityValidator.Validate(method));
        Assert.Equal("El nombre debe tener entre 3 y 50 caracteres.", error);
    }

    [Fact]
    public void Observation_is_optional_but_limited()
    {
        Assert.Empty(EntityValidator.Validate(new Category { Name = "Aseo", Observation = null }));

        var tooLong = new Category { Name = "Aseo", Observation = new string('x', CatalogItem.ObservationMaxLength + 1) };
        Assert.Single(EntityValidator.Validate(tooLong));
    }

    [Fact]
    public void Valid_product_has_no_errors()
    {
        var product = new Product { Name = "Arroz 500 g", Price = 3200m, Stock = 15, CategoryId = 1 };

        Assert.Empty(EntityValidator.Validate(product));
    }

    [Fact]
    public void Product_reports_every_broken_rule()
    {
        var product = new Product { Name = "", Price = 0m, Stock = -1, MinimumStock = -1, CategoryId = 0 };

        var errors = EntityValidator.Validate(product);

        Assert.Equal(5, errors.Count);
        Assert.Contains("El stock mínimo no puede ser negativo.", errors);
        Assert.Contains("Debe seleccionar una categoría.", errors);
        Assert.Contains("El precio debe ser mayor que cero.", errors);
        Assert.Contains("El stock no puede ser negativo.", errors);
    }

    [Fact]
    public void Validate_rejects_null() =>
        Assert.Throws<ArgumentNullException>(() => EntityValidator.Validate(null!));
}
