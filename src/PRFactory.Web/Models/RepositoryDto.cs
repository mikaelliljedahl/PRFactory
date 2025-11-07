namespace PRFactory.Web.Models;

public class RepositoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
}
