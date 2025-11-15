using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence.Repositories;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for PlanRepository operations
/// </summary>
public class PlanRepositoryTests : TestBase
{
    private readonly PlanRepository _repository;
    private readonly Mock<ILogger<PlanRepository>> _mockLogger;
    private readonly Guid _ticketId = Guid.NewGuid();

    public PlanRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<PlanRepository>>();
        _repository = new PlanRepository(DbContext, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingPlan_ReturnsPlan()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Test plan content");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan.Id, result.Id);
        Assert.Equal(_ticketId, result.TicketId);
        Assert.Equal("Test plan content", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentPlan_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_LoadsVersionHistory()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "V1");
        plan.UpdateArtifacts(userStories: "V2");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Versions);
        Assert.Equal(1, result.Versions[0].Version);
        Assert.Equal("V1", result.Versions[0].UserStories);
    }

    #endregion

    #region GetByTicketIdAsync Tests

    [Fact]
    public async Task GetByTicketIdAsync_WithExistingPlan_ReturnsPlan()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Test plan");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTicketIdAsync(_ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_ticketId, result.TicketId);
        Assert.Equal("Test plan", result.Content);
    }

    [Fact]
    public async Task GetByTicketIdAsync_WithNonExistentTicket_ReturnsNull()
    {
        // Arrange
        var nonExistentTicketId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByTicketIdAsync(nonExistentTicketId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTicketIdAsync_LoadsVersionHistory()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "Original");
        plan.UpdateArtifacts(userStories: "Updated");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByTicketIdAsync(_ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Versions);
        Assert.Equal("Original", result.Versions[0].UserStories);
    }

    #endregion

    #region GetVersionHistoryAsync Tests

    [Fact]
    public async Task GetVersionHistoryAsync_ReturnsAllVersionsOrderedByVersionDescending()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "V1");
        plan.UpdateArtifacts(userStories: "V2");
        plan.UpdateArtifacts(userStories: "V3");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionHistoryAsync(plan.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Version);
        Assert.Equal("V2", result[0].UserStories);
        Assert.Equal(1, result[1].Version);
        Assert.Equal("V1", result[1].UserStories);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_WithNonExistentPlan_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPlanId = Guid.NewGuid();

        // Act
        var result = await _repository.GetVersionHistoryAsync(nonExistentPlanId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_WithPlanWithoutVersions_ReturnsEmptyList()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Original plan");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionHistoryAsync(plan.Id);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetVersionAsync Tests

    [Fact]
    public async Task GetVersionAsync_WithExistingVersion_ReturnsVersion()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "V1");
        plan.UpdateArtifacts(userStories: "V2", createdBy: "user@example.com");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionAsync(plan.Id, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Version);
        Assert.Equal("V1", result.UserStories);
        Assert.Equal("user@example.com", result.CreatedBy);
    }

    [Fact]
    public async Task GetVersionAsync_WithNonExistentVersion_ReturnsNull()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Original");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionAsync(plan.Id, 99);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPlan_AddsPlanToDatabase()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "New plan");

        // Act
        var result = await _repository.AddAsync(plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan.Id, result.Id);

        // Verify it's in the database
        var saved = await DbContext.Plans.FindAsync(plan.Id);
        Assert.NotNull(saved);
        Assert.Equal("New plan", saved.Content);
    }

    [Fact]
    public async Task AddAsync_WithNullPlan_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WithMultiArtifactPlan_SavesAllArtifacts()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(
            _ticketId,
            userStories: "Stories",
            apiDesign: "API",
            databaseSchema: "Schema",
            testCases: "Tests",
            implementationSteps: "Steps");

        // Act
        await _repository.AddAsync(plan);

        // Assert
        var saved = await DbContext.Plans.FindAsync(plan.Id);
        Assert.NotNull(saved);
        Assert.Equal("Stories", saved.UserStories);
        Assert.Equal("API", saved.ApiDesign);
        Assert.Equal("Schema", saved.DatabaseSchema);
        Assert.Equal("Tests", saved.TestCases);
        Assert.Equal("Steps", saved.ImplementationSteps);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidPlan_UpdatesPlanInDatabase()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Original");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Reload the plan
        var loadedPlan = await DbContext.Plans.FindAsync(plan.Id);
        Assert.NotNull(loadedPlan);
        loadedPlan.UpdateContent("Updated");

        // Act
        await _repository.UpdateAsync(loadedPlan);

        // Assert
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Plans.FindAsync(plan.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Content);
    }

    [Fact]
    public async Task UpdateAsync_WithNullPlan_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WithSimpleUpdate_UpdatesPlan()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Original content");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Modify the plan
        plan.UpdateContent("Updated content");

        // Act
        await _repository.UpdateAsync(plan);

        // Assert
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Plans.FirstOrDefaultAsync(p => p.Id == plan.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated content", updated.Content);
        Assert.NotNull(updated.UpdatedAt);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPlan_RemovesPlanFromDatabase()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "To be deleted");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(plan);

        // Assert
        var deleted = await DbContext.Plans.FindAsync(plan.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithNullPlan_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.DeleteAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_CascadeDeletesVersions()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "V1");
        plan.UpdateArtifacts(userStories: "V2");
        await DbContext.Plans.AddAsync(plan);
        await DbContext.SaveChangesAsync();

        var versionCount = await DbContext.PlanVersions.CountAsync(v => v.PlanId == plan.Id);
        Assert.Equal(1, versionCount);

        // Act
        await _repository.DeleteAsync(plan);

        // Assert
        var remainingVersions = await DbContext.PlanVersions.CountAsync(v => v.PlanId == plan.Id);
        Assert.Equal(0, remainingVersions);
    }

    #endregion
}
