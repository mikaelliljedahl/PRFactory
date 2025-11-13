namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a comment made during plan review.
/// Enables team discussion and collaboration on implementation plans.
/// </summary>
public class ReviewComment
{
    /// <summary>
    /// Unique identifier for this comment
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket this comment is associated with
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// The user who authored this comment
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// The comment text content (supports markdown)
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// List of user IDs mentioned in this comment (via @mentions)
    /// </summary>
    public List<Guid> MentionedUserIds { get; private set; } = new();

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the comment was last edited (null if never edited)
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Ticket Ticket { get; private set; } = null!;
    public User Author { get; private set; } = null!;

    // EF Core constructor
    private ReviewComment() { }

    /// <summary>
    /// Creates a new review comment
    /// </summary>
    public ReviewComment(Guid ticketId, Guid authorId, string content, List<Guid>? mentionedUserIds = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty", nameof(content));

        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorId = authorId;
        Content = content.Trim();
        MentionedUserIds = mentionedUserIds ?? [];
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the comment content and mentions
    /// </summary>
    public void Update(string content, List<Guid>? mentionedUserIds = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty", nameof(content));

        Content = content.Trim();
        MentionedUserIds = mentionedUserIds ?? [];
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a specific user is mentioned in this comment
    /// </summary>
    public bool MentionsUser(Guid userId)
    {
        return MentionedUserIds.Contains(userId);
    }

    /// <summary>
    /// Adds a mention to a user (if not already mentioned)
    /// </summary>
    public void AddMention(Guid userId)
    {
        if (!MentionedUserIds.Contains(userId))
        {
            MentionedUserIds.Add(userId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a mention of a user
    /// </summary>
    public void RemoveMention(Guid userId)
    {
        if (MentionedUserIds.Remove(userId))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
