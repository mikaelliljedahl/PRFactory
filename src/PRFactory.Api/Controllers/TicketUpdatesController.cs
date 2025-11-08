using Microsoft.AspNetCore.Mvc;
using PRFactory.Api.Models;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using System.Diagnostics;

namespace PRFactory.Api.Controllers;

/// <summary>
/// Manages ticket update operations including approval, rejection, and retrieval.
///
/// IMPORTANT: This controller is designed for EXTERNAL clients only:
/// - Jira webhooks (e.g., @claude mentions)
/// - Future mobile apps
/// - Third-party integrations
/// - External API consumers
///
/// ARCHITECTURE NOTE:
/// - Internal Blazor Server components should NOT call this controller via HTTP.
/// - Internal components should inject ITicketUpdateService directly (same service this controller uses).
/// - This avoids unnecessary HTTP overhead within the same process.
///
/// This controller acts as an HTTP facade over the application service layer.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class TicketUpdatesController : ControllerBase
{
    private readonly ILogger<TicketUpdatesController> _logger;
    private readonly ITicketUpdateService _ticketUpdateService;
    private readonly ITicketRepository _ticketRepository;

    public TicketUpdatesController(
        ILogger<TicketUpdatesController> logger,
        ITicketUpdateService ticketUpdateService,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _ticketUpdateService = ticketUpdateService;
        _ticketRepository = ticketRepository;
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

            // Use application service
            var ticketUpdate = await _ticketUpdateService.GetLatestTicketUpdateAsync(ticketId);
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
            // Use application service
            var ticketUpdate = await _ticketUpdateService.UpdateTicketUpdateAsync(
                ticketUpdateId,
                request.UpdatedTitle ?? string.Empty,
                request.UpdatedDescription ?? string.Empty,
                request.AcceptanceCriteria ?? string.Empty);

            // Get ticket for ticket key
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

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
            // Use application service
            var ticketUpdate = await _ticketUpdateService.ApproveTicketUpdateAsync(
                ticketUpdateId,
                approvedBy: request.ApprovedBy);

            // Get ticket
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

            _logger.LogInformation(
                "Approved ticket update {TicketUpdateId} for ticket {TicketKey}",
                ticketUpdateId, ticket.TicketKey);

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
            // Use application service
            var ticketUpdate = await _ticketUpdateService.RejectTicketUpdateAsync(
                ticketUpdateId,
                request.Reason,
                rejectedBy: request.RejectedBy,
                regenerate: request.Regenerate);

            // Get ticket
            var ticket = await _ticketRepository.GetByIdAsync(ticketUpdate.TicketId);
            if (ticket == null)
            {
                return NotFound(new { error = "Associated ticket not found" });
            }

            _logger.LogInformation(
                "Rejected ticket update {TicketUpdateId} for ticket {TicketKey}, Regenerate={Regenerate}",
                ticketUpdateId, ticket.TicketKey, request.Regenerate);

            string message = request.Regenerate
                ? "Ticket update rejected. A new version will be generated based on your feedback."
                : "Ticket update rejected. No regeneration will occur.";

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
