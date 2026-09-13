using BuildingBlocks.AI.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BuildingBlocks.Tests;

public class SemanticKernelExecutionSettingsTests
{
    [Fact]
    public void GetDefaultSettings_applies_defaults_and_custom_extension_data()
    {
        var options = new SemanticKernelOptions
        {
            Temperature = 0.2f,
            ChatExtensionData = new Dictionary<string, object> { ["temperature"] = 0.4f, ["custom"] = "value" },
        };

        var settings = SemanticKernelExecutionSettings.GetDefaultSettings(options);

        settings.ExtensionData.ShouldContainKeyAndValue("temperature", 0.4f);
        settings.ExtensionData.ShouldContainKeyAndValue("think", false);
        settings.ExtensionData.ShouldContainKeyAndValue("custom", "value");
    }

    [Fact]
    public void GetProviderExecutionSettings_for_ollama_preserves_temperature_and_defaults()
    {
        var options = new SemanticKernelOptions { ChatProviderType = ProviderType.Ollama, Temperature = 0.3f };

        var settings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(options);

        var ollamaSettings = settings.ShouldBeOfType<OllamaPromptExecutionSettings>();
        ollamaSettings.Temperature.ShouldBe(0.3f);
        ollamaSettings.ExtensionData.ShouldContainKeyAndValue("think", false);
        ollamaSettings.ExtensionData.ShouldContainKeyAndValue("num_predict", 10000);
    }

    [Fact]
    public void GetProviderExecutionSettings_for_openai_returns_openai_settings()
    {
        var options = new SemanticKernelOptions { ChatProviderType = ProviderType.OpenAI, Temperature = 0.3f };

        var settings = SemanticKernelExecutionSettings.GetProviderExecutionSettings(options);

        var openAiSettings = settings.ShouldBeOfType<OpenAIPromptExecutionSettings>();
        openAiSettings.MaxTokens.ShouldBe(10000);
        openAiSettings.Temperature.ShouldBe(0.3f);
    }
}
