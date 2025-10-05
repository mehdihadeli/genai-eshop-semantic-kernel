using BuildingBlocks.Extensions;
using GenAIEshop.Reviews.Shared.Contracts;
using Mediator;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.ProductReviews.Features.GetProductQualitySummary;

public sealed record GetProductQualitySummary(Guid ProductId) : IQuery<GetProductQualitySummaryResult>
{
    public static GetProductQualitySummary Of(Guid productId)
    {
        productId.NotBeEmpty();
        return new GetProductQualitySummary(productId);
    }
}

public sealed class GetProductQualitySummaryHandler(
    [FromKeyedServices(GenAIEshop.Shared.Constants.Agents.ReviewsAgent)] Agent reviewsAgent,
    ICatalogServiceClient catalogServiceClient,
    ILogger<GetProductQualitySummaryHandler> logger
) : IQueryHandler<GetProductQualitySummary, GetProductQualitySummaryResult>
{
    public async ValueTask<GetProductQualitySummaryResult> Handle(GetProductQualitySummary query, CancellationToken ct)
    {
        logger.LogInformation("Getting quality summary for product {ProductId}", query.ProductId);

        var product =
            await catalogServiceClient.GetProductByIdAsync(query.ProductId, ct)
            ?? throw new InvalidOperationException($"Product {query.ProductId} not found in catalog.");

        if (!product.IsAvailable)
            throw new InvalidOperationException($"Product '{product.Name}' is currently unavailable.");

        var summaryRequest =
            @$"Provide a concise quality summary and classification for product id `{query.ProductId}` based on review analysis.";

        var agentResponse = reviewsAgent.InvokeAsync(message: summaryRequest, cancellationToken: ct);

        string message = string.Empty;
        await foreach (var item in agentResponse.ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(item.Message.Content))
                continue;
            message = item.Message.Content;
            break;
        }

        return new GetProductQualitySummaryResult(
            ProductId: query.ProductId,
            QualitySummary: message,
            GeneratedAt: DateTime.UtcNow
        );
    }
}

public sealed record GetProductQualitySummaryResult(Guid ProductId, string QualitySummary, DateTime GeneratedAt);
