using A2A;
using A2A.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Agents.A2A;

namespace BuildingBlocks.AI.A2A;

// ref: https://devblogs.microsoft.com/foundry/building-ai-agents-a2a-dotnet-sdk/
// https://github.com/a2aproject/a2a-dotnet/tree/main/samples
// https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/Demos/A2AClientServer
// https://a2aprotocol.ai/docs/guide/a2a-dotnet-sdk

public static class A2AClientExtensions
{
    public static void AddA2AClient(
        this IHostApplicationBuilder builder,
        string agentName,
        string agentHostUrl,
        string agentPath
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentHostUrl);
        var agentPathWithoutSlash = agentPath.TrimStart('/');

        if (
            !Uri.TryCreate($"{agentHostUrl}/" + agentPathWithoutSlash, UriKind.Absolute, out var agentUri)
            || !agentUri.IsAbsoluteUri
        )
        {
            throw new ArgumentException("Agent URI must be a valid absolute URI.", nameof(agentHostUrl));
        }

        // client for agent for doing service discovery for httpclient and use for SendMessage through an a2a client
        // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Agents/A2A/A2AAgent.cs#L178
        builder.Services.AddHttpClient(agentName, client => client.BaseAddress = agentUri);

        // use external service a2a based on http protocol, not json/rpc
        builder.Services.AddKeyedSingleton<A2AAgent>(
            agentName,
            (sp, _) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(agentName);
                return CreateAgentAsync(httpClient, agentUri, agentPathWithoutSlash)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        );

        // https://a2aprotocol.ai/docs/guide/a2a-dotnet-sdk#_10-2-observability
        builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing.AddSource(TaskManager.ActivitySource.Name).AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
            );
    }

    // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/A2AClientServer/A2AClient/HostClientAgent.cs#L61
    private static async Task<A2AAgent> CreateAgentAsync(HttpClient httpClient, Uri agentHostUri, string agentPath)
    {
        ArgumentNullException.ThrowIfNull(agentHostUri);

        var client = new A2AClient(agentHostUri, httpClient);
        // resolve `agent card` from a2a http discovery endpoint `.well-known/agent-card.json` in the external service
        var cardResolver = new A2ACardResolver(
            baseUrl: agentHostUri,
            httpClient: httpClient,
            agentCardPath: $"{agentPath}/{EndpointRouteBuilderExtensions.AgentCardPath}"
        );
        var agentCard = await cardResolver.GetAgentCardAsync();

        return new A2AAgent(client, agentCard);
    }
}
