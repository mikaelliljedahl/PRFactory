namespace PRFactory.Infrastructure.Agents.Graphs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

/// <summary>
/// Enhanced PlanningGraph - Multi-agent orchestration for comprehensive planning
///
/// Flow:
/// 1. PmUserStoriesAgent → generates user stories
/// 2. ArchitectApiDesignAgent + ArchitectDbSchemaAgent (parallel) → API + DB design
/// 3. QaTestCasesAgent → test cases based on API/DB/stories
/// 4. TechLeadImplementationAgent → implementation steps
/// 5. PlanArtifactStorageAgent → save all artifacts to database
/// 6. GitPlanAgent + JiraPostAgent (parallel) → commit + post
/// 7. Suspend for human approval
///
/// Revision workflow:
/// - On rejection: FeedbackAnalysisAgent → PlanRevisionAgent → re-storage → re-commit → re-review
/// - On approval: Complete and transition to implementation
/// </summary>
public class PlanningGraph(
    ILogger<PlanningGraph> logger,
    Base.ICheckpointStore checkpointStore,
    IAgentExecutor agentExecutor) : AgentGraphBase(logger, checkpointStore)
{
    public override string GraphId => "PlanningGraph";

    protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
        IAgentMessage inputMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            Logger.LogInformation(
                "Starting enhanced planning workflow for ticket {TicketId}",
                context.TicketId);

            // STEP 1: PM User Stories (Foundation - Sequential)
            Logger.LogInformation("Step 1: Generating user stories for ticket {TicketId}", context.TicketId);

            var userStoriesMessage = await ExecuteAgentAsync<PmUserStoriesAgent>(
                inputMessage, context, "user_stories", cancellationToken);

            if (userStoriesMessage == null)
            {
                return GraphExecutionResult.Failure(
                    "user_stories_failed",
                    new InvalidOperationException("User stories generation failed"));
            }

            await SaveCheckpointAsync(context, "user_stories_generated", "PmUserStoriesAgent");

            // STEP 2: Parallel Execution (API Design + DB Schema)
            Logger.LogInformation(
                "Step 2: Generating API design and database schema (parallel) for ticket {TicketId}",
                context.TicketId);

            var apiDesignTask = ExecuteAgentAsync<ArchitectApiDesignAgent>(
                userStoriesMessage, context, "api_design", cancellationToken);
            var dbSchemaTask = ExecuteAgentAsync<ArchitectDbSchemaAgent>(
                userStoriesMessage, context, "db_schema", cancellationToken);

            var parallelResults = await Task.WhenAll(apiDesignTask, dbSchemaTask);
            var apiDesignMessage = parallelResults[0];
            var dbSchemaMessage = parallelResults[1];

            if (apiDesignMessage == null)
            {
                Logger.LogWarning("API design generation failed for ticket {TicketId}", context.TicketId);
            }

            if (dbSchemaMessage == null)
            {
                Logger.LogWarning("Database schema generation failed for ticket {TicketId}", context.TicketId);
            }

            await SaveCheckpointAsync(context, "architecture_designed", "ArchitectApiDesignAgent,ArchitectDbSchemaAgent");

            // STEP 3: QA Test Cases (Sequential - depends on API + Schema)
            Logger.LogInformation("Step 3: Generating test cases for ticket {TicketId}", context.TicketId);

            var testCasesMessage = await ExecuteAgentAsync<QaTestCasesAgent>(
                userStoriesMessage, context, "test_cases", cancellationToken);

            if (testCasesMessage == null)
            {
                Logger.LogWarning("Test cases generation failed for ticket {TicketId}", context.TicketId);
            }

            await SaveCheckpointAsync(context, "test_cases_defined", "QaTestCasesAgent");

            // STEP 4: Tech Lead Implementation Steps (Sequential)
            Logger.LogInformation("Step 4: Generating implementation steps for ticket {TicketId}", context.TicketId);

            var implementationMessage = await ExecuteAgentAsync<TechLeadImplementationAgent>(
                userStoriesMessage, context, "implementation", cancellationToken);

            if (implementationMessage == null)
            {
                Logger.LogWarning("Implementation steps generation failed for ticket {TicketId}", context.TicketId);
            }

            await SaveCheckpointAsync(context, "implementation_planned", "TechLeadImplementationAgent");

            // STEP 5: Store all artifacts in database
            Logger.LogInformation("Step 5: Storing artifacts in database for ticket {TicketId}", context.TicketId);

            var storageMessage = await ExecuteAgentAsync<PlanArtifactStorageAgent>(
                implementationMessage ?? userStoriesMessage, context, "storage", cancellationToken);

            if (storageMessage == null)
            {
                return GraphExecutionResult.Failure(
                    "artifact_storage_failed",
                    new InvalidOperationException("Artifact storage failed"));
            }

            await SaveCheckpointAsync(context, "plan_stored", "PlanArtifactStorageAgent");

            // STEP 6: Commit to Git and Post to Jira (Parallel)
            Logger.LogInformation(
                "Step 6: Committing artifacts and posting to Jira (parallel) for ticket {TicketId}",
                context.TicketId);

            var gitTask = ExecuteAgentAsync<GitPlanAgent>(
                storageMessage, context, "git_plan", cancellationToken);
            var jiraTask = ExecuteAgentAsync<JiraPostAgent>(
                storageMessage, context, "jira_post", cancellationToken);

            var finalResults = await Task.WhenAll(gitTask, jiraTask);
            var gitMessage = finalResults[0];
            var jiraMessage = finalResults[1];

            if (gitMessage == null)
            {
                Logger.LogWarning("Git commit failed for ticket {TicketId}", context.TicketId);
            }

            if (jiraMessage == null)
            {
                Logger.LogWarning("Jira post failed for ticket {TicketId}", context.TicketId);
            }

            await SaveCheckpointAsync(context, "plan_posted", "GitPlanAgent,JiraPostAgent");

            // STEP 7: Suspend for human review
            Logger.LogInformation("Step 7: Suspending for human review for ticket {TicketId}", context.TicketId);

            context.State["is_suspended"] = true;
            context.State["waiting_for"] = "plan_approval";
            await SaveCheckpointAsync(context, "awaiting_approval", "HumanWaitAgent");

            // Return with appropriate output message (storageMessage guaranteed non-null at this point)
            var outputMessage = jiraMessage ?? storageMessage;

            return GraphExecutionResult.Suspended("awaiting_approval", outputMessage);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Enhanced planning workflow failed for ticket {TicketId}", context.TicketId);
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
        try
        {
            var currentState = context.State.TryGetValue("current_state", out var state)
                ? state?.ToString()
                : "unknown";

            Logger.LogInformation(
                "Resuming PlanningGraph for ticket {TicketId} from state {State}",
                context.TicketId, currentState);

            if (currentState == "awaiting_approval" || currentState == "awaiting_re_review")
            {
                // Check if approved or rejected
                if (resumeMessage is PlanApprovedMessage approvedMessage)
                {
                    var startTime = DateTime.UtcNow;
                    return await HandlePlanApprovalAsync(approvedMessage, context, cancellationToken, startTime);
                }
                else if (resumeMessage is PlanRejectedMessage rejectedMessage)
                {
                    return await HandlePlanRejectionAsync(rejectedMessage, context, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Expected PlanApprovedMessage or PlanRejectedMessage, got {resumeMessage.GetType().Name}");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot resume from state {currentState}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resume PlanningGraph for ticket {TicketId}", context.TicketId);
            context.State["is_failed"] = true;
            context.State["error"] = ex.Message;
            await SaveCheckpointAsync(context, "resume_failed", "unknown");
            return GraphExecutionResult.Failure("resume_failed", ex);
        }
    }

    /// <summary>
    /// Handle plan approval - emit plan_approved event
    /// </summary>
    private async Task<GraphExecutionResult> HandlePlanApprovalAsync(
        PlanApprovedMessage approvedMessage,
        GraphContext context,
        CancellationToken cancellationToken,
        DateTime startTime)
    {
        Logger.LogInformation(
            "Plan approved for ticket {TicketId} by {ApprovedBy}",
            context.TicketId, approvedMessage.ApprovedBy);

        // Mark as completed
        context.State["is_completed"] = true;
        context.State["is_suspended"] = false;
        context.State["approved_by"] = approvedMessage.ApprovedBy;
        context.State["approved_at"] = approvedMessage.ApprovedAt;
        await SaveCheckpointAsync(context, "plan_approved", "CompletedAgent");

        var duration = DateTime.UtcNow - startTime;

        // Emit plan_approved event for WorkflowOrchestrator
        var approvedEvent = new PlanApprovedEvent(
            context.TicketId,
            approvedMessage.ApprovedAt
        );

        Logger.LogInformation(
            "PlanningGraph completed with approval for ticket {TicketId} in {Duration}ms",
            context.TicketId, duration.TotalMilliseconds);

        return GraphExecutionResult.Success("plan_approved", approvedEvent, duration);
    }

    /// <summary>
    /// Handle plan rejection with feedback analysis and targeted regeneration
    /// </summary>
    private async Task<GraphExecutionResult> HandlePlanRejectionAsync(
        PlanRejectedMessage rejectedMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = context.TicketId;

        Logger.LogInformation(
            "Plan rejected for ticket {TicketId}. Feedback: {Feedback}",
            ticketId,
            rejectedMessage.Reason.Length > 100 ? rejectedMessage.Reason.Substring(0, 100) + "..." : rejectedMessage.Reason);

        // Increment retry count
        var retryCount = context.State.TryGetValue("plan_retry_count", out var count)
            ? Convert.ToInt32(count)
            : 0;
        retryCount++;

        context.State["plan_retry_count"] = retryCount;

        // Check retry limit
        const int maxRetries = 5;
        if (retryCount > maxRetries)
        {
            Logger.LogError(
                "Plan rejected too many times ({RetryCount}) for ticket {TicketId}, failing workflow",
                retryCount, ticketId);

            context.State["is_failed"] = true;
            await SaveCheckpointAsync(context, "too_many_rejections", "FailedAgent");
            return GraphExecutionResult.Failure(
                "too_many_rejections",
                new InvalidOperationException($"Plan rejected {retryCount} times, exceeding maximum retries"));
        }

        // STEP 1: Analyze feedback to determine affected artifacts
        Logger.LogInformation("Analyzing feedback to determine affected artifacts for ticket {TicketId}", ticketId);

        context.State["RevisionFeedback"] = rejectedMessage.Reason;
        context.State["RefinementInstructions"] = rejectedMessage.RefinementInstructions ?? string.Empty;

        var analysisMessage = await ExecuteAgentAsync<FeedbackAnalysisAgent>(
            rejectedMessage, context, "feedback_analysis", cancellationToken);

        // Get affected artifacts (fallback to all if analysis fails)
        List<string> affectedArtifacts;
        if (analysisMessage == null)
        {
            Logger.LogWarning("Feedback analysis failed, regenerating all artifacts for ticket {TicketId}", ticketId);
            affectedArtifacts = GetAllArtifactTypes();
        }
        else
        {
            affectedArtifacts = context.State.TryGetValue("AffectedArtifacts", out var artifacts)
                && artifacts is List<string> list
                ? list
                : GetAllArtifactTypes();

            Logger.LogInformation(
                "Affected artifacts identified for ticket {TicketId}: {Artifacts}",
                ticketId,
                string.Join(", ", affectedArtifacts));
        }

        context.State["AffectedArtifacts"] = affectedArtifacts;

        // STEP 2: Regenerate only affected artifacts
        Logger.LogInformation("Regenerating affected artifacts for ticket {TicketId}", ticketId);

        var revisionMessage = await ExecuteAgentAsync<PlanRevisionAgent>(
            rejectedMessage, context, "plan_revision", cancellationToken);

        if (revisionMessage == null)
        {
            Logger.LogWarning("Artifact regeneration failed for ticket {TicketId}", ticketId);
        }

        // STEP 3: Store revised artifacts (creates new version)
        Logger.LogInformation("Storing revised artifacts for ticket {TicketId}", ticketId);

        context.State["IsRevision"] = true;
        context.State["RevisionVersion"] = retryCount + 1;

        var storageMessage = await ExecuteAgentAsync<PlanArtifactStorageAgent>(
            revisionMessage ?? rejectedMessage, context, "storage", cancellationToken);

        if (storageMessage == null)
        {
            Logger.LogWarning("Storage of revised artifacts failed for ticket {TicketId}", ticketId);
        }

        await SaveCheckpointAsync(context, "revision_stored", "PlanArtifactStorageAgent");

        // STEP 4: Commit revisions and post update to Jira (Parallel)
        Logger.LogInformation(
            "Committing revisions and posting updates (parallel) for ticket {TicketId}",
            ticketId);

        var gitTask = ExecuteAgentAsync<GitPlanAgent>(
            storageMessage ?? rejectedMessage, context, "git_plan", cancellationToken);
        var jiraTask = ExecuteAgentAsync<JiraPostAgent>(
            storageMessage ?? rejectedMessage, context, "jira_post", cancellationToken);

        var finalResults = await Task.WhenAll(gitTask, jiraTask);
        var gitMessage = finalResults[0];
        var jiraMessage = finalResults[1];

        if (gitMessage == null)
        {
            Logger.LogWarning("Git revision commit failed for ticket {TicketId}", ticketId);
        }

        if (jiraMessage == null)
        {
            Logger.LogWarning("Jira revision update failed for ticket {TicketId}", ticketId);
        }

        await SaveCheckpointAsync(context, "revision_posted", "GitPlanAgent,JiraPostAgent");

        // STEP 5: Suspend for re-review
        Logger.LogInformation("Suspending for plan re-review for ticket {TicketId}", ticketId);

        context.State["is_suspended"] = true;
        context.State["waiting_for"] = "plan_re_approval";
        await SaveCheckpointAsync(context, "awaiting_re_review", "HumanWaitAgent");

        // Return with appropriate output message (fallback chain)
        var outputMessage = jiraMessage ?? storageMessage ?? revisionMessage ??
            new MessagePostedMessage(ticketId, "plan_revised", DateTime.UtcNow);

        return GraphExecutionResult.Suspended("awaiting_re_review", outputMessage);
    }

    /// <summary>
    /// Execute a specific agent and return the result message
    /// </summary>
    private async Task<IAgentMessage?> ExecuteAgentAsync<TAgent>(
        IAgentMessage inputMessage,
        GraphContext context,
        string stage,
        CancellationToken cancellationToken)
    {
        try
        {
            context.State["current_stage"] = stage;
            return await agentExecutor.ExecuteAsync<TAgent>(inputMessage, context, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Agent {AgentType} failed for ticket {TicketId}", typeof(TAgent).Name, context.TicketId);
            return null;
        }
    }

    /// <summary>
    /// Get list of all artifact types
    /// </summary>
    private List<string> GetAllArtifactTypes()
    {
        return new List<string>
        {
            "UserStories",
            "ApiDesign",
            "DatabaseSchema",
            "TestCases",
            "ImplementationSteps"
        };
    }
}

// Agent type markers for ExecuteAgentAsync<TAgent> generic resolution
public class PlanningAgent { }
public class GitPlanAgent { }
public class PmUserStoriesAgent { }
public class ArchitectApiDesignAgent { }
public class ArchitectDbSchemaAgent { }
public class QaTestCasesAgent { }
public class TechLeadImplementationAgent { }
public class PlanArtifactStorageAgent { }
public class FeedbackAnalysisAgent { }
public class PlanRevisionAgent { }

