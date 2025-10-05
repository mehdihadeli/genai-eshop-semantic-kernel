using BuildingBlocks.Extensions;
using Mediator;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Recommendation.Recommendations.Features.GettingRecommendation;

public sealed record GetProductRecommendations(string Query) : IQuery<GetProductRecommendationsResult>
{
    public static GetProductRecommendations Of(string query)
    {
        query.NotBeNullOrWhiteSpace();
        return new GetProductRecommendations(query);
    }
}

public sealed class GetProductRecommendationsHandler(
    [FromKeyedServices(GenAIEshop.Shared.Constants.Agents.ProductRecommendationAgent)] Agent productRecommendationAgent,
    ILogger<GetProductRecommendationsHandler> logger
) : IQueryHandler<GetProductRecommendations, GetProductRecommendationsResult>
{
    public async ValueTask<GetProductRecommendationsResult> Handle(
        GetProductRecommendations query,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Generating product recommendations for query: {Query}", query.Query);

        var recommendationRequest = $"Please provide product recommendations for: {query.Query}";

        var agentResponse = productRecommendationAgent.InvokeAsync(
            message: recommendationRequest,
            cancellationToken: cancellationToken
        );

        string recommendations = string.Empty;
        await foreach (var item in agentResponse.ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(item.Message.Content))
                continue;
            recommendations = item.Message.Content;
            break;
        }

        return new GetProductRecommendationsResult(Recommendations: recommendations, GeneratedAt: DateTime.UtcNow);
    }
}

public sealed record GetProductRecommendationsResult(string Recommendations, DateTime GeneratedAt);
