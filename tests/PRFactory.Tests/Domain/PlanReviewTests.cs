using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class PlanReviewTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithRequiredReviewer_SetsPropertiesCorrectly()
    {
        // Act
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Assert
        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(_ticketId, review.TicketId);
        Assert.Equal(_reviewerId, review.ReviewerId);
        Assert.True(review.IsRequired);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.True(Math.Abs((review.AssignedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(review.ReviewedAt);
        Assert.Null(review.Decision);
    }

    [Fact]
    public void Constructor_WithOptionalReviewer_SetsPropertiesCorrectly()
    {
        // Act
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: false);

        // Assert
        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(_ticketId, review.TicketId);
        Assert.Equal(_reviewerId, review.ReviewerId);
        Assert.False(review.IsRequired);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.True(Math.Abs((review.AssignedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(review.ReviewedAt);
        Assert.Null(review.Decision);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Act
        var review1 = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        var review2 = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Assert
        Assert.NotEqual(review2.Id, review1.Id);
    }

    [Fact]
    public void Constructor_SetsAssignedAtToCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Assert
        var afterCreation = DateTime.UtcNow;
        Assert.True(review.AssignedAt >= beforeCreation);
        Assert.True(review.AssignedAt <= afterCreation);
    }

    #endregion

    #region Approve Tests

    [Fact]
    public void Approve_WithPendingStatus_TransitionsToApproved()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act
        review.Approve();

        // Assert
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.NotNull(review.ReviewedAt);
        Assert.True(Math.Abs((review.ReviewedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Approve_WithDecision_StoresDecision()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        const string decision = "Looks good to me!";

        // Act
        review.Approve(decision);

        // Assert
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.Equal(decision, review.Decision);
        Assert.NotNull(review.ReviewedAt);
    }

    [Fact]
    public void Approve_WithNullDecision_AllowsNullDecision()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act
        review.Approve(null);

        // Assert
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.Null(review.Decision);
        Assert.NotNull(review.ReviewedAt);
    }

    [Fact]
    public void Approve_WithApprovedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Approve();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => review.Approve());
        Assert.Contains("Cannot approve a review with status Approved", ex.Message);
    }

    [Fact]
    public void Approve_WithRejectedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Reject("Needs improvement", regenerateCompletely: false);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => review.Approve());
        Assert.Contains("Cannot approve a review with status", ex.Message);
    }

    #endregion

    #region Reject Tests

    [Fact]
    public void Reject_ForRefinement_TransitionsToRejectedForRefinement()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        const string reason = "Please add more details to step 3";

        // Act
        review.Reject(reason, regenerateCompletely: false);

        // Assert
        Assert.Equal(ReviewStatus.RejectedForRefinement, review.Status);
        Assert.Equal(reason, review.Decision);
        Assert.NotNull(review.ReviewedAt);
        Assert.True(Math.Abs((review.ReviewedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Reject_ForRegeneration_TransitionsToRejectedForRegeneration()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        const string reason = "Completely wrong approach, please start over";

        // Act
        review.Reject(reason, regenerateCompletely: true);

        // Assert
        Assert.Equal(ReviewStatus.RejectedForRegeneration, review.Status);
        Assert.Equal(reason, review.Decision);
        Assert.NotNull(review.ReviewedAt);
        Assert.True(Math.Abs((review.ReviewedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Reject_TrimsWhitespaceInReason()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        const string reasonWithWhitespace = "  Needs more details  ";

        // Act
        review.Reject(reasonWithWhitespace, regenerateCompletely: false);

        // Assert
        Assert.Equal("Needs more details", review.Decision);
    }

    [Fact]
    public void Reject_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => review.Reject("", regenerateCompletely: false));
        Assert.Contains("Rejection reason is required", ex.Message);
        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void Reject_WithWhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => review.Reject("   ", regenerateCompletely: false));
        Assert.Contains("Rejection reason is required", ex.Message);
        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void Reject_WithNullReason_ThrowsArgumentException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => review.Reject(null!, regenerateCompletely: false));
        Assert.Contains("Rejection reason is required", ex.Message);
        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void Reject_WithApprovedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Approve();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => review.Reject("Changed my mind", regenerateCompletely: false));
        Assert.Contains("Cannot reject a review with status Approved", ex.Message);
    }

    [Fact]
    public void Reject_WithRejectedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Reject("First rejection", regenerateCompletely: false);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => review.Reject("Second rejection", regenerateCompletely: false));
        Assert.Contains("Cannot reject a review with status", ex.Message);
    }

    #endregion

    #region ResetForNewPlan Tests

    [Fact]
    public async Task ResetForNewPlan_AfterApproval_ResetsToPending()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Approve("Looks good");
        var originalAssignedAt = review.AssignedAt;
        await Task.Delay(10); // Ensure time difference

        // Act
        review.ResetForNewPlan();

        // Assert
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ReviewedAt);
        Assert.Null(review.Decision);
        Assert.True(review.AssignedAt > originalAssignedAt);
        Assert.True(Math.Abs((review.AssignedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public async Task ResetForNewPlan_AfterRejection_ResetsToPending()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);
        review.Reject("Needs changes", regenerateCompletely: false);
        var originalAssignedAt = review.AssignedAt;
        await Task.Delay(10); // Ensure time difference

        // Act
        review.ResetForNewPlan();

        // Assert
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ReviewedAt);
        Assert.Null(review.Decision);
        Assert.True(review.AssignedAt > originalAssignedAt);
        Assert.True(Math.Abs((review.AssignedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    #endregion

    #region SetRequired Tests

    [Fact]
    public void SetRequired_ChangesIsRequiredFlag()
    {
        // Arrange
        var review = new PlanReview(_ticketId, _reviewerId, isRequired: true);

        // Act
        review.SetRequired(false);

        // Assert
        Assert.False(review.IsRequired);

        // Act again
        review.SetRequired(true);

        // Assert
        Assert.True(review.IsRequired);
    }

    #endregion
}
