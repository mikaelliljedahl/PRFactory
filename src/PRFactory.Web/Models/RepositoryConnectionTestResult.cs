namespace PRFactory.Web.Models;

public class RepositoryConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AvailableBranches { get; set; } = new();
    public string? ErrorDetails { get; set; }
    public DateTime TestedAt { get; set; }
}
