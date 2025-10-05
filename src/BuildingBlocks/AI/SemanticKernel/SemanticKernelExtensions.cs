using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.A2A;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

#pragma warning disable SKEXP0110

namespace BuildingBlocks.AI.SemanticKernel;

// ref: https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-functions?pivots=programming-language-csharp
// Using KernelFunctionFactory, KernelPluginFactory and AgentKernelFunctionFactory to create kernel functions and plugins.


/// <summary>
/// Provides extension methods for Semantic Kernel to enhance agent and plugin functionality.
/// These extensions facilitate integration between different AI components and enable seamless
/// function calling between agents and plugins.
/// </summary>
public static class SemanticKernelExtensions
{
    /// <summary>
    /// Creates a KernelPlugin from a registered A2A (Agent-to-Agent) agent, enabling
    /// the agent to be called as a function by other agents or kernel components.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance to extend</param>
    /// <param name="agentName">The name of the registered A2AAgent to create a plugin for</param>
    /// <returns>
    /// A KernelPlugin that exposes the A2A agent as a callable function
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="agentName"/> is null, empty, or whitespace
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no A2AAgent is registered with the specified <paramref name="agentName"/>
    /// </exception>
    /// <remarks>
    /// This method facilitates cross-microservice agent communication by wrapping
    /// remote A2A agents as local plugin functions. The generated plugin name follows
    /// the pattern: "{agentName}A2APlugin".
    /// </remarks>
    public static KernelPlugin CreatePluginFromA2AAgent(this Kernel kernel, string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var agent = kernel.Services.GetRequiredKeyedService<A2AAgent>(agentName);

        return KernelPluginFactory.CreateFromFunctions(
            $"{agentName}A2APlugin",
            [AgentKernelFunctionFactory.CreateFromAgent(agent)]
        );
    }

    /// <summary>
    /// Maps a locally registered agent (e.g., ChatCompletionAgent) to a KernelPlugin
    /// so it can be used as a function by other agents or the kernel.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance to extend</param>
    /// <param name="agentName">The name of the registered local agent to create a plugin for</param>
    /// <returns>
    /// A KernelPlugin that exposes the local agent as a callable function
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="agentName"/> is null, empty, or whitespace
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no Agent is registered with the specified <paramref name="agentName"/>
    /// </exception>
    /// <remarks>
    /// This extension enables agent composition within the same service, allowing
    /// specialized agents to be called as functions by orchestration agents.
    /// The generated plugin name follows the pattern: "{agentName}LocalPlugin".
    /// This is particularly useful for creating hierarchical agent architectures
    /// where parent agents coordinate the work of child agents.
    /// </remarks>
    public static KernelPlugin CreatePluginFromLocalAgent(this Kernel kernel, string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var agent = kernel.Services.GetRequiredKeyedService<Agent>(agentName);

        return KernelPluginFactory.CreateFromFunctions(
            pluginName: $"{agentName}LocalPlugin",
            functions: [AgentKernelFunctionFactory.CreateFromAgent(agent)]
        );
    }

    /// <summary>
    /// Adds a Semantic Kernel plugin that exposes the provided MCP tools as kernel functions
    /// for use in semantic chat completion and agent workflows.
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="pluginName">The name to assign to the created plugin.</param>
    /// <param name="mcpClientName">mcpClientName</param>
    /// <returns>
    /// The <see cref="KernelPlugin"/> instance that was added to the kernel's plugin collection.
    /// </returns>
    /// <remarks>
    /// Each MCP tool is converted to a <see cref="KernelFunction"/> via <c>AsKernelFunction()</c>
    /// and grouped into a single plugin identified by <paramref name="pluginName"/>.
    /// </remarks>
    // https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/

    public static async Task<KernelPlugin> CreatePluginFromMcpTools(
        this Kernel kernel,
        string pluginName,
        string mcpClientName
    )
    {
        var mcpClient = kernel.Services.GetRequiredKeyedService<McpClient>(mcpClientName);
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        return kernel.CreatePluginFromFunctions(pluginName, tools.Select(aiFunction => aiFunction.AsKernelFunction()));
    }

    /// <summary>
    /// Registers all functions from the given kernel plugins as tools on an MCP server builder.
    /// </summary>
    /// <param name="builder">The MCP server builder used to register the tools.</param>
    /// <param name="plugins">The collection of kernel plugins whose functions will be exposed as MCP tools.</param>
    /// <returns>
    /// The same <see cref="IMcpServerBuilder"/> instance to enable fluent configuration.
    /// </returns>
    /// <remarks>
    /// Iterates through each plugin and its functions, creating an <see cref="McpServerTool"/>
    /// for each function and registering it with the service collection as a singleton.
    /// </remarks>
    // https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/
    public static IMcpServerBuilder AddPluginsToMcpServerTools(
        this IMcpServerBuilder builder,
        KernelPluginCollection plugins
    )
    {
        foreach (KernelPlugin plugin in plugins)
        {
            foreach (KernelFunction function in plugin)
            {
                builder.Services.AddSingleton(sp => McpServerTool.Create(function));
            }
        }

        return builder;
    }

    public static IMcpServerBuilder AddPluginToMcpServerTools(this IMcpServerBuilder builder, KernelPlugin plugin)
    {
        foreach (KernelFunction function in plugin)
        {
            builder.Services.AddSingleton(sp => McpServerTool.Create(function));
        }

        return builder;
    }
}
