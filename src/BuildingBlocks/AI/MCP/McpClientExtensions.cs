using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace BuildingBlocks.AI.MCP;

public static class McpClientExtensions
{
    public static async Task<List<ChatMessage>> MapMcpToolsPromptsToChatMessagesAsync(this McpClient mcpClient)
    {
        var prompts = await mcpClient.ListPromptsAsync().ConfigureAwait(false);

        List<ChatMessage> promptMessages = [];

        foreach (var prompt in prompts)
        {
            var chatMessages = await prompt.GetAsync().ConfigureAwait(false);
            promptMessages.AddRange(chatMessages.ToChatMessages());
        }

        return promptMessages;
    }
}
