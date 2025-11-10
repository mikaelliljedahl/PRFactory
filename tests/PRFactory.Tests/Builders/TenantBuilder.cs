using PRFactory.Domain.Entities;

namespace PRFactory.Tests.Builders;

/// <summary>
/// Fluent builder for creating Tenant entities in tests with sensible defaults
/// </summary>
public class TenantBuilder
{
    private string _name = "Test Tenant";
    private string _ticketPlatform = "Jira";
    private string _ticketPlatformUrl = "https://test-tenant.atlassian.net";
    private string _ticketPlatformApiToken = "test-jira-token";
    private string _claudeApiKey = "test-claude-api-key";
    private bool _isActive = true;
    private TenantConfiguration? _configuration;

    public TenantBuilder()
    {
    }

    public TenantBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TenantBuilder WithTicketPlatform(string platform)
    {
        _ticketPlatform = platform;
        return this;
    }

    public TenantBuilder WithTicketPlatformUrl(string url)
    {
        _ticketPlatformUrl = url;
        return this;
    }

    public TenantBuilder WithTicketPlatformApiToken(string token)
    {
        _ticketPlatformApiToken = token;
        return this;
    }

    public TenantBuilder WithClaudeApiKey(string apiKey)
    {
        _claudeApiKey = apiKey;
        return this;
    }

    public TenantBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    public TenantBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public TenantBuilder WithConfiguration(TenantConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }

    public TenantBuilder WithConfiguration(Action<TenantConfiguration> configureAction)
    {
        var config = new TenantConfiguration();
        configureAction(config);
        _configuration = config;
        return this;
    }

    public TenantBuilder WithAutoImplementation(bool enabled = true)
    {
        _configuration ??= new TenantConfiguration();
        _configuration.AutoImplementAfterPlanApproval = enabled;
        return this;
    }

    public TenantBuilder WithMaxRetries(int maxRetries)
    {
        _configuration ??= new TenantConfiguration();
        _configuration.MaxRetries = maxRetries;
        return this;
    }

    public TenantBuilder WithClaudeModel(string model)
    {
        _configuration ??= new TenantConfiguration();
        _configuration.ClaudeModel = model;
        return this;
    }

    public Tenant Build()
    {
        var tenant = Tenant.Create(
            _name,
            _ticketPlatformUrl,
            _ticketPlatformApiToken,
            _claudeApiKey,
            _ticketPlatform);

        if (_configuration != null)
        {
            tenant.UpdateConfiguration(_configuration);
        }

        if (!_isActive)
        {
            tenant.Deactivate();
        }

        return tenant;
    }
}
