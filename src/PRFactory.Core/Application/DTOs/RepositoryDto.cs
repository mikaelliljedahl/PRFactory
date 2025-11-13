namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for displaying repository information
/// </summary>
public class RepositoryDto
{
    /// <summary>
    /// Unique identifier for the repository
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this repository belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display name of the repository
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Git platform hosting the repository (GitHub, Bitbucket, AzureDevOps)
    /// </summary>
    public string GitPlatform { get; set; } = string.Empty;

    /// <summary>
    /// Clone URL for the repository
    /// </summary>
    public string CloneUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default branch name (usually "main" or "master")
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Whether this repository is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the repository was registered
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the repository was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the repository was last accessed (cloned/pulled)
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }
}
