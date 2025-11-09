using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.UnitTests.Domain;

/// <summary>
/// Tests for the Ticket entity, focusing on state transitions and business logic.
/// </summary>
public class TicketTests
{
    [Fact]
    public void Create_ValidParameters_CreatesTicketWithTriggeredState()
    {
        // Arrange
        var ticketKey = "TEST-123";
        var tenantId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        // Act
        var ticket = Ticket.Create(ticketKey, tenantId, repositoryId);

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal(ticketKey, ticket.TicketKey);
        Assert.Equal(tenantId, ticket.TenantId);
        Assert.Equal(repositoryId, ticket.RepositoryId);
        Assert.Equal(WorkflowState.Triggered, ticket.State);
        Assert.Equal("Jira", ticket.TicketSystem);
        Assert.Equal(TicketSource.WebUI, ticket.Source);
    }

    [Fact]
    public void Create_EmptyTicketKey_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Ticket.Create("", tenantId, repositoryId));
        Assert.Throws<ArgumentException>(() => Ticket.Create("   ", tenantId, repositoryId));
    }

    [Fact]
    public void Create_EmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Ticket.Create("TEST-123", Guid.Empty, repositoryId));
    }

    [Fact]
    public void Create_EmptyRepositoryId_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Ticket.Create("TEST-123", tenantId, Guid.Empty));
    }

    [Fact]
    public void TransitionTo_ValidTransition_UpdatesStateAndReturnsSuccess()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(WorkflowState.Triggered, ticket.State);

        // Act - Valid transition from Triggered to Analyzing
        var result = ticket.TransitionTo(WorkflowState.Analyzing);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.Analyzing, ticket.State);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(WorkflowState.Triggered, ticket.State);

        // Act - Invalid transition from Triggered to Completed
        var result = ticket.TransitionTo(WorkflowState.Completed);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid transition", result.ErrorMessage);
        Assert.Equal(WorkflowState.Triggered, ticket.State); // State should not change
    }

    [Fact]
    public void TransitionTo_ToCompletedState_SetsCompletedAtTimestamp()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Navigate to a state where Completed is valid
        ticket.TransitionTo(WorkflowState.Analyzing);
        ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
        ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
        ticket.TransitionTo(WorkflowState.Planning);
        ticket.TransitionTo(WorkflowState.PlanPosted);
        ticket.TransitionTo(WorkflowState.PlanUnderReview);
        ticket.TransitionTo(WorkflowState.PlanApproved);

        // Act
        var result = ticket.TransitionTo(WorkflowState.Completed);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.Completed, ticket.State);
        Assert.NotNull(ticket.CompletedAt);
    }

    [Fact]
    public void TransitionTo_ToCancelledState_SetsCompletedAtTimestamp()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act - Cancelled is valid from Triggered
        var result = ticket.TransitionTo(WorkflowState.Cancelled);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.Cancelled, ticket.State);
        Assert.NotNull(ticket.CompletedAt);
    }

    [Fact]
    public void TransitionTo_ToFailedState_SetsCompletedAtTimestamp()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act - Failed is valid from Triggered
        var result = ticket.TransitionTo(WorkflowState.Failed);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.Failed, ticket.State);
        Assert.NotNull(ticket.CompletedAt);
    }

    [Fact]
    public void GetValidTransitions_FromTriggered_ReturnsCorrectStates()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act
        var validTransitions = ticket.GetValidTransitions();

        // Assert
        Assert.Contains(WorkflowState.Analyzing, validTransitions);
        Assert.Contains(WorkflowState.Failed, validTransitions);
        Assert.Contains(WorkflowState.Cancelled, validTransitions);
        Assert.Equal(3, validTransitions.Count);
    }

    [Fact]
    public void GetValidTransitions_FromCompleted_ReturnsEmptyList()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Navigate to Completed state
        ticket.TransitionTo(WorkflowState.Analyzing);
        ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
        ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
        ticket.TransitionTo(WorkflowState.Planning);
        ticket.TransitionTo(WorkflowState.PlanPosted);
        ticket.TransitionTo(WorkflowState.PlanUnderReview);
        ticket.TransitionTo(WorkflowState.PlanApproved);
        ticket.TransitionTo(WorkflowState.Completed);

        // Act
        var validTransitions = ticket.GetValidTransitions();

        // Assert
        Assert.Empty(validTransitions);
    }

    [Fact]
    public void CanTransitionTo_ValidTransition_ReturnsTrue()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        Assert.True(ticket.CanTransitionTo(WorkflowState.Analyzing));
        Assert.True(ticket.CanTransitionTo(WorkflowState.Failed));
        Assert.True(ticket.CanTransitionTo(WorkflowState.Cancelled));
    }

    [Fact]
    public void CanTransitionTo_InvalidTransition_ReturnsFalse()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        Assert.False(ticket.CanTransitionTo(WorkflowState.Completed));
        Assert.False(ticket.CanTransitionTo(WorkflowState.Planning));
        Assert.False(ticket.CanTransitionTo(WorkflowState.Implementing));
    }

    [Fact]
    public void UpdateTicketInfo_ValidParameters_UpdatesTitleAndDescription()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        var title = "New Title";
        var description = "New Description";

        // Act
        ticket.UpdateTicketInfo(title, description);

        // Assert
        Assert.Equal(title, ticket.Title);
        Assert.Equal(description, ticket.Description);
        Assert.NotNull(ticket.UpdatedAt);
    }

    [Fact]
    public void UpdateTicketInfo_EmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ticket.UpdateTicketInfo("", "Description"));
        Assert.Throws<ArgumentException>(() => ticket.UpdateTicketInfo("   ", "Description"));
    }

    [Fact]
    public void SetPlanBranch_ValidBranchName_UpdatesBranchInfo()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        var branchName = "feature/test-123";
        var planPath = "docs/plan.md";

        // Act
        ticket.SetPlanBranch(branchName, planPath);

        // Assert
        Assert.Equal(branchName, ticket.PlanBranchName);
        Assert.Equal(planPath, ticket.PlanMarkdownPath);
        Assert.NotNull(ticket.UpdatedAt);
    }

    [Fact]
    public void SetPlanBranch_EmptyBranchName_ThrowsArgumentException()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ticket.SetPlanBranch(""));
        Assert.Throws<ArgumentException>(() => ticket.SetPlanBranch("   "));
    }

    [Fact]
    public void SetPullRequest_ValidParameters_UpdatesPullRequestInfo()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        var prUrl = "https://github.com/owner/repo/pull/1";
        var prNumber = 1;

        // Act
        ticket.SetPullRequest(prUrl, prNumber);

        // Assert
        Assert.Equal(prUrl, ticket.PullRequestUrl);
        Assert.Equal(prNumber, ticket.PullRequestNumber);
        Assert.NotNull(ticket.UpdatedAt);
    }

    [Fact]
    public void RecordError_ValidError_UpdatesErrorInfoAndIncrementsRetryCount()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        var error = "Something went wrong";
        var initialRetryCount = ticket.RetryCount;

        // Act
        ticket.RecordError(error);

        // Assert
        Assert.Equal(error, ticket.LastError);
        Assert.Equal(initialRetryCount + 1, ticket.RetryCount);
        Assert.NotNull(ticket.UpdatedAt);
    }

    [Fact]
    public void ClearError_AfterRecordingError_ClearsErrorMessage()
    {
        // Arrange
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.RecordError("Error");

        // Act
        ticket.ClearError();

        // Assert
        Assert.Null(ticket.LastError);
        Assert.NotNull(ticket.UpdatedAt);
    }
}
