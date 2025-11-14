using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;

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
    IPlanService planService,
    ICurrentUserService currentUserService) : ITicketApplicationService
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

        // Create regenerated plan revision if regenerating completely
        if (regenerateCompletely)
        {
            try
            {
                var currentUserId = await currentUserService.GetCurrentUserIdAsync();
                await planService.CreateRevisionAsync(
                    ticket.Id,
                    PlanRevisionReason.Regenerated,
                    createdByUserId: currentUserId);

                logger.LogInformation(
                    "Created regenerated plan revision for ticket {TicketId} by user {UserId}",
                    ticket.Id, currentUserId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the workflow - revision creation is not critical
                logger.LogError(ex,
                    "Failed to create regenerated plan revision for ticket {TicketId}, continuing workflow",
                    ticket.Id);
            }
        }

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

        // Create refined plan revision after plan refined
        try
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            await planService.CreateRevisionAsync(
                ticket.Id,
                PlanRevisionReason.Refined,
                createdByUserId: currentUserId);

            logger.LogInformation(
                "Created refined plan revision for ticket {TicketId} by user {UserId}",
                ticket.Id, currentUserId);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the workflow - revision creation is not critical
            logger.LogError(ex,
                "Failed to create refined plan revision for ticket {TicketId}, continuing workflow",
                ticket.Id);
        }

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
}
