using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Repositories;

/// <summary>
/// Tests for AgentConfigurationRepository implementation.
/// Validates CRUD operations, tenant filtering, and timestamp management.
/// </summary>
public class AgentConfigurationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AgentConfigurationRepository _repository;
    private readonly Mock<ILogger<AgentConfigurationRepository>> _mockLogger;

    public AgentConfigurationRepositoryTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockEncryption = new Mock<PRFactory.Infrastructure.Persistence.Encryption.IEncryptionService>();
        mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => s);
        mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s);

        _mockLogger = new Mock<ILogger<AgentConfigurationRepository>>();

        _context = new ApplicationDbContext(
            options,
            mockEncryption.Object,
            Mock.Of<ILogger<ApplicationDbContext>>());

        _repository = new AgentConfigurationRepository(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByTenantAndNameAsync_WithExistingConfiguration_ReturnsConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateConfiguration(tenantId, agentName);

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantAndNameAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configuration.Id, result.Id);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(agentName, result.AgentName);
    }

    [Fact]
    public async Task GetByTenantAndNameAsync_WithNonExistentConfiguration_ReturnsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";

        // Act
        var result = await _repository.GetByTenantAndNameAsync(tenantId, agentName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantAndNameAsync_WithDifferentTenant_ReturnsNull()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateConfiguration(tenantId1, agentName);

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        // Act - Query with different tenant ID
        var result = await _repository.GetByTenantAndNameAsync(tenantId2, agentName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantAsync_WithMultipleConfigurations_ReturnsAllForTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var config1 = CreateConfiguration(tenantId, "AnalysisAgent");
        var config2 = CreateConfiguration(tenantId, "PlanningAgent");
        var config3 = CreateConfiguration(otherTenantId, "AnalysisAgent");

        _context.AgentConfigurations.AddRange(config1, config2, config3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantAsync(tenantId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(tenantId, c.TenantId));
        Assert.Contains(result, c => c.AgentName == "AnalysisAgent");
        Assert.Contains(result, c => c.AgentName == "PlanningAgent");
    }

    [Fact]
    public async Task GetByTenantAsync_WithNoConfigurations_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByTenantAsync(tenantId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTenantAsync_ReturnsConfigurationsOrderedByName()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        var configZ = CreateConfiguration(tenantId, "ZAgent");
        var configA = CreateConfiguration(tenantId, "AnalysisAgent");
        var configM = CreateConfiguration(tenantId, "MiddleAgent");

        _context.AgentConfigurations.AddRange(configZ, configA, configM);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantAsync(tenantId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("AnalysisAgent", result[0].AgentName);
        Assert.Equal("MiddleAgent", result[1].AgentName);
        Assert.Equal("ZAgent", result[2].AgentName);
    }

    [Fact]
    public async Task CreateAsync_WithValidConfiguration_SetsTimestamps()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = CreateConfiguration(tenantId, "AnalysisAgent");
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _repository.CreateAsync(configuration);
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.InRange(result.CreatedAt, beforeCreate, afterCreate);
        Assert.InRange(result.UpdatedAt, beforeCreate, afterCreate);
        // Allow for microsecond differences in timestamp precision
        Assert.True(Math.Abs((result.CreatedAt - result.UpdatedAt).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task CreateAsync_WithValidConfiguration_PersistsToDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = CreateConfiguration(tenantId, "AnalysisAgent");

        // Act
        var result = await _repository.CreateAsync(configuration);

        // Assert - Verify persisted by querying separately
        var persisted = await _context.AgentConfigurations
            .FirstOrDefaultAsync(c => c.Id == result.Id);

        Assert.NotNull(persisted);
        Assert.Equal(configuration.AgentName, persisted.AgentName);
        Assert.Equal(configuration.Instructions, persisted.Instructions);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingConfiguration_UpdatesTimestamp()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = CreateConfiguration(tenantId, "AnalysisAgent");

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = configuration.UpdatedAt;

        // Wait a bit to ensure timestamp changes
        await Task.Delay(10);

        // Modify configuration
        configuration.Instructions = "Updated instructions";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(configuration);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var updated = await _context.AgentConfigurations.FindAsync(configuration.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated instructions", updated.Instructions);
        Assert.InRange(updated.UpdatedAt, beforeUpdate, afterUpdate);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedProperties_PersistsChanges()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = CreateConfiguration(tenantId, "AnalysisAgent");

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        // Modify multiple properties
        configuration.Instructions = "New instructions";
        configuration.MaxTokens = 16000;
        configuration.Temperature = 0.7f;
        configuration.StreamingEnabled = false;

        // Act
        await _repository.UpdateAsync(configuration);

        // Assert
        var updated = await _context.AgentConfigurations.FindAsync(configuration.Id);
        Assert.NotNull(updated);
        Assert.Equal("New instructions", updated.Instructions);
        Assert.Equal(16000, updated.MaxTokens);
        Assert.Equal(0.7f, updated.Temperature);
        Assert.False(updated.StreamingEnabled);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingConfiguration_RemovesFromDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = CreateConfiguration(tenantId, "AnalysisAgent");

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        var configId = configuration.Id;

        // Act
        await _repository.DeleteAsync(configId);

        // Assert
        var deleted = await _context.AgentConfigurations.FindAsync(configId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentConfiguration_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await _repository.DeleteAsync(nonExistentId);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleConfigurations_AllPersist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config1 = CreateConfiguration(tenantId, "AnalysisAgent");
        var config2 = CreateConfiguration(tenantId, "PlanningAgent");
        var config3 = CreateConfiguration(tenantId, "ImplementationAgent");

        // Act
        await _repository.CreateAsync(config1);
        await _repository.CreateAsync(config2);
        await _repository.CreateAsync(config3);

        // Assert
        var allConfigs = await _repository.GetByTenantAsync(tenantId);
        Assert.Equal(3, allConfigs.Count);
    }

    [Fact]
    public async Task TenantFiltering_IsolatesConfigurationsBetweenTenants()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Config = CreateConfiguration(tenant1Id, "AnalysisAgent");
        var tenant2Config = CreateConfiguration(tenant2Id, "AnalysisAgent");

        await _repository.CreateAsync(tenant1Config);
        await _repository.CreateAsync(tenant2Config);

        // Act
        var tenant1Configs = await _repository.GetByTenantAsync(tenant1Id);
        var tenant2Configs = await _repository.GetByTenantAsync(tenant2Id);

        // Assert
        Assert.Single(tenant1Configs);
        Assert.Single(tenant2Configs);
        Assert.Equal(tenant1Id, tenant1Configs[0].TenantId);
        Assert.Equal(tenant2Id, tenant2Configs[0].TenantId);
    }

    [Fact]
    public async Task CreateAsync_PreservesAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configuration = new AgentConfiguration
        {
            TenantId = tenantId,
            AgentName = "TestAgent",
            Instructions = "Test instructions",
            EnabledTools = "[\"ReadFile\", \"Grep\", \"Write\"]",
            MaxTokens = 10000,
            Temperature = 0.5f,
            StreamingEnabled = false,
            RequiresApproval = true
        };

        // Act
        var result = await _repository.CreateAsync(configuration);

        // Assert
        Assert.Equal("TestAgent", result.AgentName);
        Assert.Equal("Test instructions", result.Instructions);
        Assert.Equal("[\"ReadFile\", \"Grep\", \"Write\"]", result.EnabledTools);
        Assert.Equal(10000, result.MaxTokens);
        Assert.Equal(0.5f, result.Temperature);
        Assert.False(result.StreamingEnabled);
        Assert.True(result.RequiresApproval);
    }

    private AgentConfiguration CreateConfiguration(Guid tenantId, string agentName)
    {
        return new AgentConfiguration
        {
            TenantId = tenantId,
            AgentName = agentName,
            Instructions = $"Instructions for {agentName}",
            EnabledTools = "[\"ReadFile\", \"Grep\"]",
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false
        };
    }
}
