using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service implementation for plan review management operations
/// </summary>
public class PlanReviewService : IPlanReviewService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPlanReviewRepository _planReviewRepository;
    private readonly IReviewCommentRepository _reviewCommentRepository;
    private readonly IInlineCommentAnchorRepository _anchorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChecklistTemplateService _checklistTemplateService;
    private readonly ILogger<PlanReviewService> _logger;

    public PlanReviewService(
        ITicketRepository ticketRepository,
        IPlanReviewRepository planReviewRepository,
        IReviewCommentRepository reviewCommentRepository,
        IInlineCommentAnchorRepository anchorRepository,
        IUserRepository userRepository,
        IChecklistTemplateService checklistTemplateService,
        ILogger<PlanReviewService> logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _planReviewRepository = planReviewRepository ?? throw new ArgumentNullException(nameof(planReviewRepository));
        _reviewCommentRepository = reviewCommentRepository ?? throw new ArgumentNullException(nameof(reviewCommentRepository));
        _anchorRepository = anchorRepository ?? throw new ArgumentNullException(nameof(anchorRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _checklistTemplateService = checklistTemplateService ?? throw new ArgumentNullException(nameof(checklistTemplateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AssignReviewersAsync(
        Guid ticketId,
        List<Guid> requiredReviewerIds,
        List<Guid>? optionalReviewerIds = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket with ID {ticketId} not found.");
        }

        // Validate that all reviewer IDs exist
        var allReviewerIds = requiredReviewerIds.Concat(optionalReviewerIds ?? new List<Guid>()).ToList();
        var users = await _userRepository.GetByIdsAsync(allReviewerIds, cancellationToken);
        if (users.Count != allReviewerIds.Count)
        {
            throw new InvalidOperationException("One or more reviewer IDs are invalid.");
        }

        // Use the domain method to assign reviewers
        ticket.AssignReviewers(requiredReviewerIds, optionalReviewerIds);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        _logger.LogInformation("Assigned {RequiredCount} required and {OptionalCount} optional reviewers to ticket {TicketId}",
            requiredReviewerIds.Count, optionalReviewerIds?.Count ?? 0, ticketId);
    }

    public async Task<List<PlanReview>> GetReviewsByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _planReviewRepository.GetByTicketIdAsync(ticketId, cancellationToken);
    }

    public async Task<List<PlanReview>> GetPendingReviewsForReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _planReviewRepository.GetPendingByReviewerIdAsync(reviewerId, cancellationToken);
    }

    public async Task ApproveReviewAsync(Guid reviewId, string? decision = null, CancellationToken cancellationToken = default)
    {
        var review = await _planReviewRepository.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
        {
            throw new InvalidOperationException($"Review with ID {reviewId} not found.");
        }

        review.Approve(decision);
        await _planReviewRepository.UpdateAsync(review, cancellationToken);

        _logger.LogInformation("Review {ReviewId} approved by reviewer {ReviewerId}", reviewId, review.ReviewerId);

        // Check if ticket now has sufficient approvals
        var ticket = await _ticketRepository.GetByIdAsync(review.TicketId, cancellationToken);
        if (ticket != null && ticket.HasSufficientApprovals())
        {
            _logger.LogInformation("Ticket {TicketId} now has sufficient approvals", ticket.Id);
            // Note: The actual plan approval and workflow transition should be triggered by the UI or a background job
        }
    }

    public async Task RejectReviewAsync(Guid reviewId, string reason, bool regenerateCompletely, CancellationToken cancellationToken = default)
    {
        var review = await _planReviewRepository.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
        {
            throw new InvalidOperationException($"Review with ID {reviewId} not found.");
        }

        review.Reject(reason, regenerateCompletely);
        await _planReviewRepository.UpdateAsync(review, cancellationToken);

        _logger.LogInformation("Review {ReviewId} rejected by reviewer {ReviewerId}, regenerate: {Regenerate}",
            reviewId, review.ReviewerId, regenerateCompletely);
    }

    public async Task<ReviewComment> AddCommentAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        List<Guid>? mentionedUserIds = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket with ID {ticketId} not found.");
        }

        var author = await _userRepository.GetByIdAsync(authorId, cancellationToken);
        if (author == null)
        {
            throw new InvalidOperationException($"Author with ID {authorId} not found.");
        }

        var comment = new ReviewComment(ticketId, authorId, content, mentionedUserIds);
        await _reviewCommentRepository.CreateAsync(comment, cancellationToken);

        _logger.LogInformation("Added comment {CommentId} to ticket {TicketId} by author {AuthorId}",
            comment.Id, ticketId, authorId);

        return comment;
    }

    public async Task<List<ReviewComment>> GetCommentsByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _reviewCommentRepository.GetByTicketIdAsync(ticketId, cancellationToken);
    }

    public async Task UpdateCommentAsync(Guid commentId, string content, List<Guid>? mentionedUserIds = null, CancellationToken cancellationToken = default)
    {
        var comment = await _reviewCommentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            throw new InvalidOperationException($"Comment with ID {commentId} not found.");
        }

        comment.Update(content, mentionedUserIds);
        await _reviewCommentRepository.UpdateAsync(comment, cancellationToken);

        _logger.LogInformation("Updated comment {CommentId}", commentId);
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await _reviewCommentRepository.DeleteAsync(commentId, cancellationToken);
        _logger.LogInformation("Deleted comment {CommentId}", commentId);
    }

    public async Task<bool> HasSufficientApprovalsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket with ID {ticketId} not found.");
        }

        return ticket.HasSufficientApprovals();
    }

    public async Task ResetReviewsForNewPlanAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket with ID {ticketId} not found.");
        }

        ticket.ResetReviewsForNewPlan();
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        _logger.LogInformation("Reset all reviews for ticket {TicketId} due to new plan", ticketId);
    }

    public async Task<ReviewComment> AddCommentWithAnchorAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        int startLine,
        int endLine,
        string textSnippet,
        List<Guid>? mentionedUserIds = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket with ID {ticketId} not found.");
        }

        var author = await _userRepository.GetByIdAsync(authorId, cancellationToken);
        if (author == null)
        {
            throw new InvalidOperationException($"Author with ID {authorId} not found.");
        }

        // Create the comment first
        var comment = new ReviewComment(ticketId, authorId, content, mentionedUserIds);
        await _reviewCommentRepository.CreateAsync(comment, cancellationToken);

        // Create the inline anchor
        var anchor = InlineCommentAnchor.Create(
            comment.Id,
            startLine,
            endLine,
            textSnippet);

        await _anchorRepository.CreateAsync(anchor, cancellationToken);

        _logger.LogInformation("Added comment {CommentId} with anchor to ticket {TicketId} at lines {StartLine}-{EndLine}",
            comment.Id, ticketId, startLine, endLine);

        return comment;
    }

    public async Task<List<InlineCommentAnchor>> GetInlineCommentAnchorsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _anchorRepository.GetByTicketIdAsync(ticketId, cancellationToken);
    }

    public async Task<bool> ValidateChecklistAsync(Guid planReviewId, CancellationToken cancellationToken = default)
    {
        var review = await _planReviewRepository.GetByIdWithChecklistAsync(planReviewId, cancellationToken);
        if (review?.Checklist == null)
        {
            // No checklist = no validation required
            return true;
        }

        return review.Checklist.AllRequiredItemsChecked();
    }

    public async Task ApproveWithChecklistValidationAsync(
        Guid planReviewId,
        string? decision = null,
        CancellationToken cancellationToken = default)
    {
        // Validate checklist first
        var isValid = await ValidateChecklistAsync(planReviewId, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException(
                "Cannot approve: all required checklist items must be checked.");
        }

        // Proceed with normal approval
        await ApproveReviewAsync(planReviewId, decision, cancellationToken);
    }
}
