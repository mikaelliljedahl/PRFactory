using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Repositories;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for CheckpointRepository.
/// CRITICAL: Checkpoints enable graphs to suspend and resume at specific points.
/// </summary>
public class CheckpointRepositoryTests : TestBase
{
    private readonly CheckpointRepository _repository;
    private readonly Mock<ILogger<CheckpointRepository>> _mockLogger;

    public CheckpointRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<CheckpointRepository>>();
        _repository = new CheckpointRepository(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveCheckpointAsync_WithNewCheckpoint_SavesSuccessfully()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint = Checkpoint.Create(
            tenantId: tenant.Id,
            ticketId: ticket.Id,
            graphId: "RefinementGraph",
            checkpointId: "after_analysis",
            stateJson: "{\"current_state\":\"analyzing\",\"retry_count\":0}",
            agentName: "AnalysisAgent",
            nextAgentType: "QuestionGenerationAgent");

        // Act
        var result = await _repository.SaveCheckpointAsync(checkpoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(checkpoint.Id, result.Id);
        Assert.Equal("RefinementGraph", result.GraphId);
        Assert.Equal("after_analysis", result.CheckpointId);
        Assert.Equal(CheckpointStatus.Active, result.Status);

        var saved = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.Id == checkpoint.Id);
        Assert.NotNull(saved);
        Assert.Equal(checkpoint.TenantId, saved.TenantId);
        Assert.Equal(checkpoint.TicketId, saved.TicketId);
    }

    [Fact]
    public async Task SaveCheckpointAsync_WithExistingActiveCheckpoint_UpdatesInPlace()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var firstCheckpoint = Checkpoint.Create(
            tenantId: tenant.Id,
            ticketId: ticket.Id,
            graphId: "RefinementGraph",
            checkpointId: "checkpoint_v1",
            stateJson: "{\"retry_count\":0}");

        await _repository.SaveCheckpointAsync(firstCheckpoint);

        // Create a second checkpoint with same ticketId + graphId (should update first)
        var secondCheckpoint = Checkpoint.Create(
            tenantId: tenant.Id,
            ticketId: ticket.Id,
            graphId: "RefinementGraph",
            checkpointId: "checkpoint_v2",
            stateJson: "{\"retry_count\":1}");

        // Act
        await _repository.SaveCheckpointAsync(secondCheckpoint);

        // Assert
        var checkpoints = await DbContext.Checkpoints
            .Where(c => c.TicketId == ticket.Id && c.GraphId == "RefinementGraph" && c.Status == CheckpointStatus.Active)
            .ToListAsync();

