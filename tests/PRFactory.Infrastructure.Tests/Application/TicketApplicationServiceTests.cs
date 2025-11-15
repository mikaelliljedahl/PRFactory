using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.Results;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Git;

// Alias to resolve ambiguity
using DomainRepository = PRFactory.Domain.Entities.Repository;
using WorkflowState = PRFactory.Domain.ValueObjects.WorkflowState;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PRFactory.Infrastructure.Tests.Application;

/// <summary>
/// Unit tests for TicketApplicationService - Phase 2 methods (GetDiffContentAsync, CreatePullRequestAsync)
/// </summary>
public class TicketApplicationServiceTests
{
    private readonly Mock<ILogger<TicketApplicationService>> _loggerMock;
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<IRepositoryRepository> _repositoryRepoMock;
    private readonly Mock<IWorkflowOrchestrator> _orchestratorMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;
    private readonly Mock<ILocalGitService> _localGitServiceMock;
    private readonly Mock<IGitPlatformProvider> _gitPlatformProviderMock;
    private readonly Mock<ITicketUpdateRepository> _ticketUpdateRepoMock;
    private readonly TicketApplicationService _service;

    public TicketApplicationServiceTests()
    {
        _loggerMock = new Mock<ILogger<TicketApplicationService>>();
        _ticketRepoMock = new Mock<ITicketRepository>();
        _repositoryRepoMock = new Mock<IRepositoryRepository>();
        _orchestratorMock = new Mock<IWorkflowOrchestrator>();
        _tenantContextMock = new Mock<ITenantContext>();
        _workspaceServiceMock = new Mock<IWorkspaceService>();
        _localGitServiceMock = new Mock<ILocalGitService>();
        _gitPlatformProviderMock = new Mock<IGitPlatformProvider>();
        _ticketUpdateRepoMock = new Mock<ITicketUpdateRepository>();

        _service = new TicketApplicationService(
            _loggerMock.Object,
            _ticketRepoMock.Object,
            _repositoryRepoMock.Object,
            _orchestratorMock.Object,
            _tenantContextMock.Object,
            _workspaceServiceMock.Object,
            _localGitServiceMock.Object,
            _gitPlatformProviderMock.Object,
            _ticketUpdateRepoMock.Object
        );
    }

    #region GetDiffContentAsync Tests

    [Fact]
    public async Task GetDiffContentAsync_ReturnsNull_WhenDiffDoesNotExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetDiffContentAsync(ticketId);

