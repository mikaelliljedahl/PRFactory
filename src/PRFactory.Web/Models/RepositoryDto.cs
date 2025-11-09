namespace PRFactory.Web.Models;

public class RepositoryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GitPlatform { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int TicketCount { get; set; }
}
