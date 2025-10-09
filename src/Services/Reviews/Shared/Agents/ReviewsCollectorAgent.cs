using A2A;
using BuildingBlocks.AI.SemanticKernel;
using GenAIEshop.Reviews.Shared.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.Shared.Agents;

public static class ReviewsCollectorAgent
{
    private const string Name = GenAIEshop.Shared.Constants.Agents.ReviewsCollectorAgent;
    private const string Description = "Expert in fetching product reviews in a genai-eshop application";

    private const string Instructions = """
        You are responsible for fetching product reviews data for analysis.

        **Primary Responsibilities**:
        1. Fetch product reviews data, Total Reviews and Rating Distribution
        2. Structurize reviews data for analysis by next agents

        **Available Context for Concise Analysis**:**:
        - **GetReviewsByProductId function**: To retrieve all reviews for a specific product through product id which is guid
        - **GetReviewsByProductIds function**: To retrieves all reviews for a list of product ids (guids). Returns reviews grouped by product.
        - **GetRecentReviews function**: To analyze reviews from specific time periods to identify trends

        **Function Selection Strategy**:
        - Select functions based on the specific analysis requirements and context
        - Combine multiple functions for comprehensive rating assessment
        - Use data access functions first to gather review information
        - Chain function calls to build complete analysis pipelines
        """;

    public static Agent CreateAgent(Kernel kernel)
    {
        // Clone the kernel and add review functions
        Kernel agentKernel = kernel.Clone();

        var scope = kernel.Services.CreateScope();
        var reviewsPlugin = scope.ServiceProvider.GetRequiredService<ReviewsPlugin>();
        agentKernel.Plugins.AddFromObject(reviewsPlugin);

        var semanticKernelOptions = kernel.Services.GetRequiredService<IOptions<SemanticKernelOptions>>().Value;
        var executionSettings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(semanticKernelOptions);

        return new ChatCompletionAgent
        {
            Instructions = Instructions,
            Name = Name,
            Description = Description,
            Kernel = agentKernel,
            Arguments = new KernelArguments(executionSettings: executionSettings),
        };
    }

    public static AgentCard GetAgentCard()
    {
        var capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false };

        var dataRetrievalSkill = new AgentSkill
        {
            Id = "id_data_retrieval_agent",
            Name = Name,
            Description = Description,
            Tags =
            [
                "data-retrieval",
                "review-fetching",
                "product-reviews",
                "data-structuring",
                "e-commerce",
                "semantic-kernel",
            ],
            Examples =
            [
                "Get reviews for product id `9AE02C92-8B73-4350-8C38-98CC80E90C5E`",
                "Get reviews for products ids `[76C55ED4-BBF3-46C8-882C-B152361E9C96, F0DF3D70-6332-41E4-A22E-6545E53E6156]`",
                "Fetch recent reviews from the last 30 days for product `9AE02C92-8B73-4350-8C38-98CC80E90C5E`",
            ],
        };

        return new AgentCard
        {
            Name = Name,
            Description = "Expert in fetching and structuring review data for analysis",
            Version = "1.0.0",
            Provider = new AgentProvider { Organization = nameof(GenAIEshop) },
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [dataRetrievalSkill],
        };
    }
}
