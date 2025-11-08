using Microsoft.AspNetCore.Mvc;
using PRFactory.Api.Models;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using System.Diagnostics;

namespace PRFactory.Api.Controllers;

/// <summary>
/// Manages ticket update operations including approval, rejection, and retrieval
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class TicketUpdatesController : ControllerBase
{
    private readonly ILogger<TicketUpdatesController> _logger;
    private readonly ITicketUpdateRepository _ticketUpdateRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;

    public TicketUpdatesController(
        ILogger<TicketUpdatesController> logger,
        ITicketUpdateRepository ticketUpdateRepository,
        ITicketRepository ticketRepository,
        IWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _ticketUpdateRepository = ticketUpdateRepository;
        _ticketRepository = ticketRepository;
        _workflowOrchestrator = workflowOrchestrator;
    }

    /// <summary>
    /// Gets the latest ticket update for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID (GUID)</param>
    /// <returns>The latest ticket update</returns>
    /// <response code="200">Ticket update retrieved successfully</response>
    /// <response code="404">Ticket or ticket update not found</response>
    [HttpGet("tickets/{ticketId:guid}/updates/latest")]
    [ProducesResponseType(typeof(TicketUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestTicketUpdate(Guid ticketId)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Getting latest ticket update for ticket {TicketId}, ActivityId={ActivityId}",
            ticketId,
            activityId);

        try
        {
            // Verify ticket exists
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", ticketId);
                return NotFound(new { error = $"Ticket {ticketId} not found" });
            }

            // Get latest draft (most recent update regardless of status)
            var ticketUpdate = await _ticketUpdateRepository.GetLatestDraftByTicketIdAsync(ticketId);
            if (ticketUpdate == null)
            {
                _logger.LogWarning("No ticket updates found for ticket {TicketId}", ticketId);
                return NotFound(new { error = $"No ticket updates found for ticket {ticketId}" });
            }

            var response = MapToResponse(ticketUpdate, ticket.TicketKey);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest ticket update for ticket {TicketId}", ticketId);
            throw;
        }
    }

    /// <summary>
    /// Updates a ticket update (for manual edits)
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="request">The update request</param>
    /// <returns>Updated ticket update</returns>
    /// <response code="200">Ticket update edited successfully</response>
    /// <response code="400">Invalid request or ticket update not in editable state</response>
    /// <response code="404">Ticket update not found</response>
    [HttpPut("ticket-updates/{ticketUpdateId:guid}")]
    [ProducesResponseType(typeof(TicketUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicketUpdate(
        Guid ticketUpdateId,
        [FromBody] UpdateTicketUpdateRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Updating ticket update {TicketUpdateId}, ActivityId={ActivityId}",
            ticketUpdateId,
            activityId);

        try
        {
            var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId);
            if (ticketUpdate == null)
            {
                _logger.LogWarning("TicketUpdate {TicketUpdateId} not found", ticketUpdateId);
                return NotFound(new { error = $"TicketUpdate {ticketUpdateId} not found" });
            }

            // Verify it's a draft (can only edit drafts)
            if (!ticketUpdate.IsDraft)
            {
                return BadRequest(new { error = "Can only edit draft ticket updates" });
            }

            // Get ticket for ticket key
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

            // Apply updates (only update fields that are provided)
            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.UpdatedTitle) && request.UpdatedTitle != ticketUpdate.UpdatedTitle)
            {
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.UpdatedDescription) && request.UpdatedDescription != ticketUpdate.UpdatedDescription)
            {
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.AcceptanceCriteria) && request.AcceptanceCriteria != ticketUpdate.AcceptanceCriteria)
            {
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return BadRequest(new { error = "No changes provided" });
            }

            // Update the entity
            ticketUpdate.Update(
                updatedTitle: request.UpdatedTitle ?? ticketUpdate.UpdatedTitle,
                updatedDescription: request.UpdatedDescription ?? ticketUpdate.UpdatedDescription,
                successCriteria: ticketUpdate.SuccessCriteria, // Keep existing success criteria
                acceptanceCriteria: request.AcceptanceCriteria ?? ticketUpdate.AcceptanceCriteria
            );

            await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

            _logger.LogInformation(
                "Updated ticket update {TicketUpdateId} for ticket {TicketKey}",
                ticketUpdateId, ticket.TicketKey);

            var response = MapToResponse(ticketUpdate, ticket.TicketKey);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket update {TicketUpdateId}", ticketUpdateId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    /// <summary>
    /// Approves a ticket update
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="request">The approval request</param>
    /// <returns>Result of the approval operation</returns>
    /// <response code="200">Ticket update approved successfully</response>
    /// <response code="400">Invalid request or ticket update not in approvable state</response>
    /// <response code="404">Ticket update not found</response>
    [HttpPost("ticket-updates/{ticketUpdateId:guid}/approve")]
    [ProducesResponseType(typeof(TicketUpdateOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveTicketUpdate(
        Guid ticketUpdateId,
        [FromBody] ApproveTicketUpdateRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Approving ticket update {TicketUpdateId}, ApprovedBy={ApprovedBy}, ActivityId={ActivityId}",
            ticketUpdateId,
            request.ApprovedBy,
            activityId);

        try
        {
            var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId);
            if (ticketUpdate == null)
            {
                _logger.LogWarning("TicketUpdate {TicketUpdateId} not found", ticketUpdateId);
                return NotFound(new { error = $"TicketUpdate {ticketUpdateId} not found" });
            }

            // Get ticket
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

            // Verify it's in a draft state
            if (!ticketUpdate.IsDraft || ticketUpdate.IsApproved)
            {
                return BadRequest(new { error = "Ticket update is not in a state that can be approved" });
            }

            // Approve the ticket update
            ticketUpdate.Approve();
            await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

            // Update ticket workflow state
            ticket.UpdateWorkflowState(WorkflowState.TicketUpdateApproved);
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation(
                "Approved ticket update {TicketUpdateId} for ticket {TicketKey}",
                ticketUpdateId, ticket.TicketKey);

            // Resume workflow with approval message
            var approvalMessage = new TicketUpdateApprovedMessage(
                TicketId: ticket.Id,
                TicketUpdateId: ticketUpdateId,
                ApprovedAt: ticketUpdate.ApprovedAt!.Value,
                ApprovedBy: request.ApprovedBy ?? "Unknown"
            );

            await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, approvalMessage);

            var response = new TicketUpdateOperationResponse
            {
                Success = true,
                Message = "Ticket update approved successfully. The update will be posted to the ticket system.",
                TicketUpdate = MapToResponse(ticketUpdate, ticket.TicketKey),
                TicketState = ticket.CurrentState.ToString()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket update {TicketUpdateId}", ticketUpdateId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    /// <summary>
    /// Rejects a ticket update with a reason
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="request">The rejection request</param>
    /// <returns>Result of the rejection operation</returns>
    /// <response code="200">Ticket update rejected successfully</response>
    /// <response code="400">Invalid request or ticket update not in rejectable state</response>
    /// <response code="404">Ticket update not found</response>
    [HttpPost("ticket-updates/{ticketUpdateId:guid}/reject")]
    [ProducesResponseType(typeof(TicketUpdateOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectTicketUpdate(
        Guid ticketUpdateId,
        [FromBody] RejectTicketUpdateRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Rejecting ticket update {TicketUpdateId}, Reason={Reason}, Regenerate={Regenerate}, RejectedBy={RejectedBy}, ActivityId={ActivityId}",
            ticketUpdateId,
            request.Reason,
            request.Regenerate,
            request.RejectedBy,
            activityId);

        try
        {
            var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId);
            if (ticketUpdate == null)
            {
                _logger.LogWarning("TicketUpdate {TicketUpdateId} not found", ticketUpdateId);
                return NotFound(new { error = $"TicketUpdate {ticketUpdateId} not found" });
            }

            // Get ticket
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

            // Verify it's in a draft state
            if (!ticketUpdate.IsDraft)
            {
                return BadRequest(new { error = "Can only reject draft ticket updates" });
            }

            if (ticketUpdate.IsApproved)
            {
                return BadRequest(new { error = "Cannot reject an approved ticket update" });
            }

            // Reject the ticket update
            ticketUpdate.Reject(request.Reason);
            await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

            // Update ticket workflow state
            ticket.UpdateWorkflowState(WorkflowState.TicketUpdateRejected);
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation(
                "Rejected ticket update {TicketUpdateId} for ticket {TicketKey}, Regenerate={Regenerate}",
                ticketUpdateId, ticket.TicketKey, request.Regenerate);

            // Resume workflow with rejection message if regeneration is requested
            string message;
            if (request.Regenerate)
            {
                var rejectionMessage = new TicketUpdateRejectedMessage(
                    TicketId: ticket.Id,
                    TicketUpdateId: ticketUpdateId,
                    Reason: request.Reason
                );

                await _workflowOrchestrator.ResumeWorkflowAsync(ticket.Id, rejectionMessage);

                message = "Ticket update rejected. A new version will be generated based on your feedback.";
            }
            else
            {
                message = "Ticket update rejected. No regeneration will occur.";
            }

            var response = new TicketUpdateOperationResponse
            {
                Success = true,
                Message = message,
                TicketUpdate = MapToResponse(ticketUpdate, ticket.TicketKey),
                TicketState = ticket.CurrentState.ToString()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket update {TicketUpdateId}", ticketUpdateId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    /// <summary>
    /// Maps a TicketUpdate entity to a TicketUpdateResponse DTO
    /// </summary>
    private TicketUpdateResponse MapToResponse(Domain.Entities.TicketUpdate ticketUpdate, string ticketKey)
    {
        return new TicketUpdateResponse
        {
            TicketUpdateId = ticketUpdate.Id,
            TicketId = ticketUpdate.TicketId,
            TicketKey = ticketKey,
            UpdatedTitle = ticketUpdate.UpdatedTitle,
            UpdatedDescription = ticketUpdate.UpdatedDescription,
            AcceptanceCriteria = ticketUpdate.AcceptanceCriteria,
            Version = ticketUpdate.Version,
            IsDraft = ticketUpdate.IsDraft,
            IsApproved = ticketUpdate.IsApproved,
            RejectionReason = ticketUpdate.RejectionReason,
            GeneratedAt = ticketUpdate.GeneratedAt,
            ApprovedAt = ticketUpdate.ApprovedAt,
            PostedAt = ticketUpdate.PostedAt,
            SuccessCriteria = ticketUpdate.SuccessCriteria.Select(sc => new SuccessCriterionDto
            {
                Category = sc.Category.ToString(),
                Description = sc.Description,
                Priority = sc.Priority,
                IsTestable = sc.IsTestable
            }).ToList()
        };
    }
}
