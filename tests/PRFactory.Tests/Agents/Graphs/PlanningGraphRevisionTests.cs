using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Git;
using Xunit;

namespace PRFactory.Tests.Agents.Graphs;

/// <summary>
/// Tests for plan revision tracking integration in GitPlanAgent
/// </summary>
public class PlanningGraphRevisionTests
{
    private readonly Mock<ILogger<GitPlanAgent>> _mockLogger;
    private readonly Mock<ILocalGitService> _mockGitService;
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IPlanService> _mockPlanService;
    private readonly GitPlanAgent _gitPlanAgent;
    private readonly Guid _testTicketId;
    private readonly Ticket _testTicket;

    public PlanningGraphRevisionTests()
    {
        _mockLogger = new Mock<ILogger<GitPlanAgent>>();
        _mockGitService = new Mock<ILocalGitService>();
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockPlanService = new Mock<IPlanService>();
        _testTicketId = Guid.NewGuid();

        // Create a test ticket
        _testTicket = Ticket.Create(
            ticketKey: "TEST-123",
            tenantId: Guid.NewGuid(),
            repositoryId: Guid.NewGuid(),
            ticketSystem: "Jira");

        _gitPlanAgent = new GitPlanAgent(
            _mockLogger.Object,
            _mockGitService.Object,
            _mockTicketRepository.Object,
            _mockPlanService.Object);
    }

    [Fact]
    public async Task PlanGenerated_CreatesInitialRevision()
    {
        // Arrange
        var context = new AgentContext
        {
            TicketId = _testTicketId.ToString(),
            Ticket = _testTicket,
            RepositoryPath = "/test/repo/path",
            ImplementationPlan = "# Implementation Plan\nTest plan content"
        };

        _mockGitService
            .Setup(x => x.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("feature/test-123-implementation-plan");

        _mockGitService
            .Setup(x => x.CommitAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var revisionDto = new PlanRevisionDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            RevisionNumber = 1,
            Reason = "Initial",
            CreatedAt = DateTime.UtcNow
        };

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Initial, null))
            .ReturnsAsync(revisionDto);

        // Act
        var result = await _gitPlanAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Verify CreateRevisionAsync was called with correct parameters
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Initial, null),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created initial plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanGenerated_WhenRevisionCreationFails_DoesNotFailWorkflow()
    {
        // Arrange
        var context = new AgentContext
        {
            TicketId = _testTicketId.ToString(),
            Ticket = _testTicket,
            RepositoryPath = "/test/repo/path",
            ImplementationPlan = "# Implementation Plan\nTest plan content"
        };

        _mockGitService
            .Setup(x => x.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("feature/test-123-implementation-plan");

        _mockGitService
            .Setup(x => x.CommitAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate revision creation failure
        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(It.IsAny<Guid>(), It.IsAny<PlanRevisionReason>(), It.IsAny<Guid?>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _gitPlanAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        // Workflow should still complete successfully despite revision creation failure
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create initial plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanGenerated_CreatesRevisionWithCorrectReason()
    {
        // Arrange
        var context = new AgentContext
        {
            TicketId = _testTicketId.ToString(),
            Ticket = _testTicket,
            RepositoryPath = "/test/repo/path",
            ImplementationPlan = "# Implementation Plan\nTest plan content"
        };

        _mockGitService
            .Setup(x => x.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("feature/test-123-implementation-plan");

        _mockGitService
            .Setup(x => x.CommitAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var revisionDto = new PlanRevisionDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            RevisionNumber = 1,
            Reason = "Initial",
            CreatedAt = DateTime.UtcNow
        };

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Initial, null))
            .ReturnsAsync(revisionDto);

        // Act
        var result = await _gitPlanAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Verify revision was created with Initial reason
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(
                _testTicketId,
                PlanRevisionReason.Initial,
                null), // No user for AI-generated initial plan
            Times.Once);
    }
}
