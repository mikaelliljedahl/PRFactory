using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

public class CreateTicketModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repository is required")]
    public Guid RepositoryId { get; set; }

    public bool EnableExternalSync { get; set; }

    public string? ExternalSystem { get; set; }
}
