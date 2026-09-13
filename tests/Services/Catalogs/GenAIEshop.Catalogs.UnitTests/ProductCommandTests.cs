using BuildingBlocks.Exceptions;
using GenAIEshop.Catalogs.Products.Features.CreatingProduct;

namespace GenAIEshop.Catalogs.UnitTests;

public class ProductCommandTests
{
    [Fact]
    public void CreateProduct_of_preserves_valid_product_data()
    {
        var command = CreateProduct.Of("Keyboard", "Mechanical keyboard", 99.99m, "image.png", true);

        command.Name.ShouldBe("Keyboard");
        command.Description.ShouldBe("Mechanical keyboard");
        command.Price.ShouldBe(99.99m);
        command.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void CreateProduct_of_rejects_zero_price()
    {
        Should.Throw<ValidationException>(() =>
            CreateProduct.Of("Keyboard", "Mechanical keyboard", 0, "image.png", true)
        );
    }
}
