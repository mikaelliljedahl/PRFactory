using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Claude;

/// <summary>
/// Interface for tracking Claude API token usage
/// </summary>
public interface ITokenUsageTracker
{
    /// <summary>
    /// Track token usage for a Claude API call
    /// </summary>
    Task TrackUsageAsync(TokenUsage usage);

    /// <summary>
    /// Get token usage statistics for a time period
    /// </summary>
    Task<TokenStats> GetStatsAsync(Guid tenantId, DateTime from, DateTime to);

    /// <summary>
    /// Get token usage for a specific ticket
    /// </summary>
    Task<List<TokenUsage>> GetTicketUsageAsync(Guid ticketId);
}

/// <summary>
/// Tracks and reports on Claude API token usage
/// </summary>
public class TokenUsageTracker : ITokenUsageTracker
{
    private readonly ILogger<TokenUsageTracker> _logger;
    // In a real implementation, this would use a database context
    // For now, we'll use an in-memory list as a placeholder
    private static readonly List<TokenUsage> _usages = new();
    private static readonly object _lock = new();

    public TokenUsageTracker(ILogger<TokenUsageTracker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task TrackUsageAsync(TokenUsage usage)
    {
        lock (_lock)
        {
            _usages.Add(usage);
        }

        _logger.LogInformation(
            "Token usage tracked: {InputTokens} input, {OutputTokens} output, Model: {Model}",
            usage.InputTokens, usage.OutputTokens, usage.Model);

        // In production, this would save to database:
        // _dbContext.TokenUsages.Add(usage);
        // await _dbContext.SaveChangesAsync();

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TokenStats> GetStatsAsync(Guid tenantId, DateTime from, DateTime to)
    {
        List<TokenUsage> usages;
        lock (_lock)
        {
            usages = _usages
                .Where(u => u.TenantId == tenantId && u.Timestamp >= from && u.Timestamp <= to)
                .ToList();
        }

        var totalInput = usages.Sum(u => u.InputTokens);
        var totalOutput = usages.Sum(u => u.OutputTokens);
        var cost = CalculateCost(usages);

        _logger.LogInformation(
            "Token stats for tenant {TenantId}: {InputTokens} input, {OutputTokens} output, ${Cost}",
            tenantId, totalInput, totalOutput, cost);

        await Task.CompletedTask;

        return new TokenStats(totalInput, totalOutput, cost);
    }

    /// <inheritdoc/>
    public async Task<List<TokenUsage>> GetTicketUsageAsync(Guid ticketId)
    {
        List<TokenUsage> usages;
        lock (_lock)
        {
            usages = _usages
                .Where(u => u.TicketId == ticketId)
                .OrderBy(u => u.Timestamp)
                .ToList();
        }

        await Task.CompletedTask;
        return usages;
    }

    /// <summary>
    /// Calculate estimated cost based on Claude Sonnet 4.5 pricing
    /// </summary>
    private decimal CalculateCost(List<TokenUsage> usages)
    {
        // Claude Sonnet 4.5 pricing (as of 2025)
        // Note: These are example prices and should be updated based on actual pricing
        const decimal INPUT_COST_PER_MTK = 3.00m;   // $3 per million tokens
        const decimal OUTPUT_COST_PER_MTK = 15.00m;  // $15 per million tokens

        var totalInputTokens = usages.Sum(u => u.InputTokens);
        var totalOutputTokens = usages.Sum(u => u.OutputTokens);

        var inputCost = totalInputTokens / 1_000_000m * INPUT_COST_PER_MTK;
        var outputCost = totalOutputTokens / 1_000_000m * OUTPUT_COST_PER_MTK;

        return inputCost + outputCost;
    }
}
