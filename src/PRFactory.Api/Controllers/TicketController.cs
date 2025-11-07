using Microsoft.AspNetCore.Mvc;
using PRFactory.Api.Models;
using System.Diagnostics;

namespace PRFactory.Api.Controllers;

/// <summary>
/// Manages ticket operations and status queries
/// </summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketController : ControllerBase
{
    private readonly ILogger<TicketController> _logger;
    // TODO: Add repository interfaces when implementing PRFactory.Infrastructure
    // private readonly ITicketRepository _ticketRepository;
    // private readonly IAgentOrchestrationService _agentService;

    public TicketController(ILogger<TicketController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current status of a ticket
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key)</param>
    /// <returns>Ticket status with workflow information</returns>
    /// <response code="200">Ticket found and status returned</response>
    /// <response code="404">Ticket not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TicketStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketStatus(string id)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Getting status for ticket {TicketId}, ActivityId={ActivityId}",
            id,
            activityId);

        try
        {
            // TODO: Implement ticket repository lookup
            // var ticket = await _ticketRepository.GetByIdAsync(id);

            // Mock response for now
            var response = new TicketStatusResponse
            {
                TicketId = id,
                CurrentState = "pending",
                TenantId = "default",
                LastUpdated = DateTime.UtcNow,
                Created = DateTime.UtcNow.AddHours(-1),
                AwaitingHumanInput = false
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Ticket {TicketId} not found", id);
            return NotFound(new { error = $"Ticket {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket {TicketId}", id);
            throw;
        }
    }

    /// <summary>
    /// Lists tickets for a tenant with optional filtering
    /// </summary>
    /// <param name="state">Filter by workflow state</param>
    /// <param name="repository">Filter by repository name</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of tickets</returns>
    /// <response code="200">Tickets retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(ListTicketsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListTickets(
        [FromQuery] string? state = null,
        [FromQuery] string? repository = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { error = "Invalid pagination parameters. Page must be >= 1 and pageSize must be between 1 and 100." });
        }

        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Listing tickets: State={State}, Repository={Repository}, Page={Page}, PageSize={PageSize}, ActivityId={ActivityId}",
            state,
            repository,
            page,
            pageSize,
            activityId);

        try
        {
            // TODO: Implement ticket repository query
            // var (tickets, totalCount) = await _ticketRepository.ListAsync(state, repository, page, pageSize);

            // Mock response for now
            var response = new ListTicketsResponse
            {
                Tickets = new List<TicketStatusResponse>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 0
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tickets");
            throw;
        }
    }

    /// <summary>
    /// Manually approves an implementation plan
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key)</param>
    /// <param name="request">Approval request details</param>
    /// <returns>Result of the approval operation</returns>
    /// <response code="200">Plan approved successfully</response>
    /// <response code="400">Invalid request or plan not in approvable state</response>
    /// <response code="404">Ticket not found</response>
    [HttpPost("{id}/approve-plan")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePlan(string id, [FromBody] ApprovePlanRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Approving plan for ticket {TicketId}, Approved={Approved}, ApprovedBy={ApprovedBy}, ActivityId={ActivityId}",
            id,
            request.Approved,
            request.ApprovedBy,
            activityId);

        try
        {
            // TODO: Implement approval logic
            // 1. Look up ticket
            // var ticket = await _ticketRepository.GetByIdAsync(id);

            // 2. Validate ticket is in "awaiting_approval" state
            // if (ticket.CurrentState != "awaiting_approval")
            // {
            //     return BadRequest(new { error = "Ticket is not in a state that requires approval" });
            // }

            // 3. Resume the agent workflow with approval decision
            // await _agentService.ResumeWorkflowAsync(ticket.Id, new ApprovalDecision
            // {
            //     Approved = request.Approved,
            //     Comments = request.Comments,
            //     ApprovedBy = request.ApprovedBy
            // });

            // Mock response for now
            var response = new ApprovalResponse
            {
                Success = true,
                Message = request.Approved
                    ? "Plan approved. Implementation workflow will continue."
                    : "Plan approved with comments.",
                TicketStatus = new TicketStatusResponse
                {
                    TicketId = id,
                    CurrentState = request.Approved ? "implementation" : "planning",
                    TenantId = "default",
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow.AddHours(-2),
                    AwaitingHumanInput = false
                }
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Ticket {TicketId} not found", id);
            return NotFound(new { error = $"Ticket {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket {TicketId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving plan for ticket {TicketId}", id);
            throw;
        }
    }

    /// <summary>
    /// Rejects an implementation plan
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key)</param>
    /// <param name="request">Rejection request details</param>
    /// <returns>Result of the rejection operation</returns>
    /// <response code="200">Plan rejected successfully</response>
    /// <response code="400">Invalid request or plan not in rejectable state</response>
    /// <response code="404">Ticket not found</response>
    [HttpPost("{id}/reject-plan")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPlan(string id, [FromBody] RejectPlanRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Rejecting plan for ticket {TicketId}, Reason={Reason}, RestartPlanning={RestartPlanning}, RejectedBy={RejectedBy}, ActivityId={ActivityId}",
            id,
            request.Reason,
            request.RestartPlanning,
            request.RejectedBy,
            activityId);

        try
        {
            // TODO: Implement rejection logic
            // 1. Look up ticket
            // var ticket = await _ticketRepository.GetByIdAsync(id);

            // 2. Validate ticket is in "awaiting_approval" state
            // if (ticket.CurrentState != "awaiting_approval")
            // {
            //     return BadRequest(new { error = "Ticket is not in a state that requires approval" });
            // }

            // 3. Resume the agent workflow with rejection decision
            // await _agentService.ResumeWorkflowAsync(ticket.Id, new RejectionDecision
            // {
            //     Reason = request.Reason,
            //     RestartPlanning = request.RestartPlanning,
            //     RejectedBy = request.RejectedBy
            // });

            // Mock response for now
            var response = new ApprovalResponse
            {
                Success = true,
                Message = request.RestartPlanning
                    ? "Plan rejected. Planning workflow will restart with the provided feedback."
                    : "Plan rejected. Workflow will be closed.",
                TicketStatus = new TicketStatusResponse
                {
                    TicketId = id,
                    CurrentState = request.RestartPlanning ? "planning" : "rejected",
                    TenantId = "default",
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow.AddHours(-2),
                    AwaitingHumanInput = false,
                    ErrorMessage = request.RestartPlanning ? null : request.Reason
                }
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Ticket {TicketId} not found", id);
            return NotFound(new { error = $"Ticket {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket {TicketId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting plan for ticket {TicketId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets the workflow events history for a ticket
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key)</param>
    /// <returns>List of workflow events</returns>
    /// <response code="200">Events retrieved successfully</response>
    /// <response code="404">Ticket not found</response>
    [HttpGet("{id}/events")]
    [ProducesResponseType(typeof(List<WorkflowEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketEvents(string id)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Getting events for ticket {TicketId}, ActivityId={ActivityId}",
            id,
            activityId);

        try
        {
            // TODO: Implement event history lookup
            // var events = await _ticketRepository.GetEventsAsync(id);

            // Mock response for now
            var events = new List<WorkflowEventDto>();

            return Ok(events);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Ticket {TicketId} not found", id);
            return NotFound(new { error = $"Ticket {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for ticket {TicketId}", id);
            throw;
        }
    }
}
