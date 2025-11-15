using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class PlanTests
{
    private readonly Guid _ticketId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidTicketId_ReturnsValidPlan()
    {
        // Act
        var plan = Plan.Create(_ticketId);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(_ticketId, plan.TicketId);
        Assert.Equal(1, plan.Version);
        Assert.True(Math.Abs((plan.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(plan.UpdatedAt);
        Assert.Null(plan.Content);
        Assert.False(plan.HasMultipleArtifacts);
        Assert.Empty(plan.Versions);
    }

    [Fact]
    public void Create_WithContent_ReturnsValidPlan()
    {
        // Arrange
        var content = "# Implementation Plan\n\nThis is a test plan.";

        // Act
        var plan = Plan.Create(_ticketId, content);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(content, plan.Content);
        Assert.False(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void Create_WithEmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Plan.Create(Guid.Empty));
        Assert.Contains("ticketId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateWithArtifacts_WithValidInputs_ReturnsValidPlan()
    {
        // Arrange
        var userStories = "# User Stories\n\nStory 1...";
        var apiDesign = "openapi: 3.0.0\n...";
        var databaseSchema = "CREATE TABLE...";
        var testCases = "# Test Cases\n\nTest 1...";
        var implementationSteps = "# Implementation Steps\n\nStep 1...";

        // Act
        var plan = Plan.CreateWithArtifacts(
            _ticketId,
            userStories,
            apiDesign,
            databaseSchema,
            testCases,
            implementationSteps);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(_ticketId, plan.TicketId);
        Assert.Equal(userStories, plan.UserStories);
        Assert.Equal(apiDesign, plan.ApiDesign);
        Assert.Equal(databaseSchema, plan.DatabaseSchema);
        Assert.Equal(testCases, plan.TestCases);
        Assert.Equal(implementationSteps, plan.ImplementationSteps);
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void CreateWithArtifacts_WithEmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Plan.CreateWithArtifacts(Guid.Empty));
        Assert.Contains("ticketId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region HasMultipleArtifacts Tests

    [Fact]
    public void HasMultipleArtifacts_WithUserStories_ReturnsTrue()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "Some user stories");

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithApiDesign_ReturnsTrue()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, apiDesign: "openapi: 3.0.0");

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithDatabaseSchema_ReturnsTrue()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, databaseSchema: "CREATE TABLE...");

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithTestCases_ReturnsTrue()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, testCases: "Test cases");

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithImplementationSteps_ReturnsTrue()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, implementationSteps: "Steps");

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithoutArtifacts_ReturnsFalse()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "Legacy content");

        // Act & Assert
        Assert.False(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithOnlyLegacyContent_ReturnsFalse()
    {
        // Arrange
        var plan = Plan.Create(_ticketId, "# Implementation Plan");

        // Act & Assert
        Assert.False(plan.HasMultipleArtifacts);
    }

    #endregion

    #region UpdateArtifacts Tests

    [Fact]
    public async Task UpdateArtifacts_CreatesVersionAndIncrementsVersion()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(
            _ticketId,
            userStories: "Original stories",
            apiDesign: "Original API");

        Assert.Equal(1, plan.Version);
        Assert.Empty(plan.Versions);

        await Task.Delay(10); // Ensure time difference

        // Act
        plan.UpdateArtifacts(
            userStories: "Updated stories",
            createdBy: "user@example.com",
            revisionReason: "Fixed requirements");

        // Assert
        Assert.Equal(2, plan.Version);
        Assert.Single(plan.Versions);
        Assert.Equal(1, plan.Versions[0].Version);
        Assert.Equal("Original stories", plan.Versions[0].UserStories);
        Assert.Equal("Original API", plan.Versions[0].ApiDesign);
        Assert.Equal("user@example.com", plan.Versions[0].CreatedBy);
        Assert.Equal("Fixed requirements", plan.Versions[0].RevisionReason);
        Assert.Equal("Updated stories", plan.UserStories);
        Assert.Equal("Original API", plan.ApiDesign); // Unchanged
        Assert.NotNull(plan.UpdatedAt);
    }

    [Fact]
    public async Task UpdateArtifacts_OnlyUpdatesSpecifiedArtifacts()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(
            _ticketId,
            userStories: "Original stories",
            apiDesign: "Original API",
            databaseSchema: "Original schema");

        await Task.Delay(10);

        // Act
        plan.UpdateArtifacts(apiDesign: "Updated API");

        // Assert
        Assert.Equal("Original stories", plan.UserStories); // Unchanged
        Assert.Equal("Updated API", plan.ApiDesign); // Updated
        Assert.Equal("Original schema", plan.DatabaseSchema); // Unchanged
    }

    [Fact]
    public async Task UpdateArtifacts_MultipleUpdates_CreatesMultipleVersions()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "V1");

        await Task.Delay(10);

        // Act
        plan.UpdateArtifacts(userStories: "V2");
        await Task.Delay(10);
        plan.UpdateArtifacts(userStories: "V3");

        // Assert
        Assert.Equal(3, plan.Version);
        Assert.Equal(2, plan.Versions.Count);
        Assert.Equal("V1", plan.Versions[0].UserStories);
        Assert.Equal("V2", plan.Versions[1].UserStories);
        Assert.Equal("V3", plan.UserStories);
    }

    [Fact]
    public void UpdateArtifacts_SetsUpdatedAt()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "Original");
        Assert.Null(plan.UpdatedAt);

        // Act
        plan.UpdateArtifacts(userStories: "Updated");

        // Assert
        Assert.NotNull(plan.UpdatedAt);
        Assert.True(Math.Abs((plan.UpdatedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    #endregion

    #region CreateVersion Tests

    [Fact]
    public void CreateVersion_ReturnsSnapshotOfCurrentArtifacts()
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
        var version = plan.CreateVersion("user@example.com", "Test version");

        // Assert
        Assert.NotNull(version);
        Assert.NotEqual(Guid.Empty, version.Id);
        Assert.Equal(plan.Id, version.PlanId);
        Assert.Equal(1, version.Version);
        Assert.Equal("Stories", version.UserStories);
        Assert.Equal("API", version.ApiDesign);
        Assert.Equal("Schema", version.DatabaseSchema);
        Assert.Equal("Tests", version.TestCases);
        Assert.Equal("Steps", version.ImplementationSteps);
        Assert.Equal("user@example.com", version.CreatedBy);
        Assert.Equal("Test version", version.RevisionReason);
        Assert.True(Math.Abs((version.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void CreateVersion_WithoutMetadata_CreatesVersionWithNulls()
    {
        // Arrange
        var plan = Plan.CreateWithArtifacts(_ticketId, userStories: "Stories");

        // Act
        var version = plan.CreateVersion();

        // Assert
        Assert.NotNull(version);
        Assert.Null(version.CreatedBy);
        Assert.Null(version.RevisionReason);
    }

    #endregion

    #region UpdateContent Tests

    [Fact]
    public void UpdateContent_SetsContentAndUpdatedAt()
    {
        // Arrange
        var plan = Plan.Create(_ticketId);
        var content = "# Updated Plan\n\nNew content";

        // Act
        plan.UpdateContent(content);

        // Assert
        Assert.Equal(content, plan.Content);
        Assert.NotNull(plan.UpdatedAt);
        Assert.True(Math.Abs((plan.UpdatedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    #endregion
}
