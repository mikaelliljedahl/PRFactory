using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

public class UpdateRepositoryRequest
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

    public bool IsActive { get; set; } = true;
}
