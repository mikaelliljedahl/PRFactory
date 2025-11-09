using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Tests.Domain;

public class TicketUpdateTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private const string ValidTitle = "Improved Test Title";
    private const string ValidDescription = "This is an improved description.";
    private const string ValidAcceptanceCriteria = "- Criterion 1\n- Criterion 2";
    private readonly List<SuccessCriterion> _validSuccessCriteria = new()
    {
        SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Success criterion 1", true),
        SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Performance, "Success criterion 2", true)
    };

    [Fact]
    public void Create_WithValidInputs_ReturnsValidTicketUpdate()
    {
        // Act
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Assert
        Assert.NotNull(update);
        Assert.Equal(_ticketId, update.TicketId);
        Assert.Equal(ValidTitle, update.UpdatedTitle);
        Assert.Equal(ValidDescription, update.UpdatedDescription);
        Assert.Equal(ValidAcceptanceCriteria, update.AcceptanceCriteria);
        Assert.Equal(2, update.SuccessCriteria.Count);
        Assert.False(update.IsApproved);
        Assert.True(update.IsDraft);
        Assert.Equal(1, update.Version);
        Assert.True(Math.Abs((update.GeneratedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Create_WithEmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TicketUpdate.Create(
            Guid.Empty,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria));
        Assert.Contains("ticketId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TicketUpdate.Create(
            _ticketId,
            invalidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria));
        Assert.Contains("title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidDescription_ThrowsArgumentException(string? invalidDescription)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            invalidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria));
        Assert.Contains("description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithEmptySuccessCriteria_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            new List<SuccessCriterion>(),
            ValidAcceptanceCriteria));
        Assert.Contains("successCriteria", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidAcceptanceCriteria_ThrowsArgumentException(string? invalidCriteria)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            invalidCriteria));
        Assert.Contains("acceptanceCriteria", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Approve_SetsApprovalFields()
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Act
        update.Approve();

        // Assert
        Assert.True(update.IsApproved);
        Assert.False(update.IsDraft);
        Assert.NotNull(update.ApprovedAt);
        Assert.True(Math.Abs((update.ApprovedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Reject_WithReason_SetsRejectionFields()
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        const string rejectionReason = "Needs more details";

        // Act
        update.Reject(rejectionReason);

        // Assert
        Assert.False(update.IsApproved);
        Assert.True(update.IsDraft);
        Assert.Equal(rejectionReason, update.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Reject_WithEmptyReason_ThrowsArgumentException(string? invalidReason)
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => update.Reject(invalidReason));
        Assert.Contains("reason", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionNumber()
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria,
            version: 3);

        // Act
        update.IncrementVersion();

        // Assert
        Assert.Equal(4, update.Version); // previous version + 1
    }

    [Fact]
    public void MarkAsPosted_SetsPostedAt()
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Must approve before posting
        update.Approve();

        // Act
        update.MarkAsPosted();

        // Assert
        Assert.NotNull(update.PostedAt);
        Assert.True(Math.Abs((update.PostedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesContent()
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        const string newTitle = "Updated title";
        const string newDescription = "Updated description";
        var newCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateShouldHave(SuccessCriterionCategory.Technical, "New criterion", true)
        };

        // Act
        update.Update(newTitle, newDescription, newCriteria, ValidAcceptanceCriteria);

        // Assert
        Assert.Equal(newTitle, update.UpdatedTitle);
        Assert.Equal(newDescription, update.UpdatedDescription);
        Assert.Contains(newCriteria[0], update.SuccessCriteria);
        Assert.False(update.IsApproved);
        Assert.True(update.IsDraft);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Update_WithInvalidDescription_ThrowsArgumentException(string? invalidDescription)
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => update.Update(ValidTitle, invalidDescription, _validSuccessCriteria, ValidAcceptanceCriteria));
    }
}
