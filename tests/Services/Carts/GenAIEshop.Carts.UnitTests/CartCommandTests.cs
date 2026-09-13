using BuildingBlocks.Exceptions;
using GenAIEshop.Carts.Carts.Features.ClearingCart;

namespace GenAIEshop.Carts.UnitTests;

public class CartCommandTests
{
    [Fact]
    public void ClearCart_of_creates_command_for_valid_user()
    {
        var userId = Guid.NewGuid();

        var command = ClearCart.Of(userId);

        command.UserId.ShouldBe(userId);
    }

    [Fact]
    public void ClearCart_of_rejects_empty_user()
    {
        Should.Throw<ValidationException>(() => ClearCart.Of(Guid.Empty));
    }
}
