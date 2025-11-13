using System.ComponentModel.DataAnnotations;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for creating a new repository
/// </summary>
public class CreateRepositoryDto
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
    /// Access token for Git operations (should be encrypted at rest)
    /// </summary>
    [Required(ErrorMessage = "Access token is required")]
    [MaxLength(500, ErrorMessage = "Access token cannot exceed 500 characters")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Default branch name (usually "main" or "master")
    /// </summary>
    [MaxLength(100, ErrorMessage = "Default branch cannot exceed 100 characters")]
    public string DefaultBranch { get; set; } = "main";
}
