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
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WithReference(catalogsPostgres)
    .WaitFor(catalogsPostgres)
    .WithReference(qdrant)
    .WaitFor(qdrant);

var reviewsApi = builder
    .AddProject<Projects.Reviews>(AspireApplicationResources.Api.ReviewsApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    .WithReference(reviewsPostgres)
    .WaitFor(reviewsPostgres)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WaitFor(catalogsApi)
    .WithReference(catalogsApi); // for service discovery

var ordersApi = builder
    .AddProject<Projects.Orders>(AspireApplicationResources.Api.OrdersApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    .WithReference(ordersPostgres)
    .WaitFor(ordersPostgres)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WaitFor(catalogsApi)
    .WithReference(catalogsApi);

var cartsApi = builder
    .AddProject<Projects.Carts>(AspireApplicationResources.Api.CartsApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WaitFor(redis)
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WaitFor(catalogsApi)
    .WithReference(catalogsApi);

var mcpServerApi = builder
    .AddProject<Projects.McpServer>(AspireApplicationResources.Api.McpServerApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WaitFor(catalogsApi)
    .WithReference(catalogsApi);

var recommendationApi = builder
    .AddProject<Projects.Recommendation>(AspireApplicationResources.Api.RecommendationApi)
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    // this service will be healthy in aspire dashboard after this endpoint is available, default is `/`
    .WithHttpHealthCheck("/health")
    // .WithReference(ollamaChat)
    // .WaitFor(ollamaChat)
    // .WithReference(ollamaEmbedding)
    // .WaitFor(ollamaEmbedding)
    .WaitFor(catalogsApi)
    .WithReference(catalogsApi)
    .WaitFor(reviewsApi)
    .WithReference(reviewsApi)
    .WaitFor(mcpServerApi)
    .WithReference(mcpServerApi);

await builder.Build().RunAsync();
