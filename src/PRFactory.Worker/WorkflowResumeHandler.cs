using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Worker;

/// <summary>
/// Handles resumption of suspended workflows when webhook events arrive.
/// Loads checkpoints from the database and resumes graph execution from the suspended agent.
/// </summary>
public class WorkflowResumeHandler : IWorkflowResumeHandler
{
    private readonly ILogger<WorkflowResumeHandler> _logger;
    private readonly ICheckpointStore _checkpointStore;
    private readonly IAgentGraphExecutor _graphExecutor;
    private readonly ITicketRepository _ticketRepository;
    private readonly ActivitySource _activitySource;

    public WorkflowResumeHandler(
        ILogger<WorkflowResumeHandler> logger,
        ICheckpointStore checkpointStore,
        IAgentGraphExecutor graphExecutor,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _checkpointStore = checkpointStore;
        _graphExecutor = graphExecutor;
        _ticketRepository = ticketRepository;
        _activitySource = new ActivitySource("PRFactory.Worker.WorkflowResume");
    }

    /// <summary>
    /// Resumes a suspended workflow from its last checkpoint.
    /// </summary>
    public async Task<WorkflowExecutionResult> ResumeWorkflowAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("ResumeWorkflow");
        activity?.SetTag("ticket.id", workflow.TicketId);
        activity?.SetTag("checkpoint.id", workflow.CheckpointId);
        activity?.SetTag("suspended.agent", workflow.SuspendedAgentName);

