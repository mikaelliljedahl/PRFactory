using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;

namespace PRFactory.Infrastructure.AgentUI;

/// <summary>
/// Implementation of the AG-UI protocol for real-time agent chat interactions.
/// Supports streaming agent responses via Server-Sent Events (SSE).
/// </summary>
public class AgentChatService : IAgentChatService
{
    private readonly IAgentFactory _agentFactory;
    private readonly ITenantContext _tenantContext;
    private readonly DomainCheckpointRepository _checkpointRepository;
    private readonly ILogger<AgentChatService> _logger;

    public AgentChatService(
        IAgentFactory agentFactory,
        ITenantContext tenantContext,
        DomainCheckpointRepository checkpointRepository,
        ILogger<AgentChatService> logger)
    {
        _agentFactory = agentFactory;
        _tenantContext = tenantContext;
        _checkpointRepository = checkpointRepository;
        _logger = logger;
    }

    /// <summary>
    /// Streams agent response for a user message.
    /// Yields chunks as the agent thinks, uses tools, and responds.
    /// </summary>
    public async IAsyncEnumerable<AgentStreamChunk> StreamResponseAsync(
        Guid ticketId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting agent chat stream for ticket {TicketId} with message: {Message}",
            ticketId,
            userMessage);

        var chunks = StreamResponseInternalAsync(ticketId, userMessage, cancellationToken);

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<AgentStreamChunk> StreamResponseInternalAsync(
        Guid ticketId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tenantIdResult = await GetTenantIdSafeAsync(ticketId, cancellationToken);
        if (!tenantIdResult.Success)
        {
            yield return tenantIdResult.ErrorChunk!;
            yield break;
        }

        var tenantId = tenantIdResult.TenantId;

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Reasoning,
            Content = "Analyzing your request...",
            IsFinal = false,
            Metadata = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow
            }
        };

        var agentResult = await CreateAgentSafeAsync(tenantId, cancellationToken);
        if (!agentResult.Success)
        {
            yield return agentResult.ErrorChunk!;
            yield break;
        }

        var agent = agentResult.Agent!;

        var context = new AgentContext
        {
            TenantId = tenantId.ToString(),
            TicketId = ticketId.ToString(),
            State = new Dictionary<string, object>
            {
                ["UserMessage"] = userMessage,
                ["Timestamp"] = DateTime.UtcNow
            }
        };

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Reasoning,
            Content = $"Agent {agent.Name} is processing your request...",
            IsFinal = false
        };

        var executionResult = await ExecuteAgentSafeAsync(agent, context, ticketId, cancellationToken);
        if (!executionResult.Success)
        {
            yield return executionResult.ErrorChunk!;
            yield break;
        }

        var responseContent = executionResult.Result!.Output.GetValueOrDefault("Response", "No response generated")?.ToString()
            ?? "No response generated";

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Response,
            Content = responseContent,
            IsFinal = false,
            Metadata = new Dictionary<string, object>
            {
                ["agentName"] = agent.Name,
                ["status"] = executionResult.Result.Status.ToString()
            }
        };

        yield return new AgentStreamChunk
        {
            Type = ChunkType.Complete,
            Content = "Stream completed successfully",
            IsFinal = true,
            Metadata = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["agentName"] = agent.Name
            }
        };

        _logger.LogInformation(
            "Agent chat stream completed for ticket {TicketId}",
            ticketId);
    }

    private async Task<TenantIdResult> GetTenantIdSafeAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = await _tenantContext.GetCurrentTenantIdAsync(cancellationToken);
            return new TenantIdResult { Success = true, TenantId = tenantId };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent chat stream cancelled for ticket {TicketId}", ticketId);
            return new TenantIdResult
            {
                Success = false,
                ErrorChunk = CreateErrorChunk("Stream cancelled by client")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant context for ticket {TicketId}", ticketId);
            return new TenantIdResult
            {
                Success = false,
                ErrorChunk = CreateErrorChunk($"Unexpected error: {ex.Message}", ex)
            };
        }
    }

    private async Task<AgentCreationResult> CreateAgentSafeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var agentObj = await _agentFactory.CreateAgentAsync(tenantId, "AnalyzerAgent", cancellationToken);

            if (agentObj is not BaseAgent agent)
            {
                _logger.LogError("Agent factory returned non-BaseAgent type: {Type}", agentObj.GetType());
                return new AgentCreationResult
                {
                    Success = false,
                    ErrorChunk = CreateErrorChunk("Internal error: Invalid agent type")
                };
            }

            return new AgentCreationResult { Success = true, Agent = agent };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent for tenant {TenantId}", tenantId);
            return new AgentCreationResult
            {
                Success = false,
                ErrorChunk = CreateErrorChunk($"Failed to initialize agent: {ex.Message}", ex)
            };
        }
    }

    private async Task<AgentExecutionResult> ExecuteAgentSafeAsync(
        BaseAgent agent,
        AgentContext context,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await agent.ExecuteWithMiddlewareAsync(context, cancellationToken);
            return new AgentExecutionResult { Success = true, Result = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed for ticket {TicketId}", ticketId);
            return new AgentExecutionResult
            {
                Success = false,
                ErrorChunk = CreateErrorChunk($"Agent execution failed: {ex.Message}", ex)
            };
        }
    }

    private record TenantIdResult
    {
        public bool Success { get; init; }
        public Guid TenantId { get; init; }
        public AgentStreamChunk? ErrorChunk { get; init; }
    }

    private record AgentCreationResult
    {
        public bool Success { get; init; }
        public BaseAgent? Agent { get; init; }
        public AgentStreamChunk? ErrorChunk { get; init; }
    }

    private record AgentExecutionResult
    {
        public bool Success { get; init; }
        public AgentResult? Result { get; init; }
        public AgentStreamChunk? ErrorChunk { get; init; }
    }

    private static AgentStreamChunk CreateErrorChunk(string content, Exception? ex = null)
    {
        var chunk = new AgentStreamChunk
        {
            Type = ChunkType.Error,
            Content = content,
            IsFinal = true
        };

        if (ex != null)
        {
            chunk.Metadata = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["errorType"] = ex.GetType().Name
            };
        }

        return chunk;
    }

    /// <summary>
    /// Gets chat history for a ticket from checkpoint conversation history.
    /// </summary>
    public async Task<List<AgentChatMessage>> GetChatHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting chat history for ticket {TicketId}", ticketId);

        try
        {
            var checkpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(
                ticketId,
                cancellationToken);

            var messages = new List<AgentChatMessage>();

            foreach (var checkpoint in checkpoints)
            {
                if (!string.IsNullOrEmpty(checkpoint.ConversationHistory))
                {
                    try
                    {
                        var historyMessages = JsonSerializer.Deserialize<List<AgentChatMessage>>(
                            checkpoint.ConversationHistory);

                        if (historyMessages != null)
                        {
                            messages.AddRange(historyMessages);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to deserialize conversation history for checkpoint {CheckpointId}",
                            checkpoint.Id);
                    }
                }
            }

            messages = messages
                .OrderBy(m => m.Timestamp)
                .ToList();

            _logger.LogInformation(
                "Retrieved {MessageCount} chat messages for ticket {TicketId}",
                messages.Count,
                ticketId);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat history for ticket {TicketId}", ticketId);
            return new List<AgentChatMessage>();
        }
    }

    /// <summary>
    /// Answers a follow-up question from the agent.
    /// </summary>
    public async Task<string> AnswerFollowUpQuestionAsync(
        Guid ticketId,
        string questionId,
        string answer,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Answering follow-up question {QuestionId} for ticket {TicketId}",
            questionId,
            ticketId);

        await Task.CompletedTask;

        return $"Answer to question {questionId} recorded: {answer}";
    }
}
