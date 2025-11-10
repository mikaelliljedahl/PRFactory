using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Application;
using PRFactory.Tests.Builders;
using Xunit;
using DomainWorkflowState = PRFactory.Domain.ValueObjects.WorkflowState;

namespace PRFactory.Tests.Application;

/// <summary>
/// Comprehensive tests for TicketUpdateService covering all business logic paths
/// </summary>
public class TicketUpdateServiceTests
{
    private readonly Mock<ILogger<TicketUpdateService>> _mockLogger;
    private readonly Mock<ITicketUpdateRepository> _mockTicketUpdateRepo;
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<IWorkflowOrchestrator> _mockOrchestrator;

    public TicketUpdateServiceTests()
    {
        _mockLogger = new Mock<ILogger<TicketUpdateService>>();
        _mockTicketUpdateRepo = new Mock<ITicketUpdateRepository>();
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockOrchestrator = new Mock<IWorkflowOrchestrator>();
    }

    private TicketUpdateService CreateService()
    {
        return new TicketUpdateService(
            _mockLogger.Object,
            _mockTicketUpdateRepo.Object,
            _mockTicketRepo.Object,
            _mockOrchestrator.Object);
    }

    #region GetLatestTicketUpdateAsync Tests

    [Fact]
    public async Task GetLatestTicketUpdateAsync_WithValidTicketAndUpdate_ReturnsLatestUpdate()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .WithUpdatedTitle("Updated Title")
            .WithUpdatedDescription("Updated Description")
            .AsDraft()
            .Build();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.GetLatestDraftByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        var service = CreateService();

