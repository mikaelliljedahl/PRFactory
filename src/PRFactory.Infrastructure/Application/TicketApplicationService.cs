using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.Results;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Git;

// Type alias to resolve ambiguity between domain and graph WorkflowState
using WorkflowState = PRFactory.Domain.ValueObjects.WorkflowState;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing tickets.
/// This service encapsulates business logic and coordinates between repositories and workflow orchestration.
/// </summary>
public class TicketApplicationService(
    ILogger<TicketApplicationService> logger,
    ITicketRepository ticketRepository,
    IRepositoryRepository repositoryRepository,
    IWorkflowOrchestrator workflowOrchestrator,
    ITenantContext tenantContext,
    IWorkspaceService workspaceService,
    ILocalGitService localGitService,
    IGitPlatformProvider gitPlatformProvider,
    ITicketUpdateRepository ticketUpdateRepository) : ITicketApplicationService
{

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetAllTicketsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all tickets");

        // Get current tenant ID from context
        var tenantId = await tenantContext.GetCurrentTenantIdAsync(cancellationToken);

        // Get all tickets for the current tenant
        var tickets = await ticketRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        logger.LogDebug("Found {TicketCount} tickets for tenant {TenantId}", tickets.Count, tenantId);

        return tickets;
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting ticket {TicketId}", ticketId);
        return await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tickets for repository {RepositoryId}", repositoryId);

        // Verify repository exists
        var repository = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (repository == null)
        {
            logger.LogWarning("Repository {RepositoryId} not found", repositoryId);
            return new List<Ticket>();
        }

        return await ticketRepository.GetByRepositoryIdAsync(repositoryId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Ticket>> GetTicketsPagedAsync(
        PaginationParams paginationParams,
        WorkflowState? stateFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting paged tickets: Page {Page}, PageSize {PageSize}, SearchQuery {SearchQuery}, StateFilter {StateFilter}",
            paginationParams.Page, paginationParams.PageSize, paginationParams.SearchQuery ?? "None", stateFilter?.ToString() ?? "None");

        return await ticketRepository.GetTicketsPagedAsync(paginationParams, stateFilter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task TriggerWorkflowAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Triggering workflow for ticket {TicketId}", ticketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Get repository for additional context
        var repository = await repositoryRepository.GetByIdAsync(ticket.RepositoryId, cancellationToken);
        if (repository == null)
        {
            throw new InvalidOperationException($"Repository {ticket.RepositoryId} not found for ticket {ticketId}");
        }

        // Create trigger message
        var triggerMessage = new TriggerTicketMessage(
            TicketKey: ticket.TicketKey,
            TenantId: ticket.TenantId,
            RepositoryId: ticket.RepositoryId,
            TicketSystem: ticket.TicketSystem
        )
        {
            TicketId = ticket.Id
        };

        // Start workflow
        await workflowOrchestrator.StartWorkflowAsync(triggerMessage, cancellationToken);

        logger.LogInformation("Workflow triggered successfully for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Approving plan for ticket {TicketId}, Comments={Comments}", ticketId, comments ?? "None");

        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for plan approval
        if (ticket.State != WorkflowState.PlanUnderReview)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting plan approval. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.PlanApproved);
        await ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with approval message
        var approvalMessage = new PlanApprovedMessage(
            TicketId: ticket.Id,
            ApprovedAt: DateTime.UtcNow,
            ApprovedBy: "User" // TODO: Get from current user context
        );

        await workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, approvalMessage, cancellationToken);

        logger.LogInformation("Plan approved for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task RejectPlanAsync(Guid ticketId, string rejectionReason, bool regenerateCompletely = false, CancellationToken cancellationToken = default)
    {
        var action = regenerateCompletely ? "Rejecting and regenerating" : "Rejecting";
        logger.LogInformation("{Action} plan for ticket {TicketId}, Reason={Reason}", action, ticketId, rejectionReason);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for plan rejection
        if (ticket.State != WorkflowState.PlanUnderReview)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting plan approval. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.PlanRejected);
        await ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with rejection message
        var rejectionMessage = new PlanRejectedMessage(
            TicketId: ticket.Id,
            Reason: rejectionReason,
            RefinementInstructions: null,
            RegenerateCompletely: regenerateCompletely
        );

        await workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, rejectionMessage, cancellationToken);

        var actionComplete = regenerateCompletely ? "rejected and will regenerate" : "rejected";
        logger.LogInformation("Plan {ActionComplete} for ticket {TicketKey}", actionComplete, ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task RefinePlanAsync(Guid ticketId, string refinementInstructions, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Refining plan for ticket {TicketId} with instructions", ticketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for plan refinement
        if (ticket.State != WorkflowState.PlanUnderReview)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting plan approval. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.PlanRejected);
        await ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with refinement message
        var refinementMessage = new PlanRejectedMessage(
            TicketId: ticket.Id,
            Reason: "Plan refinement requested",
            RefinementInstructions: refinementInstructions,
            RegenerateCompletely: false
        );

        await workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, refinementMessage, cancellationToken);

        logger.LogInformation("Plan refinement requested for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Submitting answers for ticket {TicketId}, AnswerCount={Count}", ticketId, answers.Count);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for answer submission
        if (ticket.State != WorkflowState.AwaitingAnswers)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting answers. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.AnswersReceived);
        await ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with answers
        var answersMessage = new AnswersReceivedMessage(
            TicketId: ticket.Id,
            Answers: answers
        );

        await workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, answersMessage, cancellationToken);

        logger.LogInformation("Answers submitted for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task<string?> GetDiffContentAsync(Guid ticketId)
    {
        logger.LogDebug("Getting diff content for ticket {TicketId}", ticketId);

        try
        {
            var diffContent = await workspaceService.ReadDiffAsync(ticketId);

            if (diffContent == null)
            {
                logger.LogInformation("No diff available for ticket {TicketId}", ticketId);
                return null;
            }

            logger.LogInformation("Retrieved diff for ticket {TicketId}: {Size} bytes",
                ticketId, diffContent.Length);

            return diffContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving diff for ticket {TicketId}", ticketId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequestCreationResult> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null)
    {
        logger.LogInformation("Creating pull request for ticket {TicketId}, approved by {ApprovedBy}",
            ticketId, approvedBy ?? "unknown");

        try
        {
            // Get ticket and validate state
            var ticket = await ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                return PullRequestCreationResult.Failed($"Ticket {ticketId} not found");
            }

            if (ticket.State != WorkflowState.Implementing)
            {
                return PullRequestCreationResult.Failed($"Ticket is not in Implementing state (current: {ticket.State})");
            }

            // Get repository information
            var repository = await repositoryRepository.GetByIdAsync(ticket.RepositoryId);
            if (repository == null)
            {
                return PullRequestCreationResult.Failed($"Repository {ticket.RepositoryId} not found");
            }

            // Get repository path and branch name
            var repoPath = workspaceService.GetRepositoryPath(ticketId);
            var branchName = $"feature/{ticket.TicketKey}";

            // Push branch to remote
            await localGitService.PushAsync(repoPath, branchName, repository.AccessToken);

            // Build PR description
            var prDescription = await BuildPRDescriptionAsync(ticket);

            // Create PR via platform provider
            var createPrRequest = new CreatePullRequestRequest(
                SourceBranch: branchName,
                TargetBranch: repository.DefaultBranch ?? "main",
                Title: $"{ticket.TicketKey}: {ticket.Title}",
                Description: prDescription
            );

            var pr = await gitPlatformProvider.CreatePullRequestAsync(repository.Id, createPrRequest);

            // Update ticket state to PRCreated
            ticket.MarkPRCreated(pr.Number, pr.Url);
            await ticketRepository.UpdateAsync(ticket);

            // Clean up diff file (no longer needed)
            await workspaceService.DeleteDiffAsync(ticketId);

            logger.LogInformation("Pull request created for ticket {TicketId}: {PrUrl}", ticketId, pr.Url);

            return PullRequestCreationResult.Successful(pr.Url, pr.Number);
        }
        catch (LibGit2SharpException ex)
        {
            logger.LogError(ex, "Git error creating pull request for ticket {TicketId}", ticketId);
            return PullRequestCreationResult.Failed($"Git error: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error creating pull request for ticket {TicketId}", ticketId);
            return PullRequestCreationResult.Failed($"Platform API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating pull request for ticket {TicketId}", ticketId);
            return PullRequestCreationResult.Failed($"Error creating PR: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds the pull request description from ticket information.
    /// Retrieves latest ticket update if available for enhanced description.
    /// </summary>
    /// <param name="ticket">The ticket</param>
    /// <returns>PR description markdown</returns>
    private async Task<string> BuildPRDescriptionAsync(Ticket ticket)
    {
        logger.LogDebug("Building PR description for ticket {TicketId}", ticket.Id);

        // Retrieve latest approved ticket update (if available)
        var ticketUpdate = await ticketUpdateRepository.GetLatestApprovedByTicketIdAsync(ticket.Id);

        if (ticketUpdate == null)
        {
            logger.LogWarning("No ticket update found for ticket {TicketId}, using basic description", ticket.Id);
            return BuildBasicPRDescription(ticket);
        }

        return BuildDetailedPRDescription(ticket, ticketUpdate);
    }

    /// <summary>
    /// Builds a basic PR description when no ticket update is available.
    /// </summary>
    /// <param name="ticket">The ticket</param>
    /// <returns>Basic PR description markdown</returns>
    private string BuildBasicPRDescription(Ticket ticket)
    {
        return $@"## {ticket.TicketKey}: {ticket.Title}

### Description

{ticket.Description}

---

*Generated by PRFactory*
";
    }

    /// <summary>
    /// Builds a detailed PR description including ticket update artifacts.
    /// </summary>
    /// <param name="ticket">The ticket</param>
    /// <param name="ticketUpdate">The approved ticket update with refined requirements</param>
    /// <returns>Detailed PR description markdown</returns>
    private string BuildDetailedPRDescription(Ticket ticket, TicketUpdate ticketUpdate)
    {
        var description = new System.Text.StringBuilder();

        description.AppendLine($"## {ticket.TicketKey}: {ticketUpdate.UpdatedTitle}");
        description.AppendLine();

        // Ticket description (refined)
        description.AppendLine("### Description");
        description.AppendLine(ticketUpdate.UpdatedDescription);
        description.AppendLine();

        // Success criteria (if available)
        if (ticketUpdate.SuccessCriteria.Count > 0)
        {
            description.AppendLine("### Success Criteria");
            description.AppendLine();

            var mustHave = ticketUpdate.GetMustHaveCriteria();
            if (mustHave.Count > 0)
            {
                description.AppendLine("#### Must Have (Priority 0)");
                foreach (var criterion in mustHave)
                {
                    description.AppendLine($"- {criterion.Description}");
                }
                description.AppendLine();
            }

            var shouldHave = ticketUpdate.GetShouldHaveCriteria();
            if (shouldHave.Count > 0)
            {
                description.AppendLine("#### Should Have (Priority 1)");
                foreach (var criterion in shouldHave)
                {
                    description.AppendLine($"- {criterion.Description}");
                }
                description.AppendLine();
            }

            var niceToHave = ticketUpdate.GetNiceToHaveCriteria();
            if (niceToHave.Count > 0)
            {
                description.AppendLine("#### Nice to Have (Priority 2)");
                foreach (var criterion in niceToHave)
                {
                    description.AppendLine($"- {criterion.Description}");
                }
                description.AppendLine();
            }
        }

        // Acceptance criteria (if available)
        if (!string.IsNullOrWhiteSpace(ticketUpdate.AcceptanceCriteria))
        {
            description.AppendLine("### Acceptance Criteria");
            description.AppendLine(ticketUpdate.AcceptanceCriteria);
            description.AppendLine();
        }

        // Footer with timestamp
        description.AppendLine("---");
        description.AppendLine($"*Generated by PRFactory on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");

        return description.ToString();
    }
}
