using A2A;
using BuildingBlocks.AI.SemanticKernel;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GenAIEshop.Reviews.Shared.Agents;

public static class SentimentAgent
{
    private const string Name = GenAIEshop.Shared.Constants.Agents.SentimentAgent;
    private const string Description =
        "An agent that get input from previous chat history or agent and evaluates the sentiment of input data as negative, positive, or neutral.";

    private const string Instructions = """
        You are a sentiment analysis assistant for GenAI-eShop. Your role is to evaluate the emotional tone of user messages that have been processed by other Agent.

        **Primary Responsibilities**:
        - Get input from previous chat history or agent and evaluate the sentiment of input data as negative, positive, or neutral.
        - Analyze user-generated content, feedback, and communications
        - Classify sentiment as: Positive, Negative, or Neutral
        - Consider context and nuance when making assessments
        - Pay attention to emotional indicators and user satisfaction signals
        - Evaluate both explicit ratings and implicit sentiment in text
        - Try to keep previous chat history if exists as possibe and add sentiment analysis to the end of the conversation

        **Analysis Criteria**:
        - **Positive**: Happy, satisfied, excited, pleased, enthusiastic about products/services
        - **Negative**: Frustrated, disappointed, angry, dissatisfied, complaints
        - **Neutral**: Informational, factual, mixed feedback without strong emotional tone

        **Domain-Specific Considerations**:
        - Consider context expectations based on the microservice domain
        - Weight detailed content more heavily than simple statements
        - Look for specific mentions of features, quality, and performance
        - Consider the relationship between explicit ratings and textual sentiment

        **Output Requirements**:
        - Provide clear sentiment classification (Positive/Negative/Neutral)
        - Include confidence level (High/Medium/Low)
        - Brief explanation of reasoning behind the sentiment assessment
        - Highlight key phrases or aspects that influenced the sentiment
        - Consider overall user satisfaction context

        **Special Considerations**:
        - Look for patterns across multiple data points when available
        - Consider the intensity of emotional language used
        - Account for cultural differences in expression
        - Balance explicit indicators with implicit sentiment in text
        - Use some emoji to indicate sentiment

        Your analysis helps the other agents understand the user's emotional state to provide appropriate responses.
        """;

    public static Agent CreateAgent(Kernel kernel)
    {
        var semanticKernelOptions = kernel.Services.GetRequiredService<IOptions<SemanticKernelOptions>>().Value;
        // https://ollama.com/blog/thinking
        // - in ollama cli we can `ollama run qwen3:0.6b --think=false` to turn off thinking
        // - in ollama api with passing `think=false` as parameter
        var executionSettings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(semanticKernelOptions);

        return new ChatCompletionAgent
        {
            Instructions = Instructions,
            Name = Name,
            Description = Description,
            Kernel = kernel,
            Arguments = new KernelArguments(executionSettings: executionSettings),
        };
    }

    public static AgentCard GetAgentCard()
    {
        var capabilities = new AgentCapabilities { Streaming = false, PushNotifications = false };

        var sentiment = new AgentSkill
        {
            Id = "id_sentiment_agent",
            Name = Name,
            Description = Description,
            Tags = ["sentiment-analysis", "reviews", "customer-feedback", "semantic-kernel"],
            Examples =
            [
                "Analyze the sentiment of these product reviews",
                "What is the overall sentiment for customer feedback on product XYZ?",
                "Classify the emotional tone of this customer comment",
                "Evaluate whether reviews are positive, negative, or neutral",
            ],
        };

        return new AgentCard
        {
            Name = Name,
            Description = Description,
            Version = "1.0.0",
            Provider = new AgentProvider { Organization = nameof(GenAIEshop) },
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [sentiment],
        };
    }
}