        // Act
        var result = await service.GetLatestTicketUpdateAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketUpdate.Id, result.Id);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal("Updated Title", result.UpdatedTitle);
        Assert.Equal("Updated Description", result.UpdatedDescription);

        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.GetLatestDraftByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestTicketUpdateAsync_WithTicketNotFound_ReturnsNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        // Act
        var result = await service.GetLatestTicketUpdateAsync(ticketId);

        // Assert
        Assert.Null(result);

        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.GetLatestDraftByTicketIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLatestTicketUpdateAsync_WithNoUpdateFound_ReturnsNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.GetLatestDraftByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);

        var service = CreateService();

        // Act
        var result = await service.GetLatestTicketUpdateAsync(ticketId);

        // Assert
        Assert.Null(result);

        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.GetLatestDraftByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateTicketUpdateAsync Tests

    [Fact]
    public async Task UpdateTicketUpdateAsync_WithValidDraftAndChanges_UpdatesAndReturnsTicketUpdate()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .WithUpdatedTitle("Original Title")
            .WithUpdatedDescription("Original Description")
            .WithAcceptanceCriteria("- Original criteria")
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateTicketUpdateAsync(
            ticketUpdateId,
            "New Title",
            "New Description",
            "- New criteria");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Title", result.UpdatedTitle);
        Assert.Equal("New Description", result.UpdatedDescription);
        Assert.Equal("- New criteria", result.AcceptanceCriteria);

        _mockTicketUpdateRepo.Verify(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(ticketUpdate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_WithTicketUpdateNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdateId = Guid.NewGuid();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateTicketUpdateAsync(ticketUpdateId, "New Title", "New Description", "New Criteria"));

        Assert.Equal($"TicketUpdate {ticketUpdateId} not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_WithNonDraftUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsApproved()  // Not a draft
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateTicketUpdateAsync(ticketUpdateId, "New Title", "New Description", "New Criteria"));

        Assert.Equal("Can only edit draft ticket updates", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_WithAssociatedTicketNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateTicketUpdateAsync(ticketUpdateId, "New Title", "New Description", "New Criteria"));

        Assert.Equal("Associated ticket not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_WithNoChanges_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .WithUpdatedTitle("Original Title")
            .WithUpdatedDescription("Original Description")
            .WithAcceptanceCriteria("- Original criteria")
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert - passing same values as original
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateTicketUpdateAsync(
                ticketUpdateId,
                "Original Title",
                "Original Description",
                "- Original criteria"));

        Assert.Equal("No changes provided", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ApproveTicketUpdateAsync Tests

    [Fact]
    public async Task ApproveTicketUpdateAsync_WithValidDraft_ApprovesAndTriggersWorkflow()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .WithState(DomainWorkflowState.TicketUpdateUnderReview)
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(ticket.Id, It.IsAny<TicketUpdateApprovedMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ApproveTicketUpdateAsync(ticketUpdateId, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsApproved);
        Assert.False(result.IsDraft);
        Assert.NotNull(result.ApprovedAt);

        // Verify ticket state transition
        Assert.Equal(DomainWorkflowState.TicketUpdateApproved, ticket.State);

        // Verify repository calls
        _mockTicketUpdateRepo.Verify(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(ticketUpdate, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);

        // Verify workflow orchestrator was called with correct message
        _mockOrchestrator.Verify(
            x => x.ResumeWorkflowAsync(
                ticketUpdate.TicketId,
                It.Is<TicketUpdateApprovedMessage>(m =>
                    m.TicketId == ticketUpdate.TicketId &&
                    m.TicketUpdateId == ticketUpdateId &&
                    m.ApprovedBy == "test-user"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_WithNoApprovedBy_UsesUnknown()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .WithState(DomainWorkflowState.TicketUpdateUnderReview)
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(ticket.Id, It.IsAny<TicketUpdateApprovedMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act - no approvedBy parameter
        var result = await service.ApproveTicketUpdateAsync(ticketUpdateId);

        // Assert
        Assert.True(result.IsApproved);

        // Verify workflow orchestrator was called with "Unknown" as ApprovedBy
        _mockOrchestrator.Verify(
            x => x.ResumeWorkflowAsync(
                ticketUpdate.TicketId,
                It.Is<TicketUpdateApprovedMessage>(m => m.ApprovedBy == "Unknown"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_WithTicketUpdateNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdateId = Guid.NewGuid();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveTicketUpdateAsync(ticketUpdateId));

        Assert.Equal($"TicketUpdate {ticketUpdateId} not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_WithAssociatedTicketNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveTicketUpdateAsync(ticketUpdateId));

        Assert.Equal("Associated ticket not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_WithAlreadyApprovedUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsApproved()  // Already approved
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveTicketUpdateAsync(ticketUpdateId));

        Assert.Equal("Ticket update is not in a state that can be approved", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RejectTicketUpdateAsync Tests

    [Fact]
    public async Task RejectTicketUpdateAsync_WithRegenerateTrue_RejectsAndTriggersWorkflowRegeneration()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .WithState(DomainWorkflowState.TicketUpdateUnderReview)
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrchestrator
            .Setup(x => x.ResumeWorkflowAsync(ticket.Id, It.IsAny<TicketUpdateRejectedMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.RejectTicketUpdateAsync(
            ticketUpdateId,
            "Needs more detail",
            "test-user",
            regenerate: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Needs more detail", result.RejectionReason);

        // Verify ticket state transition
        Assert.Equal(DomainWorkflowState.TicketUpdateRejected, ticket.State);

        // Verify repository calls
        _mockTicketUpdateRepo.Verify(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(ticketUpdate, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);

        // Verify workflow orchestrator was called with correct message
        _mockOrchestrator.Verify(
            x => x.ResumeWorkflowAsync(
                ticketUpdate.TicketId,
                It.Is<TicketUpdateRejectedMessage>(m =>
                    m.TicketId == ticketUpdate.TicketId &&
                    m.TicketUpdateId == ticketUpdateId &&
                    m.Reason == "Needs more detail"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_WithRegenerateFalse_RejectsWithoutTriggeringWorkflow()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .WithState(DomainWorkflowState.TicketUpdateUnderReview)
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockTicketUpdateRepo
            .Setup(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTicketRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.RejectTicketUpdateAsync(
            ticketUpdateId,
            "Not needed anymore",
            "test-user",
            regenerate: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Not needed anymore", result.RejectionReason);

        // Verify ticket state transition
        Assert.Equal(DomainWorkflowState.TicketUpdateRejected, ticket.State);

        // Verify repository calls
        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(ticketUpdate, It.IsAny<CancellationToken>()), Times.Once);
        _mockTicketRepo.Verify(x => x.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);

        // Verify workflow orchestrator was NOT called
        _mockOrchestrator.Verify(
            x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_WithTicketUpdateNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketUpdateId = Guid.NewGuid();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdate?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectTicketUpdateAsync(ticketUpdateId, "Reason", "user"));

        Assert.Equal($"TicketUpdate {ticketUpdateId} not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_WithAssociatedTicketNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsDraft()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectTicketUpdateAsync(ticketUpdateId, "Reason", "user"));

        Assert.Equal("Associated ticket not found", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_WithNonDraftUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsApproved()  // Not a draft
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectTicketUpdateAsync(ticketUpdateId, "Reason", "user"));

        Assert.Equal("Can only reject draft ticket updates", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_WithAlreadyApprovedUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketUpdateId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var ticketUpdate = new TicketUpdateBuilder()
            .ForTicket(ticketId)
            .AsApproved()
            .Build();

        _mockTicketUpdateRepo
            .Setup(x => x.GetByIdAsync(ticketUpdateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketUpdate.TicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectTicketUpdateAsync(ticketUpdateId, "Reason", "user"));

        Assert.Equal("Can only reject draft ticket updates", exception.Message);

        _mockTicketUpdateRepo.Verify(x => x.UpdateAsync(It.IsAny<TicketUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrator.Verify(x => x.ResumeWorkflowAsync(It.IsAny<Guid>(), It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
