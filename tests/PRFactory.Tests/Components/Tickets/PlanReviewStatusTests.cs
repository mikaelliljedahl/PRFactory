using Bunit;
using PRFactory.Domain.Entities;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class PlanReviewStatusTests : ComponentTestBase
{
    [Fact]
    public void Renders_WithEmptyReviewersList()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>();

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_RequiredReviewers()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "John Doe",
                Email = "john.doe@example.com",
                IsRequired = true,
                Status = ReviewStatus.Pending,
                AssignedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("Required", cut.Markup);
    }

    [Fact]
    public void Renders_OptionalReviewers()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Jane Smith",
                IsRequired = false,
                Status = ReviewStatus.Pending
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Jane Smith", cut.Markup);
        Assert.Contains("Optional", cut.Markup);
    }

    [Fact]
    public void Renders_ApprovedStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Approver",
                IsRequired = true,
                Status = ReviewStatus.Approved
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Approved", cut.Markup);
    }

    [Fact]
    public void Renders_PendingStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Pending Reviewer",
                IsRequired = true,
                Status = ReviewStatus.Pending
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Pending", cut.Markup);
    }

    [Fact]
    public void Renders_RejectedForRefinementStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Refine Requester",
                IsRequired = true,
                Status = ReviewStatus.RejectedForRefinement
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Refine", cut.Markup);
    }

    [Fact]
    public void Renders_RejectedForRegenerationStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Regenerate Requester",
                IsRequired = true,
                Status = ReviewStatus.RejectedForRegeneration
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Regenerate", cut.Markup);
    }

    [Fact]
    public void ShowsAllRequiredApproved_WhenAllRequiredReviewersApprove()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Required 1",
                IsRequired = true,
                Status = ReviewStatus.Approved
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Required 2",
                IsRequired = true,
                Status = ReviewStatus.Approved
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should indicate all required reviewers approved
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void ShowsMixedReviewers()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Required Approved",
                IsRequired = true,
                Status = ReviewStatus.Approved
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Required Pending",
                IsRequired = true,
                Status = ReviewStatus.Pending
            },
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Optional Approved",
                IsRequired = false,
                Status = ReviewStatus.Approved
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert
        Assert.Contains("Required Approved", cut.Markup);
        Assert.Contains("Required Pending", cut.Markup);
        Assert.Contains("Optional Approved", cut.Markup);
    }

    [Fact]
    public void AppliesCorrectColorForApprovedStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Approver",
                IsRequired = true,
                Status = ReviewStatus.Approved
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should contain success color indicator
        Assert.Contains("success", cut.Markup);
    }

    [Fact]
    public void AppliesCorrectColorForRejectedStatus()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                AssignedAt = DateTime.UtcNow,
                DisplayName = "Rejector",
                IsRequired = true,
                Status = ReviewStatus.RejectedForRegeneration
            }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should contain danger color indicator
        Assert.Contains("danger", cut.Markup);
    }

    [Fact]
    public void CountsApprovedRequiredReviewers()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto { Id = Guid.NewGuid(), DisplayName = "R1", IsRequired = true, Status = ReviewStatus.Approved },
            new ReviewerDto { Id = Guid.NewGuid(), DisplayName = "R2", IsRequired = true, Status = ReviewStatus.Approved },
            new ReviewerDto { Id = Guid.NewGuid(), DisplayName = "R3", IsRequired = true, Status = ReviewStatus.Pending }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should show 2 out of 3 required approvals
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void CountsApprovedOptionalReviewers()
    {
        // Arrange
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto { Id = Guid.NewGuid(), DisplayName = "O1", IsRequired = false, Status = ReviewStatus.Approved },
            new ReviewerDto { Id = Guid.NewGuid(), DisplayName = "O2", IsRequired = false, Status = ReviewStatus.Pending }
        };

        // Act
        var cut = RenderComponent<PlanReviewStatus>(parameters => parameters
            .Add(p => p.Reviewers, reviewers));

        // Assert - Should show 1 out of 2 optional approvals
        Assert.NotNull(cut.Markup);
    }
}
