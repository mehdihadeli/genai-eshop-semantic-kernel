using BuildingBlocks.Constants;
using GenAIEshop.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres(name: AspireResources.Postgres)
    .WithImage("postgres", "latest")
    .WithImagePullPolicy(ImagePullPolicy.Missing);

var catalogsPostgres = postgres.AddDatabase(
    name: AspireApplicationResources.PostgresDatabase.Catalogs,
    databaseName: nameof(AspireApplicationResources.PostgresDatabase.Catalogs).ToLowerInvariant()
);

var ordersPostgres = postgres.AddDatabase(
    name: AspireApplicationResources.PostgresDatabase.Orders,
    databaseName: nameof(AspireApplicationResources.PostgresDatabase.Orders).ToLowerInvariant()
);

var reviewsPostgres = postgres.AddDatabase(
    name: AspireApplicationResources.PostgresDatabase.Reviews,
    databaseName: nameof(AspireApplicationResources.PostgresDatabase.Orders).ToLowerInvariant()
);

var redis = builder
    .AddRedis(AspireResources.Redis)
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithImage("redis/redis-stack", "latest");

var qdrant = builder
    .AddQdrant(AspireResources.Qdrant)
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithDataVolume()
    .WithImage("qdrant/qdrant", "latest");

// var semanticKernelOptions = builder.Configuration.BindOptions<SemanticKernelOptions>();
// var ollama = builder
//     .AddOllama(AspireResources.Ollama, 11434)
//     .WithImage("ollama/ollama", "0.12.1")
//     .WithImagePullPolicy(ImagePullPolicy.Missing)
//     // use existing docker compose volume for preventing download ollama image and models
//     .WithDataVolume("aspire-ollama");
//
// var ollamaChat = ollama.AddModel(AspireResources.OllamaChat, semanticKernelOptions.ChatModel);
// var ollamaEmbedding = ollama.AddModel(AspireResources.OllamaEmbedding, semanticKernelOptions.EmbeddingModel);

var catalogsApi = builder
    .AddProject<Projects.Catalogs>(AspireApplicationResources.Api.CatalogsApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .WithReference(catalogsPostgres)
    .WaitFor(catalogsPostgres)
    .WithReference(qdrant)
    .WaitFor(qdrant)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

var reviewsApi = builder
    .AddProject<Projects.Reviews>(AspireApplicationResources.Api.ReviewsApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .WithReference(reviewsPostgres)
    .WaitFor(reviewsPostgres)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

var ordersApi = builder
    .AddProject<Projects.Orders>(AspireApplicationResources.Api.OrdersApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .WithReference(ordersPostgres)
    .WaitFor(ordersPostgres)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

var cartsApi = builder
    .AddProject<Projects.Carts>(AspireApplicationResources.Api.CartsApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .WithReference(redis)
    .WaitFor(redis)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

var recommendationApi = builder
    .AddProject<Projects.Carts>(AspireApplicationResources.Api.RecommendationApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

var mcpServerApi = builder
    .AddProject<Projects.McpServer>(AspireApplicationResources.Api.McpServerApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles#control-launch-profile-selection
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#launch-profiles
    // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#ports-and-proxies
    // .NET Aspire will parse the launchSettings.json file selecting the appropriate launch profile and automatically generate endpoints
    .WithEndpoint(
        "https",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    )
    .WithEndpoint(
        "http",
        endpoint =>
        {
            endpoint.IsProxied = true;
        }
    );

await builder.Build().RunAsync();
