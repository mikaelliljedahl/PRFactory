using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Application.Services;

/// <summary>
/// Tests for PlanService revision tracking functionality
/// </summary>
public class PlanServiceRevisionTests
{
    private readonly Mock<ILogger<PlanService>> _mockLogger;
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<IRepositoryRepository> _mockRepoRepo;
    private readonly Mock<IPlanRevisionRepository> _mockPlanRevisionRepo;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public PlanServiceRevisionTests()
    {
        _mockLogger = new Mock<ILogger<PlanService>>();
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockRepoRepo = new Mock<IRepositoryRepository>();
        _mockPlanRevisionRepo = new Mock<IPlanRevisionRepository>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Set up configuration default
        _mockConfiguration.Setup(c => c["Workspace:BasePath"]).Returns("/var/prfactory/workspace");
    }

    private PlanService CreateService()
    {
        return new PlanService(
            _mockLogger.Object,
            _mockTicketRepo.Object,
            _mockRepoRepo.Object,
            _mockPlanRevisionRepo.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task GetPlanRevisionsAsync_ReturnsAllRevisionsForTicket()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revisions = new List<PlanRevision>
        {
            CreatePlanRevision(ticketId, 1, PlanRevisionReason.Initial),
            CreatePlanRevision(ticketId, 2, PlanRevisionReason.Refined),
            CreatePlanRevision(ticketId, 3, PlanRevisionReason.Regenerated)
        };

        _mockPlanRevisionRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(revisions);

        var service = CreateService();

        // Act
        var result = await service.GetPlanRevisionsAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].RevisionNumber);
        Assert.Equal(2, result[1].RevisionNumber);
        Assert.Equal(3, result[2].RevisionNumber);
        _mockPlanRevisionRepo.Verify(x => x.GetByTicketIdAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task GetPlanRevisionAsync_WithValidId_ReturnsRevision()
    {
        // Arrange
        var revisionId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var revision = CreatePlanRevision(ticketId, 1, PlanRevisionReason.Initial);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revisionId))
            .ReturnsAsync(revision);

        var service = CreateService();

        // Act
        var result = await service.GetPlanRevisionAsync(revisionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(revision.Id, result.Id);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal(1, result.RevisionNumber);
        _mockPlanRevisionRepo.Verify(x => x.GetByIdAsync(revisionId), Times.Once);
    }

