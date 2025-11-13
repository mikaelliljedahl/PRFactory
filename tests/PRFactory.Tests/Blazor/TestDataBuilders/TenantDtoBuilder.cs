using PRFactory.Web.Models;

namespace PRFactory.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating TenantDto instances for testing
/// </summary>
public class TenantDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Tenant";
    private string _ticketPlatformUrl = "https://test-tenant.atlassian.net";
    private string _ticketPlatform = "Jira";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = null;
    private bool _autoImplementAfterPlanApproval = false;
    private int _maxRetries = 3;
    private string _claudeModel = "claude-3-5-sonnet-20241022";
    private int _maxTokensPerRequest = 4096;
    private bool _enableCodeReview = false;
    private int _repositoryCount = 0;
    private int _ticketCount = 0;
    private bool _hasTicketPlatformApiToken = true;
    private bool _hasClaudeApiKey = true;

    public TenantDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TenantDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TenantDtoBuilder WithTicketPlatformUrl(string url)
    {
        _ticketPlatformUrl = url;
        return this;
    }

    public TenantDtoBuilder WithTicketPlatform(string platform)
    {
        _ticketPlatform = platform;
        return this;
    }

    public TenantDtoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public TenantDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TenantDtoBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public TenantDtoBuilder WithAutoImplementAfterPlanApproval(bool autoImplement)
    {
        _autoImplementAfterPlanApproval = autoImplement;
        return this;
    }

    public TenantDtoBuilder WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    public TenantDtoBuilder WithClaudeModel(string model)
    {
        _claudeModel = model;
        return this;
    }

    public TenantDtoBuilder WithMaxTokensPerRequest(int maxTokens)
    {
        _maxTokensPerRequest = maxTokens;
        return this;
    }

    public TenantDtoBuilder WithCodeReview(bool enabled)
    {
        _enableCodeReview = enabled;
        return this;
    }

    public TenantDtoBuilder WithRepositoryCount(int count)
    {
        _repositoryCount = count;
        return this;
    }

    public TenantDtoBuilder WithTicketCount(int count)
    {
        _ticketCount = count;
        return this;
    }

    public TenantDtoBuilder WithCredentials(bool hasTicketPlatformToken, bool hasClaudeKey)
    {
        _hasTicketPlatformApiToken = hasTicketPlatformToken;
        _hasClaudeApiKey = hasClaudeKey;
        return this;
    }

    public TenantDto Build()
    {
        return new TenantDto
        {
            Id = _id,
            Name = _name,
            TicketPlatformUrl = _ticketPlatformUrl,
            TicketPlatform = _ticketPlatform,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            AutoImplementAfterPlanApproval = _autoImplementAfterPlanApproval,
            MaxRetries = _maxRetries,
            ClaudeModel = _claudeModel,
            MaxTokensPerRequest = _maxTokensPerRequest,
            EnableCodeReview = _enableCodeReview,
            RepositoryCount = _repositoryCount,
            TicketCount = _ticketCount,
            HasTicketPlatformApiToken = _hasTicketPlatformApiToken,
            HasClaudeApiKey = _hasClaudeApiKey
        };
    }

    /// <summary>
    /// Creates a fully configured tenant with credentials
    /// </summary>
    public static TenantDtoBuilder FullyConfigured()
    {
        return new TenantDtoBuilder()
            .WithCredentials(true, true)
            .WithAutoImplementAfterPlanApproval(true)
            .WithCodeReview(true)
            .WithRepositoryCount(5)
            .WithTicketCount(25);
    }

    /// <summary>
    /// Creates a tenant missing credentials
    /// </summary>
    public static TenantDtoBuilder MissingCredentials()
    {
        return new TenantDtoBuilder()
            .WithCredentials(false, false);
    }

    /// <summary>
    /// Creates an inactive tenant
    /// </summary>
    public static TenantDtoBuilder Inactive()
    {
        return new TenantDtoBuilder()
            .WithIsActive(false);
    }

    /// <summary>
    /// Creates a tenant with auto-implementation enabled
    /// </summary>
    public static TenantDtoBuilder WithAutoImplementation()
    {
        return new TenantDtoBuilder()
            .WithAutoImplementAfterPlanApproval(true)
            .WithCredentials(true, true);
    }
}
