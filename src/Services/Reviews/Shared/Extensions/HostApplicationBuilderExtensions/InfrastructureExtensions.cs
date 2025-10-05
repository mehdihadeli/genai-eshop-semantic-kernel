using BuildingBlocks.AI.SemanticKernel;
using BuildingBlocks.Env;
using BuildingBlocks.OpenApi;
using BuildingBlocks.ProblemDetails;
using BuildingBlocks.Serialization;
using BuildingBlocks.Versioning;
using GenAIEshop.Reviews.Shared.Agents;
using GenAIEshop.Reviews.Shared.Agents.OrchestrationsAgents;
using GenAIEshop.Reviews.Shared.Plugins;
using GenAIEshop.Reviews.Shared.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.Shared.Extensions.HostApplicationBuilderExtensions;

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

        // https://github.com/martinothamar/Mediator

        builder.Services.AddMediator(options =>
        {
            //options.Assemblies = handlerScanAssemblies;
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Namespace = "Reviews";
        });

        AddAIServices(builder);

        return builder;
    }

    private static void AddAIServices(IHostApplicationBuilder builder)
    {
        builder.AddSemanticKernel();

        AddAgents(builder);
    }

    private static void AddAgents(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ReviewsPlugin>();

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.SummarizeAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return SummerizeAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.SentimentAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return SentimentAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.LanguageAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return LanguageAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.ReviewsAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return ReviewsAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.InsightsSynthesizerAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return InsightsSynthesizerAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddKeyedSingleton<Agent>(
            GenAIEshop.Shared.Constants.Agents.ReviewsCollectorAgent,
            (sp, _) =>
            {
                var kernel = sp.GetRequiredService<Kernel>();
                return ReviewsCollectorAgent.CreateAgent(kernel);
            }
        );

        builder.Services.AddSingleton<ReviewsSequentialOrchestrationAgent>();
        builder.Services.AddSingleton<ReviewsChatOrchestrationAgent>();
        builder.Services.AddSingleton<ReviewsHandOffOrchestrationAgent>();
        builder.Services.AddSingleton<IReviewsOrchestrationService, ReviewsOrchestrationService>();
        builder.Services.AddSingleton<IntelligentReviewsChatManager>();
    }
}
