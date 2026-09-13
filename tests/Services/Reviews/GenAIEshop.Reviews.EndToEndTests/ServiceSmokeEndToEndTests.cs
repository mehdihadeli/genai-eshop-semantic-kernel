using Tests.Shared;

namespace GenAIEshop.Reviews.EndToEndTests;

public class ServiceSmokeEndToEndTests : EndToEndTestBase
{
    public ServiceSmokeEndToEndTests()
        : base("reviews") { }

    [Fact]
    public async Task Service_root_supports_end_to_end_smoke_check()
    {
        if (!TestHostConfiguration.RunExternalTests)
            Assert.Skip("Set GENAI_RUN_EXTERNAL_TESTS=true to run service end-to-end tests.");

        (
            await Client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken)
        ).IsSuccessStatusCode.ShouldBeTrue();
    }
}
