using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.AI;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.AI;

public class AIAgentService : IAIAgentService
{
    private readonly ILlmProviderFactory _llmProviderFactory;
    private readonly ILogger<AIAgentService> _logger;

    public AIAgentService(
        ILlmProviderFactory llmProviderFactory,
        ILogger<AIAgentService> logger)
    {
        _llmProviderFactory = llmProviderFactory ?? throw new ArgumentNullException(nameof(llmProviderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<object> CreateAgentAsync(
        AIAgentConfiguration config,
        IEnumerable<object> tools,
        CancellationToken cancellationToken = default)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (tools == null)
            throw new ArgumentNullException(nameof(tools));

        _logger.LogInformation(
            "Creating AI agent: {AgentName} with {ToolCount} tools",
            config.AgentName,
            tools.Count());

        var stubAgent = new StubAIAgent
        {
            Name = config.AgentName,
            Instructions = config.Instructions,
            Tools = tools.ToList(),
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            StreamingEnabled = config.StreamingEnabled
        };

        return Task.FromResult<object>(stubAgent);
    }

    public async IAsyncEnumerable<AgentStreamChunk> ExecuteAgentAsync(
        object agent,
        string userMessage,
        List<AgentChatMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be empty", nameof(userMessage));

        var stubAgent = agent as StubAIAgent
            ?? throw new ArgumentException("Agent must be a StubAIAgent instance", nameof(agent));

        conversationHistory ??= new List<AgentChatMessage>();

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Reasoning,
            Content = $"Analyzing request: {userMessage}",
            IsFinal = false
        };

        await Task.Delay(300, cancellationToken);

        if (stubAgent.Tools.Any())
        {
            var tool = stubAgent.Tools.First();
            var toolName = GetToolName(tool);
            var toolDescription = GetToolDescription(tool);

            yield return new AgentStreamChunk
            {
                Type = ChunkType.ToolUse,
                Content = $"Using tool: {toolName}",
                IsFinal = false,
                Metadata = new Dictionary<string, object>
                {
                    ["toolName"] = toolName,
                    ["toolDescription"] = toolDescription
                }
            };

            await Task.Delay(200, cancellationToken);

            yield return new AgentStreamChunk
            {
                Type = ChunkType.ToolResult,
                Content = "Tool executed successfully",
                IsFinal = false,
                Metadata = new Dictionary<string, object>
                {
                    ["toolName"] = toolName,
                    ["success"] = true
                }
            };
        }

        var llmProvider = _llmProviderFactory.GetDefaultProvider();
        var prompt = BuildPrompt(stubAgent.Instructions, userMessage, conversationHistory);

        var llmOptions = new LlmOptions
        {
            MaxTokens = stubAgent.MaxTokens,
            Temperature = stubAgent.Temperature
        };

        LlmResponse? response = null;
        Exception? error = null;

        try
        {
            response = await llmProvider.SendMessageAsync(
                prompt,
                stubAgent.Instructions,
                llmOptions,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent");
            error = ex;
        }

        if (error != null)
        {
            yield return new AgentStreamChunk
            {
                Type = ChunkType.Error,
                Content = $"Error: {error.Message}",
                IsFinal = true
            };
            yield break;
        }

        if (response != null)
        {
            if (response.Success)
            {
                yield return new AgentStreamChunk
                {
                    Type = ChunkType.Response,
                    Content = response.Content,
                    IsFinal = false
                };
            }
            else
            {
                yield return new AgentStreamChunk
                {
                    Type = ChunkType.Error,
                    Content = response.ErrorMessage ?? "LLM request failed",
                    IsFinal = true
                };
                yield break;
            }
        }

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Complete,
            Content = "Agent execution complete",
            IsFinal = true
        };
    }

    private string BuildPrompt(
        string instructions,
        string userMessage,
        List<AgentChatMessage> history)
    {
        var sb = new StringBuilder();

        if (history.Any())
        {
            sb.AppendLine("Previous conversation:");
            foreach (var msg in history.TakeLast(10))
            {
                var role = msg.Type switch
                {
                    MessageType.UserMessage => "User",
                    MessageType.AssistantMessage => "Assistant",
                    MessageType.ToolInvocation => "Tool",
                    MessageType.ToolResult => "ToolResult",
                    _ => msg.Type.ToString()
                };
                sb.AppendLine($"{role}: {msg.Content}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"User: {userMessage}");

        return sb.ToString();
    }

    private string GetToolName(object tool)
    {
        var nameProperty = tool.GetType().GetProperty("Name");
        return nameProperty?.GetValue(tool)?.ToString() ?? "Unknown";
    }

    private string GetToolDescription(object tool)
    {
        var descProperty = tool.GetType().GetProperty("Description");
        return descProperty?.GetValue(tool)?.ToString() ?? "";
    }
}

internal class StubAIAgent
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<object> Tools { get; set; } = new();
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public bool StreamingEnabled { get; set; }
}
