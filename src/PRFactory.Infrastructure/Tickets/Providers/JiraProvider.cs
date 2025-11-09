using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Jira;
using PRFactory.Infrastructure.Jira.Models;
using Refit;

namespace PRFactory.Infrastructure.Tickets.Providers;

/// <summary>
/// Jira implementation of ticket platform provider
/// Wraps JiraService to provide platform-agnostic interface
/// </summary>
public class JiraProvider : ITicketPlatformProvider
{
    private readonly ILogger<JiraProvider> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly IJiraClient _jiraClient;

    public string PlatformName => "Jira";

    public JiraProvider(
        ILogger<JiraProvider> logger,
        ITenantRepository tenantRepository,
        IJiraClient jiraClient)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
        _jiraClient = jiraClient;
    }

    public async Task<CommentInfo> PostCommentAsync(
        Guid tenantId,
        string ticketKey,
        string markdownText,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        var jiraComment = await jiraService.PostCommentAsync(ticketKey, markdownText, ct);

        return new CommentInfo(
            jiraComment.Id,
            ExtractPlainText(jiraComment.Body),
            jiraComment.Created ?? DateTime.UtcNow
        );
    }

    public async Task LinkPullRequestAsync(
        Guid tenantId,
        string ticketKey,
        string prUrl,
        string prTitle,
        string? repositoryName = null,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        await jiraService.LinkPullRequestAsync(ticketKey, prUrl, prTitle, repositoryName, ct);
    }

    public async Task UpdateCustomFieldAsync(
        Guid tenantId,
        string ticketKey,
        string fieldKey,
        object value,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        await jiraService.UpdateCustomFieldAsync(ticketKey, fieldKey, value, ct);
    }

    public async Task TransitionToStatusAsync(
        Guid tenantId,
        string ticketKey,
        string statusName,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        await jiraService.TransitionToStatusAsync(ticketKey, statusName, ct);
    }

    public async Task<TicketInfo> GetTicketAsync(
        Guid tenantId,
        string ticketKey,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        var issue = await jiraService.GetIssueAsync(ticketKey, ct);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant {tenantId} not found");

        return new TicketInfo(
            issue.Key,
            issue.Fields.Summary ?? string.Empty,
            ExtractPlainText(issue.Fields.Description),
            issue.Fields.Status?.Name ?? "Unknown",
            $"{tenant.TicketPlatformUrl}/browse/{issue.Key}"
        );
    }

    public async Task UpdateTitleAsync(
        Guid tenantId,
        string ticketKey,
        string title,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        await jiraService.UpdateSummaryAsync(ticketKey, title, ct);
    }

    public async Task AddLabelsAsync(
        Guid tenantId,
        string ticketKey,
        string[] labels,
        CancellationToken ct = default)
    {
        var jiraService = await CreateJiraServiceAsync(tenantId, ct);

        await jiraService.AddLabelsAsync(ticketKey, labels, ct);
    }

    /// <summary>
    /// Creates a JiraService instance configured for the specific tenant
    /// </summary>
    private async Task<TenantJiraService> CreateJiraServiceAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant {tenantId} not found");

        if (tenant.TicketPlatform != "Jira")
            throw new InvalidOperationException($"Tenant {tenantId} is not configured for Jira (platform: {tenant.TicketPlatform})");

        // Create a tenant-specific Jira service with the tenant's credentials
        // Note: In production, consider using a factory or caching mechanism
        var jiraService = new TenantJiraService(
            _jiraClient,
            _logger,
            tenant.TicketPlatformUrl,
            tenant.TicketPlatformApiToken
        );

        return jiraService;
    }

    /// <summary>
    /// Extracts plain text from Jira ADF (Atlassian Document Format) content
    /// </summary>
    private string ExtractPlainText(JiraContent? content)
    {
        if (content == null || content.Content == null)
            return string.Empty;

        var textParts = new List<string>();

        foreach (var node in content.Content)
        {
            if (node.Content != null)
            {
                foreach (var textNode in node.Content)
                {
                    if (textNode.Text != null)
                        textParts.Add(textNode.Text);
                }
            }
        }

        return string.Join(" ", textParts);
    }
}

/// <summary>
/// Tenant-specific wrapper around JiraService
/// Allows creating JiraService instances with tenant-specific credentials
/// </summary>
internal class TenantJiraService : IJiraService
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraService> _logger;
    private readonly string _jiraUrl;
    private readonly string _jiraApiToken;

    public TenantJiraService(
        IJiraClient client,
        ILogger logger,
        string jiraUrl,
        string jiraApiToken)
    {
        _client = client;
        _logger = logger as ILogger<JiraService> ?? throw new ArgumentException("Logger must be ILogger<JiraService>", nameof(logger));
        _jiraUrl = jiraUrl;
        _jiraApiToken = jiraApiToken;
    }

    // Delegate all IJiraService methods to the underlying implementation
    // For simplicity, we'll create a new JiraService instance for each call
    // In production, this could be optimized with caching

    public Task<JiraComment> PostCommentAsync(string issueKey, string markdownText, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.PostCommentAsync(issueKey, markdownText, ct);
    }

    public Task LinkPullRequestAsync(string issueKey, string prUrl, string prTitle, string? repositoryName = null, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.LinkPullRequestAsync(issueKey, prUrl, prTitle, repositoryName, ct);
    }

    public Task UpdateCustomFieldAsync(string issueKey, string fieldKey, object value, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.UpdateCustomFieldAsync(issueKey, fieldKey, value, ct);
    }

    public Task TransitionToStatusAsync(string issueKey, string statusName, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.TransitionToStatusAsync(issueKey, statusName, ct);
    }

    public Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.GetIssueAsync(issueKey, ct);
    }

    public Task UpdateSummaryAsync(string issueKey, string summary, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.UpdateSummaryAsync(issueKey, summary, ct);
    }

    public Task AddLabelsAsync(string issueKey, string[] labels, CancellationToken ct = default)
    {
        var service = CreateService();
        return service.AddLabelsAsync(issueKey, labels, ct);
    }

    private JiraService CreateService()
    {
        // Create a minimal configuration object for JiraService
        var configDict = new Dictionary<string, string?>
        {
            ["Jira:BaseUrl"] = _jiraUrl,
            ["Jira:ApiToken"] = _jiraApiToken
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        return new JiraService(_client, _logger, configuration);
    }
}
