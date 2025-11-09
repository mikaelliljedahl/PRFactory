using PRFactory.Domain.Entities;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing a review comment in a plan discussion
/// </summary>
public class ReviewCommentDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<Guid> MentionedUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Maps a ReviewComment entity to a ReviewCommentDto
    /// </summary>
    public static ReviewCommentDto FromEntity(ReviewComment comment)
    {
        return new ReviewCommentDto
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.Author.DisplayName,
            AuthorEmail = comment.Author.Email,
            AuthorAvatarUrl = comment.Author.AvatarUrl,
            Content = comment.Content,
            MentionedUserIds = comment.MentionedUserIds,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
