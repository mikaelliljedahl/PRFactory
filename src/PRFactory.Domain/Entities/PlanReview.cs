namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an individual reviewer's review of a plan.
/// Each ticket can have multiple PlanReview records (one per assigned reviewer).
/// </summary>
public class PlanReview
{
    /// <summary>
    /// Unique identifier for this review
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket whose plan is being reviewed
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// The user assigned to review the plan
    /// </summary>
    public Guid ReviewerId { get; private set; }

    /// <summary>
    /// Current status of this review
    /// </summary>
    public ReviewStatus Status { get; private set; }

    /// <summary>
    /// Whether this reviewer's approval is required (vs optional)
    /// Only required reviewers count toward the approval threshold
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// When the review was assigned to this reviewer
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// When the reviewer completed their review (approved or rejected)
    /// Null if review is still pending
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// Brief explanation of the reviewer's decision (approval note or rejection reason)
    /// </summary>
    public string? Decision { get; private set; }

    // Navigation properties
    public Ticket Ticket { get; private set; } = null!;
    public User Reviewer { get; private set; } = null!;

    // EF Core constructor
    private PlanReview() { }

    /// <summary>
    /// Creates a new plan review assignment
    /// </summary>
    public PlanReview(Guid ticketId, Guid reviewerId, bool isRequired)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        ReviewerId = reviewerId;
        IsRequired = isRequired;
        Status = ReviewStatus.Pending;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approve the plan
    /// </summary>
    /// <param name="decision">Optional note explaining the approval</param>
    public void Approve(string? decision = null)
    {
        if (Status != ReviewStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a review with status {Status}. Only pending reviews can be approved.");

        Status = ReviewStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        Decision = decision;
    }

    /// <summary>
    /// Reject the plan
    /// </summary>
    /// <param name="reason">Explanation of why the plan was rejected</param>
    /// <param name="regenerateCompletely">If true, requests complete regeneration. If false, requests refinement.</param>
    public void Reject(string reason, bool regenerateCompletely)
    {
        if (Status != ReviewStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a review with status {Status}. Only pending reviews can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required", nameof(reason));

        Status = regenerateCompletely
            ? ReviewStatus.RejectedForRegeneration
            : ReviewStatus.RejectedForRefinement;
        ReviewedAt = DateTime.UtcNow;
        Decision = reason.Trim();
    }

    /// <summary>
    /// Reset this review to pending status for a new plan iteration
    /// Called when the plan is regenerated/refined and reviewers need to review again
    /// </summary>
    public void ResetForNewPlan()
    {
        Status = ReviewStatus.Pending;
        ReviewedAt = null;
        Decision = null;
        AssignedAt = DateTime.UtcNow; // Update assignment time for the new plan
    }

    /// <summary>
    /// Update whether this reviewer is required or optional
    /// </summary>
    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }
}
