using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Tests.Domain;

/// <summary>
/// Tests for Ticket entity's team review methods (multi-reviewer plan approval workflow)
/// </summary>
public class TicketTeamReviewTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _repositoryId = Guid.NewGuid();
    private const string ValidTicketKey = "PROJ-123";

    #region Helper Methods

    /// <summary>
    /// Creates a ticket in PlanPosted state (ready for reviewer assignment)
    /// </summary>
    private Ticket CreateTicketInPlanPostedState()
    {
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Transition through workflow to PlanPosted state
        ticket.TransitionTo(WorkflowState.Analyzing);
        ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
        ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
        ticket.TransitionTo(WorkflowState.Planning);
        ticket.TransitionTo(WorkflowState.PlanPosted);

        return ticket;
    }

    #endregion

    #region AssignReviewers Tests (8 tests)

    [Fact]
    public void AssignReviewers_WithRequiredReviewersOnly_CreatesReviewsAndSetsCount()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var requiredReviewers = new List<Guid> { reviewer1, reviewer2 };

        // Act
        ticket.AssignReviewers(requiredReviewers);

        // Assert
        Assert.Equal(2, ticket.PlanReviews.Count);
        Assert.All(ticket.PlanReviews, r =>
        {
            Assert.True(r.IsRequired);
            Assert.Equal(ReviewStatus.Pending, r.Status);
        });
        Assert.Equal(2, ticket.RequiredApprovalCount);
        Assert.Equal(WorkflowState.PlanUnderReview, ticket.State);
    }

    [Fact]
    public void AssignReviewers_WithRequiredAndOptionalReviewers_CreatesAllReviews()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var required1 = Guid.NewGuid();
        var required2 = Guid.NewGuid();
        var optional1 = Guid.NewGuid();
        var requiredReviewers = new List<Guid> { required1, required2 };
        var optionalReviewers = new List<Guid> { optional1 };

        // Act
        ticket.AssignReviewers(requiredReviewers, optionalReviewers);

        // Assert
        Assert.Equal(3, ticket.PlanReviews.Count);
        Assert.Equal(2, ticket.PlanReviews.Count(r => r.IsRequired));
        Assert.Equal(1, ticket.PlanReviews.Count(r => !r.IsRequired));
        Assert.Equal(2, ticket.RequiredApprovalCount);
    }

    [Fact]
    public void AssignReviewers_WhenPlanPosted_TransitionsToPlanUnderReview()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var requiredReviewers = new List<Guid> { Guid.NewGuid() };

        // Act
        ticket.AssignReviewers(requiredReviewers);

        // Assert
        Assert.Equal(WorkflowState.PlanUnderReview, ticket.State);
    }

    [Fact]
    public void AssignReviewers_WhenAlreadyPlanUnderReview_DoesNotTransitionAgain()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        ticket.TransitionTo(WorkflowState.PlanUnderReview);
        var requiredReviewers = new List<Guid> { Guid.NewGuid() };
        var initialEventsCount = ticket.Events.Count;

        // Act
        ticket.AssignReviewers(requiredReviewers);

        // Assert
        Assert.Equal(WorkflowState.PlanUnderReview, ticket.State);
        // Should not have added a new state transition event
        Assert.Equal(initialEventsCount, ticket.Events.Count);
    }

    [Fact]
    public void AssignReviewers_WhenInvalidState_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var requiredReviewers = new List<Guid> { Guid.NewGuid() };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ticket.AssignReviewers(requiredReviewers));
        Assert.Contains("Plan must be posted first", ex.Message);
    }

    [Fact]
    public void AssignReviewers_WithEmptyRequiredList_ThrowsArgumentException()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var emptyList = new List<Guid>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ticket.AssignReviewers(emptyList));
        Assert.Contains("At least one required reviewer", ex.Message);
    }

    [Fact]
    public void AssignReviewers_WithNullRequiredList_ThrowsArgumentException()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ticket.AssignReviewers(null!));
        Assert.Contains("At least one required reviewer", ex.Message);
    }

    [Fact]
    public void AssignReviewers_WhenReassigning_ClearsExistingReviews()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var initialReviewers = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        ticket.AssignReviewers(initialReviewers);

        // Approve one review to verify it gets cleared
        ticket.PlanReviews[0].Approve("Looks good");

        // Act - Reassign with new reviewers
        var newReviewers = new List<Guid> { Guid.NewGuid() };
        ticket.AssignReviewers(newReviewers);

        // Assert
        var single = Assert.Single(ticket.PlanReviews);
        Assert.Equal(ReviewStatus.Pending, single.Status);
        Assert.Equal(1, ticket.RequiredApprovalCount);
    }

    #endregion

    #region HasSufficientApprovals Tests (7 tests)

    [Fact]
    public void HasSufficientApprovals_WithNoReviewers_ReturnsTrue()
    {
        // Arrange - Ticket without reviewers (backward compatibility)
        var ticket = CreateTicketInPlanPostedState();

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.True(result, "backward compatibility requires no reviewers = auto-approve");
    }

    [Fact]
    public void HasSufficientApprovals_WithSingleRequiredReviewerApproved_ReturnsTrue()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Approve the review
        ticket.PlanReviews[0].Approve("LGTM");

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSufficientApprovals_WithSingleRequiredReviewerPending_ReturnsFalse()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.False(result, "review is still pending");
    }

    [Fact]
    public void HasSufficientApprovals_WithMultipleRequired_AllApproved_ReturnsTrue()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var reviewer3 = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer1, reviewer2, reviewer3 });

        // Approve all reviews
        foreach (var review in ticket.PlanReviews)
        {
            review.Approve("Approved");
        }

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSufficientApprovals_WithMultipleRequired_PartiallyApproved_ReturnsFalse()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var reviewer3 = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer1, reviewer2, reviewer3 });

        // Approve only 2 out of 3
        ticket.PlanReviews[0].Approve("Approved");
        ticket.PlanReviews[1].Approve("Approved");
        // reviewer3 still pending

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.False(result, "only 2 of 3 required approvals received");
    }

    [Fact]
    public void HasSufficientApprovals_WithOptionalReviewerApproved_DoesNotCount()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var required = Guid.NewGuid();
        var optional = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { required }, new List<Guid> { optional });

        // Only optional reviewer approves
        var optionalReview = ticket.PlanReviews.First(r => !r.IsRequired);
        optionalReview.Approve("Optional approval");

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.False(result, "optional approvals do not count toward threshold");
    }

    [Fact]
    public void HasSufficientApprovals_WithMixedReviewers_OnlyCountsRequired()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var required1 = Guid.NewGuid();
        var required2 = Guid.NewGuid();
        var optional1 = Guid.NewGuid();
        var optional2 = Guid.NewGuid();
        ticket.AssignReviewers(
            new List<Guid> { required1, required2 },
            new List<Guid> { optional1, optional2 });

        // Required reviewers approve
        ticket.PlanReviews.Where(r => r.IsRequired).ToList().ForEach(r => r.Approve("Approved"));

        // Optional reviewers do NOT approve (should not matter)

        // Act
        var result = ticket.HasSufficientApprovals();

        // Assert
        Assert.True(result, "both required reviewers approved, optional reviewers do not affect outcome");
    }

    #endregion

    #region HasRejections Tests (3 tests)

    [Fact]
    public void HasRejections_WithNoRejections_ReturnsFalse()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Review is pending (not rejected)

        // Act
        var result = ticket.HasRejections();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRejections_WithRejectedForRefinement_ReturnsTrue()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Reject for refinement
        ticket.PlanReviews[0].Reject("Needs minor changes", regenerateCompletely: false);

        // Act
        var result = ticket.HasRejections();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRejections_WithRejectedForRegeneration_ReturnsTrue()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Reject for regeneration
        ticket.PlanReviews[0].Reject("Completely wrong approach", regenerateCompletely: true);

        // Act
        var result = ticket.HasRejections();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetRejectionDetails Tests (4 tests)

    [Fact]
    public void GetRejectionDetails_WithNoRejections_ReturnsNull()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        // Act
        var result = ticket.GetRejectionDetails();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRejectionDetails_WithRejectedForRefinement_ReturnsCorrectDetails()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        const string reason = "Please add error handling";
        ticket.PlanReviews[0].Reject(reason, regenerateCompletely: false);

        // Act
        var result = ticket.GetRejectionDetails();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reason, result!.Value.Reason);
        Assert.False(result.Value.RegenerateCompletely);
    }

    [Fact]
    public void GetRejectionDetails_WithRejectedForRegeneration_ReturnsCorrectDetails()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });

        const string reason = "Wrong architecture entirely";
        ticket.PlanReviews[0].Reject(reason, regenerateCompletely: true);

        // Act
        var result = ticket.GetRejectionDetails();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reason, result!.Value.Reason);
        Assert.True(result.Value.RegenerateCompletely);
    }

    [Fact]
    public void GetRejectionDetails_WithMultipleRejections_ReturnsFirstRejection()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer1, reviewer2 });

        const string firstReason = "First rejection reason";
        const string secondReason = "Second rejection reason";

        ticket.PlanReviews[0].Reject(firstReason, regenerateCompletely: false);
        ticket.PlanReviews[1].Reject(secondReason, regenerateCompletely: true);

        // Act
        var result = ticket.GetRejectionDetails();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(firstReason, result!.Value.Reason);
    }

    #endregion

    #region ResetReviewsForNewPlan Tests (2 tests)

    [Fact]
    public void ResetReviewsForNewPlan_ResetsAllReviewsToPending()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var reviewer3 = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer1, reviewer2, reviewer3 });

        // Set various review statuses
        ticket.PlanReviews[0].Approve("Approved");
        ticket.PlanReviews[1].Reject("Needs changes", regenerateCompletely: false);
        // reviewer3 still pending

        // Act
        ticket.ResetReviewsForNewPlan();

        // Assert - all reviews should be reset to pending
        Assert.All(ticket.PlanReviews, r =>
        {
            Assert.Equal(ReviewStatus.Pending, r.Status);
            Assert.Null(r.ReviewedAt);
            Assert.Null(r.Decision);
        });
    }

    [Fact]
    public void ResetReviewsForNewPlan_ClearsPlanApprovedAt()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer });
        ticket.PlanReviews[0].Approve("Approved");
        ticket.ApprovePlan(); // Sets PlanApprovedAt

        // Sanity check: plan was approved
        Assert.NotNull(ticket.PlanApprovedAt);

        // Act
        ticket.ResetReviewsForNewPlan();

        // Assert - plan approval should be cleared when resetting for new plan
        Assert.Null(ticket.PlanApprovedAt);
    }

    #endregion

    #region ApprovePlan Test (1 test)

    [Fact]
    public void ApprovePlan_WithInsufficientApprovals_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = CreateTicketInPlanPostedState();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var reviewer3 = Guid.NewGuid();
        ticket.AssignReviewers(new List<Guid> { reviewer1, reviewer2, reviewer3 });

        // Only 2 out of 3 approve
        ticket.PlanReviews[0].Approve("Approved");
        ticket.PlanReviews[1].Approve("Approved");
        // reviewer3 still pending

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ticket.ApprovePlan());
        Assert.Contains("Insufficient approvals", ex.Message);
        Assert.Contains("Required: 3", ex.Message);
        Assert.Contains("Received: 2", ex.Message);
    }

    #endregion
}
