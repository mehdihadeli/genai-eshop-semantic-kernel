using BuildingBlocks.Extensions;
using GenAIEshop.Orders.Orders.Dtos;
using GenAIEshop.Orders.Orders.Models;
using GenAIEshop.Orders.Shared.Contracts;
using GenAIEshop.Orders.Shared.Data;
using Mediator;

namespace GenAIEshop.Orders.Orders.Features.CheckoutCart;

public sealed record CheckoutCarts(Guid UserId, string ShippingAddress) : ICommand<CheckoutResult>
{
    public static CheckoutCarts Of(Guid userId, string? shippingAddress)
    {
        userId.NotBeEmpty();
        shippingAddress.NotBeNullOrWhiteSpace();

        return new CheckoutCarts(userId, shippingAddress);
    }
}

public sealed class CheckoutHandler(
    ICartsServiceClient cartsServiceClient,
    OrdersDbContext dbContext,
    ICatalogServiceClient catalogServiceClient,
    ILogger<CheckoutHandler> logger
) : ICommandHandler<CheckoutCarts, CheckoutResult>
{
    public async ValueTask<CheckoutResult> Handle(CheckoutCarts carts, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting checkout for user {UserId}", carts.UserId);

        // 1. Get cart
        var cart =
            await cartsServiceClient.GetCartAsync(carts.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found or empty.");

        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Cannot checkout empty cart.");

        foreach (var item in cart.Items)
        {
            var product =
                await catalogServiceClient.GetProductByIdAsync(item.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {item.ProductId} not found in catalog.");

            if (!product.IsAvailable)
                throw new InvalidOperationException($"Product '{product.Name}' is currently unavailable.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = carts.UserId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = carts.ShippingAddress,
            CreatedAt = cart.CreatedAt,
            CreatedBy = cart.CreatedBy,
            LastModifiedAt = DateTime.UtcNow,
            LastModifiedBy = carts.UserId,
            Items = cart
                .Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                })
                .ToList(),
        };

        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cartsServiceClient.ClearCartAsync(carts.UserId, cancellationToken);

        return new CheckoutResult(order.ToDto());
    }
}

public sealed record CheckoutResult(OrderDto Order);