        // Should only have 1 active checkpoint (updated in place)
        Assert.Single(checkpoints);
        Assert.Equal("checkpoint_v2", checkpoints[0].CheckpointId);
        Assert.Equal("{\"retry_count\":1}", checkpoints[0].StateJson);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_WithMultipleCheckpoints_ReturnsMostRecent()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        // Create checkpoints at different times (simulated by adding delays in test)
        var checkpoint1 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint1);
        await DbContext.SaveChangesAsync();

        await Task.Delay(10); // Ensure different CreatedAt timestamps

        var checkpoint2 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp2", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint2);
        await DbContext.SaveChangesAsync();

        await Task.Delay(10);

        var checkpoint3 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp3", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestCheckpointAsync(ticket.Id, "RefinementGraph");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cp3", result.CheckpointId); // Most recent
        Assert.True(result.CreatedAt >= checkpoint2.CreatedAt);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_FiltersByTicketIdAndGraphId()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket1 = CreateTestTicket(tenant.Id, "TICKET-1");
        var ticket2 = CreateTestTicket(tenant.Id, "TICKET-2");
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddRangeAsync(ticket1, ticket2);
        await DbContext.SaveChangesAsync();

        var checkpointTicket1Refinement = Checkpoint.Create(tenant.Id, ticket1.Id, "RefinementGraph", "cp1", "{}");
        var checkpointTicket1Planning = Checkpoint.Create(tenant.Id, ticket1.Id, "PlanningGraph", "cp2", "{}");
        var checkpointTicket2Refinement = Checkpoint.Create(tenant.Id, ticket2.Id, "RefinementGraph", "cp3", "{}");

        await DbContext.Checkpoints.AddRangeAsync(checkpointTicket1Refinement, checkpointTicket1Planning, checkpointTicket2Refinement);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestCheckpointAsync(ticket1.Id, "RefinementGraph");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket1.Id, result.TicketId);
        Assert.Equal("RefinementGraph", result.GraphId);
        Assert.Equal("cp1", result.CheckpointId);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_WithNoCheckpoints_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestCheckpointAsync(ticket.Id, "NonExistentGraph");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_OnlyReturnsActiveCheckpoints()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var activeCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "active", "{}");
        await DbContext.Checkpoints.AddAsync(activeCheckpoint);
        await DbContext.SaveChangesAsync();

        await Task.Delay(10);

        var deletedCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "deleted", "{}");
        deletedCheckpoint.MarkAsDeleted();
        await DbContext.Checkpoints.AddAsync(deletedCheckpoint);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestCheckpointAsync(ticket.Id, "RefinementGraph");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("active", result.CheckpointId);
        Assert.Equal(CheckpointStatus.Active, result.Status);
    }

    [Fact]
    public async Task MarkAsResumedAsync_UpdatesStatusAndTimestamp()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.MarkAsResumedAsync(checkpoint.Id);

        // Assert
        var updated = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.Id == checkpoint.Id);
        Assert.NotNull(updated);
        Assert.Equal(CheckpointStatus.Resumed, updated.Status);
        Assert.NotNull(updated.ResumedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task MarkAsResumedAsync_WithNonExistentCheckpoint_LogsWarning()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        await _repository.MarkAsResumedAsync(nonExistentId);

        // Assert - verify logger was called with warning (Moq verification)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("non-existent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCheckpointAsync_MarksAsDeleted()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteCheckpointAsync(checkpoint.Id);

        // Assert (soft delete, not hard delete)
        var deleted = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.Id == checkpoint.Id);
        Assert.NotNull(deleted);
        Assert.Equal(CheckpointStatus.Deleted, deleted.Status);
        Assert.NotNull(deleted.UpdatedAt);
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WithNonExistentCheckpoint_LogsWarning()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        await _repository.DeleteCheckpointAsync(nonExistentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("non-existent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCheckpointsByTicketIdAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint1 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint1);
        await DbContext.SaveChangesAsync();

        await Task.Delay(10);

        var checkpoint2 = Checkpoint.Create(tenant.Id, ticket.Id, "PlanningGraph", "cp2", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint2);
        await DbContext.SaveChangesAsync();

        await Task.Delay(10);

        var checkpoint3 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp3", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint3);
        await DbContext.SaveChangesAsync();

        // Act
        var results = await _repository.GetCheckpointsByTicketIdAsync(ticket.Id);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("cp3", results[0].CheckpointId); // Most recent first
        Assert.Equal("cp2", results[1].CheckpointId);
        Assert.Equal("cp1", results[2].CheckpointId);
    }

    [Fact]
    public async Task GetActiveCheckpointsByTenantAsync_FiltersMultiTenantCorrectly()
    {
        // Arrange
        var tenant1 = CreateTestTenant("Tenant1");
        var tenant2 = CreateTestTenant("Tenant2");
        var ticket1 = CreateTestTicket(tenant1.Id, "TICKET-1");
        var ticket2 = CreateTestTicket(tenant2.Id, "TICKET-2");

        await DbContext.Tenants.AddRangeAsync(tenant1, tenant2);
        await DbContext.Tickets.AddRangeAsync(ticket1, ticket2);
        await DbContext.SaveChangesAsync();

        var checkpoint1Tenant1 = Checkpoint.Create(tenant1.Id, ticket1.Id, "RefinementGraph", "cp1", "{}");
        var checkpoint2Tenant1 = Checkpoint.Create(tenant1.Id, ticket1.Id, "PlanningGraph", "cp2", "{}");
        var checkpoint1Tenant2 = Checkpoint.Create(tenant2.Id, ticket2.Id, "RefinementGraph", "cp3", "{}");

        await DbContext.Checkpoints.AddRangeAsync(checkpoint1Tenant1, checkpoint2Tenant1, checkpoint1Tenant2);
        await DbContext.SaveChangesAsync();

        // Act
        var resultsTenant1 = await _repository.GetActiveCheckpointsByTenantAsync(tenant1.Id);
        var resultsTenant2 = await _repository.GetActiveCheckpointsByTenantAsync(tenant2.Id);

        // Assert - Multi-tenant isolation
        Assert.Equal(2, resultsTenant1.Count);
        Assert.All(resultsTenant1, cp => Assert.Equal(tenant1.Id, cp.TenantId));

        Assert.Single(resultsTenant2);
        Assert.All(resultsTenant2, cp => Assert.Equal(tenant2.Id, cp.TenantId));
    }

    [Fact]
    public async Task GetActiveCheckpointsByTenantAsync_OnlyReturnsActiveCheckpoints()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var activeCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "active", "{}");
        var deletedCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "PlanningGraph", "deleted", "{}");
        deletedCheckpoint.MarkAsDeleted();

        var resumedCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "ImplementationGraph", "resumed", "{}");
        resumedCheckpoint.MarkAsResumed();

        await DbContext.Checkpoints.AddRangeAsync(activeCheckpoint, deletedCheckpoint, resumedCheckpoint);
        await DbContext.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveCheckpointsByTenantAsync(tenant.Id);

        // Assert
        Assert.Single(results);
        Assert.Equal("active", results[0].CheckpointId);
        Assert.Equal(CheckpointStatus.Active, results[0].Status);
    }

    [Fact]
    public async Task GetCheckpointHistoryAsync_ReturnsAllCheckpointsForTicketAndGraph()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint1 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        var checkpoint2 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp2", "{}");
        checkpoint2.MarkAsResumed();

        var checkpoint3 = Checkpoint.Create(tenant.Id, ticket.Id, "PlanningGraph", "cp3", "{}"); // Different graph

        await DbContext.Checkpoints.AddRangeAsync(checkpoint1, checkpoint2, checkpoint3);
        await DbContext.SaveChangesAsync();

        // Act
        var results = await _repository.GetCheckpointHistoryAsync(ticket.Id, "RefinementGraph");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, cp => Assert.Equal("RefinementGraph", cp.GraphId));
        Assert.Contains(results, cp => cp.CheckpointId == "cp1");
        Assert.Contains(results, cp => cp.CheckpointId == "cp2");
    }

    [Fact]
    public async Task ExpireOldCheckpointsAsync_ExpiresCheckpointsOlderThanTimeSpan()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        // Create checkpoint and manually set CreatedAt to past (reflection to bypass private setter)
        var oldCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "old", "{}");
        var oldCreatedAt = DateTime.UtcNow.AddDays(-10);
        typeof(Checkpoint).GetProperty(nameof(Checkpoint.CreatedAt))!.SetValue(oldCheckpoint, oldCreatedAt);

        var recentCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "PlanningGraph", "recent", "{}");

        await DbContext.Checkpoints.AddRangeAsync(oldCheckpoint, recentCheckpoint);
        await DbContext.SaveChangesAsync();

        // Act
        var expiredCount = await _repository.ExpireOldCheckpointsAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, expiredCount);

        var expired = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.CheckpointId == "old");
        Assert.NotNull(expired);
        Assert.Equal(CheckpointStatus.Expired, expired.Status);

        var stillActive = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.CheckpointId == "recent");
        Assert.NotNull(stillActive);
        Assert.Equal(CheckpointStatus.Active, stillActive.Status);
    }

    [Fact]
    public async Task ExpireOldCheckpointsAsync_OnlyExpiresActiveCheckpoints()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var oldActiveCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "old_active", "{}");
        var oldCreatedAt = DateTime.UtcNow.AddDays(-10);
        typeof(Checkpoint).GetProperty(nameof(Checkpoint.CreatedAt))!.SetValue(oldActiveCheckpoint, oldCreatedAt);

        var oldDeletedCheckpoint = Checkpoint.Create(tenant.Id, ticket.Id, "PlanningGraph", "old_deleted", "{}");
        typeof(Checkpoint).GetProperty(nameof(Checkpoint.CreatedAt))!.SetValue(oldDeletedCheckpoint, oldCreatedAt);
        oldDeletedCheckpoint.MarkAsDeleted();

        await DbContext.Checkpoints.AddRangeAsync(oldActiveCheckpoint, oldDeletedCheckpoint);
        await DbContext.SaveChangesAsync();

        // Act
        var expiredCount = await _repository.ExpireOldCheckpointsAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, expiredCount); // Only the Active one should be expired

        var expiredActive = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.CheckpointId == "old_active");
        Assert.Equal(CheckpointStatus.Expired, expiredActive!.Status);

        var stillDeleted = await DbContext.Checkpoints.FirstOrDefaultAsync(c => c.CheckpointId == "old_deleted");
        Assert.Equal(CheckpointStatus.Deleted, stillDeleted!.Status); // Unchanged
    }

    [Fact]
    public async Task GetCheckpointByIdAsync_IncludesTicketAndTenantNavigationProperties()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var checkpoint = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{}");
        await DbContext.Checkpoints.AddAsync(checkpoint);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetCheckpointByIdAsync(checkpoint.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Ticket);
        Assert.NotNull(result.Tenant);
        Assert.Equal(ticket.TicketKey, result.Ticket.TicketKey);
        Assert.Equal(tenant.Name, result.Tenant.Name);
    }

    [Fact]
    public async Task CheckpointSerialization_StateJsonPreservedCorrectly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        var complexStateJson = @"{
            ""current_state"": ""analyzing"",
            ""current_agent"": ""AnalysisAgent"",
            ""retry_count"": 2,
            ""metadata"": {
                ""key1"": ""value1"",
                ""key2"": 42
            }
        }";

        var checkpoint = Checkpoint.Create(
            tenant.Id,
            ticket.Id,
            "RefinementGraph",
            "cp1",
            complexStateJson);

        // Act
        await _repository.SaveCheckpointAsync(checkpoint);
        var retrieved = await _repository.GetLatestCheckpointAsync(ticket.Id, "RefinementGraph");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(complexStateJson, retrieved.StateJson);
    }

    [Fact]
    public async Task ConcurrentCheckpointSaves_HandledCorrectly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var ticket = CreateTestTicket(tenant.Id);
        await DbContext.Tenants.AddAsync(tenant);
        await DbContext.Tickets.AddAsync(ticket);
        await DbContext.SaveChangesAsync();

        // Create two checkpoints for same ticket/graph concurrently
        var checkpoint1 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp1", "{\"version\":1}");
        var checkpoint2 = Checkpoint.Create(tenant.Id, ticket.Id, "RefinementGraph", "cp2", "{\"version\":2}");

        // Act - simulate concurrent saves (in reality, these would be from different threads)
        var task1 = _repository.SaveCheckpointAsync(checkpoint1);
        var task2 = _repository.SaveCheckpointAsync(checkpoint2);
        await Task.WhenAll(task1, task2);

        // Assert - only one Active checkpoint should remain (last one wins)
        var activeCheckpoints = await DbContext.Checkpoints
            .Where(c => c.TicketId == ticket.Id && c.GraphId == "RefinementGraph" && c.Status == CheckpointStatus.Active)
            .ToListAsync();

        Assert.Single(activeCheckpoints);
        // The last saved checkpoint should be the one that remains
        Assert.Contains(activeCheckpoints[0].CheckpointId, new[] { "cp1", "cp2" });
    }

    // Helper methods
    private static Tenant CreateTestTenant(string name = "Test Tenant")
    {
        return Tenant.Create(
            name: name,
            identityProvider: "AzureAD",
            externalTenantId: "test-tenant-id",
            ticketPlatformUrl: "https://test.atlassian.net",
            ticketPlatformApiToken: "test-token",
            claudeApiKey: "test-claude-key",
            ticketPlatform: "Jira");
    }

    private Ticket CreateTestTicket(Guid tenantId, string ticketKey = "TICKET-123")
    {
        var repository = Repository.Create(
            tenantId: tenantId,
            name: "test-repo",
            gitPlatform: "GitHub",
            cloneUrl: "https://github.com/test/repo.git",
            accessToken: "test-token",
            defaultBranch: "main");

        DbContext.Repositories.Add(repository);
        DbContext.SaveChanges();

        return Ticket.Create(
            ticketKey: ticketKey,
            tenantId: tenantId,
            repositoryId: repository.Id,
            ticketSystem: "Jira",
            source: TicketSource.Jira);
    }
}
