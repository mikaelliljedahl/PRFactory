using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Jira;

namespace PRFactory.AgentTools.Jira;

/// <summary>
/// Transition Jira ticket to a new status.
/// </summary>
public class TransitionJiraTicketTool : ToolBase
{
    private readonly IJiraService _jiraService;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "TransitionJiraTicket";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Transition Jira ticket to a new status";

    /// <summary>
    /// Create a new TransitionJiraTicketTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="jiraService">Jira service</param>
    public TransitionJiraTicketTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        IJiraService jiraService)
        : base(logger, tenantContext)
    {
        _jiraService = jiraService;
    }

    /// <summary>
    /// Execute the tool to transition ticket status
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var ticketKey = context.GetParameter<string>("ticketKey");
        var transitionName = context.GetParameter<string>("transitionName");

        // 1. Validate ticket key format
        ValidateTicketKey(ticketKey);

        // 2. Validate transition name
        if (string.IsNullOrWhiteSpace(transitionName))
        {
            throw new ArgumentException("Transition name cannot be empty");
        }

        if (transitionName.Length > 100)
        {
            throw new ArgumentException("Transition name cannot exceed 100 characters");
        }

        // 3. Transition ticket with timeout
        await _jiraService.TransitionToStatusAsync(ticketKey, transitionName, CancellationToken.None);

        _logger.LogInformation(
            "Transitioned Jira ticket {TicketKey} to status '{TransitionName}' for tenant {TenantId}",
            ticketKey, transitionName, context.TenantId);

        return $"Successfully transitioned {ticketKey} to '{transitionName}'";
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

        if (!context.Parameters.ContainsKey("transitionName"))
            throw new ArgumentException("Parameter 'transitionName' is required");

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
