using PRFactory.Domain.Entities;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing a reviewer assigned to a plan review
/// </summary>
public class ReviewerDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public ReviewStatus Status { get; set; }
    public bool IsRequired { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Decision { get; set; }

    /// <summary>
    /// Maps a PlanReview entity to a ReviewerDto
    /// </summary>
    public static ReviewerDto FromEntity(PlanReview review)
    {
        return new ReviewerDto
        {
            Id = review.Reviewer.Id,
            DisplayName = review.Reviewer.DisplayName,
            Email = review.Reviewer.Email,
            AvatarUrl = review.Reviewer.AvatarUrl,
            Status = review.Status,
            IsRequired = review.IsRequired,
            AssignedAt = review.AssignedAt,
            ReviewedAt = review.ReviewedAt,
            Decision = review.Decision
        };
    }
}
