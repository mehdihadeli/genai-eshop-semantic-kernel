using BuildingBlocks.Exceptions;
using GenAIEshop.Orders.Orders.Features.UpdatingOrderStatus;
using GenAIEshop.Orders.Orders.Models;

namespace GenAIEshop.Orders.UnitTests;

public class OrderCommandTests
{
    [Fact]
    public void UpdateOrderStatus_of_preserves_order_and_user()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = UpdateOrderStatus.Of(orderId, userId, OrderStatus.Confirmed);

        command.OrderId.ShouldBe(orderId);
        command.UserId.ShouldBe(userId);
        command.NewStatus.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void UpdateOrderStatus_of_rejects_empty_order()
    {
        Should.Throw<ValidationException>(() =>
            UpdateOrderStatus.Of(Guid.Empty, Guid.NewGuid(), OrderStatus.Confirmed)
        );
    }
}
