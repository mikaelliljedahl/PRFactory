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
        Assert.NotNull(ticket);
        Assert.Equal(ValidTicketKey, ticket.TicketKey);
        Assert.Equal(_tenantId, ticket.TenantId);
        Assert.Equal(_repositoryId, ticket.RepositoryId);
        Assert.Equal(WorkflowState.Triggered, ticket.State);
        Assert.Empty(ticket.Questions);
        Assert.Empty(ticket.Answers);
        Assert.True(Math.Abs((ticket.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTicketKey_ThrowsArgumentException(string? invalidKey)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Ticket.Create(invalidKey!, _tenantId, _repositoryId));
        Assert.Contains("ticketKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Ticket.Create(ValidTicketKey, Guid.Empty, _repositoryId));
        Assert.Contains("tenantId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithEmptyRepositoryId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Ticket.Create(ValidTicketKey, _tenantId, Guid.Empty));
        Assert.Contains("repositoryId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransitionTo_ValidTransition_UpdatesState()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        ticket.TransitionTo(WorkflowState.Analyzing);
        Assert.NotNull(ticket.UpdatedAt);
        var originalUpdatedAt = ticket.UpdatedAt.Value;
        await Task.Delay(10); // Ensure time difference

        // Act
        var result = ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.TicketUpdateGenerated, ticket.State);
        Assert.True(ticket.UpdatedAt > originalUpdatedAt);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowState.Completed, ticket.State);
        Assert.NotNull(ticket.CompletedAt);
        Assert.True(Math.Abs((ticket.CompletedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
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
        var single = Assert.Single(ticket.Questions);
        Assert.Equal(question, single);
    }

    [Fact]
    public void AddQuestion_WithNullQuestion_ThrowsArgumentNullException()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ticket.AddQuestion(null!));
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
        Assert.True(result.IsSuccess);
        var single = Assert.Single(ticket.Answers);
        Assert.Equal(question.Id, single.QuestionId);
        Assert.Equal("The expected behavior is X", single.Text);
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
        Assert.False(result.IsSuccess);
        Assert.Empty(ticket.Answers);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AddAnswer_WithEmptyAnswerText_ReturnsFailure(string? invalidAnswer)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        var question = Question.Create("What is the expected behavior?", "requirements");
        ticket.AddQuestion(question);

        // Act
        var result = ticket.AddAnswer(question.Id, invalidAnswer!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Empty(ticket.Answers);
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
        Assert.Equal(branchName, ticket.PlanBranchName);
        Assert.Equal(markdownPath, ticket.PlanMarkdownPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetPlanBranch_WithEmptyBranch_ThrowsArgumentException(string? invalidBranch)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ticket.SetPlanBranch(invalidBranch!, "plans/proj-123.md"));
    }

    [Fact]
    public void ApprovePlan_SetsPlanApprovedAt()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        ticket.ApprovePlan();

        // Assert
        Assert.NotNull(ticket.PlanApprovedAt);
        Assert.True(Math.Abs((ticket.PlanApprovedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
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
        Assert.Equal(branchName, ticket.ImplementationBranchName);
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
        Assert.Equal(prUrl, ticket.PullRequestUrl);
        Assert.Equal(prNumber, ticket.PullRequestNumber);
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
        Assert.Equal(1, ticket.RetryCount);
        Assert.Equal(errorMessage, ticket.LastError);
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
        Assert.Null(ticket.LastError);
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
        Assert.Equal("value1", ticket.GetMetadata<string>("key1"));
        Assert.Equal(42, ticket.GetMetadata<int>("key2"));
    }

    [Fact]
    public void GetMetadata_NonExistentKey_ReturnsDefault()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        var value = ticket.GetMetadata<string>("nonexistent");

        // Assert
        Assert.Null(value);
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
        Assert.Equal(externalId, ticket.ExternalTicketId);
        Assert.Equal(TicketSource.Jira, ticket.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetExternalTicketId_WithEmptyId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ticket.SetExternalTicketId(invalidId!, TicketSource.Jira));
    }

    [Fact]
    public void SetExternalTicketId_WithWebUISource_ThrowsArgumentException()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ticket.SetExternalTicketId("123", TicketSource.WebUI));
        Assert.Contains("Web UI", ex.Message);
    }

    [Fact]
    public void MarkAsSynced_SetsLastSyncedAt()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);

        // Act
        ticket.MarkAsSynced();

        // Assert
        Assert.NotNull(ticket.LastSyncedAt);
        Assert.True(Math.Abs((ticket.LastSyncedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void MarkPRCreated_SetsPropertiesCorrectly()
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
        ticket.TransitionTo(WorkflowState.Implementing);

        const int prNumber = 123;
        const string prUrl = "https://github.com/org/repo/pull/123";

        // Act
        ticket.MarkPRCreated(prNumber, prUrl);

        // Assert
        Assert.Equal(prNumber, ticket.PullRequestNumber);
        Assert.Equal(prUrl, ticket.PullRequestUrl);
    }

    [Fact]
    public void MarkPRCreated_TransitionsToPRCreatedState()
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
        ticket.TransitionTo(WorkflowState.Implementing);

        const int prNumber = 456;
        const string prUrl = "https://github.com/org/repo/pull/456";

        // Act
        ticket.MarkPRCreated(prNumber, prUrl);

        // Assert
        Assert.Equal(WorkflowState.PRCreated, ticket.State);
    }

    [Fact]
    public void MarkPRCreated_ThrowsException_WhenNotInImplementingState()
    {
        // Arrange
        var ticket = Ticket.Create(ValidTicketKey, _tenantId, _repositoryId);
        ticket.TransitionTo(WorkflowState.Analyzing);

        const int prNumber = 789;
        const string prUrl = "https://github.com/org/repo/pull/789";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ticket.MarkPRCreated(prNumber, prUrl));
        Assert.Contains("Cannot mark PR created when ticket is in", ex.Message);
        Assert.Contains("Analyzing", ex.Message);
        Assert.Contains("Expected Implementing", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MarkPRCreated_ThrowsException_WhenPrUrlEmpty(string? invalidUrl)
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
        ticket.TransitionTo(WorkflowState.Implementing);

        const int prNumber = 999;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ticket.MarkPRCreated(prNumber, invalidUrl!));
        Assert.Contains("PR URL cannot be empty", ex.Message);
    }
}
