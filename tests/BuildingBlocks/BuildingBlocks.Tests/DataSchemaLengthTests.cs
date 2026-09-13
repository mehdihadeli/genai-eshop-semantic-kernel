namespace BuildingBlocks.Tests;

public class DataSchemaLengthTests
{
    [Fact]
    public void MaxText_has_expected_length()
    {
        DataSchemaLength.MaxText.ShouldBe(10000);
    }
}
