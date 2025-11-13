using System.ComponentModel.DataAnnotations;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for updating an existing repository
/// </summary>
public class UpdateRepositoryDto
{
    /// <summary>
    /// Display name of the repository
    /// </summary>
    [Required(ErrorMessage = "Repository name is required")]
    [MaxLength(200, ErrorMessage = "Repository name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Git platform hosting the repository (GitHub, Bitbucket, AzureDevOps)
    /// </summary>
    [Required(ErrorMessage = "Git platform is required")]
    [MaxLength(50, ErrorMessage = "Git platform cannot exceed 50 characters")]
    public string GitPlatform { get; set; } = string.Empty;

    /// <summary>
    /// Clone URL for the repository
    /// </summary>
    [Required(ErrorMessage = "Clone URL is required")]
    [Url(ErrorMessage = "Clone URL must be a valid URL")]
    [MaxLength(500, ErrorMessage = "Clone URL cannot exceed 500 characters")]
    public string CloneUrl { get; set; } = string.Empty;

    /// <summary>
    /// Access token for Git operations (optional, only update if provided)
    /// </summary>
    [MaxLength(500, ErrorMessage = "Access token cannot exceed 500 characters")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Default branch name (usually "main" or "master")
    /// </summary>
    [Required(ErrorMessage = "Default branch is required")]
    [MaxLength(100, ErrorMessage = "Default branch cannot exceed 100 characters")]
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Whether this repository is active
    /// </summary>
    public bool IsActive { get; set; }
}
