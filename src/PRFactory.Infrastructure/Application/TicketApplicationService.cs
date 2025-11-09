using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing tickets.
/// This service encapsulates business logic and coordinates between repositories and workflow orchestration.
/// </summary>
public class TicketApplicationService : ITicketApplicationService
{
    private readonly ILogger<TicketApplicationService> _logger;
    private readonly ITicketRepository _ticketRepository;
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;

    public TicketApplicationService(
        ILogger<TicketApplicationService> logger,
        ITicketRepository ticketRepository,
        IRepositoryRepository repositoryRepository,
        IWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _repositoryRepository = repositoryRepository;
        _workflowOrchestrator = workflowOrchestrator;
    }

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetAllTicketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tickets");

        // TODO: Add tenant context to filter by current tenant
        // For now, we'll need to get a tenantId somehow (from context, session, etc.)
        // This is a placeholder - in a real implementation, you'd get this from the current user/session
        throw new NotImplementedException("Tenant context not yet implemented. Need to filter tickets by current tenant.");
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting ticket {TicketId}", ticketId);
        return await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tickets for repository {RepositoryId}", repositoryId);

        // Verify repository exists
        var repository = await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (repository == null)
        {
            _logger.LogWarning("Repository {RepositoryId} not found", repositoryId);
            return new List<Ticket>();
        }

        return await _ticketRepository.GetByRepositoryIdAsync(repositoryId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task TriggerWorkflowAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering workflow for ticket {TicketId}", ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Get repository for additional context
        var repository = await _repositoryRepository.GetByIdAsync(ticket.RepositoryId, cancellationToken);
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
        await _workflowOrchestrator.StartWorkflowAsync(triggerMessage, cancellationToken);

        _logger.LogInformation("Workflow triggered successfully for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving plan for ticket {TicketId}, Comments={Comments}", ticketId, comments ?? "None");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for plan approval
        if (ticket.State != WorkflowState.AwaitingPlanApproval)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting plan approval. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.PlanApproved);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with approval message
        var approvalMessage = new PlanApprovedMessage(
            TicketId: ticket.Id,
            ApprovedAt: DateTime.UtcNow,
            ApprovedBy: "User" // TODO: Get from current user context
        );

        await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, approvalMessage, cancellationToken);

        _logger.LogInformation("Plan approved for ticket {TicketKey}", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task RejectPlanAsync(Guid ticketId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rejecting plan for ticket {TicketId}, Reason={Reason}", ticketId, rejectionReason);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Verify ticket is in correct state for plan rejection
        if (ticket.State != WorkflowState.AwaitingPlanApproval)
        {
            throw new InvalidOperationException(
                $"Ticket {ticket.TicketKey} is not awaiting plan approval. Current state: {ticket.State}");
        }

        // Update ticket state
        ticket.TransitionTo(WorkflowState.PlanRejected);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with rejection message
        var rejectionMessage = new PlanRejectedMessage(
            TicketId: ticket.Id,
            Reason: rejectionReason
        );

        await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, rejectionMessage, cancellationToken);

        _logger.LogInformation("Plan rejected for ticket {TicketKey}, will regenerate", ticket.TicketKey);
    }

    /// <inheritdoc/>
    public async Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting answers for ticket {TicketId}, AnswerCount={Count}", ticketId, answers.Count);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
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
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        // Resume workflow with answers
        var answersMessage = new AnswersReceivedMessage(
            TicketId: ticket.Id,
            Answers: answers
        );

        await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, answersMessage, cancellationToken);

        _logger.LogInformation("Answers submitted for ticket {TicketKey}", ticket.TicketKey);
    }
}
