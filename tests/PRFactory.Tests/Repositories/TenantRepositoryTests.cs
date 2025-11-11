using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Repositories;
using PRFactory.Tests.Builders;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for TenantRepository operations
/// </summary>
public class TenantRepositoryTests : TestBase
{
    private readonly TenantRepository _repository;
    private readonly Mock<ILogger<TenantRepository>> _mockLogger;

    public TenantRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<TenantRepository>>();
        _repository = new TenantRepository(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingTenant_ReturnsTenant()
    {
        // Arrange
        var tenant = new TenantBuilder()
            .WithName("Test Tenant")
            .Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Test Tenant", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTenant_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_WithExistingName_ReturnsTenant()
    {
        // Arrange
        var tenant = new TenantBuilder()
            .WithName("Acme Corp")
            .Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Acme Corp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Acme Corp", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("Nonexistent Corp");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTenantsOrderedByName()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Zebra Corp").Build();
        var tenant2 = new TenantBuilder().WithName("Alpha Corp").Build();
        var tenant3 = new TenantBuilder().WithName("Beta Corp").Build();

        DbContext.Tenants.AddRange(tenant1, tenant2, tenant3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha Corp", result[0].Name);
        Assert.Equal("Beta Corp", result[1].Name);
        Assert.Equal("Zebra Corp", result[2].Name);
    }

    [Fact]
    public async Task GetActiveTenantsAsync_ReturnsOnlyActiveTenants()
    {
        // Arrange
        var activeTenant1 = new TenantBuilder().WithName("Active 1").AsActive().Build();
        var activeTenant2 = new TenantBuilder().WithName("Active 2").AsActive().Build();
        var inactiveTenant = new TenantBuilder().WithName("Inactive").AsInactive().Build();

        DbContext.Tenants.AddRange(activeTenant1, activeTenant2, inactiveTenant);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTenantsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.IsActive));
        Assert.Contains(result, t => t.Name == "Active 1");
        Assert.Contains(result, t => t.Name == "Active 2");
        Assert.DoesNotContain(result, t => t.Name == "Inactive");
    }

    [Fact]
    public async Task GetByIdWithRepositoriesAsync_IncludesRepositories()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository1 = new RepositoryBuilder().ForTenant(tenant.Id).WithName("Repo 1").Build();
        var repository2 = new RepositoryBuilder().ForTenant(tenant.Id).WithName("Repo 2").Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.AddRange(repository1, repository2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithRepositoriesAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Repositories.Count);
        Assert.Contains(result.Repositories, r => r.Name == "Repo 1");
        Assert.Contains(result.Repositories, r => r.Name == "Repo 2");
    }

    [Fact]
    public async Task GetByIdWithRepositoriesAndTicketsAsync_IncludesBothRepositoriesAndTickets()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T1")
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T2")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithRepositoriesAndTicketsAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Repositories);
        Assert.Equal(2, result.Tickets.Count);
        Assert.Contains(result.Tickets, t => t.TicketKey == "T1");
        Assert.Contains(result.Tickets, t => t.TicketKey == "T2");
    }

    [Fact]
    public async Task GetByJiraUrlAsync_WithMatchingUrl_ReturnsTenants()
    {
        // Arrange
        var jiraUrl = "https://company.atlassian.net";
        var tenant1 = new TenantBuilder()
            .WithName("Tenant 1")
            .WithTicketPlatformUrl(jiraUrl)
            .Build();
        var tenant2 = new TenantBuilder()
            .WithName("Tenant 2")
            .WithTicketPlatformUrl(jiraUrl)
            .Build();
        var tenant3 = new TenantBuilder()
            .WithName("Tenant 3")
            .WithTicketPlatformUrl("https://different.atlassian.net")
            .Build();

        DbContext.Tenants.AddRange(tenant1, tenant2, tenant3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByJiraUrlAsync(jiraUrl);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Name == "Tenant 1");
        Assert.Contains(result, t => t.Name == "Tenant 2");
        Assert.DoesNotContain(result, t => t.Name == "Tenant 3");
    }

    [Fact]
    public async Task AddAsync_CreatesNewTenant()
    {
        // Arrange
        var tenant = Tenant.Create(
            "New Tenant",
            "AzureAD",
            "test-external-tenant-id",
            "https://new-tenant.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");

        // Act
        var result = await _repository.AddAsync(tenant);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        var savedTenant = await DbContext.Tenants.FindAsync(result.Id);
        Assert.NotNull(savedTenant);
        Assert.Equal("New Tenant", savedTenant.Name);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingTenant()
    {
        // Arrange
        var tenant = new TenantBuilder()
            .WithName("Original Name")
            .Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        tenant.UpdatePlatformSettings(ticketPlatformUrl: "https://updated-url.atlassian.net");
        await _repository.UpdateAsync(tenant);

        // Assert
        var updatedTenant = await DbContext.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updatedTenant);
        Assert.Equal("https://updated-url.atlassian.net", updatedTenant.TicketPlatformUrl);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTenant()
    {
        // Arrange
        var tenant = new TenantBuilder()
            .WithName("To Delete")
            .Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(tenant.Id);

        // Assert
        var deletedTenant = await DbContext.Tenants.FindAsync(tenant.Id);
        Assert.Null(deletedTenant);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - should not throw
        await _repository.DeleteAsync(nonExistentId);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingName_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantBuilder()
            .WithName("Existing Tenant")
            .Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync("Existing Tenant");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentName_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("Nonexistent Tenant");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetActiveInactiveCountsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var active1 = new TenantBuilder().WithName("Active 1").AsActive().Build();
        var active2 = new TenantBuilder().WithName("Active 2").AsActive().Build();
        var active3 = new TenantBuilder().WithName("Active 3").AsActive().Build();
        var inactive1 = new TenantBuilder().WithName("Inactive 1").AsInactive().Build();
        var inactive2 = new TenantBuilder().WithName("Inactive 2").AsInactive().Build();

        DbContext.Tenants.AddRange(active1, active2, active3, inactive1, inactive2);
        await DbContext.SaveChangesAsync();

        // Act
        var (activeCount, inactiveCount) = await _repository.GetActiveInactiveCountsAsync();

        // Assert
        Assert.Equal(3, activeCount);
        Assert.Equal(2, inactiveCount);
    }

    [Fact]
    public async Task UpdateConfiguration_PersistsConfiguration()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();

        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        var newConfig = new TenantConfiguration
        {
            AutoImplementAfterPlanApproval = true,
            MaxRetries = 5,
            ClaudeModel = "claude-sonnet-4-5-20250929",
            MaxTokensPerRequest = 10000
        };

        // Act
        tenant.UpdateConfiguration(newConfig);
        await _repository.UpdateAsync(tenant);

        // Assert
        var updatedTenant = await DbContext.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updatedTenant);
        Assert.True(updatedTenant.Configuration.AutoImplementAfterPlanApproval);
        Assert.Equal(5, updatedTenant.Configuration.MaxRetries);
        Assert.Equal("claude-sonnet-4-5-20250929", updatedTenant.Configuration.ClaudeModel);
        Assert.Equal(10000, updatedTenant.Configuration.MaxTokensPerRequest);
    }
}
