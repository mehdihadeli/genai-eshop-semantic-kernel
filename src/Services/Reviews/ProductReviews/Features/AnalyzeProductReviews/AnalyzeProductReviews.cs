using BuildingBlocks.AI.SemanticKernel;
using BuildingBlocks.Extensions;
using GenAIEshop.Reviews.Shared.Contracts;
using GenAIEshop.Reviews.Shared.Services;
using Mediator;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.ProductReviews.Features.AnalyzeProductReviews;

public sealed record AnalyzeProductReviews(Guid ProductId, AgentOrchestrationType AgentOrchestrationType)
    : ICommand<AnalyzeProductReviewsResult>
{
    public static AnalyzeProductReviews Of(Guid productId, AgentOrchestrationType agentOrchestrationType)
    {
        productId.NotBeEmpty();
        return new AnalyzeProductReviews(productId, agentOrchestrationType);
    }
}

public sealed class AnalyzeProductReviewsHandler(
    [FromKeyedServices(GenAIEshop.Shared.Constants.Agents.ReviewsAgent)] Agent agent,
    IReviewsOrchestrationService reviewsOrchestrationService,
    ICatalogServiceClient catalogServiceClient,
    ILogger<AnalyzeProductReviewsHandler> logger
) : ICommandHandler<AnalyzeProductReviews, AnalyzeProductReviewsResult>
{
    public async ValueTask<AnalyzeProductReviewsResult> Handle(AnalyzeProductReviews command, CancellationToken ct)
    {
        logger.LogInformation("Analyzing reviews for product {ProductId}", command.ProductId);

        var product =
            await catalogServiceClient.GetProductByIdAsync(command.ProductId, ct)
            ?? throw new InvalidOperationException($"Product {command.ProductId} not found in catalog.");

        if (!product.IsAvailable)
            throw new InvalidOperationException($"Product '{product.Name}' is currently unavailable.");

        var analysisRequest =
            $"Please analyze all reviews for product with id `{command.ProductId}` and perform sentiment analysis, summarization and provide comprehensive quality assessment with sentiment analysis and key insights.";

        string message = string.Empty;
        switch (command.AgentOrchestrationType)
        {
            case AgentOrchestrationType.Normal:
                var agentResponse = agent.InvokeAsync(message: analysisRequest, cancellationToken: ct);

                await foreach (var item in agentResponse)
                {
                    if (string.IsNullOrWhiteSpace(item.Message.Content))
                        continue;
                    message = item.Message.Content;
                    break;
                }
                break;
            case AgentOrchestrationType.Sequential:
                message = await reviewsOrchestrationService.AnalyzeReviewsUsingSequentialOrchestrationAsync(
                    analysisRequest
                );
                break;
            case AgentOrchestrationType.GroupChat:
                message = await reviewsOrchestrationService.AnalyzeReviewsUsingChatGroupOrchestrationAsync(
                    analysisRequest
                );
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new AnalyzeProductReviewsResult(
            ProductId: command.ProductId,
            Analysis: message,
            GeneratedAt: DateTime.UtcNow
        );
    }
}

public sealed record AnalyzeProductReviewsResult(Guid ProductId, string Analysis, DateTime GeneratedAt);
