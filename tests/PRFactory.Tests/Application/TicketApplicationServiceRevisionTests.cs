using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Application;
using Xunit;

// Type alias to resolve ambiguity between domain and graph WorkflowState
using WorkflowState = PRFactory.Domain.ValueObjects.WorkflowState;

namespace PRFactory.Tests.Application;

/// <summary>
/// Tests for plan revision tracking integration in TicketApplicationService
/// </summary>
public class TicketApplicationServiceRevisionTests
{
    private readonly Mock<ILogger<TicketApplicationService>> _mockLogger;
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IRepositoryRepository> _mockRepositoryRepository;
    private readonly Mock<IWorkflowOrchestrator> _mockWorkflowOrchestrator;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IPlanService> _mockPlanService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly TicketApplicationService _ticketService;
    private readonly Guid _testTicketId;
    private readonly Guid _testUserId;
    private readonly Ticket _testTicket;

    public TicketApplicationServiceRevisionTests()
    {
        _mockLogger = new Mock<ILogger<TicketApplicationService>>();
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockRepositoryRepository = new Mock<IRepositoryRepository>();
        _mockWorkflowOrchestrator = new Mock<IWorkflowOrchestrator>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockPlanService = new Mock<IPlanService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _testTicketId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        // Create test ticket in PlanUnderReview state
        _testTicket = Ticket.Create(
            ticketKey: "TEST-123",
            tenantId: Guid.NewGuid(),
            repositoryId: Guid.NewGuid(),
            ticketSystem: "Jira");

        // Transition to PlanUnderReview state through valid state transitions
        _testTicket.TransitionTo(WorkflowState.Analyzing);
        _testTicket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        _testTicket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        _testTicket.TransitionTo(WorkflowState.TicketUpdateApproved);
        _testTicket.TransitionTo(WorkflowState.TicketUpdatePosted);
        _testTicket.TransitionTo(WorkflowState.Planning);
        _testTicket.TransitionTo(WorkflowState.PlanPosted);
        _testTicket.TransitionTo(WorkflowState.PlanUnderReview);

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicket);

        _mockCurrentUserService
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(_testUserId);

        _ticketService = new TicketApplicationService(
            _mockLogger.Object,
            _mockTicketRepository.Object,
            _mockRepositoryRepository.Object,
            _mockWorkflowOrchestrator.Object,
            _mockTenantContext.Object,
            _mockPlanService.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task PlanRefined_CreatesRefinedRevision()
    {
        // Arrange
        var refinementInstructions = "Please add error handling";
        var revisionDto = new PlanRevisionDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            RevisionNumber = 2,
            Reason = "Refined",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Refined, _testUserId))
            .ReturnsAsync(revisionDto);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RefinePlanAsync(_testTicketId, refinementInstructions);

        // Assert
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Refined, _testUserId),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created refined plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanRefined_WhenRevisionCreationFails_DoesNotFailWorkflow()
    {
        // Arrange
        var refinementInstructions = "Please add error handling";

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(It.IsAny<Guid>(), It.IsAny<PlanRevisionReason>(), It.IsAny<Guid?>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RefinePlanAsync(_testTicketId, refinementInstructions);

        // Assert - should not throw exception
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create refined plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanRegenerated_CreatesRegeneratedRevision()
    {
        // Arrange
        var rejectionReason = "Plan needs complete restructure";
        var revisionDto = new PlanRevisionDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            RevisionNumber = 3,
            Reason = "Regenerated",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Regenerated, _testUserId))
            .ReturnsAsync(revisionDto);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RejectPlanAsync(_testTicketId, rejectionReason, regenerateCompletely: true);

        // Assert
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Regenerated, _testUserId),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created regenerated plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanRejected_WithoutRegeneration_DoesNotCreateRevision()
    {
        // Arrange
        var rejectionReason = "Plan needs minor updates";

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RejectPlanAsync(_testTicketId, rejectionReason, regenerateCompletely: false);

        // Assert - CreateRevisionAsync should NOT be called
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(It.IsAny<Guid>(), It.IsAny<PlanRevisionReason>(), It.IsAny<Guid?>()),
            Times.Never);
    }

    [Fact]
    public async Task PlanRegenerated_WhenRevisionCreationFails_DoesNotFailWorkflow()
    {
        // Arrange
        var rejectionReason = "Plan needs complete restructure";

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(It.IsAny<Guid>(), It.IsAny<PlanRevisionReason>(), It.IsAny<Guid?>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RejectPlanAsync(_testTicketId, rejectionReason, regenerateCompletely: true);

        // Assert - should not throw exception
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create regenerated plan revision")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanRefined_UsesCurrentUserIdAsCreatedBy()
    {
        // Arrange
        var refinementInstructions = "Please add error handling";
        var revisionDto = new PlanRevisionDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            RevisionNumber = 2,
            Reason = "Refined",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockPlanService
            .Setup(x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Refined, _testUserId))
            .ReturnsAsync(revisionDto);

        _mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWorkflowOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _ticketService.RefinePlanAsync(_testTicketId, refinementInstructions);

        // Assert
        _mockCurrentUserService.Verify(x => x.GetCurrentUserIdAsync(), Times.Once);
        _mockPlanService.Verify(
            x => x.CreateRevisionAsync(_testTicketId, PlanRevisionReason.Refined, _testUserId),
            Times.Once);
    }
}
