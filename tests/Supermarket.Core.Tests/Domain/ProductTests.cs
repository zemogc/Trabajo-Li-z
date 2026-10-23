using Supermarket.Core.Domain;

namespace Supermarket.Core.Tests.Domain;

public class ProductTests
{
    [Theory]
    [InlineData(4, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, false)]
    [InlineData(0, 0, true)]
    public void Stock_at_or_below_the_minimum_is_low(int stock, int minimum, bool expected) =>
        Assert.Equal(expected, new Product { Stock = stock, MinimumStock = minimum }.IsLowStock);

    [Fact]
    public void New_products_start_with_the_default_minimum_stock()
    {
        var product = new Product();

        Assert.Equal(Product.DefaultMinimumStock, product.MinimumStock);
        Assert.True(product.IsNew);
    }
}
