using System.Data.Common;
using A2A;
using A2A.AspNetCore;
using BuildingBlocks.Constants;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable SKEXP0010

namespace BuildingBlocks.AI.SemanticKernel;

public static class DependencyInjectionExtensions
{
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
                CheckAspireChatModelOption(builder, options);

                ArgumentException.ThrowIfNullOrEmpty(options.ChatEndpoint);
                ArgumentException.ThrowIfNullOrEmpty(options.ChatModel);

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IChatCompletionService` which is dedicated to semantic kernel.
                builder.Services.AddOllamaChatCompletion(
                    modelId: options.ChatModel,
                    endpoint: new Uri(options.ChatEndpoint)
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddOllamaChatClient(
                    modelId: options.ChatModel,
                    endpoint: new Uri(options.ChatEndpoint)
                );

                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-AzureOpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatDeploymentName);
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                builder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatDeploymentName,
                    // The Azure OpenAI resource endpoint to use. This should not include model deployment or operation information. For example: https://my-resource.openai.azure.com.
                    endpoint: options.ChatEndpoint,
                    apiKey: options.ChatApiKey,
                    modelId: options.ChatModel
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddAzureOpenAIChatClient(
                    deploymentName: options.ChatDeploymentName,
                    // The Azure OpenAI resource endpoint to use. This should not include model deployment or operation information. For example: https://my-resource.openai.azure.com.
                    endpoint: options.ChatEndpoint,
                    apiKey: options.ChatApiKey,
                    modelId: options.ChatModel
                );
                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-OpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                // supports both `openai` and `openrouter` endpoints
                string openaiEndpoint = options.ChatEndpoint ?? "https://api.openai.com/v1";

                builder.Services.AddOpenAIChatCompletion(
                    modelId: options.ChatModel,
                    endpoint: new Uri(openaiEndpoint),
                    apiKey: options.ChatApiKey
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddOpenAIChatClient(
                    modelId: options.ChatModel,
                    endpoint: new Uri(openaiEndpoint),
                    apiKey: options.ChatApiKey
                );

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
                CheckAspireEmbeddingModelOption(builder, options);

                if (string.IsNullOrEmpty(options.EmbeddingEndpoint) || string.IsNullOrEmpty(options.EmbeddingModel))
                    return;

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddOllamaEmbeddingGenerator(
                    modelId: options.EmbeddingModel,
                    endpoint: new Uri(options.EmbeddingEndpoint)
                );

                // Register `ITextEmbeddingGenerationService` which is dedicated to semantic kernel
                builder.Services.AddOllamaTextEmbeddingGeneration(
                    modelId: options.EmbeddingModel,
                    endpoint: new Uri(options.EmbeddingEndpoint)
                );
                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingDeploymentName);

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: options.EmbeddingDeploymentName,
                    endpoint: options.EmbeddingEndpoint,
                    modelId: options.EmbeddingModel,
                    apiKey: options.ChatApiKey
                );

                // Register `ITextEmbeddingGenerationService` which is dedicated to semantic kernel
                builder.Services.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: options.EmbeddingDeploymentName,
                    endpoint: options.EmbeddingEndpoint,
                    modelId: options.EmbeddingModel,
                    apiKey: options.ChatApiKey
                );
                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-OpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                builder.Services.AddOpenAIEmbeddingGenerator(
                    modelId: options.EmbeddingModel,
                    apiKey: options.EmbeddingApiKey
                );

                // Register `ITextEmbeddingGenerationService` which is dedicated to semantic kernel
                builder.Services.AddOpenAITextEmbeddingGeneration(
                    modelId: options.EmbeddingModel,
                    apiKey: options.EmbeddingApiKey
                );
                break;
        }
    }

    private static void AddOpenTelemetry(IHostApplicationBuilder builder)
    {
        // https://devblogs.microsoft.com/semantic-kernel/observability-in-semantic-kernel/
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/gen-ai/README.md
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/enterprise-readiness/observability/telemetry-with-aspire-dashboard?tabs=Powershell&pivots=programming-language-csharp
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

        var activitySourceName = "Microsoft.SemanticKernel*";
        var extensionsAiActivity = "*Microsoft.Extensions.AI";

        builder
            .Services.AddOpenTelemetry()
            .WithTracing(x =>
            {
                x.AddSource(activitySourceName);
                x.AddSource(extensionsAiActivity);
            })
            .WithMetrics(x => x.AddMeter(activitySourceName));

        builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing.AddSource(TaskManager.ActivitySource.Name).AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
            );
    }

    private static void CheckAspireEmbeddingModelOption(IHostApplicationBuilder builder, SemanticKernelOptions options)
    {
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
    }

    private static void CheckAspireChatModelOption(IHostApplicationBuilder builder, SemanticKernelOptions options)
    {
        if (builder.Configuration.GetConnectionString(AspireResources.OllamaChat) is { } connectionString)
        {
            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (connectionBuilder.ContainsKey("Endpoint"))
            {
                // override existing chat endpoint with aspire connection string
                options.ChatEndpoint = connectionBuilder["Endpoint"].ToString();
            }

            if (connectionBuilder.ContainsKey("Model"))
            {
                // override the existing chat model with aspire connection string
                options.ChatModel = (string)connectionBuilder["Model"];
            }
        }
    }
}
