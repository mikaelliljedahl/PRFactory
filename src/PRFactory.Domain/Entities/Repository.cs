namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a Git repository that PRFactory can work with.
/// </summary>
public class Repository
{
    /// <summary>
    /// Unique identifier for the repository
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The tenant this repository belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Display name of the repository
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Git platform hosting the repository (GitHub, Bitbucket, AzureDevOps)
    /// </summary>
    public string GitPlatform { get; private set; } = string.Empty;

    /// <summary>
    /// Clone URL for the repository
    /// </summary>
    public string CloneUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Default branch name (usually "main" or "master")
    /// </summary>
    public string DefaultBranch { get; private set; } = "main";

    /// <summary>
    /// Access token for Git operations (should be encrypted at rest)
    /// </summary>
    public string AccessToken { get; private set; } = string.Empty;

    /// <summary>
    /// When the repository was registered
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the repository was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// When the repository was last accessed (cloned/pulled)
    /// </summary>
    public DateTime? LastAccessedAt { get; private set; }

    /// <summary>
    /// Local path where the repository is cloned (optional, managed by infrastructure)
    /// </summary>
    public string? LocalPath { get; private set; }

    /// <summary>
    /// Whether this repository is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    public Tenant? Tenant { get; }

    /// <summary>
    /// Tickets associated with this repository
    /// </summary>
    public List<Ticket> Tickets { get; private set; } = new();

    private Repository() { }

    /// <summary>
    /// Creates a new repository
    /// </summary>
    public static Repository Create(
        Guid tenantId,
        string name,
        string gitPlatform,
        string cloneUrl,
        string accessToken,
        string defaultBranch = "main")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(gitPlatform))
            throw new ArgumentException("Git platform cannot be empty", nameof(gitPlatform));

        if (string.IsNullOrWhiteSpace(cloneUrl))
            throw new ArgumentException("Clone URL cannot be empty", nameof(cloneUrl));

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        // Validate git platform
        var validPlatforms = new[] { "GitHub", "Bitbucket", "AzureDevOps" };
        if (!validPlatforms.Contains(gitPlatform, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid git platform. Must be one of: {string.Join(", ", validPlatforms)}",
                nameof(gitPlatform));

        return new Repository
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            GitPlatform = gitPlatform,
            CloneUrl = cloneUrl,
            DefaultBranch = string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch,
            AccessToken = accessToken,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates repository credentials
    /// </summary>
    public void UpdateAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        AccessToken = accessToken;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the default branch
    /// </summary>
    public void UpdateDefaultBranch(string defaultBranch)
    {
        if (string.IsNullOrWhiteSpace(defaultBranch))
            throw new ArgumentException("Default branch cannot be empty", nameof(defaultBranch));

        DefaultBranch = defaultBranch;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that the repository was accessed
    /// </summary>
    public void RecordAccess(string? localPath = null)
    {
        LastAccessedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(localPath))
            LocalPath = localPath;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the repository
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the repository
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
