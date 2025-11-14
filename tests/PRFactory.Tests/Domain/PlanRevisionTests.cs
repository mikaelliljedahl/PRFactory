using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class PlanRevisionTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly int _revisionNumber = 1;
    private readonly string _branchName = "feature/test-plan";
    private readonly string _markdownPath = "PLAN.md";
    private readonly string _commitHash = "abc123def456";
    private readonly string _content = "# Implementation Plan\n\nThis is a test plan.";
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsPlanRevision()
    {
        // Act
        var revision = PlanRevision.Create(
            ticketId: _ticketId,
            revisionNumber: _revisionNumber,
            branchName: _branchName,
            markdownPath: _markdownPath,
            commitHash: _commitHash,
            content: _content,
            reason: PlanRevisionReason.Initial,
            createdByUserId: _userId);

        // Assert
        Assert.NotEqual(Guid.Empty, revision.Id);
        Assert.Equal(_ticketId, revision.TicketId);
        Assert.Equal(_revisionNumber, revision.RevisionNumber);
        Assert.Equal(_branchName, revision.BranchName);
        Assert.Equal(_markdownPath, revision.MarkdownPath);
        Assert.Equal(_commitHash, revision.CommitHash);
        Assert.Equal(_content, revision.Content);
        Assert.Equal(PlanRevisionReason.Initial, revision.Reason);
        Assert.Equal(_userId, revision.CreatedByUserId);
        Assert.True(Math.Abs((revision.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Create_WithEmptyBranchName_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PlanRevision.Create(
                ticketId: _ticketId,
                revisionNumber: _revisionNumber,
                branchName: "",
                markdownPath: _markdownPath,
                commitHash: _commitHash,
                content: _content,
                reason: PlanRevisionReason.Initial));

        Assert.Contains("Branch name is required", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyContent_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PlanRevision.Create(
                ticketId: _ticketId,
                revisionNumber: _revisionNumber,
                branchName: _branchName,
                markdownPath: _markdownPath,
                commitHash: _commitHash,
                content: "",
                reason: PlanRevisionReason.Initial));

        Assert.Contains("Content is required", exception.Message);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var revision = PlanRevision.Create(
            ticketId: _ticketId,
            revisionNumber: _revisionNumber,
            branchName: _branchName,
            markdownPath: _markdownPath,
            commitHash: _commitHash,
            content: _content,
            reason: PlanRevisionReason.Initial);

        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.True(revision.CreatedAt >= beforeCreate);
        Assert.True(revision.CreatedAt <= afterCreate);
    }

    [Fact]
    public void Create_AssignsGuidId()
    {
        // Act
        var revision1 = PlanRevision.Create(
            ticketId: _ticketId,
            revisionNumber: _revisionNumber,
            branchName: _branchName,
            markdownPath: _markdownPath,
            commitHash: _commitHash,
            content: _content,
            reason: PlanRevisionReason.Initial);

        var revision2 = PlanRevision.Create(
            ticketId: _ticketId,
            revisionNumber: _revisionNumber,
            branchName: _branchName,
            markdownPath: _markdownPath,
            commitHash: _commitHash,
            content: _content,
            reason: PlanRevisionReason.Initial);

        // Assert
        Assert.NotEqual(Guid.Empty, revision1.Id);
        Assert.NotEqual(Guid.Empty, revision2.Id);
        Assert.NotEqual(revision1.Id, revision2.Id);
    }

    [Fact]
    public void Create_WithNullCreatedByUserId_SetsToNull()
    {
        // Act
        var revision = PlanRevision.Create(
            ticketId: _ticketId,
            revisionNumber: _revisionNumber,
            branchName: _branchName,
            markdownPath: _markdownPath,
            commitHash: _commitHash,
            content: _content,
            reason: PlanRevisionReason.Initial,
            createdByUserId: null);

        // Assert
        Assert.Null(revision.CreatedByUserId);
    }

    [Fact]
    public void Create_WithEmptyTicketId_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PlanRevision.Create(
                ticketId: Guid.Empty,
                revisionNumber: _revisionNumber,
                branchName: _branchName,
                markdownPath: _markdownPath,
                commitHash: _commitHash,
                content: _content,
                reason: PlanRevisionReason.Initial));

        Assert.Contains("Ticket ID is required", exception.Message);
    }

    [Fact]
    public void Create_WithZeroRevisionNumber_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PlanRevision.Create(
                ticketId: _ticketId,
                revisionNumber: 0,
                branchName: _branchName,
                markdownPath: _markdownPath,
                commitHash: _commitHash,
                content: _content,
                reason: PlanRevisionReason.Initial));

        Assert.Contains("Revision number must be positive", exception.Message);
    }
}
