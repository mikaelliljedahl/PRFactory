using Bunit;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for PlanReviewStatus component
/// </summary>
public class PlanReviewStatusTests : TestContext
{
    [Fact]
    public void PlanReviewStatus_WithNoReviewers_ShowsNoReviewersMessage()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>();

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("No reviewers assigned yet", cut.Markup);
        Assert.Contains("Single-user approval mode is active", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithRequiredReviewers_DisplaysRequiredCount()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Jane Smith",
                Email = "jane@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("0 / 2 Approved", cut.Markup);
        Assert.Contains("Required Reviewers", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithApprovedRequiredReviewer_UpdatesCount()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Jane Smith",
                Email = "jane@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("1 / 2 Approved", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithAllRequiredApproved_ShowsSuccessMessage()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Jane Smith",
                Email = "jane@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("All required reviewers have approved", cut.Markup);
        Assert.Contains("The plan is ready to proceed", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithRejection_ShowsRejectionMessage()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.RejectedForRegeneration,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow,
                Decision = "Needs major changes"
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("The plan has been rejected", cut.Markup);
        Assert.Contains("Please address the feedback and regenerate", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithPendingReviewers_ShowsWaitingMessage()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Jane Smith",
                Email = "jane@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Waiting for 1 more required approval", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithOptionalReviewers_DisplaysOptionalSection()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Optional Reviewer",
                Email = "optional@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = false,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Optional Reviewers", cut.Markup);
        Assert.Contains("0 / 1 Approved", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithReviewedAt_DisplaysTimestamp()
    {
        // Arrange
        var reviewedAt = DateTime.UtcNow.AddHours(-2);
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow.AddDays(-1),
                ReviewedAt = reviewedAt
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Reviewed", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_WithDecision_DisplaysDecisionComment()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow,
                Decision = "Looks good to me!"
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Looks good to me!", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_ShowsCorrectStatusBadgeForApproved()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Approved", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_ShowsCorrectStatusBadgeForPending()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Pending", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_ShowsCorrectStatusBadgeForRejectedRefinement()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.RejectedForRefinement,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Rejected (Refine)", cut.Markup);
    }

    [Fact]
    public void PlanReviewStatus_ShowsCorrectStatusBadgeForRejectedRegeneration()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john@example.com",
                Status = ReviewStatus.RejectedForRegeneration,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Rejected (Regenerate)", cut.Markup);
    }
}
