using System.ComponentModel;
using GenAIEshop.Reviews.ProductReviews.Dtos;
using GenAIEshop.Reviews.Shared.Contracts;
using Mediator;
using Microsoft.SemanticKernel;

namespace GenAIEshop.Reviews.Shared.Plugins;

// https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/
// https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-functions?pivots=programming-language-csharp
// https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/
// https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/
public sealed class ReviewsPlugin(ISender sender, ICatalogServiceClient catalogServiceClient)
{
    [KernelFunction(nameof(GetReviewsByProductId))]
    [Description("Retrieves all reviews for a specific product through product `id` which is guid")]
    public async Task<IReadOnlyCollection<ReviewDetailsDto>> GetReviewsByProductId(
        [Description("Product `id` of type guid to get reviews for this product")] Guid productId
    )
    {
        var reviewsByProductResult = await sender.Send(
            ProductReviews.Features.GettingReviewsByProduct.GetReviewsByProduct.Of(productId, 1, int.MaxValue)
        );

        var product =
            await catalogServiceClient.GetProductByIdAsync(productId)
            ?? throw new InvalidOperationException($"Product {productId} not found in catalog.");

        var items = reviewsByProductResult
            .Reviews.Select(x => new ReviewDetailsDto(
                x.Id,
                x.ProductId,
                product.Name,
                product.Description,
                x.UserId,
                x.Rating,
                x.Comment,
                x.CreatedAt
            ))
            .ToList();

        return items;
    }

    [KernelFunction(nameof(GetReviewsByProductIds))]
    [Description("Retrieves all reviews for a list of product ids (guids). Returns reviews grouped by product.")]
    public async Task<Dictionary<Guid, IReadOnlyCollection<ReviewDetailsDto>>> GetReviewsByProductIds(
        [Description("List of product ids (guids) to get reviews for")] IReadOnlyCollection<Guid> productIds
    )
    {
        if (productIds == null || productIds.Count == 0)
            throw new ArgumentException("At least one productId must be provided.", nameof(productIds));

        var products = await catalogServiceClient.GetProductsByIdAsync(productIds);

        var productMap = products.ToDictionary(p => p.Id);

        var result = new Dictionary<Guid, IReadOnlyCollection<ReviewDetailsDto>>();

        foreach (var productId in productIds)
        {
            if (!productMap.TryGetValue(productId, out var product))
                continue;

            var reviewsByProductResult = await sender.Send(
                ProductReviews.Features.GettingReviewsByProduct.GetReviewsByProduct.Of(productId, 1, int.MaxValue)
            );

            var items = reviewsByProductResult
                .Reviews.Select(x => new ReviewDetailsDto(
                    x.Id,
                    x.ProductId,
                    product.Name,
                    product.Description,
                    x.UserId,
                    x.Rating,
                    x.Comment,
                    x.CreatedAt
                ))
                .ToList();

            result[productId] = items;
        }

        return result;
    }

    // [KernelFunction(nameof(GetRecentReviews))]
    // [Description(
    //     "Retrieves recent reviews for a product within a specified time frame through product name, description and id"
    // )]
    // public async Task<IReadOnlyCollection<ReviewDetailsDto>> GetRecentReviews(
    //     [Description("Product id to get reviews for this product")] Guid productId,
    //     [Description("Number of days to look back for recent reviews")] int daysBack = 30
    // )
    // {
    //     var reviews = (
    //         await sender.Send(
    //             ProductReviews.Features.GettingReviewsByProduct.GetReviewsByProduct.Of(productId, 1, int.MaxValue)
    //         )
    //     ).Reviews;
    //
    //     var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
    //
    //     var product =
    //         await catalogServiceClient.GetProductByIdAsync(productId)
    //         ?? throw new InvalidOperationException($"Product {productId} not found in catalog.");
    //
    //     var items = reviews
    //         .Where(r => r.CreatedAt >= cutoffDate)
    //         .Select(x => new ReviewDetailsDto(
    //             x.Id,
    //             x.ProductId,
    //             product.Name,
    //             product.Description,
    //             x.UserId,
    //             x.Rating,
    //             x.Comment,
    //             x.CreatedAt
    //         ))
    //         .ToList();
    //
    //     return items;
    // }
}
