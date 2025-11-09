using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

public class CreateRepositoryRequest : RepositoryFormModel
{
    [Required(ErrorMessage = "Access token is required")]
    public new string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tenant is required")]
    public new Guid TenantId { get; set; }
}
