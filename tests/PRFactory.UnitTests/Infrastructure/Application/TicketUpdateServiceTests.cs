using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using WorkflowState = PRFactory.Domain.ValueObjects.WorkflowState;

namespace PRFactory.UnitTests.Infrastructure.Application;

/// <summary>
/// Tests for TicketUpdateService using real in-memory database and mocked workflow orchestrator.
/// Tests the actual service orchestration and database interactions.
/// </summary>
public class TicketUpdateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITicketUpdateRepository _ticketUpdateRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly Mock<IWorkflowOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<TicketUpdateService>> _mockLogger;
    private readonly TicketUpdateService _service;

    public TicketUpdateServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Mock encryption service (required by DbContext)
        var mockEncryption = new Mock<IEncryptionService>();
        mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => s);
        mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s);

        var mockDbLogger = new Mock<ILogger<ApplicationDbContext>>();

        _dbContext = new ApplicationDbContext(options, mockEncryption.Object, mockDbLogger.Object);

        // Create real repositories using in-memory database
        _ticketUpdateRepository = new TicketUpdateRepository(_dbContext, new Mock<ILogger<TicketUpdateRepository>>().Object);
        _ticketRepository = new TicketRepository(_dbContext, new Mock<ILogger<TicketRepository>>().Object);

        // Mock workflow orchestrator (external dependency)
        _mockOrchestrator = new Mock<IWorkflowOrchestrator>();
        _mockLogger = new Mock<ILogger<TicketUpdateService>>();

        // Create service with real repositories and mocked orchestrator
        _service = new TicketUpdateService(
            _mockLogger.Object,
            _ticketUpdateRepository,
            _ticketRepository,
            _mockOrchestrator.Object);
    }

    [Fact]
    public async Task GetLatestTicketUpdateAsync_TicketExists_ReturnsLatestUpdate()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        // Act
        var result = await _service.GetLatestTicketUpdateAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketUpdate.Id, result.Id);
        Assert.Equal(ticket.Id, result.TicketId);
    }

    [Fact]
    public async Task GetLatestTicketUpdateAsync_TicketDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentTicketId = Guid.NewGuid();

        // Act
        var result = await _service.GetLatestTicketUpdateAsync(nonExistentTicketId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestTicketUpdateAsync_NoTicketUpdates_ReturnsNull()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();

        // Act
        var result = await _service.GetLatestTicketUpdateAsync(ticket.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_ValidDraft_ApprovesAndUpdatesTicketState()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);
        var approvedBy = "test-user";

        _mockOrchestrator
            .Setup(o => o.ResumeWorkflowAsync(
                ticket.Id,
                It.IsAny<TicketUpdateApprovedMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveTicketUpdateAsync(ticketUpdate.Id, approvedBy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsApproved);
        Assert.False(result.IsDraft);
        Assert.NotNull(result.ApprovedAt);

        // Verify ticket state was updated in database
        var updatedTicket = await _ticketRepository.GetByIdAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(WorkflowState.TicketUpdateApproved, updatedTicket.State);

        // Verify workflow orchestrator was called
        _mockOrchestrator.Verify(
            o => o.ResumeWorkflowAsync(
                ticket.Id,
                It.Is<TicketUpdateApprovedMessage>(m =>
                    m.TicketId == ticket.Id &&
                    m.TicketUpdateId == ticketUpdate.Id &&
                    m.ApprovedBy == approvedBy),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_NonExistentTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveTicketUpdateAsync(nonExistentId));
    }

    [Fact]
    public async Task ApproveTicketUpdateAsync_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        // Approve it first
        ticketUpdate.Approve();
        await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveTicketUpdateAsync(ticketUpdate.Id));
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_ValidDraft_RejectsAndUpdatesTicketState()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);
        var reason = "Needs more details";
        var rejectedBy = "test-user";

        _mockOrchestrator
            .Setup(o => o.ResumeWorkflowAsync(
                ticket.Id,
                It.IsAny<TicketUpdateRejectedMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RejectTicketUpdateAsync(ticketUpdate.Id, reason, rejectedBy, regenerate: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reason, result.RejectionReason);
        Assert.True(result.IsDraft); // Remains draft
        Assert.False(result.IsApproved);

        // Verify ticket state was updated in database
        var updatedTicket = await _ticketRepository.GetByIdAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(WorkflowState.TicketUpdateRejected, updatedTicket.State);

        // Verify workflow orchestrator was called (regenerate = true)
        _mockOrchestrator.Verify(
            o => o.ResumeWorkflowAsync(
                ticket.Id,
                It.Is<TicketUpdateRejectedMessage>(m =>
                    m.TicketId == ticket.Id &&
                    m.TicketUpdateId == ticketUpdate.Id &&
                    m.Reason == reason),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_RegenerateFalse_DoesNotTriggerOrchestrator()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);
        var reason = "Not needed";

        // Act
        var result = await _service.RejectTicketUpdateAsync(ticketUpdate.Id, reason, regenerate: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reason, result.RejectionReason);

        // Verify workflow orchestrator was NOT called
        _mockOrchestrator.Verify(
            o => o.ResumeWorkflowAsync(
                It.IsAny<Guid>(),
                It.IsAny<TicketUpdateRejectedMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_NonExistentTicketUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectTicketUpdateAsync(nonExistentId, "reason"));
    }

    [Fact]
    public async Task RejectTicketUpdateAsync_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        // Approve it first
        ticketUpdate.Approve();
        await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectTicketUpdateAsync(ticketUpdate.Id, "reason"));
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_ValidDraft_UpdatesContentSuccessfully()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newAcceptanceCriteria = "New AC";

        // Act
        var result = await _service.UpdateTicketUpdateAsync(
            ticketUpdate.Id,
            newTitle,
            newDescription,
            newAcceptanceCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newTitle, result.UpdatedTitle);
        Assert.Equal(newDescription, result.UpdatedDescription);
        Assert.Equal(newAcceptanceCriteria, result.AcceptanceCriteria);
        Assert.True(result.IsDraft);

        // Verify database was updated
        var dbTicketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdate.Id);
        Assert.NotNull(dbTicketUpdate);
        Assert.Equal(newTitle, dbTicketUpdate.UpdatedTitle);
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_NonDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        // Approve it to make it non-draft
        ticketUpdate.Approve();
        await _ticketUpdateRepository.UpdateAsync(ticketUpdate);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateTicketUpdateAsync(
                ticketUpdate.Id,
                "New Title",
                "New Desc",
                "New AC"));
    }

    [Fact]
    public async Task UpdateTicketUpdateAsync_NoChanges_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = await CreateAndSaveTicketAsync();
        var ticketUpdate = await CreateAndSaveTicketUpdateAsync(ticket.Id);

        // Act & Assert - Try to update with same values
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateTicketUpdateAsync(
                ticketUpdate.Id,
                ticketUpdate.UpdatedTitle,
                ticketUpdate.UpdatedDescription,
                ticketUpdate.AcceptanceCriteria));
    }

    // Helper methods to create test data

    private async Task<Ticket> CreateAndSaveTicketAsync()
    {
        var ticket = Ticket.Create(
            $"TEST-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            Guid.NewGuid());

        ticket.UpdateTicketInfo("Test Title", "Test Description");
        ticket.TransitionTo(WorkflowState.Analyzing);
        ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);

        var savedTicket = await _ticketRepository.AddAsync(ticket);
        return savedTicket;
    }

    private async Task<TicketUpdate> CreateAndSaveTicketUpdateAsync(Guid ticketId)
    {
        var successCriteria = new List<SuccessCriterion>
        {
            new SuccessCriterion(SuccessCriterionCategory.Functional, "Feature works", 0, true)
        };

        var ticketUpdate = TicketUpdate.Create(
            ticketId,
            "Original Title",
            "Original Description",
            successCriteria,
            "Original AC");

        var savedUpdate = await _ticketUpdateRepository.CreateAsync(ticketUpdate);
        return savedUpdate;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
