using Tests.Shared;

namespace GenAIEshop.McpServer.IntegrationTests;

public class McpServerIntegrationTests : IntegrationTestBase
{
    public McpServerIntegrationTests()
        : base("mcp") { }

    [Fact]
    public async Task Mcp_server_root_is_available_when_external_tests_are_enabled()
    {
        if (!TestHostConfiguration.RunExternalTests)
            Assert.Skip("Set GENAI_RUN_EXTERNAL_TESTS=true to run MCP integration tests.");

        (
            await Client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken)
        ).IsSuccessStatusCode.ShouldBeTrue();
    }
}
