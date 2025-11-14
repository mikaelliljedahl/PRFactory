using PRFactory.Domain.Entities;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing an inline comment anchor in a plan
/// </summary>
public class InlineCommentAnchorDto
{
    public Guid Id { get; set; }
    public Guid ReviewCommentId { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string TextSnippet { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Comment details
    public ReviewCommentDto? ReviewComment { get; set; }

    /// <summary>
    /// Gets the line range as a display string
    /// </summary>
    public string LineRangeDisplay => StartLine == EndLine
        ? StartLine.ToString()
        : $"{StartLine}-{EndLine}";

    /// <summary>
    /// Maps an InlineCommentAnchor entity to a DTO
    /// </summary>
    public static InlineCommentAnchorDto FromEntity(InlineCommentAnchor anchor, bool includeComment = false)
    {
        var dto = new InlineCommentAnchorDto
        {
            Id = anchor.Id,
            ReviewCommentId = anchor.ReviewCommentId,
            StartLine = anchor.StartLine,
            EndLine = anchor.EndLine,
            TextSnippet = anchor.TextSnippet,
            CreatedAt = anchor.CreatedAt
        };

        if (includeComment && anchor.ReviewComment != null)
        {
            dto.ReviewComment = ReviewCommentDto.FromEntity(anchor.ReviewComment);
        }

        return dto;
    }
}
