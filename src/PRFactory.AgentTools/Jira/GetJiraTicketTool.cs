using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Jira;
using System.Text.Json;

namespace PRFactory.AgentTools.Jira;

/// <summary>
/// Fetch Jira ticket details.
/// </summary>
public class GetJiraTicketTool : ToolBase
{
    private readonly IJiraService _jiraService;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "GetJiraTicket";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Fetch Jira ticket details including title, description, status, and assignee";

    /// <summary>
    /// Create a new GetJiraTicketTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="jiraService">Jira service</param>
    public GetJiraTicketTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        IJiraService jiraService)
        : base(logger, tenantContext)
    {
        _jiraService = jiraService;
    }

    /// <summary>
    /// Execute the tool to get Jira ticket details
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Ticket details as JSON</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var ticketKey = context.GetParameter<string>("ticketKey");

        // 1. Validate ticket key format (e.g., "PROJ-123")
        ValidateTicketKey(ticketKey);

        // 2. Fetch ticket with timeout and rate limiting
        var issue = await ExecuteWithTimeoutAsync(
            () => _jiraService.GetIssueAsync(ticketKey, CancellationToken.None),
            TimeSpan.FromSeconds(30)
        );

        // 3. Extract relevant information
        var result = new
        {
            key = issue.Key,
            title = issue.Fields.Summary,
            description = issue.Fields.Description?.ToString() ?? "",
            status = issue.Fields.Status?.Name ?? "Unknown",
            assignee = issue.Fields.Assignee?.DisplayName ?? "Unassigned",
            reporter = issue.Fields.Reporter?.DisplayName ?? "Unknown",
            issueType = issue.Fields.IssueType?.Name ?? "Unknown"
        };

        _logger.LogInformation(
            "Retrieved Jira ticket {TicketKey} for tenant {TenantId}",
            ticketKey, context.TenantId);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("ticketKey"))
            throw new ArgumentException("Parameter 'ticketKey' is required");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate Jira ticket key format
    /// </summary>
    /// <param name="ticketKey">Ticket key to validate</param>
    /// <exception cref="ArgumentException">Thrown when ticket key is invalid</exception>
    private static void ValidateTicketKey(string ticketKey)
    {
        if (string.IsNullOrWhiteSpace(ticketKey))
        {
            throw new ArgumentException("Ticket key cannot be empty");
        }

        // Basic format validation: PROJECT-123
        if (!System.Text.RegularExpressions.Regex.IsMatch(ticketKey, @"^[A-Z][A-Z0-9]+-\d+$"))
        {
            throw new ArgumentException(
                $"Invalid ticket key format: '{ticketKey}'. Expected format: PROJECT-123");
        }

        if (ticketKey.Length > 50)
        {
            throw new ArgumentException("Ticket key cannot exceed 50 characters");
        }
    }
}
