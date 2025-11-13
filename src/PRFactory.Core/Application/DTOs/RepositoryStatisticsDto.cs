namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for repository statistics
/// </summary>
public class RepositoryStatisticsDto
{
    /// <summary>
    /// The repository ID
    /// </summary>
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Total number of tickets associated with this repository
    /// </summary>
    public int TotalTickets { get; set; }

    /// <summary>
    /// When the repository was last accessed (cloned/pulled)
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Total number of pull requests created for this repository
    /// </summary>
    public int TotalPullRequests { get; set; }
}
