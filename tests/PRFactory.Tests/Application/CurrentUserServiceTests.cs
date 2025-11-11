using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using System.Security.Claims;
using Xunit;

namespace PRFactory.Tests.Application;

/// <summary>
/// Comprehensive tests for CurrentUserService that reads current user from HTTP context claims
/// </summary>
public class CurrentUserServiceTests
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), _dbRoot)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        var mockEncryptionService = new Mock<IEncryptionService>();
        // Setup encryption service to pass through values (no actual encryption in tests)
        mockEncryptionService
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns<string>(s => $"encrypted_{s}");
        mockEncryptionService
            .Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns<string>(s => s.StartsWith("encrypted_") ? s.Substring(10) : s);

        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();

        var context = new ApplicationDbContext(options, mockEncryptionService.Object, mockLogger.Object);
        context.Database.EnsureCreated();
        return context;
    }

    private Mock<IHttpContextAccessor> CreateHttpContextAccessor(
        string? prfactoryUserId = null,
        string? prfactoryTenantId = null,
        string? externalAuthId = null,
        bool isAuthenticated = true)
    {
        var claims = new List<Claim>();

        if (prfactoryUserId != null)
            claims.Add(new Claim("prfactory_user_id", prfactoryUserId));

        if (prfactoryTenantId != null)
            claims.Add(new Claim("prfactory_tenant_id", prfactoryTenantId));

        if (externalAuthId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, externalAuthId));

        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        return mockAccessor;
    }

    private CurrentUserService CreateService(
        ApplicationDbContext context,
        Mock<IHttpContextAccessor> mockHttpContextAccessor)
    {
        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<CurrentUserService>>().Object;

        return new CurrentUserService(mockHttpContextAccessor.Object, userRepo, logger);
    }

    #region GetCurrentUserIdAsync Tests

    [Fact]
    public async Task GetCurrentUserIdAsync_WithValidClaims_ReturnsUserId()
    {
        // Arrange
        using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: userId.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_NoClaims_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_InvalidGuid_FallsBackToExternalAuth()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create user with external auth ID
        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-123",
            "AzureAD",
            UserRole.Owner);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: "invalid-guid",
            externalAuthId: "external-auth-123",
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_NoUserIdClaim_FallsBackToExternalAuth()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create user with external auth ID
        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-456",
            "AzureAD",
            UserRole.Member);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockHttpContextAccessor = CreateHttpContextAccessor(
            externalAuthId: "external-auth-456",
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result);
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidClaims_ReturnsUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-123",
            "AzureAD",
            UserRole.Owner);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: user.Id.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoClaims_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var nonExistentUserId = Guid.NewGuid();
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: nonExistentUserId.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_FallbackToExternalAuth_ReturnsUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-789",
            "AzureAD",
            UserRole.Member);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // No prfactory_user_id claim, only external auth
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            externalAuthId: "external-auth-789",
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
    }

    #endregion

    #region GetCurrentTenantIdAsync Tests

    [Fact]
    public async Task GetCurrentTenantIdAsync_WithValidClaims_ReturnsTenantId()
    {
        // Arrange
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryTenantId: tenantId.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result);
    }

    [Fact]
    public async Task GetCurrentTenantIdAsync_FallbackToUser_ReturnsTenantId()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-123",
            "AzureAD",
            UserRole.Owner);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // No prfactory_tenant_id claim, but has user ID
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: user.Id.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result);
    }

    [Fact]
    public async Task GetCurrentTenantIdAsync_NoUser_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentTenantIdAsync_InvalidGuid_FallsBackToUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user = User.Create(
            tenant.Id,
            "user@example.com",
            "Test User",
            null,
            "external-auth-456",
            "AzureAD",
            UserRole.Member);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryTenantId: "invalid-guid",
            prfactoryUserId: user.Id.ToString(),
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result);
    }

    #endregion

    #region IsAuthenticatedAsync Tests

    [Fact]
    public async Task IsAuthenticatedAsync_AuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = CreateHttpContextAccessor(isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_UnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_NoHttpContext_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<CurrentUserService>>().Object;
        var service = new CurrentUserService(mockHttpContextAccessor.Object, userRepo, logger);

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetCurrentUserIdAsync_NullHttpContext_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<CurrentUserService>>().Object;
        var service = new CurrentUserService(mockHttpContextAccessor.Object, userRepo, logger);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NullHttpContext_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<CurrentUserService>>().Object;
        var service = new CurrentUserService(mockHttpContextAccessor.Object, userRepo, logger);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentTenantIdAsync_NullHttpContext_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<CurrentUserService>>().Object;
        var service = new CurrentUserService(mockHttpContextAccessor.Object, userRepo, logger);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithBothUserIdAndExternalAuth_PrefersUserId()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant = Tenant.Create(
            "Test Tenant",
            "AzureAD",
            "tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var user1 = User.Create(
            tenant.Id,
            "user1@example.com",
            "User 1",
            null,
            "external-auth-111",
            "AzureAD",
            UserRole.Owner);
        var user2 = User.Create(
            tenant.Id,
            "user2@example.com",
            "User 2",
            null,
            "external-auth-222",
            "AzureAD",
            UserRole.Member);
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Both prfactory_user_id and external auth ID present
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryUserId: user1.Id.ToString(),
            externalAuthId: "external-auth-222", // Different user's external auth
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user1.Id, result.Id); // Should prefer prfactory_user_id claim
        Assert.Equal("user1@example.com", result.Email);
    }

    [Fact]
    public async Task GetCurrentTenantIdAsync_WithBothTenantIdAndUser_PrefersTenantIdClaim()
    {
        // Arrange
        using var context = CreateDbContext();

        var tenant1 = Tenant.Create(
            "Tenant 1",
            "AzureAD",
            "tenant-id-1",
            "https://company1.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        var tenant2 = Tenant.Create(
            "Tenant 2",
            "AzureAD",
            "tenant-id-2",
            "https://company2.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.AddRange(tenant1, tenant2);

        var user = User.Create(
            tenant2.Id, // User belongs to tenant2
            "user@example.com",
            "Test User",
            null,
            "external-auth-123",
            "AzureAD",
            UserRole.Owner);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Both prfactory_tenant_id and user ID present
        var mockHttpContextAccessor = CreateHttpContextAccessor(
            prfactoryTenantId: tenant1.Id.ToString(), // Claim says tenant1
            prfactoryUserId: user.Id.ToString(), // User belongs to tenant2
            isAuthenticated: true);
        var service = CreateService(context, mockHttpContextAccessor);

        // Act
        var result = await service.GetCurrentTenantIdAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant1.Id, result); // Should prefer prfactory_tenant_id claim
    }

    #endregion
}
