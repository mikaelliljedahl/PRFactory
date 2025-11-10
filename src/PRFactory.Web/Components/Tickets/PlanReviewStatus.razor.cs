using Microsoft.AspNetCore.Components;
using PRFactory.Domain.Entities;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tickets;

public partial class PlanReviewStatus
{
    [Parameter, EditorRequired]
    public List<ReviewerDto> Reviewers { get; set; } = new();

    private List<ReviewerDto> GetRequiredReviewers() => Reviewers.Where(r => r.IsRequired).ToList();
    private List<ReviewerDto> GetOptionalReviewers() => Reviewers.Where(r => !r.IsRequired).ToList();

    private int ApprovedRequiredCount => GetRequiredReviewers().Count(r => r.Status == ReviewStatus.Approved);
    private int ApprovedOptionalCount => GetOptionalReviewers().Count(r => r.Status == ReviewStatus.Approved);

    private bool AllRequiredApproved
    {
        get
        {
            var requiredReviewers = GetRequiredReviewers();
            return requiredReviewers.Any() && requiredReviewers.All(r => r.Status == ReviewStatus.Approved);
        }
    }
    private bool HasRejections => Reviewers.Any(r => r.Status == ReviewStatus.RejectedForRefinement || r.Status == ReviewStatus.RejectedForRegeneration);

    private string GetStatusColor(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => "success",
            ReviewStatus.Pending => "secondary",
            ReviewStatus.RejectedForRefinement => "warning",
            ReviewStatus.RejectedForRegeneration => "danger",
            _ => "secondary"
        };
    }

    private static string GetStatusText(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => "Approved",
            ReviewStatus.Pending => "Pending",
            ReviewStatus.RejectedForRefinement => "Rejected (Refine)",
            ReviewStatus.RejectedForRegeneration => "Rejected (Regenerate)",
            _ => "Unknown"
        };
    }

    private static string? GetStatusBadgeColor(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => "success",
            ReviewStatus.RejectedForRefinement => "warning",
            ReviewStatus.RejectedForRegeneration => "danger",
            _ => null
        };
    }

    private static string? GetStatusBadgeText(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => "✓",
            ReviewStatus.RejectedForRefinement => "R",
            ReviewStatus.RejectedForRegeneration => "X",
            _ => null
        };
    }
}
