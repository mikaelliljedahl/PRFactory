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
    /// Creates a new ticket via Web UI
    /// </summary>
    /// <param name="request">Ticket creation request</param>
    /// <returns>Created ticket information</returns>
    /// <response code="201">Ticket created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Creating ticket: Title={Title}, RepositoryId={RepositoryId}, EnableExternalSync={EnableExternalSync}, ExternalSystem={ExternalSystem}, ActivityId={ActivityId}",
            request.Title,
            request.RepositoryId,
            request.EnableExternalSync,
            request.ExternalSystem,
            activityId);

        try
        {
            // TODO: Implement ticket creation
            // 1. Validate repository exists
            // var repository = await _repositoryService.GetByIdAsync(request.RepositoryId);
            // if (repository == null)
            // {
            //     return BadRequest(new { error = "Repository not found" });
            // }

            // 2. Create ticket entity
            // var ticket = new Ticket
            // {
            //     Id = Guid.NewGuid(),
            //     Title = request.Title,
            //     Description = request.Description,
            //     RepositoryId = request.RepositoryId,
            //     CurrentState = WorkflowState.Pending,
            //     CreatedAt = DateTime.UtcNow
            // };

            // 3. If external sync enabled, create ticket in external system (e.g., Jira)
            // if (request.EnableExternalSync && !string.IsNullOrEmpty(request.ExternalSystem))
            // {
            //     var externalKey = await _externalTicketService.CreateTicketAsync(ticket, request.ExternalSystem);
            //     ticket.ExternalTicketKey = externalKey;
            // }

            // 4. Save ticket to database
            // await _ticketRepository.AddAsync(ticket);

            // 5. Trigger workflow orchestrator
            // await _agentService.StartWorkflowAsync(ticket.Id);

            // Mock response for now
            var ticketId = Guid.NewGuid();
            var response = new CreateTicketResponse
            {
                TicketId = ticketId,
                TicketKey = request.EnableExternalSync ?? false ? "MOCK-001" : $"WEB-{ticketId.ToString()[..8].ToUpper()}",
                CurrentState = "pending",
                CreatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(
                nameof(GetTicketStatus),
                new { id = response.TicketKey },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            throw;
        }
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
                Message = request.RestartPlanning ?? false
                    ? "Plan rejected. Planning workflow will restart with the provided feedback."
                    : "Plan rejected. Workflow will be closed.",
                TicketStatus = new TicketStatusResponse
                {
                    TicketId = id,
                    CurrentState = request.RestartPlanning ?? false ? "planning" : "rejected",
                    TenantId = "default",
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow.AddHours(-2),
                    AwaitingHumanInput = false,
                    ErrorMessage = request.RestartPlanning ?? false ? null : request.Reason
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
    /// Gets the clarifying questions for a ticket
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key or Web UI ticket key)</param>
    /// <returns>List of questions with their answer status</returns>
    /// <response code="200">Questions retrieved successfully</response>
    /// <response code="404">Ticket not found</response>
    [HttpGet("{id}/questions")]
    [ProducesResponseType(typeof(QuestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketQuestions(string id)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Getting questions for ticket {TicketId}, ActivityId={ActivityId}",
            id,
            activityId);

        try
        {
            // TODO: Implement questions retrieval
            // 1. Look up ticket
            // var ticket = await _ticketRepository.GetByIdAsync(id);
            // if (ticket == null)
            // {
            //     return NotFound(new { error = $"Ticket {id} not found" });
            // }

            // 2. Get questions from ticket state/checkpoint
            // var questions = await _ticketRepository.GetQuestionsAsync(ticket.Id);

            // 3. Build response with question DTOs
            // var response = new QuestionsResponse
            // {
            //     TicketId = ticket.Id,
            //     Questions = questions.Select(q => new QuestionDto
            //     {
            //         Id = q.Id,
            //         Text = q.Text,
            //         IsAnswered = q.Answer != null,
            //         AnswerText = q.Answer
            //     }).ToList(),
            //     AllAnswered = questions.All(q => q.Answer != null)
            // };

            // Mock response for now
            var response = new QuestionsResponse
            {
                TicketId = Guid.NewGuid(),
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Id = "q1",
                        Text = "What is the expected behavior when the user is not authenticated?",
                        IsAnswered = false,
                        AnswerText = null
                    },
                    new QuestionDto
                    {
                        Id = "q2",
                        Text = "Should the feature support mobile devices?",
                        IsAnswered = false,
                        AnswerText = null
                    }
                },
                AllAnswered = false
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
            _logger.LogError(ex, "Error retrieving questions for ticket {TicketId}", id);
            throw;
        }
    }

    /// <summary>
    /// Submits answers to clarifying questions for a ticket
    /// </summary>
    /// <param name="id">The ticket ID (Jira issue key or Web UI ticket key)</param>
    /// <param name="request">Answers to the questions</param>
    /// <returns>Result of the submission</returns>
    /// <response code="200">Answers submitted successfully</response>
    /// <response code="400">Invalid request or ticket not in question state</response>
    /// <response code="404">Ticket not found</response>
    [HttpPost("{id}/answers")]
    [ProducesResponseType(typeof(TicketStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAnswers(string id, [FromBody] SubmitAnswersRequest request)
    {
        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Submitting answers for ticket {TicketId}, AnswerCount={AnswerCount}, ActivityId={ActivityId}",
            id,
            request.Answers.Count,
            activityId);

        try
        {
            // TODO: Implement answer submission
            // 1. Look up ticket
            // var ticket = await _ticketRepository.GetByIdAsync(id);
            // if (ticket == null)
            // {
            //     return NotFound(new { error = $"Ticket {id} not found" });
            // }

            // 2. Validate ticket is in awaiting_questions state
            // if (ticket.CurrentState != WorkflowState.AwaitingQuestions)
            // {
            //     return BadRequest(new { error = "Ticket is not awaiting answers" });
            // }

            // 3. Validate all required questions are answered
            // var questions = await _ticketRepository.GetQuestionsAsync(ticket.Id);
            // var answeredQuestionIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
            // var missingQuestions = questions.Where(q => !answeredQuestionIds.Contains(q.Id)).ToList();
            // if (missingQuestions.Any())
            // {
            //     return BadRequest(new { error = "Not all questions have been answered", missingQuestions });
            // }

            // 4. Save answers to database
            // foreach (var answer in request.Answers)
            // {
            //     await _ticketRepository.SaveAnswerAsync(ticket.Id, answer.QuestionId, answer.AnswerText);
            // }

            // 5. Resume workflow with answers
            // await _agentService.ResumeWorkflowWithAnswersAsync(ticket.Id, request.Answers);

            // Mock response for now
            var response = new TicketStatusResponse
            {
                TicketId = id,
                CurrentState = "planning",
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for ticket {TicketId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answers for ticket {TicketId}", id);
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
