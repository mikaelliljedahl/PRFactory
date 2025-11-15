using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Jira;

namespace PRFactory.AgentTools.Jira;

/// <summary>
/// Add comment to Jira ticket.
/// </summary>
public class AddJiraCommentTool : ToolBase
{
    private readonly IJiraService _jiraService;
    private const int MaxCommentLength = 10000;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "AddJiraComment";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Add comment to Jira ticket";

    /// <summary>
    /// Create a new AddJiraCommentTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="jiraService">Jira service</param>
    public AddJiraCommentTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        IJiraService jiraService)
        : base(logger, tenantContext)
    {
        _jiraService = jiraService;
    }

    /// <summary>
    /// Execute the tool to add a comment
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var ticketKey = context.GetParameter<string>("ticketKey");
        var comment = context.GetParameter<string>("comment");

        // 1. Validate ticket key format
        ValidateTicketKey(ticketKey);

        // 2. Validate comment
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty");
        }

        if (comment.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Comment length {comment.Length} exceeds limit {MaxCommentLength} characters");
        }

        // 3. Post comment with timeout
        var result = await ExecuteWithTimeoutAsync(
            () => _jiraService.PostCommentAsync(ticketKey, comment, CancellationToken.None),
            TimeSpan.FromSeconds(30)
        );

        _logger.LogInformation(
            "Added comment to Jira ticket {TicketKey} for tenant {TenantId}",
            ticketKey, context.TenantId);

        return $"Successfully added comment to {ticketKey}\nComment ID: {result.Id}";
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

        if (!context.Parameters.ContainsKey("comment"))
            throw new ArgumentException("Parameter 'comment' is required");

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
