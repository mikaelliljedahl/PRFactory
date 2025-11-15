using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that enforces token budget limits for agent execution.
/// Prevents execution when budget is exhausted and records token usage.
/// </summary>
public class TokenBudgetMiddleware : IAgentMiddleware
{
    private const string TokenBudgetCheckMessage = "Checking token budget for tenant {TenantId}, ticket {TicketId}. Available: {AvailableTokens}, Required: {RequiredTokens}";
    private const string TokenBudgetExhaustedMessage = "Token budget exhausted for tenant {TenantId}, ticket {TicketId}. Available: {AvailableTokens}, Required: {RequiredTokens}";
    private const string TokenUsageRecordedMessage = "Token usage recorded. TenantId: {TenantId}, TicketId: {TicketId}, TokensUsed: {TokensUsed}, RemainingBudget: {RemainingBudget}";
    private const string TokenMetadataKey = "TokensUsed";
    private const string EstimatedTokensKey = "EstimatedTokens";

    private readonly ITokenBudgetService _tokenBudgetService;
    private readonly ILogger<TokenBudgetMiddleware> _logger;

    public TokenBudgetMiddleware(
        ITokenBudgetService tokenBudgetService,
        ILogger<TokenBudgetMiddleware> logger)
    {
        _tokenBudgetService = tokenBudgetService ?? throw new ArgumentNullException(nameof(tokenBudgetService));
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

        // Get estimated token requirement from metadata (if available)
        var estimatedTokens = context.Metadata.ContainsKey(EstimatedTokensKey)
            ? Convert.ToInt32(context.Metadata[EstimatedTokensKey])
            : 1000; // Default estimate

        // Get current token budget from service
        TokenBudget budget;
        try
        {
            if (!Guid.TryParse(context.TenantId, out var tenantId))
            {
                _logger.LogWarning(
                    "Invalid TenantId format for token budget check: {TenantId}",
                    context.TenantId);

                // Allow execution but log warning
                return await next(context, cancellationToken);
            }

            budget = await _tokenBudgetService.GetBudgetAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve token budget for tenant {TenantId}, ticket {TicketId}. Allowing execution.",
                context.TenantId,
                context.TicketId);

            // Allow execution on budget service failure (fail open for availability)
            return await next(context, cancellationToken);
        }

        _logger.LogInformation(
            TokenBudgetCheckMessage,
            context.TenantId,
            context.TicketId,
            budget.RemainingTokens,
            estimatedTokens);

        // Check if budget is exhausted
        if (budget.RemainingTokens < estimatedTokens)
        {
            _logger.LogWarning(
                TokenBudgetExhaustedMessage,
                context.TenantId,
                context.TicketId,
                budget.RemainingTokens,
                estimatedTokens);

            // Return failure result without executing agent
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Token budget exhausted. Available: {budget.RemainingTokens}, Required: {estimatedTokens}",
                ShouldRetry = false // Don't retry budget exhaustion
            };
        }

        // Execute the next middleware or agent
        var result = await next(context, cancellationToken);

        // Record token usage after successful execution
        var tokensUsed = 0;
        if (result.Output.ContainsKey(TokenMetadataKey))
        {
            tokensUsed = Convert.ToInt32(result.Output[TokenMetadataKey]);
        }
        else
        {
            // Use estimated tokens if actual usage not reported
            tokensUsed = estimatedTokens;
            _logger.LogDebug(
                "Token usage not reported by agent {AgentName}, using estimate: {EstimatedTokens}",
                agentName,
                estimatedTokens);
        }

        // Record usage (fire and forget - don't block on recording)
        if (tokensUsed > 0 && Guid.TryParse(context.TenantId, out var parsedTenantId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _tokenBudgetService.RecordUsageAsync(
                        parsedTenantId,
                        tokensUsed,
                        agentName,
                        context.TicketId,
                        CancellationToken.None);

                    var updatedBudget = await _tokenBudgetService.GetBudgetAsync(
                        parsedTenantId,
                        CancellationToken.None);

                    _logger.LogInformation(
                        TokenUsageRecordedMessage,
                        context.TenantId,
                        context.TicketId,
                        tokensUsed,
                        updatedBudget.RemainingTokens);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to record token usage for tenant {TenantId}, ticket {TicketId}",
                        context.TenantId,
                        context.TicketId);
                }
            }, CancellationToken.None);
        }

        return result;
    }
}

/// <summary>
/// Service for managing token budgets and usage tracking.
/// </summary>
public interface ITokenBudgetService
{
    /// <summary>
    /// Gets the current token budget for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current token budget.</returns>
    Task<TokenBudget> GetBudgetAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records token usage for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="tokensUsed">Number of tokens used.</param>
    /// <param name="agentName">Name of the agent that used the tokens.</param>
    /// <param name="ticketId">The ticket ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordUsageAsync(
        Guid tenantId,
        int tokensUsed,
        string agentName,
        string ticketId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the token budget for a tenant.
/// </summary>
public class TokenBudget
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Total token allocation for the billing period.
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Number of tokens already used in the current period.
    /// </summary>
    public int UsedTokens { get; set; }

    /// <summary>
    /// Number of tokens remaining in the current period.
    /// </summary>
    public int RemainingTokens => TotalTokens - UsedTokens;

    /// <summary>
    /// Start of the current billing period.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the current billing period.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Indicates if the budget is exhausted.
    /// </summary>
    public bool IsExhausted => RemainingTokens <= 0;
}

/// <summary>
/// Stub implementation of ITokenBudgetService for development/testing.
/// Replace with actual implementation connected to billing system.
/// </summary>
public class TokenBudgetServiceStub : ITokenBudgetService
{
    private readonly ILogger<TokenBudgetServiceStub> _logger;

    public TokenBudgetServiceStub(ILogger<TokenBudgetServiceStub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<TokenBudget> GetBudgetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stub: GetBudgetAsync called for tenant {TenantId}", tenantId);

        // Return a generous budget for development/testing
        var budget = new TokenBudget
        {
            TenantId = tenantId,
            TotalTokens = 1_000_000,
            UsedTokens = 0,
            PeriodStart = DateTime.UtcNow.Date,
            PeriodEnd = DateTime.UtcNow.Date.AddMonths(1)
        };

        return Task.FromResult(budget);
    }

    public Task RecordUsageAsync(
        Guid tenantId,
        int tokensUsed,
        string agentName,
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Stub: RecordUsageAsync called. TenantId: {TenantId}, TokensUsed: {TokensUsed}, Agent: {AgentName}, Ticket: {TicketId}",
            tenantId,
            tokensUsed,
            agentName,
            ticketId);

        return Task.CompletedTask;
    }
}
