using BuildingBlocks.Extensions;
using Mediator;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Recommendation.Recommendations.Features.GettingPersonalizedRecommendation;

public sealed record GetPersonalizedRecommendations(Guid UserId, string? Query, string? Preferences, string? Category)
    : IQuery<GetPersonalizedRecommendationsResult>
{
    public static GetPersonalizedRecommendations Of(Guid userId, string? query, string? preferences, string? category)
    {
        userId.NotBeEmpty();
        return new GetPersonalizedRecommendations(userId, query, preferences, category);
    }
}

public sealed class GetPersonalizedRecommendationsHandler(
    [FromKeyedServices(GenAIEshop.Shared.Constants.Agents.ProductRecommendationAgent)] Agent productRecommendationAgent,
    ILogger<GetPersonalizedRecommendationsHandler> logger
) : IQueryHandler<GetPersonalizedRecommendations, GetPersonalizedRecommendationsResult>
{
    public async ValueTask<GetPersonalizedRecommendationsResult> Handle(
        GetPersonalizedRecommendations query,
        CancellationToken ct
    )
    {
        logger.LogInformation("Generating personalized recommendations for user {UserId}", query.UserId);

        var personalizedRequest = BuildPersonalizedRequest(query);

        var agentResponse = productRecommendationAgent.InvokeAsync(message: personalizedRequest, cancellationToken: ct);

        string recommendations = string.Empty;
        await foreach (var item in agentResponse.ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(item.Message.Content))
                continue;
            recommendations = item.Message.Content;
            break;
        }

        return new GetPersonalizedRecommendationsResult(recommendations, DateTime.UtcNow);
    }

    private static string BuildPersonalizedRequest(GetPersonalizedRecommendations query)
    {
        var request = $"Please provide personalized product recommendations for user {query.UserId}";

        if (!string.IsNullOrWhiteSpace(query.Query))
            request += $" searching for: {query.Query}";

        if (!string.IsNullOrWhiteSpace(query.Preferences))
            request += $". User preferences: {query.Preferences}";

        if (!string.IsNullOrWhiteSpace(query.Category))
            request += $". Preferred category: {query.Category}";

        return request;
    }
}

public sealed record GetPersonalizedRecommendationsResult(string Recommendations, DateTime GeneratedAt);
