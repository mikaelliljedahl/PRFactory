using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of ticket service.
/// Uses direct application service injection (Blazor Server architecture).
/// This is a facade service that converts between domain entities and DTOs.
/// </summary>
public class TicketService(
    ILogger<TicketService> logger,
    ITicketApplicationService ticketApplicationService,
    ITicketUpdateService ticketUpdateService,
    IQuestionApplicationService questionApplicationService,
    IWorkflowEventApplicationService workflowEventApplicationService,
    IPlanService planService,
    ITenantContext tenantContext,
    ITicketRepository ticketRepository,
    IPlanReviewService planReviewService,
    ICurrentUserService currentUserService) : ITicketService
{
    private const string CheckCircleIcon = "check-circle";

    public async Task<List<Ticket>> GetAllTicketsAsync(CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            return await ticketApplicationService.GetAllTicketsAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching all tickets");
            throw;
        }
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            return await ticketApplicationService.GetTicketByIdAsync(ticketId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<TicketDto?> GetTicketDtoByIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service to get entity
            var ticket = await ticketApplicationService.GetTicketByIdAsync(ticketId, ct);

            if (ticket == null)
            {
                return null;
            }

            // Map to DTO
            return new TicketDto
            {
                Id = ticket.Id,
                TicketKey = ticket.TicketKey,
                Title = ticket.Title,
                Description = ticket.Description,
                State = ticket.State,
                Source = ticket.Source,
                RepositoryId = ticket.RepositoryId,
                RepositoryName = ticket.Repository?.Name,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                CompletedAt = ticket.CompletedAt,
                PullRequestUrl = ticket.PullRequestUrl,
                PullRequestNumber = ticket.PullRequestNumber,
                PlanBranchName = ticket.PlanBranchName,
                PlanMarkdownPath = ticket.PlanMarkdownPath,
                LastError = ticket.LastError
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching ticket DTO for {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            return await ticketApplicationService.GetTicketsByRepositoryAsync(repositoryId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching tickets for repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task TriggerWorkflowAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketApplicationService.TriggerWorkflowAsync(ticketId, ct);
            logger.LogInformation("Triggered workflow for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error triggering workflow for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketApplicationService.ApprovePlanAsync(ticketId, comments, ct);
            logger.LogInformation("Approved plan for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task RejectPlanAsync(Guid ticketId, string rejectionReason, bool regenerateCompletely = false, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketApplicationService.RejectPlanAsync(ticketId, rejectionReason, regenerateCompletely, ct);
            var action = regenerateCompletely ? "Rejected and regenerating" : "Rejected";
            logger.LogInformation("{Action} plan for ticket {TicketId}", action, ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rejecting plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task RefinePlanAsync(Guid ticketId, string refinementInstructions, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketApplicationService.RefinePlanAsync(ticketId, refinementInstructions, ct);
            logger.LogInformation("Refining plan for ticket {TicketId} with instructions", ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refining plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketApplicationService.SubmitAnswersAsync(ticketId, answers, ct);
            logger.LogInformation("Submitted answers for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting answers for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<TicketUpdateDto?> GetLatestTicketUpdateAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            var ticketUpdate = await ticketUpdateService.GetLatestTicketUpdateAsync(ticketId, ct);
            if (ticketUpdate == null)
            {
                logger.LogWarning("No ticket update found for ticket {TicketId}", ticketId);
                return null;
            }

            // Map entity to DTO
            return MapToDto(ticketUpdate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching ticket update for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task UpdateTicketUpdateAsync(Guid ticketUpdateId, TicketUpdateDto ticketUpdate, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketUpdateService.UpdateTicketUpdateAsync(
                ticketUpdateId,
                ticketUpdate.UpdatedTitle,
                ticketUpdate.UpdatedDescription,
                ticketUpdate.AcceptanceCriteria,
                ct);

            logger.LogInformation("Updated ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    public async Task ApproveTicketUpdateAsync(Guid ticketUpdateId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketUpdateService.ApproveTicketUpdateAsync(ticketUpdateId, approvedBy: null, ct);
            logger.LogInformation("Approved ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    public async Task RejectTicketUpdateAsync(Guid ticketUpdateId, string rejectionReason, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await ticketUpdateService.RejectTicketUpdateAsync(
                ticketUpdateId,
                rejectionReason,
                rejectedBy: null,
                regenerate: true,
                ct);

            logger.LogInformation("Rejected ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rejecting ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            var questionsWithAnswers = await questionApplicationService.GetQuestionsWithAnswersAsync(ticketId, ct);

            // Map to DTOs
            return questionsWithAnswers.Select(qa => new QuestionDto
            {
                Id = qa.Question.Id,
                Text = qa.Question.Text,
                Category = qa.Question.Category,
                CreatedAt = qa.Question.CreatedAt,
                IsAnswered = qa.IsAnswered,
                AnswerText = qa.Answer?.Text,
                AnsweredAt = qa.Answer?.AnsweredAt
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching questions for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<List<WorkflowEventDto>> GetEventsAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            var events = await workflowEventApplicationService.GetEventsAsync(ticketId, ct);

            // Map to DTOs
            return events.Select(MapEventToDto).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching events for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<PlanDto?> GetPlanAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            var planInfo = await planService.GetPlanAsync(ticketId, ct);

            if (planInfo == null)
            {
                return null;
            }

            // Map to DTO
            return new PlanDto
            {
                BranchName = planInfo.BranchName,
                MarkdownPath = planInfo.MarkdownPath,
                Content = planInfo.Content,
                CreatedAt = planInfo.CreatedAt,
                IsApproved = planInfo.IsApproved,
                ApprovedAt = planInfo.ApprovedAt
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<Ticket> CreateTicketAsync(string ticketKey, string title, string description, Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            // Get current tenant ID
            var tenantId = await tenantContext.GetCurrentTenantIdAsync(ct);

            // Create ticket using domain factory method
            var ticket = Ticket.Create(
                ticketKey: ticketKey,
                tenantId: tenantId,
                repositoryId: repositoryId,
                ticketSystem: "Jira",
                source: TicketSource.WebUI);

            // Set ticket info
            ticket.UpdateTicketInfo(title, description);

            // Save to repository
            var savedTicket = await ticketRepository.AddAsync(ticket, ct);

            logger.LogInformation("Created ticket {TicketKey} with ID {TicketId}", ticketKey, savedTicket.Id);

            return savedTicket;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ticket {TicketKey}", ticketKey);
            throw;
        }
    }

    /// <summary>
    /// Maps a TicketUpdate entity to a TicketUpdateDto
    /// </summary>
    private TicketUpdateDto MapToDto(TicketUpdate ticketUpdate)
    {
        return new TicketUpdateDto
        {
            Id = ticketUpdate.Id,
            TicketId = ticketUpdate.TicketId,
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
                Category = sc.Category,
                Description = sc.Description,
                Priority = sc.Priority,
                IsTestable = sc.IsTestable
            }).ToList()
        };
    }

    /// <summary>
    /// Maps a WorkflowEvent entity to a WorkflowEventDto
    /// </summary>
    private WorkflowEventDto MapEventToDto(WorkflowEvent evt)
    {
        var dto = new WorkflowEventDto
        {
            Id = evt.Id,
            TicketId = evt.TicketId,
            OccurredAt = evt.OccurredAt,
            EventType = evt.EventType
        };

        // Map specific event types to friendly descriptions and icons
        switch (evt)
        {
            case WorkflowStateChanged stateChanged:
                dto.FromState = stateChanged.From;
                dto.ToState = stateChanged.To;
                dto.Reason = stateChanged.Reason;
                dto.Description = $"Changed from {stateChanged.From} to {stateChanged.To}";
                dto.Icon = GetIconForState(stateChanged.To);
                dto.Severity = GetSeverityForState(stateChanged.To);
                break;

            case QuestionAdded questionAdded:
                dto.Description = $"Question added: {questionAdded.Question.Text}";
                dto.Icon = "question-circle";
                dto.Severity = EventSeverity.Info;
                break;

            case AnswerAdded answerAdded:
                dto.Description = "Answer provided";
                dto.Icon = CheckCircleIcon;
                dto.Severity = EventSeverity.Success;
                break;

            case PlanCreated planCreated:
                dto.Description = $"Implementation plan created in branch {planCreated.BranchName}";
                dto.Icon = "file-text";
                dto.Severity = EventSeverity.Info;
                break;

            case PullRequestCreated prCreated:
                dto.Description = $"Pull request #{prCreated.PullRequestNumber} created";
                dto.Icon = "git-pull-request";
                dto.Severity = EventSeverity.Success;
                break;

            default:
                dto.Description = evt.EventType;
                dto.Icon = "circle";
                dto.Severity = EventSeverity.Info;
                break;
        }

        return dto;
    }

    /// <summary>
    /// Gets the icon name for a workflow state
    /// </summary>
    private string GetIconForState(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Triggered => "play-circle",
            WorkflowState.Analyzing => "search",
            WorkflowState.TicketUpdateGenerated => "file-text",
            WorkflowState.TicketUpdateUnderReview => "eye",
            WorkflowState.TicketUpdateApproved => CheckCircleIcon,
            WorkflowState.TicketUpdateRejected => "x-circle",
            WorkflowState.QuestionsPosted => "help-circle",
            WorkflowState.AwaitingAnswers => "clock",
            WorkflowState.AnswersReceived => "message-circle",
            WorkflowState.Planning => "clipboard",
            WorkflowState.PlanPosted => "file-text",
            WorkflowState.PlanUnderReview => "eye",
            WorkflowState.PlanApproved => CheckCircleIcon,
            WorkflowState.PlanRejected => "x-circle",
            WorkflowState.Implementing => "code",
            WorkflowState.PRCreated => "git-pull-request",
            WorkflowState.InReview => "eye",
            WorkflowState.Completed => CheckCircleIcon,
            WorkflowState.Cancelled => "x",
            WorkflowState.Failed => "alert-circle",
            _ => "circle"
        };
    }

    /// <summary>
    /// Gets the severity for a workflow state
    /// </summary>
    private EventSeverity GetSeverityForState(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Completed => EventSeverity.Success,
            WorkflowState.PlanApproved => EventSeverity.Success,
            WorkflowState.TicketUpdateApproved => EventSeverity.Success,
            WorkflowState.PRCreated => EventSeverity.Success,
            WorkflowState.Failed => EventSeverity.Error,
            WorkflowState.ImplementationFailed => EventSeverity.Error,
            WorkflowState.PlanRejected => EventSeverity.Warning,
            WorkflowState.TicketUpdateRejected => EventSeverity.Warning,
            WorkflowState.Cancelled => EventSeverity.Warning,
            WorkflowState.AwaitingAnswers => EventSeverity.Warning,
            _ => EventSeverity.Info
        };
    }

    // Team Review methods

    public async Task<List<ReviewerDto>> GetReviewersAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            var reviews = await planReviewService.GetReviewsByTicketIdAsync(ticketId, ct);
            return reviews.Select(ReviewerDto.FromEntity).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching reviewers for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task AssignReviewersAsync(Guid ticketId, List<Guid> requiredReviewerIds, List<Guid>? optionalReviewerIds = null, CancellationToken ct = default)
    {
        try
        {
            await planReviewService.AssignReviewersAsync(ticketId, requiredReviewerIds, optionalReviewerIds, ct);
            logger.LogInformation("Assigned reviewers to ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning reviewers to ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task ApproveReviewAsync(Guid ticketId, Guid reviewerId, string? decision = null, CancellationToken ct = default)
    {
        try
        {
            // Find the review for this ticket and reviewer
            var reviews = await planReviewService.GetReviewsByTicketIdAsync(ticketId, ct);
            var review = reviews.FirstOrDefault(r => r.ReviewerId == reviewerId);

            if (review == null)
            {
                throw new InvalidOperationException($"No review found for ticket {ticketId} and reviewer {reviewerId}");
            }

            await planReviewService.ApproveReviewAsync(review.Id, decision, ct);
            logger.LogInformation("Approved review for ticket {TicketId} by reviewer {ReviewerId}", ticketId, reviewerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving review for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task RejectReviewAsync(Guid ticketId, Guid reviewerId, string reason, bool regenerateCompletely, CancellationToken ct = default)
    {
        try
        {
            // Find the review for this ticket and reviewer
            var reviews = await planReviewService.GetReviewsByTicketIdAsync(ticketId, ct);
            var review = reviews.FirstOrDefault(r => r.ReviewerId == reviewerId);

            if (review == null)
            {
                throw new InvalidOperationException($"No review found for ticket {ticketId} and reviewer {reviewerId}");
            }

            await planReviewService.RejectReviewAsync(review.Id, reason, regenerateCompletely, ct);
            logger.LogInformation("Rejected review for ticket {TicketId} by reviewer {ReviewerId}", ticketId, reviewerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rejecting review for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<List<ReviewCommentDto>> GetCommentsAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            var comments = await planReviewService.GetCommentsByTicketIdAsync(ticketId, ct);
            return comments.Select(ReviewCommentDto.FromEntity).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching comments for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<ReviewCommentDto> AddCommentAsync(Guid ticketId, string content, List<Guid>? mentionedUserIds = null, CancellationToken ct = default)
    {
        try
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync(ct);
            if (!currentUserId.HasValue)
            {
                throw new InvalidOperationException("No authenticated user found");
            }

            var comment = await planReviewService.AddCommentAsync(ticketId, currentUserId.Value, content, mentionedUserIds, ct);
            logger.LogInformation("Added comment to ticket {TicketId} by user {UserId}", ticketId, currentUserId.Value);

            return ReviewCommentDto.FromEntity(comment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding comment to ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<bool> HasSufficientApprovalsAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            return await planReviewService.HasSufficientApprovalsAsync(ticketId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking approvals for ticket {TicketId}", ticketId);
            throw;
        }
    }
}
