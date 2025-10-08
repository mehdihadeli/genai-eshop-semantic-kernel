using A2A;
using BuildingBlocks.AI.SemanticKernel;
using GenAIEshop.Reviews.Shared.Plugins;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Ollama;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0130

namespace GenAIEshop.Reviews.Shared.Agents;

/// <summary>
/// Provides comprehensive product review analysis and quality assessment capabilities.
/// This agent coordinates with specialized agents to deliver detailed insights into product performance
/// based on customer reviews, ratings, and feedback.
/// </summary>
public static class ReviewsAgent
{
    private const string Name = GenAIEshop.Shared.Constants.Agents.ReviewsAgent;

    // Used by Other agents for agent discovery and routing - helps other agents understand what this agent can do
    private const string Description = "Provides comprehensive product review analysis and quality assessment.";

    // Used by: The agent itself internally and guides the agent's behavior and responsibility - how it should think and respond
    private const string Instructions = """
        You are responsible for comprehensive product review analysis and quality assessment for GenAI-Eshop.

        **Primary Responsibilities**:
        1. Analyze product reviews to determine overall quality and customer satisfaction
        2. Provide detailed insights into product strengths and weaknesses based on customer feedback
        3. Identify trends and patterns in customer reviews over time
        4. Handle sentiment analysis by leveraging SentimentAgent when needed
        5. Handle multilingual reviews by leveraging LanguageAgent when needed

        **Available Context for Concise Analysis**:**:
        - **GetReviewsByProductId function**: To retrieve all reviews for a specific product through product id which is guid
        - **GetReviewsByProductIds function**: To retrieves all reviews for a list of product ids (guids). Returns reviews grouped by product.
        - **GetRecentReviews function**: To analyze reviews from specific time periods to identify trends
        - **SentimentAgent**: Use `SentimentAgent` to analyze emotional tone and satisfaction levels in reviews
        - **SummerizeAgent**: Use `SummerizeAgent` to create concise summaries of review content and key insights if reviews are long
        - **LanguageAgent**: Use `LanguageAgent` to detect language and translate non-English reviews to English

        **Function Selection Strategy**:
        - Select functions based on the specific analysis requirements and context
        - Combine multiple functions for comprehensive rating assessment
        - Use data access functions first to gather review information
        - Leverage specialized agents for in-depth analysis of specific aspects
        - Chain function calls to build complete analysis pipelines

        **Output Format**:
        Always provide:
        - Include the product name and a brief description in your analysis
        - Overall quality classification with confidence level
        - Summary of key positive and negative aspects using SummerizeAgent
        - Sentiment analysis overview using SentimentAgent
        - Reviews statistics (average rating, total reviews, distribution)
        - Recommendations for product improvement (if applicable)

        **Special Considerations**:
        - Consider review volume and credibility
        - Weight recent reviews more heavily
        - Look for consistency in feedback patterns
        - Consider product category and price point expectations
        - Handle conflicting reviews by analyzing underlying patterns
        - Select appropriate functions based on analysis depth required
        - Combine statistical data with qualitative insights for comprehensive assessment
        - Add md icons to the output to make it more appealing
        """;

    /// <summary>
    /// Creates and configures a ReviewsAgent instance with access to review data and specialized analysis agents.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance used for agent execution and plugin management</param>
    /// <returns>A configured ChatCompletionAgent capable of comprehensive review analysis</returns>
    /// <remarks>
    /// This agent includes:
    /// - Access to review data through ReviewsPlugin
    /// - Integration with SentimentAgent for emotional tone analysis
    /// - Integration with SummerizeAgent for review summarization
    /// - Auto function selection with retained argument types
    /// - Low temperature setting for consistent, reliable analysis
    /// </remarks>
    public static Agent CreateAgent(Kernel kernel)
    {
        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-functions?pivots=programming-language-csharp
        // Clone kernel instance to allow for agent-specific plug-in definition
        Kernel agentKernel = kernel.Clone();

        var scope = kernel.Services.CreateScope();
        var semanticKernelOptions = scope.ServiceProvider.GetRequiredService<IOptions<SemanticKernelOptions>>().Value;

        // Map specialized agents as child agents
        var languageAgent = kernel.CreatePluginFromLocalAgent(GenAIEshop.Shared.Constants.Agents.LanguageAgent);
        var sentimentPlugin = kernel.CreatePluginFromLocalAgent(GenAIEshop.Shared.Constants.Agents.SentimentAgent);
        var summerizePlugin = kernel.CreatePluginFromLocalAgent(GenAIEshop.Shared.Constants.Agents.SummarizeAgent);

        agentKernel.Plugins.Add(languageAgent);
        agentKernel.Plugins.Add(sentimentPlugin);
        agentKernel.Plugins.Add(summerizePlugin);

        var reviewsPlugin = scope.ServiceProvider.GetRequiredService<ReviewsPlugin>();
        agentKernel.Plugins.AddFromObject(reviewsPlugin);

        // https://ollama.com/blog/thinking
        // - in ollama cli we can `ollama run qwen3:0.6b --think=false` to turn off thinking
        // - in ollama api with passing `think=false` as parameter
        var executionSettings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(semanticKernelOptions);

        return new ChatCompletionAgent
        {
            Instructions = Instructions,
            Name = Name,
            Description = Description,
            Kernel = agentKernel,
            UseImmutableKernel = true,
            Arguments = new KernelArguments(executionSettings: executionSettings),
        };
    }

    /// <summary>
    /// Generates an AgentCard that describes the capabilities and configuration of the ReviewsAgent to use in the A2A.
    /// </summary>
    /// <returns>An AgentCard containing metadata about the agent's skills, capabilities, and examples</returns>
    /// <remarks>
    /// The AgentCard is used for:
    /// - Agent discovery and registration
    /// - UI presentation of agent capabilities
    /// - Documentation generation
    /// - Agent orchestration and routing decisions
    /// </remarks>
    public static AgentCard GetAgentCard()
    {
        var capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false };

        var reviews = new AgentSkill
        {
            Id = "id_reviews_agent",
            Name = Name,
            Description = Description,
            Tags = ["reviews", "product-analysis", "quality-assessment", "semantic-kernel", "customer-feedback"],
            Examples =
            [
                "Analyze all reviews for product id `A7F48F79-EF05-48B4-BBE0-1818C593B18C` and provide comprehensive assessment",
                "Analyze reviews for products ids `[8134F0E7-08B5-44E5-A5E9-63B5B34E0BF8, 5F9ED1A7-4913-496B-B68B-C9350A82A638]` and provide comprehensive assessment",
                "What is the overall quality of this product based on customer reviews?",
                "Provide complete product evaluation using all available review data",
                "Generate comprehensive review insights with sentiment and summary for product XYZ-789",
                "How do customers rate this product and what are the common complaints?",
            ],
        };

        return new AgentCard
        {
            Name = Name,
            Description = "Provides comprehensive product review analysis and quality assessment",
            Version = "1.0.0",
            Provider = new AgentProvider { Organization = nameof(GenAIEshop) },
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [reviews],
        };
    }
}
