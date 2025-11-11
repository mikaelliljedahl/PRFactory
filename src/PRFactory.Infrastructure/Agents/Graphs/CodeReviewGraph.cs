using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Specialized;

namespace PRFactory.Infrastructure.Agents.Graphs;

/// <summary>
/// CodeReviewGraph - Automated code review workflow
/// Flow: CodeReviewAgent → Parse Results → Post Feedback → Loop or Complete
///
/// Features:
/// - Reviews PR with configurable LLM provider
/// - Posts feedback to PR if issues found
/// - Loops back to ImplementationGraph for fixes (max 3 iterations)
/// - Posts approval comment if no issues
/// </summary>
public class CodeReviewGraph : AgentGraphBase
{
    private readonly IAgentExecutor _agentExecutor;

    public override string GraphId => "CodeReviewGraph";

    public CodeReviewGraph(
        ILogger<CodeReviewGraph> logger,
        ICheckpointStore checkpointStore,
        IAgentExecutor agentExecutor)
        : base(logger, checkpointStore)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
    }

    protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
        IAgentMessage inputMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Input should be ReviewCodeMessage
            if (inputMessage is not ReviewCodeMessage reviewMessage)
            {
                throw new InvalidOperationException(
                    $"Expected ReviewCodeMessage, got {inputMessage.GetType().Name}");
            }

            Logger.LogInformation(
                "Starting code review graph for ticket {TicketId}, PR #{PrNumber}",
                context.TicketId, reviewMessage.PullRequestNumber);

            return await ExecuteCodeReviewAsync(reviewMessage, context, cancellationToken, startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CodeReviewGraph failed for ticket {TicketId}", context.TicketId);
            context.State["is_failed"] = true;
            context.State["error"] = ex.Message;
            await SaveCheckpointAsync(context, "failed", "unknown");
            return GraphExecutionResult.Failure("failed", ex);
        }
    }

    protected override async Task<GraphExecutionResult> ResumeCoreAsync(
        IAgentMessage resumeMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var currentState = context.State.TryGetValue("current_state", out var state)
                ? state?.ToString()
                : "unknown";

            Logger.LogInformation(
                "Resuming CodeReviewGraph for ticket {TicketId} from state {State}",
                context.TicketId, currentState);

            // Resume not typically needed for code review (it's automated)
            // But we support it for consistency
            throw new NotImplementedException(
                "CodeReviewGraph resume not yet implemented. Reviews are fully automated.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resume CodeReviewGraph for ticket {TicketId}", context.TicketId);
            context.State["is_failed"] = true;
            context.State["error"] = ex.Message;
            await SaveCheckpointAsync(context, "resume_failed", "unknown");
            return GraphExecutionResult.Failure("resume_failed", ex);
        }
    }

    /// <summary>
    /// Execute the code review workflow
    /// </summary>
    private async Task<GraphExecutionResult> ExecuteCodeReviewAsync(
        ReviewCodeMessage reviewMessage,
        GraphContext context,
        CancellationToken cancellationToken,
        DateTime startTime)
    {
        // Get retry count from context
        var retryCount = context.State.TryGetValue("review_retry_count", out var count)
            ? Convert.ToInt32(count)
            : 0;

        Logger.LogInformation(
            "Executing code review for ticket {TicketId}, attempt {Attempt}",
            context.TicketId, retryCount + 1);

        // Stage 1: Execute CodeReviewAgent
        Logger.LogInformation("Stage 1: CodeReviewAgent for ticket {TicketId}", context.TicketId);

        var reviewResult = await ExecuteAgentAsync<CodeReviewAgent>(
            reviewMessage, context, "code_review", cancellationToken);

        await SaveCheckpointAsync(context, "review_completed", "CodeReviewAgent");

        // Parse review results from agent output
        if (!TryParseReviewResult(reviewResult, out var reviewId, out var hasCriticalIssues, out var criticalIssues, out var suggestions))
        {
            Logger.LogError("Failed to parse code review results from agent output");
            context.State["is_failed"] = true;
            await SaveCheckpointAsync(context, "parse_failed", "CodeReviewAgent");
            return GraphExecutionResult.Failure(
                "parse_failed",
                new InvalidOperationException("Failed to parse code review results"));
        }

        Logger.LogInformation(
            "Code review result: ReviewId={ReviewId}, HasIssues={HasIssues}, Critical={Critical}, Suggestions={Suggestions}",
            reviewId, hasCriticalIssues, criticalIssues.Count, suggestions.Count);

        // Store review data in context for agents to access
        context.State["review_id"] = reviewId;
        context.State["critical_issues"] = criticalIssues;
        context.State["suggestions"] = suggestions;

        // Stage 2: Check if there are critical issues
        if (hasCriticalIssues || suggestions.Count > 0)
        {
            Logger.LogInformation(
                "Review found {CriticalCount} critical issues and {SuggestionCount} suggestions",
                criticalIssues.Count, suggestions.Count);

            // Post feedback to PR (Stage 3)
            Logger.LogInformation("Stage 3: PostReviewCommentsAgent for ticket {TicketId}", context.TicketId);
            await ExecuteAgentAsync<PostReviewCommentsAgent>(
                reviewMessage, context, "post_comments", cancellationToken);
            await SaveCheckpointAsync(context, "feedback_posted", "PostReviewCommentsAgent");

            // Check if within retry limit
            const int maxRetries = 3;
            if (retryCount >= maxRetries)
            {
                Logger.LogWarning(
                    "Review retry limit reached ({RetryCount}/{MaxRetries}) for ticket {TicketId}",
                    retryCount, maxRetries, context.TicketId);

                // Post warning and complete
                // TODO: Post warning comment to PR
                context.State["is_completed"] = true;
                context.State["completed_with_warnings"] = true;
                context.State["retry_count"] = retryCount;
                await SaveCheckpointAsync(context, "max_retries_reached", "CompletedAgent");

                var duration = DateTime.UtcNow - startTime;
                return GraphExecutionResult.Success(
                    "max_retries_reached",
                    new CodeReviewCompleteMessage(
                        TicketId: context.TicketId,
                        CodeReviewResultId: reviewId,
                        HasCriticalIssues: true,
                        CriticalIssues: criticalIssues,
                        Suggestions: suggestions,
                        CompletedAt: DateTime.UtcNow
                    ),
                    duration);
            }

            // Loop back to ImplementationGraph for fixes
            Logger.LogInformation(
                "Looping back to ImplementationGraph for fixes, attempt {Attempt}",
                retryCount + 2);

            context.State["review_retry_count"] = retryCount + 1;
            context.State["pending_fixes"] = true;
            context.State["critical_issues"] = criticalIssues;
            context.State["suggestions"] = suggestions;
            await SaveCheckpointAsync(context, "awaiting_fixes", "ImplementationGraph");

            // Emit message to trigger ImplementationGraph
            var fixMessage = new FixCodeIssuesMessage(
                context.TicketId,
                Issues: criticalIssues,
                ReviewFeedback: string.Join("\n", criticalIssues.Concat(suggestions))
            );

            // Return suspended state - WorkflowOrchestrator will handle transition
            return GraphExecutionResult.Suspended("awaiting_fixes", fixMessage);
        }

        // Stage 4: No issues - post approval comment
        Logger.LogInformation(
            "Review passed with no critical issues for ticket {TicketId}",
            context.TicketId);

        Logger.LogInformation("Stage 4: PostApprovalCommentAgent for ticket {TicketId}", context.TicketId);
        await ExecuteAgentAsync<PostApprovalCommentAgent>(
            reviewMessage, context, "post_approval", cancellationToken);

        context.State["is_completed"] = true;
        context.State["is_approved"] = true;
        context.State["retry_count"] = retryCount;
        await SaveCheckpointAsync(context, "review_approved", "CompletedAgent");

        var completedDuration = DateTime.UtcNow - startTime;

        Logger.LogInformation(
            "CodeReviewGraph completed successfully for ticket {TicketId} in {Duration}ms",
            context.TicketId, completedDuration.TotalMilliseconds);

        return GraphExecutionResult.Success(
            "review_approved",
            new CodeReviewCompleteMessage(
                TicketId: context.TicketId,
                CodeReviewResultId: reviewId,
                HasCriticalIssues: false,
                CriticalIssues: new List<string>(),
                Suggestions: suggestions,
                CompletedAt: DateTime.UtcNow
            ),
            completedDuration);
    }

    /// <summary>
    /// Execute a specific agent and return the result message
    /// </summary>
    private async Task<IAgentMessage> ExecuteAgentAsync<TAgent>(
        IAgentMessage inputMessage,
        GraphContext context,
        string stage,
        CancellationToken cancellationToken)
    {
        context.State["current_stage"] = stage;
        return await _agentExecutor.ExecuteAsync<TAgent>(inputMessage, context, cancellationToken);
    }

    /// <summary>
    /// Parse review result from agent output
    /// </summary>
    private bool TryParseReviewResult(
        IAgentMessage? reviewResult,
        out Guid reviewId,
        out bool hasCriticalIssues,
        out List<string> criticalIssues,
        out List<string> suggestions)
    {
        reviewId = Guid.Empty;
        hasCriticalIssues = false;
        criticalIssues = new List<string>();
        suggestions = new List<string>();

        // Validate input
        if (reviewResult == null)
        {
            Logger.LogWarning("Review result is null");
            return false;
        }

        // Check if it's the expected CodeReviewCompleteMessage type
        if (reviewResult is not CodeReviewCompleteMessage reviewMessage)
        {
            Logger.LogWarning(
                "Expected CodeReviewCompleteMessage, got {MessageType}",
                reviewResult.GetType().Name);
            return false;
        }

        // Extract review ID
        if (reviewMessage.CodeReviewResultId == Guid.Empty)
        {
            Logger.LogWarning("Review result ID is empty");
            return false;
        }

        reviewId = reviewMessage.CodeReviewResultId;
        hasCriticalIssues = reviewMessage.HasCriticalIssues;

        // Extract critical issues - handle null list gracefully
        if (reviewMessage.CriticalIssues != null)
        {
            criticalIssues = new List<string>(reviewMessage.CriticalIssues);
            if (criticalIssues.Count > 0)
            {
                Logger.LogInformation("Parsed {Count} critical issues from review result", criticalIssues.Count);
            }
        }
        else
        {
            Logger.LogWarning("Critical issues list is null in review message");
            criticalIssues = new List<string>();
        }

        // Extract suggestions - handle null list gracefully
        if (reviewMessage.Suggestions != null)
        {
            suggestions = new List<string>(reviewMessage.Suggestions);
            if (suggestions.Count > 0)
            {
                Logger.LogInformation("Parsed {Count} suggestions from review result", suggestions.Count);
            }
        }
        else
        {
            Logger.LogWarning("Suggestions list is null in review message");
            suggestions = new List<string>();
        }

        Logger.LogInformation(
            "Successfully parsed code review result: ReviewId={ReviewId}, HasCriticalIssues={HasCriticalIssues}, CriticalIssuesCount={CriticalCount}, SuggestionsCount={SuggestionCount}",
            reviewId, hasCriticalIssues, criticalIssues.Count, suggestions.Count);

        return true;
    }
}
