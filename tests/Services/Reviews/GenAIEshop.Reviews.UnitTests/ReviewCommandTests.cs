using BuildingBlocks.Exceptions;
using GenAIEshop.Reviews.ProductReviews.Features.CreatingReview;

namespace GenAIEshop.Reviews.UnitTests;

public class ReviewCommandTests
{
    [Fact]
    public void CreateReview_of_accepts_rating_in_valid_range()
    {
        var productId = Guid.NewGuid();

        var command = CreateReview.Of(productId, 5, "Excellent product");

        command.ProductId.ShouldBe(productId);
        command.Rating.ShouldBe(5);
        command.Comment.ShouldBe("Excellent product");
    }

    [Fact]
    public void CreateReview_of_rejects_rating_outside_valid_range()
    {
        Should.Throw<ValidationException>(() => CreateReview.Of(Guid.NewGuid(), 6, "Too high"));
    }
}
