using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that ensures strict tenant isolation during agent execution.
/// Validates tenant context and prevents cross-tenant data access.
/// </summary>
public class TenantIsolationMiddleware : IAgentMiddleware
{
    private const string TenantIdStateKey = "ValidatedTenantId";
    private const string TenantValidationStartedMessage = "Tenant validation started. TenantId: {TenantId}, TicketId: {TicketId}, AgentName: {AgentName}";
    private const string TenantValidationCompletedMessage = "Tenant validation completed successfully. TenantId: {TenantId}, TicketId: {TicketId}";
    private const string TenantIsolationViolationMessage = "Tenant isolation violation detected. Expected: {ExpectedTenantId}, Actual: {ActualTenantId}, TicketId: {TicketId}";
    private const string TenantContextMismatchMessage = "Tenant context mismatch. Context TenantId: {ContextTenantId}, Current TenantId: {CurrentTenantId}, TicketId: {TicketId}";

    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(
        ITenantContext tenantContext,
        ILogger<TenantIsolationMiddleware> logger)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
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

        // Validate TenantId is not empty
        if (string.IsNullOrEmpty(context.TenantId))
        {
            _logger.LogError(
                "TenantId is empty for ticket {TicketId}, agent {AgentName}",
                context.TicketId,
                agentName);

            throw new TenantIsolationException("TenantId cannot be empty")
            {
                TicketId = context.TicketId,
                AgentName = agentName
            };
        }

        // Get current tenant from ITenantContext
        Guid currentTenantId;
        try
        {
            currentTenantId = await _tenantContext.GetCurrentTenantIdAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve current tenant context for ticket {TicketId}",
                context.TicketId);

            throw new TenantIsolationException("Failed to retrieve current tenant context", ex)
            {
                TicketId = context.TicketId,
                AgentName = agentName
            };
        }

        // Validate tenant context matches context.TenantId
        if (!Guid.TryParse(context.TenantId, out var contextTenantId))
        {
            _logger.LogError(
                "Invalid TenantId format: {TenantId}, TicketId: {TicketId}",
                context.TenantId,
                context.TicketId);

            throw new TenantIsolationException($"Invalid TenantId format: {context.TenantId}")
            {
                TicketId = context.TicketId,
                AgentName = agentName,
                ExpectedTenantId = context.TenantId,
                ActualTenantId = currentTenantId.ToString()
            };
        }

        if (currentTenantId != contextTenantId)
        {
            _logger.LogError(
                TenantContextMismatchMessage,
                context.TenantId,
                currentTenantId,
                context.TicketId);

            throw new TenantIsolationException($"Tenant context mismatch. Expected: {context.TenantId}, Actual: {currentTenantId}")
            {
                TicketId = context.TicketId,
                AgentName = agentName,
                ExpectedTenantId = context.TenantId,
                ActualTenantId = currentTenantId.ToString()
            };
        }

        _logger.LogInformation(
            TenantValidationStartedMessage,
            context.TenantId,
            context.TicketId,
            agentName);

        // Store validated tenant ID in context state for downstream validation
        var originalTenantId = context.TenantId;
        context.State[TenantIdStateKey] = originalTenantId;

        AgentResult result;
        try
        {
            // Execute the next middleware or agent
            result = await next(context, cancellationToken);

            // Validate tenant wasn't changed during execution
            if (context.TenantId != originalTenantId)
            {
                _logger.LogError(
                    TenantIsolationViolationMessage,
                    originalTenantId,
                    context.TenantId,
                    context.TicketId);

                throw new TenantIsolationException($"Tenant ID was changed during execution. Original: {originalTenantId}, Current: {context.TenantId}")
                {
                    TicketId = context.TicketId,
                    AgentName = agentName,
                    ExpectedTenantId = originalTenantId,
                    ActualTenantId = context.TenantId
                };
            }

            _logger.LogInformation(
                TenantValidationCompletedMessage,
                context.TenantId,
                context.TicketId);

            return result;
        }
        catch (TenantIsolationException)
        {
            // Re-throw tenant isolation exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during agent execution with tenant validation. TenantId: {TenantId}, TicketId: {TicketId}",
                context.TenantId,
                context.TicketId);

            throw;
        }
    }
}

/// <summary>
/// Exception thrown when tenant isolation validation fails.
/// Indicates a security violation or misconfiguration.
/// </summary>
public class TenantIsolationException : Exception
{
    public TenantIsolationException(string message) : base(message) { }

    public TenantIsolationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// The ticket ID where the violation occurred.
    /// </summary>
    public string? TicketId { get; set; }

    /// <summary>
    /// The agent name where the violation occurred.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// The expected tenant ID.
    /// </summary>
    public string? ExpectedTenantId { get; set; }

    /// <summary>
    /// The actual tenant ID that was found.
    /// </summary>
    public string? ActualTenantId { get; set; }
}