        // Assert
        Assert.Null(result);
        _workspaceServiceMock.Verify(x => x.ReadDiffAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task GetDiffContentAsync_ReturnsDiff_WhenExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var diffContent = "diff --git a/file.txt b/file.txt\nindex 1234567..89abcdef 100644\n--- a/file.txt\n+++ b/file.txt\n@@ -1,1 +1,1 @@\n-old content\n+new content";
        _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
            .ReturnsAsync(diffContent);

        // Act
        var result = await _service.GetDiffContentAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(diffContent, result);
        _workspaceServiceMock.Verify(x => x.ReadDiffAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task GetDiffContentAsync_LogsAppropriately()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var diffContent = "diff content";
        _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
            .ReturnsAsync(diffContent);

        // Act
        await _service.GetDiffContentAsync(ticketId);

        // Assert - Verify logging occurred (using Moq verification)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting diff content for ticket")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved diff for ticket")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CreatePullRequestAsync Tests

    [Fact]
    public async Task CreatePullRequestAsync_FailsForInvalidState()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Planning);
        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not in Implementing state", result.ErrorMessage);
        Assert.Contains("Planning", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePullRequestAsync_FailsWhenTicketNotFound()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePullRequestAsync_CreatesSuccessfully()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var prInfo = new PullRequestInfo(123, "https://github.com/repo/pull/123", "https://github.com/repo/pull/123");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId, "testUser");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("https://github.com/repo/pull/123", result.PullRequestUrl);
        Assert.Equal(123, result.PullRequestNumber);
        Assert.Null(result.ErrorMessage);

        // Verify all operations were called
        _localGitServiceMock.Verify(x => x.PushAsync(
            "/workspace/repo",
            "feature/TEST-123",
            It.IsAny<string>()), Times.Once);
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.Is<CreatePullRequestRequest>(r =>
                r.SourceBranch == "feature/TEST-123" &&
                r.TargetBranch == "main" &&
                r.Title.Contains("TEST-123"))), Times.Once);
        _ticketRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Ticket>(), default), Times.Once);
        _workspaceServiceMock.Verify(x => x.DeleteDiffAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_HandlesGitPushFailure()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new LibGit2SharpException("Push failed: authentication required"));

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Git error", result.ErrorMessage);
        Assert.Contains("authentication required", result.ErrorMessage);

        // Verify PR was not created
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.IsAny<CreatePullRequestRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreatePullRequestAsync_HandlesPlatformApiFailure()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ThrowsAsync(new HttpRequestException("API rate limit exceeded"));

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Platform API error", result.ErrorMessage);
        Assert.Contains("rate limit exceeded", result.ErrorMessage);

        // Verify ticket was not updated
        _ticketRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Ticket>(), default), Times.Never);
    }

    [Fact]
    public async Task CreatePullRequestAsync_UpdatesTicketState()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var prInfo = new PullRequestInfo(456, "https://github.com/repo/pull/456", "https://github.com/repo/pull/456");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(WorkflowState.PRCreated, ticket.State);
        Assert.Equal("https://github.com/repo/pull/456", ticket.PullRequestUrl);
        Assert.Equal(456, ticket.PullRequestNumber);
    }

    [Fact]
    public async Task CreatePullRequestAsync_DeletesDiffFile()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var prInfo = new PullRequestInfo(789, "https://github.com/repo/pull/789", "https://github.com/repo/pull/789");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);
        _workspaceServiceMock.Verify(x => x.DeleteDiffAsync(ticketId), Times.Once);
    }

    #endregion

    #region BuildPRDescriptionAsync Tests

    [Fact]
    public async Task CreatePullRequestAsync_CreatesPR_WithBasicDescription_WhenNoTicketUpdate()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var prInfo = new PullRequestInfo(100, "https://github.com/repo/pull/100", "https://github.com/repo/pull/100");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(ticketId, default))
            .ReturnsAsync((TicketUpdate?)null);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);

        // Verify PR description contains basic ticket info
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.Is<CreatePullRequestRequest>(req =>
                req.Description.Contains("TEST-123") &&
                req.Description.Contains("Test Ticket") &&
                req.Description.Contains("Test description") &&
                req.Description.Contains("Generated by PRFactory"))),
            Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_CreatesPR_WithDetailedDescription_WhenTicketUpdateExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var ticketUpdate = CreateTestTicketUpdate(ticketId);
        var prInfo = new PullRequestInfo(200, "https://github.com/repo/pull/200", "https://github.com/repo/pull/200");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);

        // Verify PR description contains detailed ticket update info
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.Is<CreatePullRequestRequest>(req =>
                req.Description.Contains("Refined Test Ticket") &&
                req.Description.Contains("Refined description with more details") &&
                req.Description.Contains("Success Criteria") &&
                req.Description.Contains("Generated by PRFactory"))),
            Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_IncludesSuccessCriteria_WhenAvailable()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var ticketUpdate = CreateTestTicketUpdate(ticketId);
        var prInfo = new PullRequestInfo(300, "https://github.com/repo/pull/300", "https://github.com/repo/pull/300");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);

        // Verify PR description contains success criteria
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.Is<CreatePullRequestRequest>(req =>
                req.Description.Contains("Must Have (Priority 0)") &&
                req.Description.Contains("Feature must be implemented") &&
                req.Description.Contains("Should Have (Priority 1)") &&
                req.Description.Contains("Feature should have tests"))),
            Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_IncludesAcceptanceCriteria_WhenAvailable()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticket = CreateTestTicket(ticketId, WorkflowState.Implementing, repositoryId);
        var repository = CreateTestRepository(repositoryId);
        var ticketUpdate = CreateTestTicketUpdate(ticketId);
        var prInfo = new PullRequestInfo(400, "https://github.com/repo/pull/400", "https://github.com/repo/pull/400");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);
        _repositoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(repository);
        _workspaceServiceMock.Setup(x => x.GetRepositoryPath(ticketId))
            .Returns("/workspace/repo");
        _localGitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ticketUpdateRepoMock.Setup(x => x.GetLatestApprovedByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);
        _gitPlatformProviderMock.Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>()))
            .ReturnsAsync(prInfo);
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), default))
            .Returns(Task.CompletedTask);
        _workspaceServiceMock.Setup(x => x.DeleteDiffAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.True(result.Success);

        // Verify PR description contains acceptance criteria
        _gitPlatformProviderMock.Verify(x => x.CreatePullRequestAsync(
            It.IsAny<Guid>(),
            It.Is<CreatePullRequestRequest>(req =>
                req.Description.Contains("Acceptance Criteria") &&
                req.Description.Contains("All tests pass") &&
                req.Description.Contains("Code review completed"))),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Ticket CreateTestTicket(Guid ticketId, WorkflowState state, Guid? repositoryId = null)
    {
        var ticket = Ticket.Create(
            ticketKey: "TEST-123",
            tenantId: Guid.NewGuid(),
            repositoryId: repositoryId ?? Guid.NewGuid(),
            ticketSystem: "Jira",
            source: TicketSource.WebUI);

        ticket.UpdateTicketInfo("Test Ticket", "Test description");

        // Transition to the desired state (using reflection to set private state field)
        var stateField = typeof(Ticket).GetProperty("State")!;
        stateField.SetValue(ticket, state);

        return ticket;
    }

    private static DomainRepository CreateTestRepository(Guid repositoryId)
    {
        return DomainRepository.Create(
            name: "test-repo",
            cloneUrl: "https://github.com/test/repo.git",
            defaultBranch: "main",
            gitPlatform: "GitHub",
            accessToken: "test-token",
            tenantId: Guid.NewGuid());
    }

    private static TicketUpdate CreateTestTicketUpdate(Guid ticketId)
    {
        var successCriteria = new List<PRFactory.Domain.ValueObjects.SuccessCriterion>
        {
            new PRFactory.Domain.ValueObjects.SuccessCriterion(
                category: PRFactory.Domain.ValueObjects.SuccessCriterionCategory.Functional,
                description: "Feature must be implemented",
                priority: 0,
                isTestable: true),
            new PRFactory.Domain.ValueObjects.SuccessCriterion(
                category: PRFactory.Domain.ValueObjects.SuccessCriterionCategory.Testing,
                description: "Feature should have tests",
                priority: 1,
                isTestable: true)
        };

        var ticketUpdate = TicketUpdate.Create(
            ticketId: ticketId,
            updatedTitle: "Refined Test Ticket",
            updatedDescription: "Refined description with more details",
            successCriteria: successCriteria,
            acceptanceCriteria: "- All tests pass\n- Code review completed",
            version: 1);

        // Approve the ticket update
        ticketUpdate.Approve();

        return ticketUpdate;
    }

    #endregion
}
