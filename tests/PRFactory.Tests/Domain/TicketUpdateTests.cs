using FluentAssertions;
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
        update.Should().NotBeNull();
        update.TicketId.Should().Be(_ticketId);
        update.UpdatedTitle.Should().Be(ValidTitle);
        update.UpdatedDescription.Should().Be(ValidDescription);
        update.AcceptanceCriteria.Should().Be(ValidAcceptanceCriteria);
        update.SuccessCriteria.Should().HaveCount(2);
        update.IsApproved.Should().BeFalse();
        update.IsDraft.Should().BeTrue();
        update.Version.Should().Be(1);
        update.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithEmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => TicketUpdate.Create(
            Guid.Empty,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ticketId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Act & Assert
        var act = () => TicketUpdate.Create(
            _ticketId,
            invalidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidDescription_ThrowsArgumentException(string invalidDescription)
    {
        // Act & Assert
        var act = () => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            invalidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*description*");
    }

    [Fact]
    public void Create_WithEmptySuccessCriteria_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            new List<SuccessCriterion>(),
            ValidAcceptanceCriteria);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*successCriteria*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidAcceptanceCriteria_ThrowsArgumentException(string invalidCriteria)
    {
        // Act & Assert
        var act = () => TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            invalidCriteria);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*acceptanceCriteria*");
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
        update.IsApproved.Should().BeTrue();
        update.IsDraft.Should().BeFalse();
        update.ApprovedAt.Should().NotBeNull();
        update.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
        update.IsApproved.Should().BeFalse();
        update.IsDraft.Should().BeTrue();
        update.RejectionReason.Should().Be(rejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Reject_WithEmptyReason_ThrowsArgumentException(string invalidReason)
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Act & Assert
        var act = () => update.Reject(invalidReason);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*reason*");
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
        update.Version.Should().Be(4); // previous version + 1
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
        update.PostedAt.Should().NotBeNull();
        update.PostedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
        update.UpdatedTitle.Should().Be(newTitle);
        update.UpdatedDescription.Should().Be(newDescription);
        update.SuccessCriteria.Should().Contain(newCriteria[0]);
        update.IsApproved.Should().BeFalse();
        update.IsDraft.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Update_WithInvalidDescription_ThrowsArgumentException(string invalidDescription)
    {
        // Arrange
        var update = TicketUpdate.Create(
            _ticketId,
            ValidTitle,
            ValidDescription,
            _validSuccessCriteria,
            ValidAcceptanceCriteria);

        // Act & Assert
        var act = () => update.Update(ValidTitle, invalidDescription, _validSuccessCriteria, ValidAcceptanceCriteria);
        act.Should().Throw<ArgumentException>();
    }
}
