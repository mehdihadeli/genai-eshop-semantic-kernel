using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using OllamaSharp;

namespace BuildingBlocks.AI.Extensions;

// https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai/ollama/OllamaExamples/DependencyInjection.cs
// https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai/ollama/OllamaExamples/Middleware.cs
// https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai/ollama/OllamaExamples/ToolCalling.cs
// https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem
// https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/SemanticKernelMigration
public static class OllamaExtensions
{
    private static readonly string DefaultSemanticKernelSourceName = "Microsoft.SemanticKernel.Experimental";
    private static readonly string DefaultExtensionsAISourceName = "Experimental.Microsoft.Extensions.AI";

    public static AspireOllamaApiClientBuilder AddOllamaChatCompletion(
        this AspireOllamaApiClientBuilder aspireOllamaApiClientBuilder,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        aspireOllamaApiClientBuilder.HostBuilder.Services.AddSingleton<IChatCompletionService>(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(aspireOllamaApiClientBuilder.ServiceKey);
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;
            var chatClientBuilder = chatClient.AsBuilder();
            if (!aspireOllamaApiClientBuilder.DisableTracing)
            {
                chatClientBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultSemanticKernelSourceName,
                    configure: configureOpenTelemetry
                );
            }

            if (useCache)
            {
                chatClientBuilder.UseDistributedCache();
            }

            chatClientBuilder = chatClientBuilder.UseLogging().UseKernelFunctionInvocation();
            chatClient = chatClientBuilder.Build(sp);

            return chatClient.AsChatCompletionService();
        });

        return aspireOllamaApiClientBuilder;
    }

    public static IServiceCollection AddOllamaChatCompletion(
        IServiceCollection services,
        IOllamaApiClient ollamaApiClient,
        bool disableTracing = false,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        return services.AddSingleton<IChatCompletionService>(sp =>
        {
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;
            var chatClientBuilder = chatClient.AsBuilder();
            if (!disableTracing)
            {
                chatClientBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultSemanticKernelSourceName,
                    configure: configureOpenTelemetry
                );
            }

            if (useCache)
            {
                chatClientBuilder.UseDistributedCache();
            }

            // using `UseKernelFunctionInvocation` because we want to use semantic kernel features for function calls for both ChatCompletion and ChatClient that use in AgentChatCompletion by selectors
            chatClientBuilder = chatClientBuilder.UseLogging().UseKernelFunctionInvocation();
            chatClient = chatClientBuilder.Build(sp);

            return chatClient.AsChatCompletionService();
        });
    }

    public static IServiceCollection AddOllamaChatCompletion(
        IServiceCollection services,
        string ollamaApiClientName,
        bool disableTracing = false,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        return services.AddSingleton<IChatCompletionService>(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(ollamaApiClientName);
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;
            var chatClientBuilder = chatClient.AsBuilder();
            if (!disableTracing)
            {
                chatClientBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultSemanticKernelSourceName,
                    configure: configureOpenTelemetry
                );
            }

            if (useCache)
            {
                chatClientBuilder.UseDistributedCache();
            }

            chatClientBuilder = chatClientBuilder.UseLogging().UseKernelFunctionInvocation();
            chatClient = chatClientBuilder.Build(sp);

            return chatClient.AsChatCompletionService();
        });
    }

    public static AspireOllamaApiClientBuilder AddOllamaChatClient(
        this AspireOllamaApiClientBuilder builder,
        Action<ChatClientBuilder>? chatClientBuilderConfigs = null,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var chatClientBuilder = builder.HostBuilder.Services.AddChatClient(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(builder.ServiceKey);
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;

            return chatClient;
        });

        if (!builder.DisableTracing)
        {
            chatClientBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            chatClientBuilder.UseDistributedCache();
        }

        chatClientBuilder = chatClientBuilder.UseLogging().UseKernelFunctionInvocation();

        chatClientBuilderConfigs?.Invoke(chatClientBuilder);

        return builder;
    }

    public static ChatClientBuilder AddOllamaChatClient(
        IServiceCollection services,
        IOllamaApiClient ollamaApiClient,
        bool disableTracing = false,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var chatClientBuilder = services.AddChatClient(sp =>
        {
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;

            return chatClient;
        });

        if (!disableTracing)
        {
            chatClientBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            chatClientBuilder.UseDistributedCache();
        }

        return chatClientBuilder.UseLogging().UseKernelFunctionInvocation();
    }

    public static ChatClientBuilder AddOllamaChatClient(
        IServiceCollection services,
        string ollamaApiClientName,
        bool disableTracing = false,
        Action<OpenTelemetryChatClient>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var chatClientBuilder = services.AddChatClient(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(ollamaApiClientName);
            // the implementor of `IOllamaApiClient` also implements `IChatClient` like `OllamaApiClient`
            IChatClient chatClient = (IChatClient)ollamaApiClient;

            return chatClient;
        });

        if (!disableTracing)
        {
            chatClientBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            chatClientBuilder.UseDistributedCache();
        }

        return chatClientBuilder.UseLogging().UseKernelFunctionInvocation();
    }

    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddOllamaEmbeddingGenerator(
        this AspireOllamaApiClientBuilder aspireOllamaApiClientBuilder,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var embeddingGeneratorBuilder = aspireOllamaApiClientBuilder.HostBuilder.Services.AddEmbeddingGenerator(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(aspireOllamaApiClientBuilder.ServiceKey);

            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;

            return embeddingGenerator;
        });

        if (!aspireOllamaApiClientBuilder.DisableTracing)
        {
            embeddingGeneratorBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            embeddingGeneratorBuilder.UseDistributedCache();
        }

        return embeddingGeneratorBuilder.UseLogging();
    }

    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddOllamaEmbeddingGenerator(
        IServiceCollection services,
        IOllamaApiClient ollamaApiClient,
        bool disableTracing = false,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var embeddingGeneratorBuilder = services.AddEmbeddingGenerator(sp =>
        {
            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;

            return embeddingGenerator;
        });

        if (!disableTracing)
        {
            embeddingGeneratorBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            embeddingGeneratorBuilder.UseDistributedCache();
        }

        return embeddingGeneratorBuilder.UseLogging();
    }

    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddOllamaEmbeddingGenerator(
        IServiceCollection services,
        string ollamaApiClientName,
        bool disableTracing = false,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        var embeddingGeneratorBuilder = services.AddEmbeddingGenerator(sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(ollamaApiClientName);
            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;

            return embeddingGenerator;
        });

        if (!disableTracing)
        {
            embeddingGeneratorBuilder.UseOpenTelemetry(
                sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                configure: configureOpenTelemetry
            );
        }

        if (useCache)
        {
            embeddingGeneratorBuilder.UseDistributedCache();
        }

        return embeddingGeneratorBuilder.UseLogging();
    }

    public static IServiceCollection AddOllamaEmbeddingGeneration(
        this AspireOllamaApiClientBuilder aspireOllamaApiClientBuilder,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        Func<IServiceProvider, ITextEmbeddingGenerationService> factory = sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(aspireOllamaApiClientBuilder.ServiceKey);

            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;
            var embeddingGeneratorBuilder = embeddingGenerator.AsBuilder();

            if (!aspireOllamaApiClientBuilder.DisableTracing)
            {
                embeddingGeneratorBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                    configure: configureOpenTelemetry
                );
            }

            if (useCache)
            {
                embeddingGeneratorBuilder.UseDistributedCache();
            }

            embeddingGenerator = embeddingGeneratorBuilder.UseLogging().Build(sp);

            return embeddingGenerator.AsTextEmbeddingGenerationService(sp);
        };

        aspireOllamaApiClientBuilder.HostBuilder.Services.AddSingleton<IEmbeddingGenerationService<string, float>>(
            factory
        );
        aspireOllamaApiClientBuilder.HostBuilder.Services.AddSingleton(factory);

        return aspireOllamaApiClientBuilder.HostBuilder.Services;
    }

    public static IServiceCollection AddOllamaEmbeddingGeneration(
        IServiceCollection services,
        string ollamaApiClientName,
        bool disableTracing = false,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        Func<IServiceProvider, ITextEmbeddingGenerationService> factory = sp =>
        {
            var ollamaApiClient = sp.GetRequiredKeyedService<IOllamaApiClient>(ollamaApiClientName);
            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;
            var embeddingGeneratorBuilder = embeddingGenerator.AsBuilder();
            if (!disableTracing)
            {
                embeddingGeneratorBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                    configure: configureOpenTelemetry
                );
            }
            if (useCache)
            {
                embeddingGeneratorBuilder.UseDistributedCache();
            }

            embeddingGenerator = embeddingGeneratorBuilder.UseLogging().Build(sp);

            return embeddingGenerator.AsTextEmbeddingGenerationService(sp);
        };

        services.AddSingleton<IEmbeddingGenerationService<string, float>>(factory);
        services.AddSingleton(factory);

        return services;
    }

    public static IServiceCollection AddOllamaEmbeddingGeneration(
        IServiceCollection services,
        IOllamaApiClient ollamaApiClient,
        bool disableTracing = false,
        Action<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>? configureOpenTelemetry = null,
        string? openTelemetrySourceName = null,
        bool useCache = false
    )
    {
        Func<IServiceProvider, ITextEmbeddingGenerationService> factory = sp =>
        {
            // the implementor of `IOllamaApiClient` also implements `IEmbeddingGenerator<string, Embedding<float>` like `OllamaApiClient`
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
                (IEmbeddingGenerator<string, Embedding<float>>)ollamaApiClient;
            var embeddingGeneratorBuilder = embeddingGenerator.AsBuilder();
            if (!disableTracing)
            {
                embeddingGeneratorBuilder.UseOpenTelemetry(
                    sourceName: openTelemetrySourceName ?? DefaultExtensionsAISourceName,
                    configure: configureOpenTelemetry
                );
            }

            if (useCache)
            {
                embeddingGeneratorBuilder.UseDistributedCache();
            }

            embeddingGenerator = embeddingGeneratorBuilder.UseLogging().Build(sp);

            return embeddingGenerator.AsTextEmbeddingGenerationService(sp);
        };

        services.AddSingleton<IEmbeddingGenerationService<string, float>>(factory);
        services.AddSingleton(factory);

        return services;
    }
}
