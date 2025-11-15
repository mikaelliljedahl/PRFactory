using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that creates comprehensive audit logs for all agent executions.
/// Records execution details for compliance, debugging, and analytics.
/// </summary>
public class AuditLoggingMiddleware : IAgentMiddleware
{
    private const string AuditLogCreatedMessage = "Audit log created for agent execution. AuditId: {AuditId}, TenantId: {TenantId}, TicketId: {TicketId}, AgentName: {AgentName}";
    private const string AuditLogPersistedMessage = "Audit log persisted. AuditId: {AuditId}, Success: {Success}, DurationMs: {DurationMs}";
    private const string AuditLogPersistenceFailedMessage = "Failed to persist audit log. AuditId: {AuditId}";

    private readonly IAgentExecutionAuditService _auditService;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(
        IAgentExecutionAuditService auditService,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default)
    {
        var agentName = context.Metadata.ContainsKey("CurrentPhase")
            ? context.Metadata["CurrentPhase"]?.ToString() ?? "Unknown"
            : "Unknown";

        // Create audit log before execution
        var auditLog = new AgentExecutionAuditLog
        {
            AuditId = Guid.NewGuid(),
            TenantId = context.TenantId,
            TicketId = context.TicketId,
            AgentName = agentName,
            StartedAt = DateTime.UtcNow,
            InputState = SerializeState(context.State)
        };

        _logger.LogInformation(
            AuditLogCreatedMessage,
            auditLog.AuditId,
            auditLog.TenantId,
            auditLog.TicketId,
            auditLog.AgentName);

        var startTime = DateTime.UtcNow;
        AgentResult? result = null;
        Exception? exception = null;

        try
        {
            // Execute the next middleware or agent
            result = await next(context, cancellationToken);

            // Record successful execution
            auditLog.CompletedAt = DateTime.UtcNow;
            auditLog.DurationMs = (long)(auditLog.CompletedAt.Value - startTime).TotalMilliseconds;
            auditLog.Success = result.Status == AgentStatus.Completed;
            auditLog.Status = result.Status.ToString();
            auditLog.OutputData = SerializeOutput(result.Output);

            if (!auditLog.Success && !string.IsNullOrEmpty(result.Error))
            {
                auditLog.ErrorMessage = result.Error;
                auditLog.ErrorType = "AgentError";
            }

            return result;
        }
        catch (Exception ex)
        {
            exception = ex;

            // Record failed execution
            auditLog.CompletedAt = DateTime.UtcNow;
            auditLog.DurationMs = (long)(auditLog.CompletedAt.Value - startTime).TotalMilliseconds;
            auditLog.Success = false;
            auditLog.Status = AgentStatus.Failed.ToString();
            auditLog.ErrorMessage = ex.Message;
            auditLog.ErrorType = ex.GetType().Name;

            throw;
        }
        finally
        {
            // Persist audit log (fire and forget - don't block execution)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditService.SaveAuditLogAsync(auditLog, CancellationToken.None);

                    _logger.LogInformation(
                        AuditLogPersistedMessage,
                        auditLog.AuditId,
                        auditLog.Success,
                        auditLog.DurationMs);
                }
                catch (Exception persistEx)
                {
                    _logger.LogError(
                        persistEx,
                        AuditLogPersistenceFailedMessage,
                        auditLog.AuditId);
                }
            }, CancellationToken.None);
        }
    }

    private string? SerializeState(Dictionary<string, object> state)
    {
        try
        {
            if (state == null || state.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize agent state for audit log");
            return $"[Serialization failed: {ex.Message}]";
        }
    }

    private string? SerializeOutput(Dictionary<string, object> output)
    {
        try
        {
            if (output == null || output.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(output, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize agent output for audit log");
            return $"[Serialization failed: {ex.Message}]";
        }
    }
}

/// <summary>
/// Service for persisting agent execution audit logs.
/// </summary>
public interface IAgentExecutionAuditService
{
    /// <summary>
    /// Saves an audit log entry for an agent execution.
    /// </summary>
    /// <param name="auditLog">The audit log to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAuditLogAsync(AgentExecutionAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the ticket.</returns>
    Task<List<AgentExecutionAuditLog>> GetAuditLogsByTicketAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific tenant within a date range.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="startDate">Start date for the query.</param>
    /// <param name="endDate">End date for the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the tenant.</returns>
    Task<List<AgentExecutionAuditLog>> GetAuditLogsByTenantAsync(
        string tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a comprehensive audit log entry for an agent execution.
/// </summary>
public class AgentExecutionAuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid AuditId { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The ticket ID.
    /// </summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the agent that executed.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when execution completed (null if still running).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Indicates if execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Agent execution status (Completed, Failed, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Serialized input state (JSON).
    /// </summary>
    public string? InputState { get; set; }

    /// <summary>
    /// Serialized output data (JSON).
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error type (exception class name) if execution failed.
    /// </summary>
    public string? ErrorType { get; set; }
}

/// <summary>
/// Stub implementation of IAgentExecutionAuditService for development/testing.
/// Replace with actual implementation connected to database or external audit system.
/// </summary>
public class AgentExecutionAuditServiceStub : IAgentExecutionAuditService
{
    private readonly ILogger<AgentExecutionAuditServiceStub> _logger;
    private readonly List<AgentExecutionAuditLog> _auditLogs = new();
    private readonly object _lock = new();

    public AgentExecutionAuditServiceStub(ILogger<AgentExecutionAuditServiceStub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SaveAuditLogAsync(AgentExecutionAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Stub: SaveAuditLogAsync called. AuditId: {AuditId}, TenantId: {TenantId}, TicketId: {TicketId}, AgentName: {AgentName}, Success: {Success}",
            auditLog.AuditId,
            auditLog.TenantId,
            auditLog.TicketId,
            auditLog.AgentName,
            auditLog.Success);

        lock (_lock)
        {
            _auditLogs.Add(auditLog);
        }

        return Task.CompletedTask;
    }

    public Task<List<AgentExecutionAuditLog>> GetAuditLogsByTicketAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stub: GetAuditLogsByTicketAsync called for ticket {TicketId}", ticketId);

        lock (_lock)
        {
            var logs = _auditLogs
                .Where(log => log.TicketId == ticketId)
                .OrderBy(log => log.StartedAt)
                .ToList();

            return Task.FromResult(logs);
        }
    }

    public Task<List<AgentExecutionAuditLog>> GetAuditLogsByTenantAsync(
        string tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Stub: GetAuditLogsByTenantAsync called. TenantId: {TenantId}, StartDate: {StartDate}, EndDate: {EndDate}",
            tenantId,
            startDate,
            endDate);

        lock (_lock)
        {
            var logs = _auditLogs
                .Where(log => log.TenantId == tenantId &&
                             log.StartedAt >= startDate &&
                             log.StartedAt <= endDate)
                .OrderBy(log => log.StartedAt)
                .ToList();

            return Task.FromResult(logs);
        }
    }
}
