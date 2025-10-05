using System.ClientModel;
using System.Data.Common;
using A2A;
using A2A.AspNetCore;
using Azure.AI.OpenAI;
using BuildingBlocks.Constants;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using OllamaSharp;
using OpenAI;

#pragma warning disable SKEXP0010

namespace BuildingBlocks.AI.SemanticKernel;

public static class DependencyInjectionExtensions
{
    private const string ActivitySourceName = "Microsoft.SemanticKernel*";

    public static IHostApplicationBuilder AddSemanticKernel(this IHostApplicationBuilder builder)
    {
        var options = builder.Configuration.BindOptions<SemanticKernelOptions>();
        builder.Services.AddConfigurationOptions<SemanticKernelOptions>();

        builder.Services.AddKernel();
        builder.AddSemanticChatCompletion(options);
        builder.AddSemanticEmbeddingGenerator(options);

        AddOpenTelemetry(builder);

        return builder;
    }

    private static void AddSemanticChatCompletion(this IHostApplicationBuilder builder, SemanticKernelOptions options)
    {
        switch (options.ChatProviderType)
        {
            case ProviderType.Ollama:
                // https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/ollama?tabs=dotnet-cli%2Cdocker#client-integration
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-Ollama%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                if (builder.Configuration.GetConnectionString(AspireResources.OllamaChat) is { } connectionString)
                {
                    var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

                    if (connectionBuilder.ContainsKey("Endpoint"))
                    {
                        options.ChatEndpoint = connectionBuilder["Endpoint"].ToString();
                    }

                    if (connectionBuilder.ContainsKey("Model"))
                    {
                        options.ChatModel = (string)connectionBuilder["Model"];
                    }
                }

                ArgumentException.ThrowIfNullOrEmpty(options.ChatEndpoint);
                ArgumentException.ThrowIfNullOrEmpty(options.ChatModel);

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                OllamaApiClient ollamaClient = new(options.ChatEndpoint, options.ChatModel);
                builder.Services.AddOllamaChatCompletion(ollamaClient);
                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-AzureOpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatDeploymentName);
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                var azureClient = new AzureOpenAIClient(
                    // The Azure OpenAI resource endpoint to use. This should not include model deployment or operation information. For example: https://my-resource.openai.azure.com.
                    new Uri(options.ChatEndpoint),
                    new ApiKeyCredential(options.ChatApiKey)
                );

                builder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatDeploymentName,
                    modelId: options.ChatModel,
                    azureOpenAIClient: azureClient
                );

                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-OpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                // supports both `openai` and `openrouter` endpoints
                string openaiEndpoint = options.ChatEndpoint ?? "https://api.openai.com/v1";

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(options.ChatApiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(openaiEndpoint), EnableDistributedTracing = true }
                );

                builder.Services.AddOpenAIChatCompletion(openAIClient: openAiClient, modelId: options.ChatModel);

                break;
        }
    }

    private static void AddSemanticEmbeddingGenerator(
        this IHostApplicationBuilder builder,
        SemanticKernelOptions options
    )
    {
        if (string.IsNullOrEmpty(options.EmbeddingEndpoint) && string.IsNullOrEmpty(options.EmbeddingModel))
            throw new ArgumentException("Embedding endpoint or model is not configured.");

        switch (options.EmbeddingProviderType)
        {
            case ProviderType.Ollama:
                // https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/ollama?tabs=dotnet-cli%2Cdocker#client-integration
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-Ollama&pivots=programming-language-csharp
                if (builder.Configuration.GetConnectionString(AspireResources.OllamaEmbedding) is { } connectionString)
                {
                    var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

                    if (connectionBuilder.ContainsKey("Endpoint"))
                    {
                        options.EmbeddingEndpoint = connectionBuilder["Endpoint"].ToString();
                    }

                    if (connectionBuilder.ContainsKey("Model"))
                    {
                        options.EmbeddingModel = (string)connectionBuilder["Model"];
                    }
                }

                if (string.IsNullOrEmpty(options.EmbeddingEndpoint) || string.IsNullOrEmpty(options.EmbeddingModel))
                    return;

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                var ollamaClient = new OllamaApiClient(options.EmbeddingEndpoint, options.EmbeddingModel);
                builder.Services.AddOllamaEmbeddingGenerator(ollamaClient);
                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingDeploymentName);

                var azureClient = new AzureOpenAIClient(
                    // The Azure OpenAI resource endpoint to use. This should not include model deployment or operation information. For example: https://my-resource.openai.azure.com.
                    new Uri(options.EmbeddingEndpoint),
                    new ApiKeyCredential(options.ChatApiKey)
                );

                builder.Services.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: options.EmbeddingDeploymentName,
                    azureOpenAIClient: azureClient,
                    modelId: options.EmbeddingModel
                );

                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-OpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);
                var openAiClient = new OpenAIClient(apiKey: options.EmbeddingApiKey);

                builder.Services.AddOpenAIEmbeddingGenerator(
                    openAIClient: openAiClient,
                    modelId: options.EmbeddingModel
                );
                break;
        }
    }

    private static void AddOpenTelemetry(IHostApplicationBuilder builder)
    {
        // https://devblogs.microsoft.com/semantic-kernel/observability-in-semantic-kernel/
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/gen-ai/README.md
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/enterprise-readiness/observability/telemetry-with-aspire-dashboard?tabs=Powershell&pivots=programming-language-csharp
        AppContext.SetSwitch(
            "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive",
            builder.Environment.IsDevelopment()
        );

        builder
            .Services.AddOpenTelemetry()
            .WithTracing(x => x.AddSource(ActivitySourceName))
            .WithMetrics(x => x.AddMeter(ActivitySourceName));

        builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing.AddSource(TaskManager.ActivitySource.Name).AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
            );
    }
}
