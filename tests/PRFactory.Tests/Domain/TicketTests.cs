using FluentAssertions;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Tests.Domain;

public class TicketTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _repositoryId = Guid.NewGuid();
    private const string ValidTicketKey = "PROJ-123";

    [Fact]
    public void Create_WithValidInputs_ReturnsValidTicket()
    {
        // Act
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Assert
        ticket.Should().NotBeNull();
        ticket.TicketKey.Should().Be(ValidTicketKey);
        ticket.TenantId.Should().Be(_tenantId);
        ticket.RepositoryId.Should().Be(_repositoryId);
        ticket.State.Should().Be(WorkflowState.Triggered);
        ticket.Questions.Should().BeEmpty();
        ticket.Answers.Should().BeEmpty();
        ticket.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTicketKey_ThrowsArgumentException(string invalidKey)
    {
        // Act & Assert
        var act = () => Ticket.Create(invalidKey, _tenantId, _repositoryId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ticketKey*");
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Ticket.Create(ValidTicketKey, Guid.Empty, _repositoryId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tenantId*");
    }

    [Fact]
    public void Create_WithEmptyRepositoryId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Ticket.Create(ValidTicketKey, _tenantId, Guid.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*repositoryId*");
    }

    [Fact]
    public void TransitionTo_ValidTransition_UpdatesState()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        ticket.TransitionTo(WorkflowState.Analyzing);
        var originalUpdatedAt = ticket.UpdatedAt.Value;
        Thread.Sleep(10); // Ensure time difference

        // Act
        var result = ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.State.Should().Be(WorkflowState.TicketUpdateGenerated);
        ticket.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void TransitionTo_TerminalState_SetsCompletedAt()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
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
        result.IsSuccess.Should().BeTrue();
        ticket.State.Should().Be(WorkflowState.Completed);
        ticket.CompletedAt.Should().NotBeNull();
        ticket.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddQuestion_WithValidQuestion_AddsToQuestionsList()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var question = Question.Create("What is the expected behavior?", "requirements");

        // Act
        ticket.AddQuestion(question);

        // Assert
        ticket.Questions.Should().HaveCount(1);
        ticket.Questions.First().Should().Be(question);
    }

    [Fact]
    public void AddQuestion_WithNullQuestion_ThrowsArgumentNullException()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        var act = () => ticket.AddQuestion(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddAnswer_WithValidAnswer_AddsToAnswersList()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var question = Question.Create("What is the expected behavior?", "requirements");
        ticket.AddQuestion(question);

        // Act
        var result = ticket.AddAnswer(question.Id, "The expected behavior is X");

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Answers.Should().HaveCount(1);
        ticket.Answers.First().QuestionId.Should().Be(question.Id);
        ticket.Answers.First().Text.Should().Be("The expected behavior is X");
    }

    [Fact]
    public void AddAnswer_WithNonExistentQuestionId_ReturnsFailure()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var nonExistentQuestionId = Guid.NewGuid().ToString();

        // Act
        var result = ticket.AddAnswer(nonExistentQuestionId, "Some answer");

        // Assert
        result.IsSuccess.Should().BeFalse();
        ticket.Answers.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AddAnswer_WithEmptyAnswerText_ReturnsFailure(string invalidAnswer)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var question = Question.Create("What is the expected behavior?", "requirements");
        ticket.AddQuestion(question);

        // Act
        var result = ticket.AddAnswer(question.Id, invalidAnswer);

        // Assert
        result.IsSuccess.Should().BeFalse();
        ticket.Answers.Should().BeEmpty();
    }

    [Fact]
    public void SetPlanBranch_WithValidBranch_SetsBranchAndPath()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        const string branchName = "feature/proj-123";
        const string markdownPath = "plans/proj-123.md";

        // Act
        ticket.SetPlanBranch(branchName, markdownPath);

        // Assert
        ticket.PlanBranchName.Should().Be(branchName);
        ticket.PlanMarkdownPath.Should().Be(markdownPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetPlanBranch_WithEmptyBranch_ThrowsArgumentException(string invalidBranch)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        var act = () => ticket.SetPlanBranch(invalidBranch, "plans/proj-123.md");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApprovePlan_SetsPlanApprovedAt()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        ticket.ApprovePlan();

        // Assert
        ticket.PlanApprovedAt.Should().NotBeNull();
        ticket.PlanApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetImplementationBranch_WithValidBranch_SetsBranchName()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        const string branchName = "impl/proj-123";

        // Act
        ticket.SetImplementationBranch(branchName);

        // Assert
        ticket.ImplementationBranchName.Should().Be(branchName);
    }

    [Fact]
    public void SetPullRequest_WithValidUrlAndNumber_SetsPullRequestInfo()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        const string prUrl = "https://github.com/org/repo/pull/123";
        const int prNumber = 123;

        // Act
        ticket.SetPullRequest(prUrl, prNumber);

        // Assert
        ticket.PullRequestUrl.Should().Be(prUrl);
        ticket.PullRequestNumber.Should().Be(prNumber);
    }

    [Fact]
    public void RecordError_IncrementsRetryCountAndSetsLastError()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        const string errorMessage = "Something went wrong";

        // Act
        ticket.RecordError(errorMessage);

        // Assert
        ticket.RetryCount.Should().Be(1);
        ticket.LastError.Should().Be(errorMessage);
    }

    [Fact]
    public void ClearError_NullsLastError()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        ticket.RecordError("Some error");

        // Act
        ticket.ClearError();

        // Assert
        ticket.LastError.Should().BeNull();
    }

    [Fact]
    public void SetMetadata_SetsKeyValuePairs()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        ticket.SetMetadata("key1", "value1");
        ticket.SetMetadata("key2", 42);

        // Assert
        ticket.GetMetadata<string>("key1").Should().Be("value1");
        ticket.GetMetadata<int>("key2").Should().Be(42);
    }

    [Fact]
    public void GetMetadata_NonExistentKey_ReturnsDefault()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        var value = ticket.GetMetadata<string>("nonexistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void SetExternalTicketId_WithValidId_SetsExternalTicketId()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        const string externalId = "JIRA-123";

        // Act
        ticket.SetExternalTicketId(externalId, TicketSource.Jira);

        // Assert
        ticket.ExternalTicketId.Should().Be(externalId);
        ticket.Source.Should().Be(TicketSource.Jira);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetExternalTicketId_WithEmptyId_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        var act = () => ticket.SetExternalTicketId(invalidId, TicketSource.Jira);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetExternalTicketId_WithWebUISource_ThrowsArgumentException()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        var act = () => ticket.SetExternalTicketId("123", TicketSource.WebUI);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Web UI*");
    }

    [Fact]
    public void MarkAsSynced_SetsLastSyncedAt()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        ticket.MarkAsSynced();

        // Assert
        ticket.LastSyncedAt.Should().NotBeNull();
        ticket.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
