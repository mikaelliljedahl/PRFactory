using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a generated update to a ticket with refined title, description, success criteria, and acceptance criteria.
/// This entity stores the AI-generated refinements that will be posted back to the ticket system after approval.
/// </summary>
public class TicketUpdate
{
    /// <summary>
    /// Unique identifier for the ticket update
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket this update belongs to
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// Refined/updated title for the ticket
    /// </summary>
    public string UpdatedTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Refined/updated description for the ticket
    /// </summary>
    public string UpdatedDescription { get; private set; } = string.Empty;

    /// <summary>
    /// List of success criteria that define what needs to be achieved
    /// </summary>
    public List<SuccessCriterion> SuccessCriteria { get; private set; } = new();

    /// <summary>
    /// Acceptance criteria in a structured format (markdown or bullet points)
    /// </summary>
    public string AcceptanceCriteria { get; private set; } = string.Empty;

    /// <summary>
    /// Version number for tracking regenerations (starts at 1)
    /// </summary>
    public int Version { get; private set; } = 1;

    /// <summary>
    /// Indicates whether this is a draft (not yet approved)
    /// </summary>
    public bool IsDraft { get; private set; } = true;

    /// <summary>
    /// Indicates whether this update has been approved by the user
    /// </summary>
    public bool IsApproved { get; private set; } = false;

    /// <summary>
    /// Reason for rejection if the update was rejected (null if not rejected)
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// When the update was generated
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// When the update was approved (null if not yet approved)
    /// </summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// When the update was posted to the ticket system (null if not yet posted)
    /// </summary>
    public DateTime? PostedAt { get; private set; }

    /// <summary>
    /// Navigation property to the parent ticket
    /// </summary>
    public Ticket? Ticket { get; private set; }

    private TicketUpdate() { }

    /// <summary>
    /// Creates a new ticket update
    /// </summary>
    public static TicketUpdate Create(
        Guid ticketId,
        string updatedTitle,
        string updatedDescription,
        List<SuccessCriterion> successCriteria,
        string acceptanceCriteria,
        int version = 1)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        if (string.IsNullOrWhiteSpace(updatedTitle))
            throw new ArgumentException("Updated title cannot be empty", nameof(updatedTitle));

        if (string.IsNullOrWhiteSpace(updatedDescription))
            throw new ArgumentException("Updated description cannot be empty", nameof(updatedDescription));

        if (successCriteria == null || !successCriteria.Any())
            throw new ArgumentException("Success criteria cannot be empty", nameof(successCriteria));

        if (string.IsNullOrWhiteSpace(acceptanceCriteria))
            throw new ArgumentException("Acceptance criteria cannot be empty", nameof(acceptanceCriteria));

        if (version < 1)
            throw new ArgumentException("Version must be at least 1", nameof(version));

        var ticketUpdate = new TicketUpdate
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = updatedTitle,
            UpdatedDescription = updatedDescription,
            SuccessCriteria = successCriteria,
            AcceptanceCriteria = acceptanceCriteria,
            Version = version,
            IsDraft = true,
            IsApproved = false,
            GeneratedAt = DateTime.UtcNow
        };

        return ticketUpdate;
    }

    /// <summary>
    /// Approves this ticket update
    /// </summary>
    public void Approve()
    {
        if (!IsDraft)
            throw new InvalidOperationException("Cannot approve a non-draft ticket update");

        if (IsApproved)
            throw new InvalidOperationException("Ticket update is already approved");

        IsApproved = true;
        IsDraft = false;
        ApprovedAt = DateTime.UtcNow;
        RejectionReason = null; // Clear any previous rejection reason
    }

    /// <summary>
    /// Rejects this ticket update with a reason
    /// </summary>
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason cannot be empty", nameof(reason));

        if (!IsDraft)
            throw new InvalidOperationException("Cannot reject a non-draft ticket update");

        if (IsApproved)
            throw new InvalidOperationException("Cannot reject an approved ticket update");

        RejectionReason = reason;
        // Keep as draft for potential regeneration
    }

    /// <summary>
    /// Marks this update as posted to the ticket system
    /// </summary>
    public void MarkAsPosted()
    {
        if (!IsApproved)
            throw new InvalidOperationException("Cannot post an unapproved ticket update");

        if (PostedAt.HasValue)
            throw new InvalidOperationException("Ticket update has already been posted");

        PostedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the ticket update content (used for regeneration after rejection)
    /// </summary>
    public void Update(
        string updatedTitle,
        string updatedDescription,
        List<SuccessCriterion> successCriteria,
        string acceptanceCriteria)
    {
        if (string.IsNullOrWhiteSpace(updatedTitle))
            throw new ArgumentException("Updated title cannot be empty", nameof(updatedTitle));

        if (string.IsNullOrWhiteSpace(updatedDescription))
            throw new ArgumentException("Updated description cannot be empty", nameof(updatedDescription));

        if (successCriteria == null || !successCriteria.Any())
            throw new ArgumentException("Success criteria cannot be empty", nameof(successCriteria));

        if (string.IsNullOrWhiteSpace(acceptanceCriteria))
            throw new ArgumentException("Acceptance criteria cannot be empty", nameof(acceptanceCriteria));

        UpdatedTitle = updatedTitle;
        UpdatedDescription = updatedDescription;
        SuccessCriteria = successCriteria;
        AcceptanceCriteria = acceptanceCriteria;
        GeneratedAt = DateTime.UtcNow;

        // Reset approval state when updating
        IsApproved = false;
        IsDraft = true;
        ApprovedAt = null;
        RejectionReason = null;
    }

    /// <summary>
    /// Increments the version number (called when creating a new version after rejection)
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }

    /// <summary>
    /// Gets success criteria by category
    /// </summary>
    public List<SuccessCriterion> GetSuccessCriteriaByCategory(SuccessCriterionCategory category)
    {
        return SuccessCriteria.Where(sc => sc.Category == category).ToList();
    }

    /// <summary>
    /// Gets must-have success criteria (Priority 0)
    /// </summary>
    public List<SuccessCriterion> GetMustHaveCriteria()
    {
        return SuccessCriteria.Where(sc => sc.Priority == 0).ToList();
    }

    /// <summary>
    /// Gets should-have success criteria (Priority 1)
    /// </summary>
    public List<SuccessCriterion> GetShouldHaveCriteria()
    {
        return SuccessCriteria.Where(sc => sc.Priority == 1).ToList();
    }

    /// <summary>
    /// Gets nice-to-have success criteria (Priority 2)
    /// </summary>
    public List<SuccessCriterion> GetNiceToHaveCriteria()
    {
        return SuccessCriteria.Where(sc => sc.Priority == 2).ToList();
    }

    /// <summary>
    /// Gets testable success criteria
    /// </summary>
    public List<SuccessCriterion> GetTestableCriteria()
    {
        return SuccessCriteria.Where(sc => sc.IsTestable).ToList();
    }

    /// <summary>
    /// Checks if the update is ready to be posted (approved and not yet posted)
    /// </summary>
    public bool IsReadyToPost()
    {
        return IsApproved && !PostedAt.HasValue;
    }
}
