namespace PRFactory.Web.Models;

/// <summary>
/// Simple DTO for repository information used in dropdowns and filters
/// </summary>
public class RepositoryInfo
{
    /// <summary>
    /// Repository unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Repository name
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
