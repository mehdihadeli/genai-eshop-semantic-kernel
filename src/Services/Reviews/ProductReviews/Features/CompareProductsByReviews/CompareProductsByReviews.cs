using BuildingBlocks.Exceptions;
using BuildingBlocks.Extensions;
using GenAIEshop.Reviews.Shared.Contracts;
using GenAIEshop.Reviews.Shared.Dtos;
using Mediator;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.ProductReviews.Features.CompareProductsByReviews;

public sealed record CompareProductsByReviews(List<Guid> ProductIds) : IQuery<CompareProductsByReviewsResult>
{
    public static CompareProductsByReviews Of(params Guid[] productIds)
    {
        if (productIds.Length < 2)
            throw new ValidationException("At least two product IDs are required for comparison.");

        foreach (var productId in productIds)
            productId.NotBeEmpty();

        return new CompareProductsByReviews(productIds.ToList());
    }
}

public sealed class CompareProductsByReviewsHandler(
    [FromKeyedServices(GenAIEshop.Shared.Constants.Agents.ReviewsAgent)] Agent reviewsAgent,
    ICatalogServiceClient catalogServiceClient,
    ILogger<CompareProductsByReviewsHandler> logger
) : IQueryHandler<CompareProductsByReviews, CompareProductsByReviewsResult>
{
    public async ValueTask<CompareProductsByReviewsResult> Handle(CompareProductsByReviews query, CancellationToken ct)
    {
        logger.LogInformation("Comparing {ProductCount} products by reviews", query.ProductIds.Count);

        var products =
            await catalogServiceClient.GetProductsByIdAsync(query.ProductIds, ct)
            ?? throw new InvalidOperationException("One or more products not found in catalog.");

        foreach (ProductDto product in products)
        {
            if (!product.IsAvailable)
                throw new InvalidOperationException($"Product '{product.Name}' is currently unavailable.");
        }

        var comparisonRequest =
            @$"Compare products with ids `{string.Join(',', query.ProductIds)}` based on their reviews and provide a comparative analysis.";

        var agentResponse = reviewsAgent.InvokeAsync(message: comparisonRequest, cancellationToken: ct);

        string message = string.Empty;
        await foreach (var item in agentResponse.ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(item.Message.Content))
                continue;
            message = item.Message.Content;
            break;
        }

        return new CompareProductsByReviewsResult(
            ProductIds: query.ProductIds,
            ComparisonAnalysis: message,
            GeneratedAt: DateTime.UtcNow
        );
    }
}

public sealed record CompareProductsByReviewsResult(
    List<Guid> ProductIds,
    string ComparisonAnalysis,
    DateTime GeneratedAt
);
