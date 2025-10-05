using BuildingBlocks.Extensions;
using GenAIEshop.Orders.Orders.Dtos;
using GenAIEshop.Orders.Orders.Models;
using GenAIEshop.Orders.Shared.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GenAIEshop.Orders.Orders.Features.UpdatingOrderStatus;

public sealed record UpdateOrderStatus(Guid OrderId, Guid UserId, OrderStatus NewStatus)
    : ICommand<UpdateOrderStatusResult>
{
    public static UpdateOrderStatus Of(Guid orderId, Guid userId, OrderStatus newStatus)
    {
        orderId.NotBeEmpty();
        userId.NotBeEmpty();

        return new UpdateOrderStatus(orderId, userId, newStatus);
    }
}

public sealed class UpdateOrderStatusHandler(OrdersDbContext dbContext, ILogger<UpdateOrderStatusHandler> logger)
    : ICommandHandler<UpdateOrderStatus, UpdateOrderStatusResult>
{
    public async ValueTask<UpdateOrderStatusResult> Handle(
        UpdateOrderStatus command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Updating order {OrderId} status to {NewStatus} for user {UserId}",
            command.OrderId,
            command.NewStatus,
            command.UserId
        );

        var order =
            await dbContext.Orders.FirstOrDefaultAsync(
                o => o.Id == command.OrderId && o.UserId == command.UserId,
                cancellationToken
            ) ?? throw new InvalidOperationException("Order not found.");

        if (!IsValidStatusTransition(order.Status, command.NewStatus))
            throw new InvalidOperationException($"Cannot transition order from {order.Status} to {command.NewStatus}.");

        // Update properties
        order.Status = command.NewStatus;
        order.LastModifiedAt = DateTime.UtcNow;
        order.LastModifiedBy = command.UserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateOrderStatusResult(order.ToDto());
    }

    private bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
    {
        return current switch
        {
            OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => next is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => next is OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            _ => false,
        };
    }
}

public sealed record UpdateOrderStatusResult(OrderDto Order);
