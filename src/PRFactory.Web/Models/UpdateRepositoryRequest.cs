namespace PRFactory.Web.Models;

public class UpdateRepositoryRequest : RepositoryFormModel
{
    public bool IsActive { get; set; } = true;
}
