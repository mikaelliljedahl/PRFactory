using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PRFactory.Tests.Application;

/// <summary>
/// Comprehensive tests for ProvisioningService that auto-provisions tenants and users from external identity providers
/// </summary>
public class ProvisioningServiceTests
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

    private ProvisioningService CreateService(ApplicationDbContext context)
    {
        var tenantRepo = new TenantRepository(context, new Mock<ILogger<TenantRepository>>().Object);
        var userRepo = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var logger = new Mock<ILogger<ProvisioningService>>().Object;

        return new ProvisioningService(tenantRepo, userRepo, logger);
    }

    #region FirstUserFromOrganization Tests

    [Fact]
    public async Task ProvisionUserAsync_FirstUserFromOrganization_CreatesTenantAndOwnerUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe");

        // Assert
        Assert.NotNull(tenant);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("AzureAD", tenant.IdentityProvider);
        Assert.Equal("contoso-tenant-id", tenant.ExternalTenantId);
        Assert.Equal("Contoso", tenant.Name); // Extracted from email domain
        Assert.False(tenant.IsActive); // New tenants start inactive
        Assert.True(isNewTenant);

        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("john@contoso.com", user.Email);
        Assert.Equal("John Doe", user.DisplayName);
        Assert.Equal(UserRole.Owner, user.Role); // First user is Owner
        Assert.Equal(tenant.Id, user.TenantId);
        Assert.Equal("azure-ad-user-123", user.ExternalAuthId);
        Assert.Equal("AzureAD", user.IdentityProvider);
        Assert.True(user.IsActive);

        // Verify saved to database
        var savedTenant = await context.Tenants.FindAsync(tenant.Id);
        var savedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(savedTenant);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task ProvisionUserAsync_FirstUserFromGoogleWorkspace_UsesDomainAsTenantName()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "google-user-456",
            identityProvider: "GoogleWorkspace",
            externalTenantId: "acmecorp.com",
            email: "alice@acmecorp.com",
            displayName: "Alice Smith");

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal("GoogleWorkspace", tenant.IdentityProvider);
        Assert.Equal("acmecorp.com", tenant.ExternalTenantId);
        Assert.Equal("acmecorp.com", tenant.Name); // Uses external tenant ID as name for Google Workspace
        Assert.True(isNewTenant);

        Assert.NotNull(user);
        Assert.Equal("alice@acmecorp.com", user.Email);
        Assert.Equal(UserRole.Owner, user.Role); // First user is Owner
        Assert.Equal("GoogleWorkspace", user.IdentityProvider);
    }

    #endregion

    #region SecondUserFromOrganization Tests

    [Fact]
    public async Task ProvisionUserAsync_SecondUserFromOrganization_UsesSameTenantCreatesMemberUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Create first user (Owner)
        var (tenant, firstUser, _) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe");

        // Act - Create second user from same organization
        var (tenant2, secondUser, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-456",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "jane@contoso.com",
            displayName: "Jane Smith");

        // Assert
        Assert.NotNull(tenant2);
        Assert.Equal(tenant.Id, tenant2.Id); // Same tenant
        Assert.False(isNewTenant); // Existing tenant

        Assert.NotNull(secondUser);
        Assert.Equal(tenant.Id, secondUser.TenantId); // Same tenant as first user
        Assert.Equal("jane@contoso.com", secondUser.Email);
        Assert.Equal("Jane Smith", secondUser.DisplayName);
        Assert.Equal(UserRole.Member, secondUser.Role); // Second user is Member, not Owner
        Assert.Equal("azure-ad-user-456", secondUser.ExternalAuthId);

        // Verify both users exist for the same tenant
        var usersInTenant = await context.Users.Where(u => u.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(2, usersInTenant.Count);
        Assert.Contains(usersInTenant, u => u.Role == UserRole.Owner);
        Assert.Contains(usersInTenant, u => u.Role == UserRole.Member);
    }

    #endregion

    #region ExistingUser Tests

    [Fact]
    public async Task ProvisionUserAsync_ExistingUser_UpdatesLastSeen()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Create user first time
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe");

        var originalLastSeen = user.LastSeenAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(10);

        // Act - Provision same user again (simulates login)
        var (tenant2, user2, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe");

        // Assert
        Assert.Equal(user.Id, user2.Id); // Same user
        Assert.Equal(tenant.Id, tenant2.Id); // Same tenant
        Assert.False(isNewTenant);
        Assert.NotNull(user2.LastSeenAt);
        Assert.NotEqual(originalLastSeen, user2.LastSeenAt); // LastSeen updated
    }

    [Fact]
    public async Task ProvisionUserAsync_ExistingUser_UpdatesProfileIfChanged()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Create user first time
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe",
            avatarUrl: "https://avatar.com/old.jpg");

        // Act - Provision same user again with updated profile
        var (tenant2, user2, _) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Smith", // Changed name
            avatarUrl: "https://avatar.com/new.jpg"); // Changed avatar

        // Assert
        Assert.Equal(user.Id, user2.Id); // Same user
        Assert.Equal("John Smith", user2.DisplayName); // Updated name
        Assert.Equal("https://avatar.com/new.jpg", user2.AvatarUrl); // Updated avatar
    }

    #endregion

    #region UserExistsByEmail Tests

    [Fact]
    public async Task ProvisionUserAsync_UserExistsByEmail_LinksExternalAuth()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Create tenant and user manually (simulates user created without external auth)
        var tenant = Tenant.Create(
            "Contoso",
            "AzureAD",
            "contoso-tenant-id",
            "https://company.atlassian.net",
            "api-token",
            "claude-key",
            "Jira");
        context.Tenants.Add(tenant);

        var existingUser = new User(tenant.Id, "john@contoso.com", "John Doe");
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        Assert.Null(existingUser.ExternalAuthId); // No external auth initially

        // Act - Provision with external auth
        var (tenant2, user2, _) = await service.ProvisionUserAsync(
            externalUserId: "azure-ad-user-123",
            identityProvider: "AzureAD",
            externalTenantId: "contoso-tenant-id",
            email: "john@contoso.com",
            displayName: "John Doe");

        // Assert
        Assert.Equal(existingUser.Id, user2.Id); // Same user
        Assert.Equal("azure-ad-user-123", user2.ExternalAuthId); // External auth linked
        Assert.Equal("AzureAD", user2.IdentityProvider);
    }

    #endregion

    #region PersonalAccount Tests

    [Fact]
    public async Task ProvisionUserAsync_PersonalAccount_CreatesPersonalTenant()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act - No external tenant ID (personal account)
        var (tenant, user, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "personal-user-789",
            identityProvider: "Personal",
            externalTenantId: null, // No external tenant ID
            email: "user@gmail.com",
            displayName: "Personal User");

        // Assert
        Assert.NotNull(tenant);
        Assert.True(isNewTenant);
        Assert.Equal("Personal", tenant.IdentityProvider);
        Assert.Equal("user@gmail.com", tenant.ExternalTenantId); // Uses email as external tenant ID
        Assert.Equal("gmail.com", tenant.Name); // Uses email domain as name

        Assert.NotNull(user);
        Assert.Equal(UserRole.Owner, user.Role); // Personal account user is Owner
        Assert.Equal("user@gmail.com", user.Email);
        Assert.Equal("Personal", user.IdentityProvider);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ProvisionUserAsync_NullExternalUserId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: null!,
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: "user@example.com",
                displayName: "User"));

        Assert.Contains("External user ID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ProvisionUserAsync_EmptyExternalUserId_ThrowsArgumentException(string invalidUserId)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: invalidUserId,
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: "user@example.com",
                displayName: "User"));

        Assert.Contains("External user ID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProvisionUserAsync_NullIdentityProvider_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: "user-123",
                identityProvider: null!,
                externalTenantId: "tenant-id",
                email: "user@example.com",
                displayName: "User"));

        Assert.Contains("Identity provider", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProvisionUserAsync_NullEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: "user-123",
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: null!,
                displayName: "User"));

        Assert.Contains("Email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ProvisionUserAsync_EmptyEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: "user-123",
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: invalidEmail,
                displayName: "User"));

        Assert.Contains("Email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProvisionUserAsync_NullDisplayName_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: "user-123",
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: "user@example.com",
                displayName: null!));

        Assert.Contains("Display name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ProvisionUserAsync_EmptyDisplayName_ThrowsArgumentException(string invalidDisplayName)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProvisionUserAsync(
                externalUserId: "user-123",
                identityProvider: "AzureAD",
                externalTenantId: "tenant-id",
                email: "user@example.com",
                displayName: invalidDisplayName));

        Assert.Contains("Display name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region AvatarUrl Tests

    [Fact]
    public async Task ProvisionUserAsync_WithAvatarUrl_SetsAvatarUrl()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "user@example.com",
            displayName: "User",
            avatarUrl: "https://avatar.example.com/user.jpg");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("https://avatar.example.com/user.jpg", user.AvatarUrl);
    }

    [Fact]
    public async Task ProvisionUserAsync_WithoutAvatarUrl_AvatarUrlIsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "user@example.com",
            displayName: "User");

        // Assert
        Assert.NotNull(user);
        Assert.Null(user.AvatarUrl);
    }

    #endregion

    #region EmailNormalization Tests

    [Fact]
    public async Task ProvisionUserAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "User@EXAMPLE.COM",
            displayName: "User");

        // Assert
        Assert.Equal("user@example.com", user.Email); // Normalized to lowercase
    }

    [Fact]
    public async Task ProvisionUserAsync_FindsExistingUserByNormalizedEmail()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Create user with lowercase email
        var (tenant, user1, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "user@example.com",
            displayName: "User");

        // Act - Try to create user with uppercase email
        var (tenant2, user2, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "USER@EXAMPLE.COM", // Uppercase
            displayName: "User");

        // Assert
        Assert.Equal(user1.Id, user2.Id); // Same user found by normalized email
    }

    #endregion

    #region TenantCreation Tests

    [Fact]
    public async Task ProvisionUserAsync_NewTenantStartsInactive()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, isNewTenant) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "user@example.com",
            displayName: "User");

        // Assert
        Assert.True(isNewTenant);
        Assert.False(tenant.IsActive); // New tenants require configuration before activation
    }

    [Fact]
    public async Task ProvisionUserAsync_NewTenantHasPlaceholderCredentials()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var (tenant, user, _) = await service.ProvisionUserAsync(
            externalUserId: "user-123",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-id",
            email: "user@example.com",
            displayName: "User");

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal("https://placeholder.example.com", tenant.TicketPlatformUrl);
        Assert.Equal("PLACEHOLDER_TOKEN", tenant.TicketPlatformApiToken);
        // Claude API key may be from environment or placeholder
        Assert.False(string.IsNullOrWhiteSpace(tenant.ClaudeApiKey));
    }

    #endregion
}
