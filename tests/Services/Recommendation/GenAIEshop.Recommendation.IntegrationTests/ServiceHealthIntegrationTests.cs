using Tests.Shared;

namespace GenAIEshop.Recommendation.IntegrationTests;

public class ServiceHealthIntegrationTests : IntegrationTestBase
{
    public ServiceHealthIntegrationTests()
        : base("recommendation") { }

    [Fact]
    public async Task Service_root_is_available_when_external_tests_are_enabled()
    {
        if (!TestHostConfiguration.RunExternalTests)
            Assert.Skip("Set GENAI_RUN_EXTERNAL_TESTS=true to run service integration tests.");

        (
            await Client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken)
        ).IsSuccessStatusCode.ShouldBeTrue();
    }
}
