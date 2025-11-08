using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing ticket updates.
/// This service encapsulates business logic and coordinates between repositories and workflow orchestration.
/// </summary>
public class TicketUpdateService : ITicketUpdateService
{
    private readonly ILogger<TicketUpdateService> _logger;
    private readonly ITicketUpdateRepository _ticketUpdateRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;

    public TicketUpdateService(
        ILogger<TicketUpdateService> logger,
        ITicketUpdateRepository ticketUpdateRepository,
        ITicketRepository ticketRepository,
        IWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _ticketUpdateRepository = ticketUpdateRepository;
        _ticketRepository = ticketRepository;
        _workflowOrchestrator = workflowOrchestrator;
    }

    /// <inheritdoc/>
    public async Task<TicketUpdate?> GetLatestTicketUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting latest ticket update for ticket {TicketId}", ticketId);

        // Verify ticket exists
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return null;
        }

        // Get latest draft (most recent update regardless of status)
        var ticketUpdate = await _ticketUpdateRepository.GetLatestDraftByTicketIdAsync(ticketId, cancellationToken);

        if (ticketUpdate == null)
        {
            _logger.LogDebug("No ticket updates found for ticket {TicketId}", ticketId);
        }

        return ticketUpdate;
    }

    /// <inheritdoc/>
    public async Task<TicketUpdate> UpdateTicketUpdateAsync(
        Guid ticketUpdateId,
        string updatedTitle,
        string updatedDescription,
        string acceptanceCriteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating ticket update {TicketUpdateId}", ticketUpdateId);

        var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId, cancellationToken);
        if (ticketUpdate == null)
        {
            throw new InvalidOperationException($"TicketUpdate {ticketUpdateId} not found");
        }

        // Verify it's a draft (can only edit drafts)
        if (!ticketUpdate.IsDraft)
        {
            throw new InvalidOperationException("Can only edit draft ticket updates");
        }

        // Verify ticket exists
        var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException("Associated ticket not found");
        }

        // Validate that at least one field changed
        var hasChanges =
            updatedTitle != ticketUpdate.UpdatedTitle ||
            updatedDescription != ticketUpdate.UpdatedDescription ||
            acceptanceCriteria != ticketUpdate.AcceptanceCriteria;

        if (!hasChanges)
        {
            throw new InvalidOperationException("No changes provided");
        }

        // Update the entity (keeps existing success criteria)
        ticketUpdate.Update(
            updatedTitle: updatedTitle,
            updatedDescription: updatedDescription,
            successCriteria: ticketUpdate.SuccessCriteria, // Keep existing success criteria
            acceptanceCriteria: acceptanceCriteria
        );

        await _ticketUpdateRepository.UpdateAsync(ticketUpdate, cancellationToken);

        _logger.LogInformation(
            "Updated ticket update {TicketUpdateId} for ticket {TicketKey}",
            ticketUpdateId, ticket.TicketKey);

        return ticketUpdate;
    }

    /// <inheritdoc/>
    public async Task<TicketUpdate> ApproveTicketUpdateAsync(
        Guid ticketUpdateId,
        string? approvedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Approving ticket update {TicketUpdateId}, ApprovedBy={ApprovedBy}",
            ticketUpdateId,
            approvedBy ?? "Unknown");

        var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId, cancellationToken);
        if (ticketUpdate == null)
        {
            throw new InvalidOperationException($"TicketUpdate {ticketUpdateId} not found");
        }

        // Get ticket
        var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException("Associated ticket not found");
        }

        // Verify it's in a draft state
        if (!ticketUpdate.IsDraft || ticketUpdate.IsApproved)
        {
            throw new InvalidOperationException("Ticket update is not in a state that can be approved");
        }

        // Approve the ticket update
        ticketUpdate.Approve();
        await _ticketUpdateRepository.UpdateAsync(ticketUpdate, cancellationToken);

        // Update ticket workflow state
        ticket.TransitionTo(PRFactory.Domain.ValueObjects.WorkflowState.TicketUpdateApproved);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        _logger.LogInformation(
            "Approved ticket update {TicketUpdateId} for ticket {TicketKey}",
            ticketUpdateId, ticket.TicketKey);

        // Resume workflow with approval message
        var approvalMessage = new TicketUpdateApprovedMessage(
            TicketId: ticket.Id,
            TicketUpdateId: ticketUpdateId,
            ApprovedAt: ticketUpdate.ApprovedAt!.Value,
            ApprovedBy: approvedBy ?? "Unknown"
        );

        await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, approvalMessage, cancellationToken);

        return ticketUpdate;
    }

    /// <inheritdoc/>
    public async Task<TicketUpdate> RejectTicketUpdateAsync(
        Guid ticketUpdateId,
        string reason,
        string? rejectedBy = null,
        bool regenerate = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rejecting ticket update {TicketUpdateId}, Reason={Reason}, Regenerate={Regenerate}, RejectedBy={RejectedBy}",
            ticketUpdateId,
            reason,
            regenerate,
            rejectedBy ?? "Unknown");

        var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId, cancellationToken);
        if (ticketUpdate == null)
        {
            throw new InvalidOperationException($"TicketUpdate {ticketUpdateId} not found");
        }

        // Get ticket
        var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException("Associated ticket not found");
        }

        // Verify it's in a draft state
        if (!ticketUpdate.IsDraft)
        {
            throw new InvalidOperationException("Can only reject draft ticket updates");
        }

        if (ticketUpdate.IsApproved)
        {
            throw new InvalidOperationException("Cannot reject an approved ticket update");
        }

        // Reject the ticket update
        ticketUpdate.Reject(reason);
        await _ticketUpdateRepository.UpdateAsync(ticketUpdate, cancellationToken);

        // Update ticket workflow state
        ticket.TransitionTo(PRFactory.Domain.ValueObjects.WorkflowState.TicketUpdateRejected);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        _logger.LogInformation(
            "Rejected ticket update {TicketUpdateId} for ticket {TicketKey}, Regenerate={Regenerate}",
            ticketUpdateId, ticket.TicketKey, regenerate);

        // Resume workflow with rejection message if regeneration is requested
        if (regenerate)
        {
            var rejectionMessage = new TicketUpdateRejectedMessage(
                TicketId: ticket.Id,
                TicketUpdateId: ticketUpdateId,
                Reason: reason
            );

            await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, rejectionMessage, cancellationToken);
        }

        return ticketUpdate;
    }
}
