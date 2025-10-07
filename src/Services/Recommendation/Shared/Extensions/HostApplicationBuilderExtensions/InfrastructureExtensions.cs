using BuildingBlocks.AI.A2A;
using BuildingBlocks.AI.MCP;
using BuildingBlocks.AI.SemanticKernel;
using BuildingBlocks.OpenApi;
using BuildingBlocks.ProblemDetails;
using BuildingBlocks.Serialization;
using BuildingBlocks.Versioning;
using GenAIEshop.Recommendation.Shared.Agents;
using GenAIEshop.Shared.Constants;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Recommendation.Shared.Extensions.HostApplicationBuilderExtensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddCustomProblemDetails();

        // Apply to other places rather than controller response like openapi document generation, and customizes the default JSON serialization behavior for Minimal APIs
        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            SystemTextJsonSerializerOptions.SetDefaultOptions(options.SerializerOptions);
        });

        builder.AddVersioning();
        builder.AddAspnetOpenApi(["v1"]);

        AddAIServices(builder);

        // https://github.com/martinothamar/Mediator
        builder.Services.AddMediator(options =>
        {
            //options.Assemblies = handlerScanAssemblies;
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Namespace = "Recommendation";
        });

        return builder;
    }

    private static void AddAIServices(IHostApplicationBuilder builder)
    {
        builder.AddSemanticKernel();

        builder.AddHttpMcpClient(
            mcpClientName: Mcp.SharedMcpTools,
            mcpServerUrl: $"https+http://{AspireApplicationResources.Api.McpServerApi}"
        );

        // ref: https://github.com/modelcontextprotocol/servers/tree/main/src/time
        builder.AddStdioMcpClient(
            mcpClientName: Mcp.DateTimeMcpTools,
            command: "docker",
            arguments: ["run", "-i", "--rm", "-e", "LOCAL_TIMEZONE", "mcp/time"]
        );

        AddAgents(builder);
    }

    private static void AddAgents(IHostApplicationBuilder builder)
    {
        builder.AddA2AClient(
            agentName: GenAIEshop.Shared.Constants.Agents.ReviewsAgent,
            agentHostUrl: $"https+http://{AspireApplicationResources.Api.ReviewsApi}",
            agentPath: "/reviews"
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.ProductRecommendationAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();

                return ProductRecommendationAgent.CreateAgentAsync(kernel).GetAwaiter().GetResult();
            }
        );
    }
}
