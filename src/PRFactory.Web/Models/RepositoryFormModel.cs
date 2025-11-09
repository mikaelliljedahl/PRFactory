using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// Base class for repository form models (Create and Update requests).
/// Contains common properties used by the RepositoryForm component.
/// </summary>
public abstract class RepositoryFormModel
{
    [Required(ErrorMessage = "Repository name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Git platform is required")]
    public string GitPlatform { get; set; } = string.Empty;

    [Required(ErrorMessage = "Clone URL is required")]
    [Url(ErrorMessage = "Clone URL must be a valid URL")]
    public string CloneUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Default branch is required")]
    [StringLength(100, ErrorMessage = "Branch name cannot exceed 100 characters")]
    public string DefaultBranch { get; set; } = "main";

    public string? AccessToken { get; set; }

    /// <summary>
    /// Tenant ID for the repository.
    /// Required for CreateRepositoryRequest, not used for UpdateRepositoryRequest.
    /// </summary>
    public Guid TenantId { get; set; }
}
