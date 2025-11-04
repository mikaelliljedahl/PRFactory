namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Represents token usage for a Claude API call
/// </summary>
public class TokenUsage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TicketId { get; set; }
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Token usage statistics for a time period
/// </summary>
/// <param name="TotalInputTokens">Total input tokens used</param>
/// <param name="TotalOutputTokens">Total output tokens used</param>
/// <param name="EstimatedCost">Estimated cost in USD</param>
public record TokenStats(int TotalInputTokens, int TotalOutputTokens, decimal EstimatedCost);
