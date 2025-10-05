using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;

#pragma warning disable SKEXP0001

namespace BuildingBlocks.AI.SemanticKernel;

public static class SemanticKernelExecutionSettings
{
    public static PromptExecutionSettings GetDefaultSettings(SemanticKernelOptions semanticKernelOptions)
    {
        // https://ollama.com/blog/thinking
        // - in ollama cli we can `ollama run qwen3:0.6b --think=false` to turn off thinking
        // - in ollama api with passing `think=false` as parameter
        var defaultExtensionData = new Dictionary<string, object>
        {
            { "think", false },
            { "temperature", semanticKernelOptions.Temperature },
            { "num_predict", 10000 },
            { "max_tokens", 10000 },
        };

        var mergedExtensionData = MergeExtensionData(defaultExtensionData, semanticKernelOptions.ChatExtensionData);

        return new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }
            ),
            ExtensionData = mergedExtensionData,
        };
    }

    public static PromptExecutionSettings GetProviderExecutionSettings(SemanticKernelOptions semanticKernelOptions)
    {
        switch (semanticKernelOptions.ChatProviderType)
        {
            case ProviderType.Ollama:
                // https://ollama.com/blog/thinking
                // - in ollama cli we can `ollama run qwen3:0.6b --think=false` to turn off thinking
                // - in ollama api with passing `think=false` as parameter
                var ollamaDefaultExtensionData = new Dictionary<string, object>
                {
                    { "think", false },
                    { "temperature", semanticKernelOptions.Temperature },
                    { "num_predict", 10000 },
                };
                var ollamaMergedExtensionData = MergeExtensionData(
                    ollamaDefaultExtensionData,
                    semanticKernelOptions.ChatExtensionData
                );

                return new OllamaPromptExecutionSettings
                {
                    Temperature = semanticKernelOptions.Temperature,
                    ExtensionData = ollamaMergedExtensionData,
                };
            case ProviderType.Azure:
                var azureDefaultExtensionData = new Dictionary<string, object> { };
                var azureMergedExtensionData = MergeExtensionData(
                    azureDefaultExtensionData,
                    semanticKernelOptions.ChatExtensionData
                );
                return new AzureOpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                        options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }
                    ),
                    Temperature = semanticKernelOptions.Temperature,
                    ExtensionData = azureMergedExtensionData,
                    MaxTokens = 10000,
                };
            case ProviderType.OpenAI:
                var openAIDefaultExtensionData = new Dictionary<string, object> { };
                var openAIMergedExtensionData = MergeExtensionData(
                    openAIDefaultExtensionData,
                    semanticKernelOptions.ChatExtensionData
                );
                return new AzureOpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                        options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }
                    ),
                    Temperature = semanticKernelOptions.Temperature,
                    ExtensionData = openAIMergedExtensionData,
                    MaxTokens = 10000,
                };
            default:
                throw new Exception("Unknown provider type");
        }
    }

    private static Dictionary<string, object> MergeExtensionData(
        Dictionary<string, object> baseExtensionData,
        Dictionary<string, object> appSettingsExtensionData
    )
    {
        var merged = new Dictionary<string, object>(baseExtensionData);

        foreach (var item in appSettingsExtensionData)
        {
            merged[item.Key] = item.Value;
        }

        return merged;
    }
}