        try
        {
            _logger.LogInformation(
                "Loading checkpoint {CheckpointId} for ticket {TicketId}",
                workflow.CheckpointId,
                workflow.TicketId);

            // 1. Load checkpoint from database
            var checkpoint = await _checkpointStore.LoadCheckpointAsync(
                workflow.CheckpointId,
                cancellationToken);

            if (checkpoint == null)
            {
                _logger.LogError(
                    "Checkpoint {CheckpointId} not found for ticket {TicketId}",
                    workflow.CheckpointId,
                    workflow.TicketId);

                return new WorkflowExecutionResult
                {
                    IsSuccess = false,
                    Message = $"Checkpoint {workflow.CheckpointId} not found"
                };
            }

            activity?.SetTag("checkpoint.agent", checkpoint.AgentName);
            activity?.SetTag("checkpoint.state", checkpoint.State);

            // 2. Validate checkpoint is for HumanWaitAgent
            if (!IsHumanWaitCheckpoint(checkpoint))
            {
                _logger.LogWarning(
                    "Checkpoint {CheckpointId} is not a HumanWait checkpoint (agent: {AgentName})",
                    workflow.CheckpointId,
                    checkpoint.AgentName);

                return new WorkflowExecutionResult
                {
                    IsSuccess = false,
                    Message = $"Checkpoint is not a valid resume point (agent: {checkpoint.AgentName})"
                };
            }

            // 3. Load ticket context
            var ticket = await _ticketRepository.GetByIdAsync(
                workflow.TicketId,
                cancellationToken);

            if (ticket == null)
            {
                _logger.LogError(
                    "Ticket {TicketId} not found",
                    workflow.TicketId);

                return new WorkflowExecutionResult
                {
                    IsSuccess = false,
                    Message = $"Ticket {workflow.TicketId} not found"
                };
            }

            // 4. Determine the next agent based on the resume message
            var nextAgentName = DetermineNextAgent(workflow.ResumeMessage, checkpoint);

            if (string.IsNullOrEmpty(nextAgentName))
            {
                _logger.LogError(
                    "Unable to determine next agent for ticket {TicketId}",
                    workflow.TicketId);

                return new WorkflowExecutionResult
                {
                    IsSuccess = false,
                    Message = "Unable to determine next agent from resume message"
                };
            }

            _logger.LogInformation(
                "Resuming workflow for ticket {TicketId} at agent {NextAgent}",
                workflow.TicketId,
                nextAgentName);

            activity?.SetTag("next.agent", nextAgentName);

            // 5. Create agent context from checkpoint
            var agentContext = CreateAgentContextFromCheckpoint(checkpoint, ticket);

            // 6. Resume graph execution from the next agent
            var result = await _graphExecutor.ResumeFromCheckpointAsync(
                checkpoint,
                workflow.ResumeMessage!,
                nextAgentName,
                agentContext,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Workflow resumed successfully for ticket {TicketId}",
                    workflow.TicketId);
            }
            else
            {
                _logger.LogWarning(
                    "Workflow resume completed with issues for ticket {TicketId}: {Message}",
                    workflow.TicketId,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resume workflow for ticket {TicketId}",
                workflow.TicketId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new WorkflowExecutionResult
            {
                IsSuccess = false,
                Message = $"Resume failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates a webhook event and creates a resume message.
    /// </summary>
    public async Task<(bool IsValid, IAgentMessage? ResumeMessage)> ValidateAndCreateResumeMessageAsync(
        Guid ticketId,
        string eventType,
        object eventData,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("ValidateWebhookEvent");
        activity?.SetTag("ticket.id", ticketId);
        activity?.SetTag("event.type", eventType);

        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found for webhook event", ticketId);
                return (false, null);
            }

            // Load the current checkpoint to understand what the workflow is waiting for
            var checkpoint = await _checkpointStore.LoadLatestCheckpointAsync(
                ticketId,
                cancellationToken);

            if (checkpoint == null)
            {
                _logger.LogWarning(
                    "No checkpoint found for ticket {TicketId}, cannot resume",
                    ticketId);
                return (false, null);
            }

            // Parse event data and create appropriate message
            IAgentMessage? resumeMessage = eventType switch
            {
                "answers_received" => ParseAnswersReceivedEvent(ticketId, eventData),
                "plan_approved" => ParsePlanApprovedEvent(ticketId, eventData),
                "plan_rejected" => ParsePlanRejectedEvent(ticketId, eventData),
                _ => null
            };

            if (resumeMessage == null)
            {
                _logger.LogWarning(
                    "Unknown or unsupported event type: {EventType}",
                    eventType);
                return (false, null);
            }

            _logger.LogInformation(
                "Created resume message of type {MessageType} for ticket {TicketId}",
                resumeMessage.GetType().Name,
                ticketId);

            return (true, resumeMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error validating webhook event for ticket {TicketId}",
                ticketId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return (false, null);
        }
    }

    /// <summary>
    /// Checks if a checkpoint represents a HumanWaitAgent suspension point.
    /// </summary>
    private bool IsHumanWaitCheckpoint(CheckpointData checkpoint)
    {
        return checkpoint.AgentName.Equals("HumanWaitAgent", StringComparison.OrdinalIgnoreCase)
            || checkpoint.State.Equals("Suspended", StringComparison.OrdinalIgnoreCase)
            || checkpoint.State.Equals("AwaitingAnswers", StringComparison.OrdinalIgnoreCase)
            || checkpoint.State.Equals("PlanUnderReview", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines the next agent to execute based on the resume message.
    /// </summary>
    private string DetermineNextAgent(IAgentMessage? message, CheckpointData checkpoint)
    {
        if (message == null)
            return string.Empty;

        return message switch
        {
            AnswersReceivedMessage => "AnswerProcessingAgent",
            PlanApprovedMessage => "ImplementationAgent",
            PlanRejectedMessage => "PlanningAgent",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Creates an agent execution context from a checkpoint.
    /// </summary>
    private AgentContext CreateAgentContextFromCheckpoint(
        CheckpointData checkpoint,
        object ticket)
    {
        // Extract context data from checkpoint
        var context = new AgentContext
        {
            TicketId = checkpoint.TicketId.ToString(),
            TenantId = checkpoint.TenantId?.ToString() ?? string.Empty,
            State = checkpoint.StateData ?? new Dictionary<string, object>()
        };

        // Restore any saved state from the checkpoint
        if (checkpoint.StateData != null)
        {
            foreach (var kvp in checkpoint.StateData)
            {
                context.State[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    /// <summary>
    /// Parses webhook data for answers received event.
    /// </summary>
    private AnswersReceivedMessage? ParseAnswersReceivedEvent(
        Guid ticketId,
        object eventData)
    {
        try
        {
            // Parse eventData (could be JSON, dictionary, etc.)
            var answers = ExtractAnswersFromEventData(eventData);

            if (answers == null || answers.Count == 0)
            {
                _logger.LogWarning(
                    "No answers found in event data for ticket {TicketId}",
                    ticketId);
                return null;
            }

            return new AnswersReceivedMessage(ticketId, answers);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error parsing answers received event for ticket {TicketId}",
                ticketId);
            return null;
        }
    }

    /// <summary>
    /// Parses webhook data for plan approved event.
    /// </summary>
    private PlanApprovedMessage? ParsePlanApprovedEvent(
        Guid ticketId,
        object eventData)
    {
        try
        {
            // Extract approval details from event data
            var approvedBy = ExtractStringFromEventData(eventData, "approved_by", "system");
            var approvedAt = ExtractDateTimeFromEventData(eventData, "approved_at", DateTime.UtcNow);

            return new PlanApprovedMessage(ticketId, approvedAt, approvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error parsing plan approved event for ticket {TicketId}",
                ticketId);
            return null;
        }
    }

    /// <summary>
    /// Parses webhook data for plan rejected event.
    /// </summary>
    private PlanRejectedMessage? ParsePlanRejectedEvent(
        Guid ticketId,
        object eventData)
    {
        try
        {
            var reason = ExtractStringFromEventData(eventData, "reason", "No reason provided");

            return new PlanRejectedMessage(ticketId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error parsing plan rejected event for ticket {TicketId}",
                ticketId);
            return null;
        }
    }

    /// <summary>
    /// Extracts answers dictionary from event data.
    /// </summary>
    private Dictionary<string, string>? ExtractAnswersFromEventData(object eventData)
    {
        // Implementation depends on how webhook data is structured
        // This is a simplified version
        if (eventData is Dictionary<string, string> dict)
            return dict;

        if (eventData is Dictionary<string, object> objDict)
        {
            return objDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
        }

        return null;
    }

    /// <summary>
    /// Extracts a string value from event data.
    /// </summary>
    private string ExtractStringFromEventData(
        object eventData,
        string key,
        string defaultValue)
    {
        if (eventData is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Extracts a DateTime value from event data.
    /// </summary>
    private DateTime ExtractDateTimeFromEventData(
        object eventData,
        string key,
        DateTime defaultValue)
    {
        if (eventData is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
        {
            if (value is DateTime dt)
                return dt;

            if (DateTime.TryParse(value?.ToString(), out var parsed))
                return parsed;
        }

        return defaultValue;
    }
}

/// <summary>
/// Interface for workflow resume handler.
/// </summary>
public interface IWorkflowResumeHandler
{
    Task<WorkflowExecutionResult> ResumeWorkflowAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken);

    Task<(bool IsValid, IAgentMessage? ResumeMessage)> ValidateAndCreateResumeMessageAsync(
        Guid ticketId,
        string eventType,
        object eventData,
        CancellationToken cancellationToken);
}

/// <summary>
/// Interface for checkpoint storage.
/// </summary>
public interface ICheckpointStore
{
    Task<CheckpointData?> LoadCheckpointAsync(
        Guid checkpointId,
        CancellationToken cancellationToken);

    Task<CheckpointData?> LoadLatestCheckpointAsync(
        Guid ticketId,
        CancellationToken cancellationToken);

    Task SaveCheckpointAsync(
        CheckpointData checkpoint,
        CancellationToken cancellationToken);
}

/// <summary>
/// Interface for ticket repository.
/// </summary>
public interface ITicketRepository
{
    Task<object?> GetByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents checkpoint data stored in the database.
/// </summary>
public class CheckpointData
{
    public Guid CheckpointId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? TenantId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public Dictionary<string, object>? StateData { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Agent execution context.
/// </summary>
public class AgentContext
{
    public string TicketId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();

    public void RestoreFromCheckpoint(CheckpointData checkpoint)
    {
        if (checkpoint.StateData != null)
        {
            foreach (var kvp in checkpoint.StateData)
            {
                State[kvp.Key] = kvp.Value;
            }
        }
    }
}

/// <summary>
/// Extension methods for IAgentGraphExecutor.
/// </summary>
public static class AgentGraphExecutorExtensions
{
    public static Task<WorkflowExecutionResult> ResumeFromCheckpointAsync(
        this IAgentGraphExecutor executor,
        CheckpointData checkpoint,
        IAgentMessage resumeMessage,
        string nextAgentName,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // This would be implemented by the actual graph executor
        // The executor would load the graph state and continue from the specified agent
        throw new NotImplementedException(
            "ResumeFromCheckpointAsync must be implemented by the agent framework integration");
    }
}
