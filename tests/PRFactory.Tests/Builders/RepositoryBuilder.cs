using PRFactory.Domain.Entities;

namespace PRFactory.Tests.Builders;

/// <summary>
/// Fluent builder for creating Repository entities in tests with sensible defaults
/// </summary>
public class RepositoryBuilder
{
    private Guid _tenantId = Guid.NewGuid();
    private string _name = "test-repo";
    private string _gitPlatform = "GitHub";
    private string _cloneUrl = "https://github.com/test-org/test-repo.git";
    private string _accessToken = "test-access-token";
    private string _defaultBranch = "main";
    private bool _isActive = true;
    private string? _localPath;

    public RepositoryBuilder()
    {
    }

    public RepositoryBuilder ForTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public RepositoryBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RepositoryBuilder WithPlatform(string platform)
    {
        _gitPlatform = platform;
        return this;
    }

    public RepositoryBuilder AsGitHub()
    {
        _gitPlatform = "GitHub";
        _cloneUrl = $"https://github.com/test-org/{_name}.git";
        return this;
    }

    public RepositoryBuilder AsBitbucket()
    {
        _gitPlatform = "Bitbucket";
        _cloneUrl = $"https://bitbucket.org/test-org/{_name}.git";
        return this;
    }

    public RepositoryBuilder AsAzureDevOps()
    {
        _gitPlatform = "AzureDevOps";
        _cloneUrl = $"https://dev.azure.com/test-org/test-project/_git/{_name}";
        return this;
    }

    public RepositoryBuilder WithCloneUrl(string cloneUrl)
    {
        _cloneUrl = cloneUrl;
        return this;
    }

    public RepositoryBuilder WithAccessToken(string token)
    {
        _accessToken = token;
        return this;
    }

    public RepositoryBuilder WithDefaultBranch(string branch)
    {
        _defaultBranch = branch;
        return this;
    }

    public RepositoryBuilder WithLocalPath(string path)
    {
        _localPath = path;
        return this;
    }

    public RepositoryBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    public RepositoryBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public Repository Build()
    {
        var repository = Repository.Create(
            _tenantId,
            _name,
            _gitPlatform,
            _cloneUrl,
            _accessToken,
            _defaultBranch);

        if (!string.IsNullOrEmpty(_localPath))
        {
            repository.RecordAccess(_localPath);
        }

        if (!_isActive)
        {
            repository.Deactivate();
        }

        return repository;
    }
}
