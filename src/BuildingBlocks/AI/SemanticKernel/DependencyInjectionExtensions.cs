using A2A;
using A2A.AspNetCore;
using BuildingBlocks.AI.Extensions;
using BuildingBlocks.Constants;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        if (string.IsNullOrEmpty(options.ChatEndpoint) && string.IsNullOrEmpty(options.ChatModel))
            throw new ArgumentException("Chat endpoint or model is not configured.");

        // - In `ChatCompletionAgent`, during `GetChatCompletionService`, it first tries to find a registered `IChatCompletionService` and `PromptExecutionSettings`.
        // If not found, it then looks for a registered `IChatClient`, and after resolving it, attempts to convert it to a `ChatCompletionService` using `AsChatCompletionService`.
        // Finally, it returns an `IChatCompletionService` and calls `GetChatMessageContentsAsync` on the `ChatCompletionService` within `ChatCompletionAgent`.

        // - In `PromptExecutionSettingsExtensions.ToChatOptions`, during the conversion from `PromptExecutionSettings` to `ChatOptions`, if a `FunctionChoiceBehavior` is not provided, the Semantic Kernel does not automatically discover all tools in the kernel or add them to `ChatOptions.Tools`.
        // Based on the `AutoFunctionChoiceBehavior` type, it sets `ChatOptions.ToolMode` to either `ChatToolMode.Auto` or `ChatToolMode.RequireAny`.
        // (When using `FunctionChoiceBehavior.Required(functions)`, you should explicitly pass the required functions.)
        // When using a `ChatCompletionAgent`, you can pass `PromptExecutionSettings`, which will also be used if an `IChatClient` is registered and applied as its `ChatOptions`.
        // However, when using `IChatClient` directly in normal mode, it only accepts `ChatOptions` and does not support `AutoFunctionChoiceBehavior`.
        // Therefore, you must explicitly add all the required tools, and based on the provided tools, you can set `ChatOptions.ToolMode` to either `ChatToolMode.Auto` or `ChatToolMode.RequireAny` to select the appropriate function automatically.

        switch (options.ChatProviderType)
        {
            case ProviderType.Ollama:
                var ollamaApiClientBuilder = builder.AddOllamaApiClient(
                    AspireResources.OllamaChat,
                    settings =>
                    {
                        settings.SelectedModel = options.ChatModel;
                        settings.Endpoint = new Uri(options.ChatEndpoint);
                        settings.DisableTracing = false;
                    }
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                ollamaApiClientBuilder.AddOllamaChatClient(configureOpenTelemetry: otel =>
                    otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IChatCompletionService` which is dedicated to semantic kernel.
                ollamaApiClientBuilder.AddOllamaChatCompletion(configureOpenTelemetry: otel =>
                    otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-AzureOpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatDeploymentName);
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                var azureOpenAIApiClientBuilder = builder.AddAzureOpenAIClient(
                    new AzureOpenAIApiClientSettings
                    {
                        ApiKey = options.ChatApiKey,
                        Endpoint = options.ChatEndpoint,
                        DisableTracing = false,
                    }
                );

                azureOpenAIApiClientBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatDeploymentName,
                    model: options.ChatModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                azureOpenAIApiClientBuilder.AddAzureOpenAIChatClient(
                    deploymentName: options.ChatDeploymentName,
                    model: options.ChatModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/?tabs=csharp-OpenAI%2Cpython-AzureOpenAI%2Cjava-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.ChatApiKey);

                var openAiApiClientBuilder = builder.AddOpenAIClient(
                    new OpenAIApiClientSettings
                    {
                        ApiKey = options.ChatApiKey,
                        Endpoint = options.ChatEndpoint,
                        DisableTracing = false,
                    }
                );

                openAiApiClientBuilder.AddOpenAIChatCompletion(
                    deploymentName: options.ChatDeploymentName,
                    model: options.ChatModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem#semantic-kernel-for-net
                // Register `IChatClient` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                openAiApiClientBuilder.AddOpenAIChatClient(
                    deploymentName: options.ChatDeploymentName,
                    model: options.ChatModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
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
                var ollamaApiClientBuilder = builder.AddOllamaApiClient(
                    AspireResources.OllamaEmbedding,
                    settings =>
                    {
                        settings.SelectedModel = options.EmbeddingModel;
                        settings.Endpoint = new Uri(options.EmbeddingEndpoint);
                    }
                );

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                ollamaApiClientBuilder.AddOllamaEmbeddingGenerator(configureOpenTelemetry: otel =>
                    otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // Register `IEmbeddingGenerationService` which is dedicated to semantic kernel
                ollamaApiClientBuilder.AddOllamaEmbeddingGeneration(configureOpenTelemetry: otel =>
                    otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                break;
            case ProviderType.Azure:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-AzureOpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingDeploymentName);

                var azureOpenAiApiClientBuilder = builder.AddAzureOpenAIClient(
                    new AzureOpenAIApiClientSettings
                    {
                        ApiKey = options.ChatApiKey,
                        Endpoint = options.ChatEndpoint,
                        DisableTracing = false,
                    }
                );

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                azureOpenAiApiClientBuilder.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: options.EmbeddingDeploymentName,
                    model: options.EmbeddingModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // Register `ITextEmbeddingGenerationService` which is dedicated to semantic kernel
                azureOpenAiApiClientBuilder.AddAzureOpenAIEmbeddingGeneration(
                    deploymentName: options.EmbeddingDeploymentName,
                    model: options.EmbeddingModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                break;
            case ProviderType.OpenAI:
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/?tabs=csharp-OpenAI&pivots=programming-language-csharp
                ArgumentException.ThrowIfNullOrEmpty(options.EmbeddingApiKey);

                var openAiApiClientBuilder = builder.AddOpenAIClient(
                    new OpenAIApiClientSettings
                    {
                        ApiKey = options.ChatApiKey,
                        Endpoint = options.ChatEndpoint,
                        DisableTracing = false,
                    }
                );

                // https://devblogs.microsoft.com/semantic-kernel/introducing-new-ollama-connector-for-local-models/
                // Register `IEmbeddingGenerator<string, Embedding<float>>` which is based on `Microsoft.Extensions.AI` abstractions and implemented by semantic kernel connectors. To use the same client abstractions across different AI frameworks.
                openAiApiClientBuilder.AddOpenAIEmbeddingGenerator(
                    deploymentName: options.EmbeddingDeploymentName,
                    model: options.EmbeddingModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
                );

                // Register `ITextEmbeddingGenerationService` which is dedicated to semantic kernel
                openAiApiClientBuilder.AddOpenAIEmbeddingGeneration(
                    deploymentName: options.EmbeddingDeploymentName,
                    model: options.EmbeddingModel,
                    configureOpenTelemetry: otel => otel.EnableSensitiveData = builder.Environment.IsDevelopment()
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
}
