using BuildingBlocks.Exceptions;
using GenAIEshop.Recommendation.Recommendations.Features.GettingRecommendation;

namespace GenAIEshop.Recommendation.UnitTests;

public class RecommendationQueryTests
{
    [Fact]
    public void GetProductRecommendations_of_preserves_query()
    {
        var query = GetProductRecommendations.Of("wireless headphones");

        query.Query.ShouldBe("wireless headphones");
    }

    [Fact]
    public void GetProductRecommendations_of_rejects_blank_query()
    {
        Should.Throw<ValidationException>(() => GetProductRecommendations.Of(" "));
    }
}
