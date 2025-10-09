using BuildingBlocks.AI.SemanticKernel;
using GenAIEshop.Reviews.Shared.Agents.OrchestrationsAgents;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110

namespace GenAIEshop.Reviews.Shared.Services;

public class ReviewsOrchestrationService(
    ReviewsSequentialOrchestrationAgent reviewsSequentialOrchestrationAgent,
    ReviewsChatOrchestrationAgent reviewsChatOrchestrationAgent,
    IntelligentReviewsChatManager intelligentReviewsChatManager,
    IChatCompletionService chatCompletionService,
    Kernel kernel,
    IOptions<SemanticKernelOptions> semanticKernelOptions,
    ILogger<ReviewsSequentialOrchestrationAgent> logger
) : IReviewsOrchestrationService
{
    private readonly ChatHistory _history = new();

    public async Task<string> AnalyzeReviewsUsingChatGroupOrchestrationAsync(string prompt)
    {
        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-orchestration/?pivots=programming-language-csharp#preparing-your-development-environment
        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-orchestration/group-chat?pivots=programming-language-csharp

        _history.Add(new ChatMessageContent(AuthorRole.User, prompt));

        // A runtime is required to manage the execution of agents. Here, we use InProcessRuntime and start it before invoking the orchestration.
        var runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var sequentialOrchestration = new GroupChatOrchestration(
            intelligentReviewsChatManager,
            reviewsChatOrchestrationAgent.ReviewsCollectorAgent,
            reviewsChatOrchestrationAgent.LanguageAgent,
            reviewsChatOrchestrationAgent.SentimentAgent,
            reviewsChatOrchestrationAgent.InsightsSynthesizerAgent
        )
        {
            ResponseCallback = ResponseCallbackAsync,
            InputTransform = InputTransformAsync,
            ResultTransform = ResultTransformAsync,
            Name = "Reviews Orchestration",
        };

        // Invoke the orchestration with an initial prompt.
        var result = await sequentialOrchestration.InvokeAsync(prompt, runtime);

        // Wait for the orchestration to complete and retrieve the final output.
        string output = await result.GetValueAsync(TimeSpan.FromSeconds(60));

        await runtime.RunUntilIdleAsync();

        _history.Clear();

        return output;
    }

    public async Task<string> AnalyzeReviewsUsingSequentialOrchestrationAsync(string prompt)
    {
        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-orchestration/?pivots=programming-language-csharp#preparing-your-development-environment
        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-orchestration/sequential?pivots=programming-language-csharp

        _history.Add(new ChatMessageContent(AuthorRole.User, prompt));

        // A runtime is required to manage the execution of agents. Here, we use InProcessRuntime and start it before invoking the orchestration.
        var runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var sequentialOrchestration = new SequentialOrchestration(
            reviewsSequentialOrchestrationAgent.ReviewsCollectorAgent,
            reviewsSequentialOrchestrationAgent.LanguageAgent,
            reviewsSequentialOrchestrationAgent.SentimentAgent,
            reviewsSequentialOrchestrationAgent.InsightsSynthesizerAgent
        )
        {
            ResponseCallback = ResponseCallbackAsync,
            InputTransform = InputTransformAsync,
            ResultTransform = ResultTransformAsync,
            Name = "Reviews Orchestration",
        };

        // Invoke the orchestration with an initial prompt. The output will flow through each agent in sequence.
        var result = await sequentialOrchestration.InvokeAsync(prompt, runtime);

        // Wait for the orchestration to complete and retrieve the final output.
        string output = await result.GetValueAsync(TimeSpan.FromSeconds(60));

        await runtime.RunUntilIdleAsync();

        _history.Clear();

        return output;
    }

    private ValueTask ResponseCallbackAsync(ChatMessageContent response)
    {
        logger.LogInformation(
            "Agent response received from **{AuthorName}**:\n {Content}",
            response.AuthorName ?? "Unknown",
            response.Content
        );

        _history.Add(response);

        return ValueTask.CompletedTask;
    }

    private ValueTask<string?> ResultTransformAsync(
        IList<ChatMessageContent> result,
        CancellationToken cancellationToken
    )
    {
        var lastContent = result.LastOrDefault(msg => !string.IsNullOrWhiteSpace(msg.Content));

        return ValueTask.FromResult(lastContent is not null ? lastContent.Content : string.Empty);
    }

    private async ValueTask<IEnumerable<ChatMessageContent>> InputTransformAsync(
        string input,
        CancellationToken cancellationToken
    )
    {
        var executionSettings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(
            semanticKernelOptions.Value
        );

        var result = await chatCompletionService.GetChatMessageContentsAsync(
            chatHistory: _history,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken
        );

        return result;
    }
}
