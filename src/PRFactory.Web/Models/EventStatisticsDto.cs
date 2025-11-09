namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing workflow event statistics
/// </summary>
public class EventStatisticsDto
{
    /// <summary>
    /// Total number of events
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of error events (WorkflowStateChanged to Failed states)
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of success events (WorkflowStateChanged to Completed)
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average duration in seconds (for completed workflows)
    /// </summary>
    public double AverageDurationSeconds { get; set; }

    /// <summary>
    /// Event counts by type
    /// </summary>
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();

    /// <summary>
    /// Date range start (if filtered)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Date range end (if filtered)
    /// </summary>
    public DateTime? EndDate { get; set; }
}
