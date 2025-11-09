namespace PRFactory.Web.Models;

/// <summary>
/// DTO for error statistics display in the Web UI
/// </summary>
public class ErrorStatisticsDto
{
    public int TotalErrors { get; set; }
    public int UnresolvedErrors { get; set; }
    public int ResolvedErrors { get; set; }
    public int CriticalErrors { get; set; }
    public int HighErrors { get; set; }
    public int MediumErrors { get; set; }
    public int LowErrors { get; set; }
    public Dictionary<string, int> ErrorsByEntityType { get; set; } = new();
    public Dictionary<DateTime, int> ErrorsByDate { get; set; } = new();

    // Computed properties for UI
    public double ResolutionRate => TotalErrors > 0
        ? Math.Round((double)ResolvedErrors / TotalErrors * 100, 2)
        : 0;

    public string HealthStatus
    {
        get
        {
            if (CriticalErrors > 0) return "Critical";
            if (HighErrors > 5) return "Warning";
            if (UnresolvedErrors > 10) return "Attention";
            return "Healthy";
        }
    }

    public string HealthStatusClass => HealthStatus switch
    {
        "Critical" => "danger",
        "Warning" => "warning",
        "Attention" => "info",
        "Healthy" => "success",
        _ => "secondary"
    };
}
