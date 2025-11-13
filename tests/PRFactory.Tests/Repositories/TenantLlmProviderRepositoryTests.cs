using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence.Repositories;
using PRFactory.Tests.Builders;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for TenantLlmProviderRepository operations
/// </summary>
public class TenantLlmProviderRepositoryTests : TestBase
{
    private readonly TenantLlmProviderRepository _repository;
    private readonly Mock<ILogger<TenantLlmProviderRepository>> _mockLogger;

    public TenantLlmProviderRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<TenantLlmProviderRepository>>();
        _repository = new TenantLlmProviderRepository(DbContext, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingProvider_ReturnsProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenant.Id,
            "Production Claude",
            "claude-sonnet-4-5-20250929");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(provider.Id, result.Id);
        Assert.Equal("Production Claude", result.Name);
        Assert.Equal(LlmProviderType.AnthropicNative, result.ProviderType);
        Assert.True(result.UsesOAuth);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentProvider_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTenantNavigation()
    {
        // Arrange
        var tenant = new TenantBuilder().WithName("Test Tenant Corp").Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Provider 1");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Tenant);
        Assert.Equal("Test Tenant Corp", result.Tenant.Name);
    }

    #endregion

    #region GetByTenantIdAsync Tests

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsAllProvidersForTenant()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var provider1 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Provider A");
        var provider2 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Provider B");
        var provider3 = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "Provider C");

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(provider1, provider2, provider3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Provider A");
        Assert.Contains(result, p => p.Name == "Provider B");
        Assert.DoesNotContain(result, p => p.Name == "Provider C");
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsOrderedByName()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var providerZ = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Z Provider");
        var providerA = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "A Provider");
        var providerM = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "M Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.AddRange(providerZ, providerA, providerM);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("A Provider", result[0].Name);
        Assert.Equal("M Provider", result[1].Name);
        Assert.Equal("Z Provider", result[2].Name);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithNoProviders_ReturnsEmptyList()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_IncludesActiveAndInactiveProviders()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var activeProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Active Provider");
        var inactiveProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Inactive Provider");
        inactiveProvider.Deactivate();

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.AddRange(activeProvider, inactiveProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Active Provider" && p.IsActive);
        Assert.Contains(result, p => p.Name == "Inactive Provider" && !p.IsActive);
    }

    #endregion

    #region GetDefaultProviderAsync Tests

    [Fact]
    public async Task GetDefaultProviderAsync_ReturnsDefaultProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var defaultProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Default Provider");
        defaultProvider.SetAsDefault();
        var otherProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Other Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.AddRange(defaultProvider, otherProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultProviderAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultProvider.Id, result.Id);
        Assert.Equal("Default Provider", result.Name);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task GetDefaultProviderAsync_WithNoDefaultProvider_ReturnsNull()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Non-Default Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultProviderAsync(tenant.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDefaultProviderAsync_OnlyReturnsActiveDefaultProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var inactiveDefaultProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Inactive Default");
        inactiveDefaultProvider.SetAsDefault();
        inactiveDefaultProvider.Deactivate();

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(inactiveDefaultProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultProviderAsync(tenant.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDefaultProviderAsync_IsolatesByTenant()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var tenant1Provider = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Tenant 1 Default");
        tenant1Provider.SetAsDefault();

        var tenant2Provider = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "Tenant 2 Default");
        tenant2Provider.SetAsDefault();

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(tenant1Provider, tenant2Provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultProviderAsync(tenant1.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tenant 1 Default", result.Name);
        Assert.Equal(tenant1.Id, result.TenantId);
    }

    #endregion

    #region GetActiveProvidersAsync Tests

    [Fact]
    public async Task GetActiveProvidersAsync_ReturnsOnlyActiveProviders()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var activeProvider1 = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Active 1");
        var activeProvider2 = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Active 2");
        var inactiveProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Inactive");
        inactiveProvider.Deactivate();

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.AddRange(activeProvider1, activeProvider2, inactiveProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveProvidersAsync(tenant.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsActive));
        Assert.Contains(result, p => p.Name == "Active 1");
        Assert.Contains(result, p => p.Name == "Active 2");
        Assert.DoesNotContain(result, p => p.Name == "Inactive");
    }

    [Fact]
    public async Task GetActiveProvidersAsync_OrdersByDefaultThenName()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var providerA = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "A Provider");
        var providerB = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "B Provider");
        var defaultProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Z Default Provider");
        defaultProvider.SetAsDefault();

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.AddRange(providerA, providerB, defaultProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveProvidersAsync(tenant.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Z Default Provider", result[0].Name); // Default first
        Assert.True(result[0].IsDefault);
        Assert.Equal("A Provider", result[1].Name); // Then alphabetical
        Assert.Equal("B Provider", result[2].Name);
    }

    [Fact]
    public async Task GetActiveProvidersAsync_IsolatesByTenant()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var tenant1Provider = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Tenant 1 Provider");
        var tenant2Provider = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "Tenant 2 Provider");

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(tenant1Provider, tenant2Provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveProvidersAsync(tenant1.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Tenant 1 Provider", result[0].Name);
        Assert.Equal(tenant1.Id, result[0].TenantId);
    }

    [Fact]
    public async Task GetActiveProvidersAsync_WithNoActiveProviders_ReturnsEmptyList()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var inactiveProvider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Inactive Provider");
        inactiveProvider.Deactivate();

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(inactiveProvider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveProvidersAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_CreatesOAuthProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenant.Id,
            "New OAuth Provider",
            "claude-sonnet-4-5-20250929");

        // Act
        await _repository.AddAsync(provider);

        // Assert
        var savedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(savedProvider);
        Assert.Equal("New OAuth Provider", savedProvider.Name);
        Assert.Equal(LlmProviderType.AnthropicNative, savedProvider.ProviderType);
        Assert.True(savedProvider.UsesOAuth);
        Assert.True(savedProvider.IsActive);
        Assert.False(savedProvider.IsDefault);
    }

    [Fact]
    public async Task AddAsync_CreatesApiKeyProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        var config = new ApiKeyProviderConfiguration(
            tenant.Id,
            "Z.ai Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-api-key",
            "claude-sonnet-4-5",
            300000,
            false,
            null);

        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Act
        await _repository.AddAsync(provider);

        // Assert
        var savedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(savedProvider);
        Assert.Equal("Z.ai Provider", savedProvider.Name);
        Assert.Equal(LlmProviderType.ZAi, savedProvider.ProviderType);
        Assert.False(savedProvider.UsesOAuth);
        Assert.Equal("https://api.z.ai", savedProvider.ApiBaseUrl);
        Assert.Equal("encrypted-api-key", savedProvider.EncryptedApiToken);
    }

    [Fact]
    public async Task AddAsync_CreatesMinimaxM2Provider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        var modelOverrides = new Dictionary<string, string>
        {
            { "small_fast_model", "MiniMax-M2" },
            { "default_sonnet_model", "MiniMax-M2" }
        };

        var config = new ApiKeyProviderConfiguration(
            tenant.Id,
            "Minimax Provider",
            LlmProviderType.MinimaxM2,
            "https://api.minimax.chat",
            "encrypted-minimax-key",
            "MiniMax-M2",
            300000,
            true,
            modelOverrides);

        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Act
        await _repository.AddAsync(provider);

        // Assert
        var savedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(savedProvider);
        Assert.Equal(LlmProviderType.MinimaxM2, savedProvider.ProviderType);
        Assert.True(savedProvider.DisableNonEssentialTraffic);
        Assert.NotNull(savedProvider.ModelOverrides);
        Assert.Equal(2, savedProvider.ModelOverrides.Count);
        Assert.Equal("MiniMax-M2", savedProvider.ModelOverrides["small_fast_model"]);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ModifiesExistingProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Original Name");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        provider.UpdateConfiguration(name: "Updated Name");
        await _repository.UpdateAsync(provider);

        // Assert
        var updatedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(updatedProvider);
        Assert.Equal("Updated Name", updatedProvider.Name);
        Assert.NotNull(updatedProvider.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesApiToken()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        provider.UpdateApiToken("new-encrypted-token", isOAuthToken: true);
        await _repository.UpdateAsync(provider);

        // Assert
        var updatedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(updatedProvider);
        Assert.Equal("new-encrypted-token", updatedProvider.EncryptedApiToken);
        Assert.NotNull(updatedProvider.OAuthTokenRefreshedAt);
    }

    [Fact]
    public async Task UpdateAsync_TogglesDefaultStatus()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        provider.SetAsDefault();
        await _repository.UpdateAsync(provider);

        // Assert
        var updatedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(updatedProvider);
        Assert.True(updatedProvider.IsDefault);
    }

    [Fact]
    public async Task UpdateAsync_TogglesActiveStatus()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "Provider");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        provider.Deactivate();
        await _repository.UpdateAsync(provider);

        // Assert
        var updatedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(updatedProvider);
        Assert.False(updatedProvider.IsActive);

        // Reactivate
        provider.Activate();
        await _repository.UpdateAsync(provider);

        updatedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.NotNull(updatedProvider);
        Assert.True(updatedProvider.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesProvider()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "To Delete");

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(provider.Id);

        // Assert
        var deletedProvider = await DbContext.TenantLlmProviders.FindAsync(provider.Id);
        Assert.Null(deletedProvider);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exception = await Record.ExceptionAsync(async () => await _repository.DeleteAsync(nonExistentId));

        // Assert - should not throw
        Assert.Null(exception);
    }

    #endregion

    #region Multi-Tenant Isolation Tests

    [Fact]
    public async Task MultiTenantIsolation_GetByIdAsync_OnlyReturnsTenantOwnedProvider()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var provider1 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Tenant 1 Provider");
        var provider2 = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "Tenant 2 Provider");

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(provider1, provider2);
        await DbContext.SaveChangesAsync();

        // Act - Tenant 1 gets their provider
        var result1 = await _repository.GetByIdAsync(provider1.Id);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(tenant1.Id, result1.TenantId);

        // Act - Tenant 2 cannot access Tenant 1's provider via TenantId filter
        var tenant2Providers = await _repository.GetByTenantIdAsync(tenant2.Id);

        // Assert
        Assert.DoesNotContain(tenant2Providers, p => p.Id == provider1.Id);
    }

    [Fact]
    public async Task MultiTenantIsolation_AllMethodsRespectTenantBoundaries()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var tenant1Provider1 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "T1 Provider 1");
        tenant1Provider1.SetAsDefault();
        var tenant1Provider2 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "T1 Provider 2");

        var tenant2Provider = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "T2 Provider");
        tenant2Provider.SetAsDefault();

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(tenant1Provider1, tenant1Provider2, tenant2Provider);
        await DbContext.SaveChangesAsync();

        // Act & Assert - GetByTenantIdAsync
        var tenant1All = await _repository.GetByTenantIdAsync(tenant1.Id);
        Assert.Equal(2, tenant1All.Count);
        Assert.All(tenant1All, p => Assert.Equal(tenant1.Id, p.TenantId));

        // Act & Assert - GetDefaultProviderAsync
        var tenant1Default = await _repository.GetDefaultProviderAsync(tenant1.Id);
        Assert.NotNull(tenant1Default);
        Assert.Equal(tenant1.Id, tenant1Default.TenantId);
        Assert.Equal("T1 Provider 1", tenant1Default.Name);

        var tenant2Default = await _repository.GetDefaultProviderAsync(tenant2.Id);
        Assert.NotNull(tenant2Default);
        Assert.Equal(tenant2.Id, tenant2Default.TenantId);
        Assert.Equal("T2 Provider", tenant2Default.Name);

        // Act & Assert - GetActiveProvidersAsync
        var tenant1Active = await _repository.GetActiveProvidersAsync(tenant1.Id);
        Assert.Equal(2, tenant1Active.Count);
        Assert.All(tenant1Active, p => Assert.Equal(tenant1.Id, p.TenantId));

        var tenant2Active = await _repository.GetActiveProvidersAsync(tenant2.Id);
        Assert.Single(tenant2Active);
        Assert.Equal(tenant2.Id, tenant2Active[0].TenantId);
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public async Task Repository_HandlesMultipleProvidersWithSameName()
    {
        // Arrange - Different tenants can have providers with same name
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();

        var provider1 = TenantLlmProvider.CreateOAuthProvider(tenant1.Id, "Production Claude");
        var provider2 = TenantLlmProvider.CreateOAuthProvider(tenant2.Id, "Production Claude");

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.TenantLlmProviders.AddRange(provider1, provider2);
        await DbContext.SaveChangesAsync();

        // Act
        var tenant1Providers = await _repository.GetByTenantIdAsync(tenant1.Id);
        var tenant2Providers = await _repository.GetByTenantIdAsync(tenant2.Id);

        // Assert
        Assert.Single(tenant1Providers);
        Assert.Single(tenant2Providers);
        Assert.Equal("Production Claude", tenant1Providers[0].Name);
        Assert.Equal("Production Claude", tenant2Providers[0].Name);
        Assert.NotEqual(tenant1Providers[0].Id, tenant2Providers[0].Id);
    }

    [Fact]
    public async Task Repository_HandlesProviderWithNullOptionalFields()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var provider = TenantLlmProvider.CreateOAuthProvider(tenant.Id, "OAuth Provider");
        // OAuth provider has null ApiBaseUrl, null ModelOverrides, null EncryptedApiToken initially

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ApiBaseUrl);
        Assert.Null(result.ModelOverrides);
        Assert.Null(result.EncryptedApiToken);
    }

    [Fact]
    public async Task Repository_HandlesProviderWithAllFieldsPopulated()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var modelOverrides = new Dictionary<string, string>
        {
            { "default_sonnet_model", "MiniMax-M2" },
            { "small_fast_model", "MiniMax-M2" },
            { "large_model", "MiniMax-M2" }
        };

        var config = new ApiKeyProviderConfiguration(
            tenant.Id,
            "Full Config Provider",
            LlmProviderType.MinimaxM2,
            "https://api.minimax.chat/v1",
            "encrypted-token-12345",
            "MiniMax-M2",
            600000,
            true,
            modelOverrides);

        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        DbContext.Tenants.Add(tenant);
        DbContext.TenantLlmProviders.Add(provider);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Full Config Provider", result.Name);
        Assert.Equal("https://api.minimax.chat/v1", result.ApiBaseUrl);
        Assert.Equal("encrypted-token-12345", result.EncryptedApiToken);
        Assert.Equal("MiniMax-M2", result.DefaultModel);
        Assert.Equal(600000, result.TimeoutMs);
        Assert.True(result.DisableNonEssentialTraffic);
        Assert.NotNull(result.ModelOverrides);
        Assert.Equal(3, result.ModelOverrides.Count);
    }

    #endregion
}
