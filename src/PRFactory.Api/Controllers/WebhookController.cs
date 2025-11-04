using Microsoft.AspNetCore.Mvc;
using PRFactory.Api.Models;
using System.Diagnostics;

namespace PRFactory.Api.Controllers;

/// <summary>
/// Handles incoming Jira webhooks
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Produces("application/json")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    // TODO: Add agent orchestration service when implementing PRFactory.Infrastructure
    // private readonly IAgentOrchestrationService _agentService;

    public WebhookController(ILogger<WebhookController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receives Jira webhook events
    /// </summary>
    /// <param name="payload">The Jira webhook payload</param>
    /// <returns>200 OK for successful receipt, 400 for invalid payloads</returns>
    /// <response code="200">Webhook received and queued for processing</response>
    /// <response code="400">Invalid webhook payload</response>
    /// <response code="401">Invalid signature</response>
    [HttpPost("jira")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveJiraWebhook([FromBody] JiraWebhookPayload payload)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Received Jira webhook: Event={EventType}, IssueKey={IssueKey}, ActivityId={ActivityId}",
            payload.WebhookEvent,
            payload.Issue?.Key,
            activityId);

        // Validate payload
        if (payload.Issue == null)
        {
            _logger.LogWarning("Webhook payload missing issue data");
            return BadRequest(new { error = "Issue data is required" });
        }

        try
        {
            // Route to appropriate workflow based on event type
            var workflowType = DetermineWorkflowType(payload);

            _logger.LogInformation(
                "Routing to workflow: Type={WorkflowType}, IssueKey={IssueKey}",
                workflowType,
                payload.Issue.Key);

            // Queue the workflow for async processing
            // This ensures we return 200 OK quickly
            await QueueWorkflowAsync(payload, workflowType, activityId);

            return Ok(new WebhookResponse
            {
                Success = true,
                Message = "Webhook received and queued for processing",
                ActivityId = activityId,
                IssueKey = payload.Issue.Key,
                WorkflowType = workflowType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for issue {IssueKey}", payload.Issue.Key);
            throw;
        }
    }

    /// <summary>
    /// Health check endpoint for webhook configuration
    /// </summary>
    /// <returns>200 OK with status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "PRFactory Webhook",
            timestamp = DateTime.UtcNow
        });
    }

    private static string DetermineWorkflowType(JiraWebhookPayload payload)
    {
        // Determine which workflow to trigger based on the webhook event
        return payload.WebhookEvent switch
        {
            // New ticket created with specific label/custom field
            "jira:issue_created" => WorkflowTypes.Trigger,

            // Comment added - could be answers or approval
            "comment_created" or "jira:issue_updated" when payload.Comment != null =>
                DetermineCommentWorkflow(payload.Comment),

            // Issue updated - check if it's a status change that triggers workflow
            "jira:issue_updated" when payload.ChangeLog?.Items.Any(i => i.Field == "status") == true =>
                WorkflowTypes.Trigger,

            // Default: ignore
            _ => WorkflowTypes.Ignore
        };
    }

    private static string DetermineCommentWorkflow(JiraComment comment)
    {
        var body = comment.Body.ToLowerInvariant().Trim();

        // Check for approval commands
        if (body.Contains("@prfactory approve") || body.Contains("@prfactory approved"))
        {
            return WorkflowTypes.Approval;
        }

        // Check for rejection commands
        if (body.Contains("@prfactory reject") || body.Contains("@prfactory rejected"))
        {
            return WorkflowTypes.Rejection;
        }

        // Check if it's an answer to questions (format: Q1: answer, Q2: answer)
        if (body.Contains("q1:") || body.Contains("q2:") || body.Contains("@prfactory answer"))
        {
            return WorkflowTypes.Answer;
        }

        // Default: might be general discussion, ignore
        return WorkflowTypes.Ignore;
    }

    private async Task QueueWorkflowAsync(JiraWebhookPayload payload, string workflowType, string activityId)
    {
        // TODO: Implement agent orchestration service integration
        // For now, just log that we would queue the workflow
        _logger.LogInformation(
            "Workflow queued: Type={WorkflowType}, IssueKey={IssueKey}, ActivityId={ActivityId}",
            workflowType,
            payload.Issue?.Key,
            activityId);

        // In the real implementation, this would:
        // 1. Create/update Ticket entity in database
        // 2. Trigger appropriate agent graph based on workflowType
        // 3. Use Agent Framework's checkpoint mechanism for persistence

        await Task.CompletedTask;
    }
}

/// <summary>
/// Workflow type constants
/// </summary>
public static class WorkflowTypes
{
    public const string Trigger = "trigger";
    public const string Answer = "answer";
    public const string Approval = "approval";
    public const string Rejection = "rejection";
    public const string Ignore = "ignore";
}

/// <summary>
/// Response for webhook receipt
/// </summary>
public class WebhookResponse
{
    /// <summary>
    /// Whether the webhook was successfully received
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Activity ID for tracing
    /// </summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>
    /// Jira issue key
    /// </summary>
    public string IssueKey { get; set; } = string.Empty;

    /// <summary>
    /// Workflow type that was triggered
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;
}
