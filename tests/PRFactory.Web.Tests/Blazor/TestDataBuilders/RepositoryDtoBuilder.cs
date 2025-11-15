using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating RepositoryDto instances for testing
/// </summary>
public class RepositoryDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private string _tenantName = "Test Tenant";
    private string _name = "test-repository";
    private string _gitPlatform = "GitHub";
    private string _cloneUrl = "https://github.com/test-org/test-repository.git";
    private string _defaultBranch = "main";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = null;
    private DateTime? _lastAccessedAt = null;
    private int _ticketCount = 0;

    public RepositoryDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public RepositoryDtoBuilder WithTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public RepositoryDtoBuilder WithTenantName(string tenantName)
    {
        _tenantName = tenantName;
        return this;
    }

    public RepositoryDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RepositoryDtoBuilder WithGitPlatform(string platform)
    {
        _gitPlatform = platform;
        return this;
    }

    public RepositoryDtoBuilder WithCloneUrl(string cloneUrl)
    {
        _cloneUrl = cloneUrl;
        return this;
    }

    public RepositoryDtoBuilder WithDefaultBranch(string branch)
    {
        _defaultBranch = branch;
        return this;
    }

    public RepositoryDtoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public RepositoryDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public RepositoryDtoBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public RepositoryDtoBuilder WithLastAccessedAt(DateTime? lastAccessedAt)
    {
        _lastAccessedAt = lastAccessedAt;
        return this;
    }

    public RepositoryDtoBuilder WithTicketCount(int count)
    {
        _ticketCount = count;
        return this;
    }

    public RepositoryDto Build()
    {
        return new RepositoryDto
        {
            Id = _id,
            TenantId = _tenantId,
            TenantName = _tenantName,
            Name = _name,
            GitPlatform = _gitPlatform,
            CloneUrl = _cloneUrl,
            DefaultBranch = _defaultBranch,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            LastAccessedAt = _lastAccessedAt,
            TicketCount = _ticketCount
        };
    }

    /// <summary>
    /// Creates a GitHub repository
    /// </summary>
    public static RepositoryDtoBuilder GitHub()
    {
        return new RepositoryDtoBuilder()
            .WithGitPlatform("GitHub")
            .WithCloneUrl("https://github.com/test-org/test-repository.git");
    }

    /// <summary>
    /// Creates a Bitbucket repository
    /// </summary>
    public static RepositoryDtoBuilder Bitbucket()
    {
        return new RepositoryDtoBuilder()
            .WithGitPlatform("Bitbucket")
            .WithCloneUrl("https://bitbucket.org/test-org/test-repository.git");
    }

    /// <summary>
    /// Creates an Azure DevOps repository
    /// </summary>
    public static RepositoryDtoBuilder AzureDevOps()
    {
        return new RepositoryDtoBuilder()
            .WithGitPlatform("AzureDevOps")
            .WithCloneUrl("https://dev.azure.com/test-org/test-project/_git/test-repository");
    }

    /// <summary>
    /// Creates an active repository with tickets
    /// </summary>
    public static RepositoryDtoBuilder WithActivity()
    {
        return new RepositoryDtoBuilder()
            .WithIsActive(true)
            .WithTicketCount(10)
            .WithLastAccessedAt(DateTime.UtcNow.AddHours(-2));
    }

    /// <summary>
    /// Creates an inactive repository
    /// </summary>
    public static RepositoryDtoBuilder Inactive()
    {
        return new RepositoryDtoBuilder()
            .WithIsActive(false);
    }
}
