using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.UnitTests.Domain;

/// <summary>
/// Tests for the TicketUpdate entity, focusing on approval/rejection workflows and business logic.
/// </summary>
public class TicketUpdateTests
{
    [Fact]
    public void Create_ValidParameters_CreatesTicketUpdateAsDraft()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var title = "Updated Title";
        var description = "Updated Description";
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Feature works correctly", 0, true)
        };
        var acceptanceCriteria = "- Test passes\n- Code reviewed";

        // Act
        var ticketUpdate = TicketUpdate.Create(ticketId, title, description, successCriteria, acceptanceCriteria);

        // Assert
        Assert.NotNull(ticketUpdate);
        Assert.Equal(ticketId, ticketUpdate.TicketId);
        Assert.Equal(title, ticketUpdate.UpdatedTitle);
        Assert.Equal(description, ticketUpdate.UpdatedDescription);
        Assert.Equal(acceptanceCriteria, ticketUpdate.AcceptanceCriteria);
        Assert.Single(ticketUpdate.SuccessCriteria);
        Assert.True(ticketUpdate.IsDraft);
        Assert.False(ticketUpdate.IsApproved);
        Assert.Equal(1, ticketUpdate.Version);
        Assert.Null(ticketUpdate.ApprovedAt);
        Assert.Null(ticketUpdate.PostedAt);
    }

    [Fact]
    public void Create_EmptyTicketId_ThrowsArgumentException()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Test", 0, true)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TicketUpdate.Create(Guid.Empty, "Title", "Description", successCriteria, "AC"));
    }

    [Fact]
    public void Create_EmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Test", 0, true)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TicketUpdate.Create(Guid.NewGuid(), "", "Description", successCriteria, "AC"));
        Assert.Throws<ArgumentException>(() =>
            TicketUpdate.Create(Guid.NewGuid(), "   ", "Description", successCriteria, "AC"));
    }

    [Fact]
    public void Create_EmptySuccessCriteria_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TicketUpdate.Create(Guid.NewGuid(), "Title", "Description", new List<SuccessCriterion>(), "AC"));
        Assert.Throws<ArgumentException>(() =>
            TicketUpdate.Create(Guid.NewGuid(), "Title", "Description", null!, "AC"));
    }

    [Fact]
    public void Approve_DraftTicketUpdate_ChangesStateAndSetsApprovedAt()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        Assert.True(ticketUpdate.IsDraft);
        Assert.False(ticketUpdate.IsApproved);

        // Act
        ticketUpdate.Approve();

        // Assert
        Assert.False(ticketUpdate.IsDraft);
        Assert.True(ticketUpdate.IsApproved);
        Assert.NotNull(ticketUpdate.ApprovedAt);
        Assert.Null(ticketUpdate.RejectionReason);
    }

    [Fact]
    public void Approve_NonDraftTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve(); // First approval

        // Act & Assert - Trying to approve again should fail
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.Approve());
    }

    [Fact]
    public void Approve_AlreadyApprovedTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.Approve());
    }

    [Fact]
    public void Reject_DraftTicketUpdate_SetsRejectionReason()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        var reason = "Needs more details";

        // Act
        ticketUpdate.Reject(reason);

        // Assert
        Assert.Equal(reason, ticketUpdate.RejectionReason);
        Assert.True(ticketUpdate.IsDraft); // Remains draft for regeneration
        Assert.False(ticketUpdate.IsApproved);
    }

    [Fact]
    public void Reject_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ticketUpdate.Reject(""));
        Assert.Throws<ArgumentException>(() => ticketUpdate.Reject("   "));
    }

    [Fact]
    public void Reject_NonDraftTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.Reject("Reason"));
    }

    [Fact]
    public void Reject_ApprovedTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.Reject("Reason"));
    }

    [Fact]
    public void MarkAsPosted_ApprovedTicketUpdate_SetsPostedAt()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();

        // Act
        ticketUpdate.MarkAsPosted();

        // Assert
        Assert.NotNull(ticketUpdate.PostedAt);
    }

    [Fact]
    public void MarkAsPosted_UnapprovedTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.MarkAsPosted());
    }

    [Fact]
    public void MarkAsPosted_AlreadyPosted_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();
        ticketUpdate.MarkAsPosted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticketUpdate.MarkAsPosted());
    }

    [Fact]
    public void Update_ValidParameters_UpdatesContentAndResetsApprovalState()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve(); // Approve first

        var newTitle = "New Title";
        var newDescription = "New Description";
        var newSuccessCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Performance, "New criterion", 1, false)
        };
        var newAcceptanceCriteria = "New AC";

        // Act
        ticketUpdate.Update(newTitle, newDescription, newSuccessCriteria, newAcceptanceCriteria);

        // Assert
        Assert.Equal(newTitle, ticketUpdate.UpdatedTitle);
        Assert.Equal(newDescription, ticketUpdate.UpdatedDescription);
        Assert.Equal(newAcceptanceCriteria, ticketUpdate.AcceptanceCriteria);
        Assert.Single(ticketUpdate.SuccessCriteria);
        Assert.True(ticketUpdate.IsDraft); // Reset to draft
        Assert.False(ticketUpdate.IsApproved); // Reset approval
        Assert.Null(ticketUpdate.ApprovedAt); // Reset timestamp
        Assert.Null(ticketUpdate.RejectionReason); // Reset rejection
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionNumber()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        var initialVersion = ticketUpdate.Version;

        // Act
        ticketUpdate.IncrementVersion();

        // Assert
        Assert.Equal(initialVersion + 1, ticketUpdate.Version);
    }

    [Fact]
    public void GetSuccessCriteriaByCategory_FiltersByCategory()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Functional test", 0, true),
            new SuccessCriterion(SuccessCriterionCategory.Performance, "Performance test", 1, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Another functional", 0, false)
        };
        var ticketUpdate = TicketUpdate.Create(
            Guid.NewGuid(), "Title", "Desc", successCriteria, "AC");

        // Act
        var functionalCriteria = ticketUpdate.GetSuccessCriteriaByCategory(SuccessCriterionCategory.Functional);
        var performanceCriteria = ticketUpdate.GetSuccessCriteriaByCategory(SuccessCriterionCategory.Performance);

        // Assert
        Assert.Equal(2, functionalCriteria.Count);
        Assert.Single(performanceCriteria);
    }

    [Fact]
    public void GetMustHaveCriteria_ReturnsOnlyPriority0()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Must have", 0, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Should have", 1, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Nice to have", 2, false)
        };
        var ticketUpdate = TicketUpdate.Create(
            Guid.NewGuid(), "Title", "Desc", successCriteria, "AC");

        // Act
        var mustHave = ticketUpdate.GetMustHaveCriteria();

        // Assert
        Assert.Single(mustHave);
        Assert.Equal(0, mustHave[0].Priority);
    }

    [Fact]
    public void GetShouldHaveCriteria_ReturnsOnlyPriority1()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Must have", 0, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Should have", 1, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Nice to have", 2, false)
        };
        var ticketUpdate = TicketUpdate.Create(
            Guid.NewGuid(), "Title", "Desc", successCriteria, "AC");

        // Act
        var shouldHave = ticketUpdate.GetShouldHaveCriteria();

        // Assert
        Assert.Single(shouldHave);
        Assert.Equal(1, shouldHave[0].Priority);
    }

    [Fact]
    public void GetNiceToHaveCriteria_ReturnsOnlyPriority2()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Must have", 0, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Should have", 1, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Nice to have", 2, false)
        };
        var ticketUpdate = TicketUpdate.Create(
            Guid.NewGuid(), "Title", "Desc", successCriteria, "AC");

        // Act
        var niceToHave = ticketUpdate.GetNiceToHaveCriteria();

        // Assert
        Assert.Single(niceToHave);
        Assert.Equal(2, niceToHave[0].Priority);
    }

    [Fact]
    public void GetTestableCriteria_ReturnsOnlyTestable()
    {
        // Arrange
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Testable", 0, true),
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Not testable", 0, false),
            new SuccessCriterion(SuccessCriterionCategory.Performance, "Also testable", 1, true)
        };
        var ticketUpdate = TicketUpdate.Create(
            Guid.NewGuid(), "Title", "Desc", successCriteria, "AC");

        // Act
        var testable = ticketUpdate.GetTestableCriteria();

        // Assert
        Assert.Equal(2, testable.Count);
        Assert.All(testable, sc => Assert.True(sc.IsTestable));
    }

    [Fact]
    public void IsReadyToPost_ApprovedAndNotPosted_ReturnsTrue()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();

        // Act
        var isReady = ticketUpdate.IsReadyToPost();

        // Assert
        Assert.True(isReady);
    }

    [Fact]
    public void IsReadyToPost_NotApproved_ReturnsFalse()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();

        // Act
        var isReady = ticketUpdate.IsReadyToPost();

        // Assert
        Assert.False(isReady);
    }

    [Fact]
    public void IsReadyToPost_AlreadyPosted_ReturnsFalse()
    {
        // Arrange
        var ticketUpdate = CreateValidTicketUpdate();
        ticketUpdate.Approve();
        ticketUpdate.MarkAsPosted();

        // Act
        var isReady = ticketUpdate.IsReadyToPost();

        // Assert
        Assert.False(isReady);
    }

    // Helper method to create a valid ticket update for testing
    private static TicketUpdate CreateValidTicketUpdate()
    {
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Feature works", 0, true)
        };

        return TicketUpdate.Create(
            Guid.NewGuid(),
            "Test Title",
            "Test Description",
            successCriteria,
            "Test Acceptance Criteria");
    }
}