    [Fact]
    public async Task GetPlanRevisionAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var revisionId = Guid.NewGuid();

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revisionId))
            .ReturnsAsync((PlanRevision?)null);

        var service = CreateService();

        // Act
        var result = await service.GetPlanRevisionAsync(revisionId);

        // Assert
        Assert.Null(result);
        _mockPlanRevisionRepo.Verify(x => x.GetByIdAsync(revisionId), Times.Once);
    }

    [Fact]
    public async Task CompareRevisionsAsync_ReturnsDiffLines()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revision1Id = Guid.NewGuid();
        var revision2Id = Guid.NewGuid();

        var revision1 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 1,
            branchName: "feature/plan-1",
            markdownPath: "PLAN.md",
            commitHash: "abc123",
            content: "Line 1\nLine 2\nLine 3",
            reason: PlanRevisionReason.Initial);

        var revision2 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 2,
            branchName: "feature/plan-2",
            markdownPath: "PLAN.md",
            commitHash: "def456",
            content: "Line 1\nLine 2 Modified\nLine 3",
            reason: PlanRevisionReason.Refined);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision1Id))
            .ReturnsAsync(revision1);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision2Id))
            .ReturnsAsync(revision2);

        var service = CreateService();

        // Act
        var result = await service.CompareRevisionsAsync(revision1Id, revision2Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Revision1);
        Assert.NotNull(result.Revision2);
        Assert.NotNull(result.DiffLines);
        Assert.NotEmpty(result.DiffLines);
    }

    [Fact]
    public async Task CompareRevisionsAsync_WithDifferentTickets_ThrowsException()
    {
        // Arrange
        var ticketId1 = Guid.NewGuid();
        var ticketId2 = Guid.NewGuid();
        var revision1Id = Guid.NewGuid();
        var revision2Id = Guid.NewGuid();

        var revision1 = CreatePlanRevision(ticketId1, 1, PlanRevisionReason.Initial);
        var revision2 = CreatePlanRevision(ticketId2, 1, PlanRevisionReason.Initial);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision1Id))
            .ReturnsAsync(revision1);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision2Id))
            .ReturnsAsync(revision2);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CompareRevisionsAsync(revision1Id, revision2Id));

        Assert.Contains("must be from the same ticket", exception.Message);
    }

    [Fact]
    public async Task GenerateTextDiff_DetectsAddedLines()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revision1Id = Guid.NewGuid();
        var revision2Id = Guid.NewGuid();

        var revision1 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 1,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "abc123",
            content: "Line 1\nLine 2",
            reason: PlanRevisionReason.Initial);

        var revision2 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 2,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "def456",
            content: "Line 1\nLine 2\nLine 3",
            reason: PlanRevisionReason.Refined);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision1Id))
            .ReturnsAsync(revision1);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision2Id))
            .ReturnsAsync(revision2);

        var service = CreateService();

        // Act
        var result = await service.CompareRevisionsAsync(revision1Id, revision2Id);

        // Assert
        Assert.Contains(result.DiffLines, line => line.Type == DiffLineType.Added);
    }

    [Fact]
    public async Task GenerateTextDiff_DetectsRemovedLines()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revision1Id = Guid.NewGuid();
        var revision2Id = Guid.NewGuid();

        var revision1 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 1,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "abc123",
            content: "Line 1\nLine 2\nLine 3",
            reason: PlanRevisionReason.Initial);

        var revision2 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 2,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "def456",
            content: "Line 1\nLine 2",
            reason: PlanRevisionReason.Refined);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision1Id))
            .ReturnsAsync(revision1);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision2Id))
            .ReturnsAsync(revision2);

        var service = CreateService();

        // Act
        var result = await service.CompareRevisionsAsync(revision1Id, revision2Id);

        // Assert
        Assert.Contains(result.DiffLines, line => line.Type == DiffLineType.Removed);
    }

    [Fact]
    public async Task GenerateTextDiff_DetectsModifiedLines()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revision1Id = Guid.NewGuid();
        var revision2Id = Guid.NewGuid();

        var revision1 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 1,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "abc123",
            content: "Line 1\nOriginal Line 2\nLine 3",
            reason: PlanRevisionReason.Initial);

        var revision2 = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: 2,
            branchName: "feature/plan",
            markdownPath: "PLAN.md",
            commitHash: "def456",
            content: "Line 1\nModified Line 2\nLine 3",
            reason: PlanRevisionReason.Refined);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision1Id))
            .ReturnsAsync(revision1);

        _mockPlanRevisionRepo
            .Setup(x => x.GetByIdAsync(revision2Id))
            .ReturnsAsync(revision2);

        var service = CreateService();

        // Act
        var result = await service.CompareRevisionsAsync(revision1Id, revision2Id);

        // Assert
        Assert.Contains(result.DiffLines, line => line.Type == DiffLineType.Modified);
    }

    // Helper method to create test PlanRevision entities
    private PlanRevision CreatePlanRevision(Guid ticketId, int revisionNumber, PlanRevisionReason reason)
    {
        return PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: revisionNumber,
            branchName: $"feature/plan-rev-{revisionNumber}",
            markdownPath: "PLAN.md",
            commitHash: $"commit-hash-{revisionNumber}",
            content: $"# Implementation Plan - Revision {revisionNumber}\n\nThis is revision {revisionNumber}.",
            reason: reason,
            createdByUserId: null);
    }
}
