using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Jira;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Centralized error handling and recovery agent.
/// Analyzes errors, determines if they are recoverable, and suggests recovery actions.
/// Updates ticket status and posts error information to Jira if needed.
/// </summary>
public class ErrorHandlingAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IJiraService _jiraService;

    public override string Name => "ErrorHandlingAgent";
    public override string Description => "Handle errors and determine recovery actions";

    // Maximum retry attempts for different error types
    private const int MaxGeneralRetries = 3;
    private const int MaxApiRetries = 5;
    private const int MaxNetworkRetries = 3;

    public ErrorHandlingAgent(
        ILogger<ErrorHandlingAgent> logger,
        ITicketRepository ticketRepository,
        IJiraService jiraService)
        : base(logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _jiraService = jiraService ?? throw new ArgumentNullException(nameof(jiraService));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Ticket == null)
        {
            Logger.LogError("Ticket entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Ticket entity is required for error handling"
            };
        }

        // Extract error information from context
        var errorMessage = context.ErrorMessage
            ?? context.State.GetValueOrDefault("ErrorMessage")?.ToString()
            ?? "Unknown error";

        var errorDetails = context.State.GetValueOrDefault("ErrorDetails")?.ToString()
            ?? string.Empty;

        var failedAgentName = context.State.GetValueOrDefault("FailedAgent")?.ToString()
            ?? "Unknown";

        Logger.LogInformation(
            "Handling error for ticket {JiraKey}. Failed agent: {AgentName}, Error: {ErrorMessage}",
            context.Ticket.TicketKey,
            failedAgentName,
            errorMessage);

        try
        {
            // Analyze the error
            var errorAnalysis = AnalyzeError(errorMessage, errorDetails, context.Ticket.RetryCount);

            // Determine recovery action
            var recoveryAction = DetermineRecoveryAction(errorAnalysis, context.Ticket);

            // Record error in ticket
            context.Ticket.RecordError(errorMessage);

            // Update ticket state based on recovery action
            var stateTransitionResult = await UpdateTicketStateAsync(
                context.Ticket,
                recoveryAction,
                errorMessage,
                cancellationToken);

            if (!stateTransitionResult)
            {
                Logger.LogWarning(
                    "Failed to update ticket state for {JiraKey}, continuing with error handling",
                    context.Ticket.TicketKey);
            }

            // Post error notification to Jira if needed
            if (recoveryAction.ShouldNotifyUser)
            {
                await PostErrorNotificationToJiraAsync(
                    context.Ticket,
                    errorAnalysis,
                    recoveryAction,
                    cancellationToken);
            }

            // Save ticket changes
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            Logger.LogInformation(
                "Error handling completed for ticket {JiraKey}. Recovery action: {Action}, Should retry: {ShouldRetry}",
                context.Ticket.TicketKey,
                recoveryAction.Action,
                recoveryAction.ShouldRetry);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                ShouldRetry = recoveryAction.ShouldRetry,
                Output = new Dictionary<string, object>
                {
                    ["RecoveryAction"] = recoveryAction.Action,
                    ["ShouldRetry"] = recoveryAction.ShouldRetry,
                    ["IsRecoverable"] = errorAnalysis.IsRecoverable,
                    ["ErrorType"] = errorAnalysis.ErrorType,
                    ["RetryCount"] = context.Ticket.RetryCount,
                    ["UserNotified"] = recoveryAction.ShouldNotifyUser,
                    ["ErrorMessage"] = errorMessage,
                    ["Recommendation"] = recoveryAction.Recommendation
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error handling failed for ticket {JiraKey}",
                context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Error handling failed: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Analyzes the error to determine its type and recoverability
    /// </summary>
    private ErrorAnalysis AnalyzeError(string errorMessage, string errorDetails, int currentRetryCount)
    {
        var errorLower = errorMessage.ToLowerInvariant();
        var detailsLower = errorDetails.ToLowerInvariant();

        // Network/connectivity errors
        if (errorLower.Contains("timeout") ||
            errorLower.Contains("connection") ||
            errorLower.Contains("network") ||
            errorLower.Contains("unreachable"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "Network",
                IsRecoverable = currentRetryCount < MaxNetworkRetries,
                Severity = "Medium",
                Category = "Transient"
            };
        }

        // API rate limiting
        if (errorLower.Contains("rate limit") ||
            errorLower.Contains("429") ||
            errorLower.Contains("too many requests"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "RateLimit",
                IsRecoverable = currentRetryCount < MaxApiRetries,
                Severity = "Low",
                Category = "Transient"
            };
        }

        // Authentication/authorization errors
        if (errorLower.Contains("unauthorized") ||
            errorLower.Contains("forbidden") ||
            errorLower.Contains("401") ||
            errorLower.Contains("403") ||
            errorLower.Contains("authentication") ||
            errorLower.Contains("credential"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "Authentication",
                IsRecoverable = false,
                Severity = "High",
                Category = "Configuration"
            };
        }

        // Git/repository errors
        if (errorLower.Contains("git") ||
            errorLower.Contains("repository") ||
            errorLower.Contains("clone") ||
            errorLower.Contains("merge conflict"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "Repository",
                IsRecoverable = currentRetryCount < MaxGeneralRetries,
                Severity = "Medium",
                Category = "Repository"
            };
        }

        // Claude API errors
        if (errorLower.Contains("claude") ||
            errorLower.Contains("anthropic") ||
            errorLower.Contains("overloaded") ||
            errorLower.Contains("model"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "AI",
                IsRecoverable = currentRetryCount < MaxApiRetries,
                Severity = "Medium",
                Category = "ExternalService"
            };
        }

        // Validation errors
        if (errorLower.Contains("validation") ||
            errorLower.Contains("invalid") ||
            errorLower.Contains("required"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "Validation",
                IsRecoverable = false,
                Severity = "High",
                Category = "Input"
            };
        }

        // File system errors
        if (errorLower.Contains("file") ||
            errorLower.Contains("directory") ||
            errorLower.Contains("path") ||
            errorLower.Contains("permission denied"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "FileSystem",
                IsRecoverable = currentRetryCount < MaxGeneralRetries,
                Severity = "Medium",
                Category = "System"
            };
        }

        // Default: unknown error
        return new ErrorAnalysis
        {
            ErrorType = "Unknown",
            IsRecoverable = currentRetryCount < MaxGeneralRetries,
            Severity = "High",
            Category = "Unknown"
        };
    }

    /// <summary>
    /// Determines the recovery action based on error analysis
    /// </summary>
    private RecoveryAction DetermineRecoveryAction(ErrorAnalysis analysis, Domain.Entities.Ticket ticket)
    {
        // If not recoverable, fail the workflow
        if (!analysis.IsRecoverable)
        {
            return new RecoveryAction
            {
                Action = "Fail",
                ShouldRetry = false,
                ShouldNotifyUser = true,
                Recommendation = GetFailureRecommendation(analysis.ErrorType)
            };
        }

        // If retry count is within limits, retry
        if (analysis.IsRecoverable)
        {
            var delaySeconds = CalculateRetryDelay(ticket.RetryCount, analysis.ErrorType);

            return new RecoveryAction
            {
                Action = "Retry",
                ShouldRetry = true,
                ShouldNotifyUser = analysis.Severity == "High",
                DelaySeconds = delaySeconds,
                Recommendation = $"Retry after {delaySeconds} seconds delay. Retry attempt {ticket.RetryCount + 1}."
            };
        }

        // Fallback: skip the operation
        return new RecoveryAction
        {
            Action = "Skip",
            ShouldRetry = false,
            ShouldNotifyUser = true,
            Recommendation = "Operation skipped due to repeated failures."
        };
    }

    /// <summary>
    /// Calculates retry delay based on retry count and error type
    /// </summary>
    private int CalculateRetryDelay(int retryCount, string errorType)
    {
        // Exponential backoff with jitter
        var baseDelay = errorType switch
        {
            "Network" => 5,
            "RateLimit" => 60,
            "AI" => 10,
            _ => 5
        };

        var exponentialDelay = baseDelay * Math.Pow(2, retryCount);
        var jitter = new Random().Next(0, 5);

        return (int)exponentialDelay + jitter;
    }

    /// <summary>
    /// Gets a recommendation for a non-recoverable failure
    /// </summary>
    private string GetFailureRecommendation(string errorType)
    {
        return errorType switch
        {
            "Authentication" => "Check repository credentials and API tokens in tenant configuration.",
            "Validation" => "Review ticket requirements and ensure all required fields are provided.",
            "Unknown" => "Review error logs and contact support if the issue persists.",
            _ => "Review the error details and take corrective action."
        };
    }

    /// <summary>
    /// Updates ticket state based on recovery action
    /// </summary>
    private async Task<bool> UpdateTicketStateAsync(
        Domain.Entities.Ticket ticket,
        RecoveryAction recoveryAction,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (recoveryAction.Action == "Fail")
            {
                var transitionResult = ticket.TransitionTo(WorkflowState.Failed, errorMessage);
                return transitionResult.IsSuccess;
            }
            else if (recoveryAction.Action == "Retry")
            {
                // State remains unchanged for retry
                Logger.LogInformation(
                    "Ticket {JiraKey} will be retried, state remains {State}",
                    ticket.TicketKey,
                    ticket.State);
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to update ticket state for {JiraKey}",
                ticket.TicketKey);
            return false;
        }
    }

    /// <summary>
    /// Posts error notification to Jira
    /// </summary>
    private async Task PostErrorNotificationToJiraAsync(
        Domain.Entities.Ticket ticket,
        ErrorAnalysis analysis,
        RecoveryAction recoveryAction,
        CancellationToken cancellationToken)
    {
        try
        {
            var markdown = BuildErrorNotificationMarkdown(ticket, analysis, recoveryAction);

            await _jiraService.PostCommentAsync(
                ticket.TicketKey,
                markdown,
                cancellationToken);

            Logger.LogInformation(
                "Posted error notification to Jira for ticket {JiraKey}",
                ticket.TicketKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to post error notification to Jira for ticket {JiraKey}",
                ticket.TicketKey);
            // Don't rethrow - posting notification failure shouldn't fail error handling
            // The error has been logged for investigation
        }
    }

    /// <summary>
    /// Builds error notification markdown for Jira
    /// </summary>
    private string BuildErrorNotificationMarkdown(
        Domain.Entities.Ticket ticket,
        ErrorAnalysis analysis,
        RecoveryAction recoveryAction)
    {
        var markdown = $@"## ⚠️ Error Encountered

**Error Type:** {analysis.ErrorType}
**Severity:** {analysis.Severity}
**Recoverable:** {(analysis.IsRecoverable ? "Yes" : "No")}

**Error Message:**
```
{ticket.LastError}
```

**Recovery Action:** {recoveryAction.Action}

";

        if (recoveryAction.ShouldRetry)
        {
            markdown += $"**Retry Attempt:** {ticket.RetryCount + 1}\n";
            markdown += $"**Next Retry:** After {recoveryAction.DelaySeconds} seconds\n\n";
        }

        markdown += $"**Recommendation:**\n{recoveryAction.Recommendation}\n";

        return markdown;
    }

    /// <summary>
    /// Error analysis result
    /// </summary>
    private class ErrorAnalysis
    {
        public string ErrorType { get; set; } = string.Empty;
        public bool IsRecoverable { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Recovery action information
    /// </summary>
    private class RecoveryAction
    {
        public string Action { get; set; } = string.Empty;
        public bool ShouldRetry { get; set; }
        public bool ShouldNotifyUser { get; set; }
        public int DelaySeconds { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}
