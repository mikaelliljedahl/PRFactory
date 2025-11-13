using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Tests.Application.Services;

/// <summary>
/// Unit tests for TenantLlmProviderService
/// </summary>
public class TenantLlmProviderServiceTests
{
    private readonly Mock<ITenantLlmProviderRepository> _mockRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILogger<TenantLlmProviderService>> _mockLogger;
    private readonly TenantLlmProviderService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    public TenantLlmProviderServiceTests()
    {
        _mockRepository = new Mock<ITenantLlmProviderRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<TenantLlmProviderService>>();

        // Default: Current user belongs to _testTenantId
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTenantId);

        // Default: Return a valid tenant
        var testTenant = Tenant.Create(
            name: "Test Tenant",
            identityProvider: "AzureAD",
            externalTenantId: "tenant-ext-123",
            ticketPlatformUrl: "https://test.atlassian.net",
            ticketPlatformApiToken: "token",
            claudeApiKey: "key"
        );
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testTenant);

        // Default encryption behavior
        _mockEncryptionService
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns<string>(s => $"encrypted_{s}");

        _mockEncryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns<string>(s => s.Replace("encrypted_", ""));

        _service = new TenantLlmProviderService(
            _mockRepository.Object,
            _mockTenantRepository.Object,
            _mockCurrentUserService.Object,
            _mockEncryptionService.Object,
            _mockLogger.Object
        );
    }

    #region GetProvidersForTenantAsync Tests

    [Fact]
    public async Task GetProvidersForTenantAsync_ReturnsAllProvidersForCurrentTenant()
    {
        // Arrange
        var providers = new List<TenantLlmProvider>
        {
            CreateTestProvider(_testTenantId, "Provider 1"),
            CreateTestProvider(_testTenantId, "Provider 2")
        };

        _mockRepository
            .Setup(x => x.GetByTenantIdAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _service.GetProvidersForTenantAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Provider 1", result[0].Name);
        Assert.Equal("Provider 2", result[1].Name);
        _mockRepository.Verify(x => x.GetByTenantIdAsync(_testTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProvidersForTenantAsync_ReturnsEmptyList_WhenNoProvidersExist()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetByTenantIdAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantLlmProvider>());

        // Act
        var result = await _service.GetProvidersForTenantAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetProviderByIdAsync Tests

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsProvider_WhenProviderBelongsToCurrentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Test Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.GetProviderByIdAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providerId, result.Id);
        Assert.Equal("Test Provider", result.Name);
    }

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsNull_WhenProviderDoesNotExist()
    {
        // Arrange
        var providerId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantLlmProvider?)null);

        // Act
        var result = await _service.GetProviderByIdAsync(providerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProviderByIdAsync_ThrowsUnauthorizedException_WhenProviderBelongsToDifferentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_otherTenantId, "Other Tenant Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetProviderByIdAsync(providerId));

        Assert.Contains("does not belong to the current tenant", exception.Message);
    }

    #endregion

    #region CreateApiKeyProviderAsync Tests

    [Fact]
    public async Task CreateApiKeyProviderAsync_CreatesProvider_WithEncryptedApiKey()
    {
        // Arrange
        var dto = new CreateApiKeyProviderDto
        {
            Name = "Z.ai Provider",
            ProviderType = "ZAi",
            ApiKey = "test-api-key-123",
            ApiBaseUrl = "https://api.z.ai",
            DefaultModel = "claude-sonnet-4-5",
            TimeoutMs = 300000,
            DisableNonEssentialTraffic = false
        };

        TenantLlmProvider? capturedProvider = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()))
            .Callback<TenantLlmProvider, CancellationToken>((p, _) => capturedProvider = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateApiKeyProviderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Z.ai Provider", result.Name);
        Assert.Equal("ZAi", result.ProviderType);
        Assert.Equal(_testTenantId, result.TenantId);

        // Verify encryption was called
        _mockEncryptionService.Verify(x => x.Encrypt("test-api-key-123"), Times.Once);

        // Verify provider was saved
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify encrypted API key was set
        Assert.NotNull(capturedProvider);
        Assert.Equal("encrypted_test-api-key-123", capturedProvider.EncryptedApiToken);
    }

    [Fact]
    public async Task CreateApiKeyProviderAsync_ThrowsArgumentException_ForInvalidProviderType()
    {
        // Arrange
        var dto = new CreateApiKeyProviderDto
        {
            Name = "Invalid Provider",
            ProviderType = "InvalidType",
            ApiKey = "test-key",
            ApiBaseUrl = "https://api.example.com",
            DefaultModel = "some-model"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateApiKeyProviderAsync(dto));

        Assert.Contains("Invalid provider type", exception.Message);
    }

    [Fact]
    public async Task CreateApiKeyProviderAsync_SetsModelOverrides_WhenProvided()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            { "small_fast_model", "MiniMax-M2" },
            { "default_sonnet_model", "MiniMax-M2" }
        };

        var dto = new CreateApiKeyProviderDto
        {
            Name = "Minimax Provider",
            ProviderType = "MinimaxM2",
            ApiKey = "test-key",
            ApiBaseUrl = "https://api.minimax.ai",
            DefaultModel = "MiniMax-M2",
            ModelOverrides = modelOverrides
        };

        TenantLlmProvider? capturedProvider = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()))
            .Callback<TenantLlmProvider, CancellationToken>((p, _) => capturedProvider = p)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateApiKeyProviderAsync(dto);

        // Assert
        Assert.NotNull(capturedProvider);
        Assert.NotNull(capturedProvider.ModelOverrides);
        Assert.Equal(2, capturedProvider.ModelOverrides.Count);
        Assert.Equal("MiniMax-M2", capturedProvider.ModelOverrides["small_fast_model"]);
    }

    #endregion

    #region CreateOAuthProviderAsync Tests

    [Fact]
    public async Task CreateOAuthProviderAsync_CreatesAnthropicOAuthProvider()
    {
        // Arrange
        var dto = new CreateOAuthProviderDto
        {
            Name = "Anthropic Native",
            DefaultModel = "claude-sonnet-4-5-20250929"
        };

        TenantLlmProvider? capturedProvider = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()))
            .Callback<TenantLlmProvider, CancellationToken>((p, _) => capturedProvider = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateOAuthProviderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Anthropic Native", result.Name);
        Assert.Equal("AnthropicNative", result.ProviderType);
        Assert.True(result.UsesOAuth);
        Assert.Equal(_testTenantId, result.TenantId);

        // Verify provider was saved
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify OAuth provider has no API token initially
        Assert.NotNull(capturedProvider);
        Assert.Null(capturedProvider.EncryptedApiToken);
    }

    #endregion

    #region UpdateProviderAsync Tests

    [Fact]
    public async Task UpdateProviderAsync_UpdatesProviderConfiguration()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Original Name", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var dto = new UpdateProviderDto
        {
            Name = "Updated Name",
            ApiBaseUrl = "https://new-api.example.com",
            DefaultModel = "new-model",
            TimeoutMs = 600000,
            DisableNonEssentialTraffic = true,
            IsActive = true
        };

        // Act
        var result = await _service.UpdateProviderAsync(providerId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.True(result.IsActive);

        _mockRepository.Verify(x => x.UpdateAsync(provider, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProviderAsync_ThrowsUnauthorizedException_WhenProviderBelongsToDifferentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_otherTenantId, "Other Tenant Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var dto = new UpdateProviderDto
        {
            Name = "Updated Name",
            DefaultModel = "model",
            TimeoutMs = 300000,
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateProviderAsync(providerId, dto));

        Assert.Contains("does not belong to the current tenant", exception.Message);
    }

    [Fact]
    public async Task UpdateProviderAsync_ThrowsException_WhenProviderNotFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantLlmProvider?)null);

        var dto = new UpdateProviderDto
        {
            Name = "Updated Name",
            DefaultModel = "model",
            TimeoutMs = 300000,
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProviderAsync(providerId, dto));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateProviderAsync_CanDeactivateProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Active Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var dto = new UpdateProviderDto
        {
            Name = "Active Provider",
            DefaultModel = "model",
            TimeoutMs = 300000,
            IsActive = false // Deactivate
        };

        // Act
        var result = await _service.UpdateProviderAsync(providerId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region DeleteProviderAsync Tests

    [Fact]
    public async Task DeleteProviderAsync_DeactivatesProvider_WhenProviderBelongsToCurrentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Provider to Delete", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        await _service.DeleteProviderAsync(providerId);

        // Assert
        Assert.False(provider.IsActive); // Should be deactivated (soft delete)
        _mockRepository.Verify(x => x.UpdateAsync(provider, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProviderAsync_ThrowsUnauthorizedException_WhenProviderBelongsToDifferentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_otherTenantId, "Other Tenant Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteProviderAsync(providerId));

        Assert.Contains("does not belong to the current tenant", exception.Message);
    }

    [Fact]
    public async Task DeleteProviderAsync_ThrowsException_WhenProviderNotFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantLlmProvider?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteProviderAsync(providerId));

        Assert.Contains("not found", exception.Message);
    }

    #endregion

    #region SetDefaultProviderAsync Tests

    [Fact]
    public async Task SetDefaultProviderAsync_SetsProviderAsDefault_AndClearsOtherDefaults()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider1 = CreateTestProvider(_testTenantId, "Provider 1", providerId);
        var provider2 = CreateTestProvider(_testTenantId, "Provider 2");
        provider2.SetAsDefault(); // This was previously the default

        var allProviders = new List<TenantLlmProvider> { provider1, provider2 };

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider1);

        _mockRepository
            .Setup(x => x.GetByTenantIdAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allProviders);

        // Act
        var result = await _service.SetDefaultProviderAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providerId, result.Id);
        Assert.True(result.IsDefault);
        Assert.True(provider1.IsDefault);
        Assert.False(provider2.IsDefault);

        // Verify both providers were updated (provider2 to clear default, provider1 to set default)
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<TenantLlmProvider>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        // Verify tenant was updated
        _mockTenantRepository.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefaultProviderAsync_ThrowsUnauthorizedException_WhenProviderBelongsToDifferentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_otherTenantId, "Other Tenant Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.SetDefaultProviderAsync(providerId));

        Assert.Contains("does not belong to the current tenant", exception.Message);
    }

    [Fact]
    public async Task SetDefaultProviderAsync_WorksWhenNoOtherDefaultExists()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Provider 1", providerId);

        var allProviders = new List<TenantLlmProvider> { provider };

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _mockRepository
            .Setup(x => x.GetByTenantIdAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allProviders);

        // Act
        var result = await _service.SetDefaultProviderAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsDefault);
        Assert.True(provider.IsDefault);

        // Only the target provider should be updated (no other defaults to clear)
        _mockRepository.Verify(x => x.UpdateAsync(provider, It.IsAny<CancellationToken>()), Times.Once);
        // Verify tenant was updated
        _mockTenantRepository.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region TestProviderConnectionAsync Tests

    [Fact]
    public async Task TestProviderConnectionAsync_ReturnsSuccess_WhenProviderHasValidToken()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Test Provider", providerId);
        provider.UpdateApiToken("encrypted_valid-token");

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.TestProviderConnectionAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("valid", result.Message);
        Assert.True(result.ResponseTimeMs >= 0);

        // Verify decryption was called
        _mockEncryptionService.Verify(x => x.Decrypt("encrypted_valid-token"), Times.Once);
    }

    [Fact]
    public async Task TestProviderConnectionAsync_ReturnsFailure_WhenProviderHasNoToken()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Test Provider", providerId);
        // No token set

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.TestProviderConnectionAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("no API token", result.Message);
    }

    [Fact]
    public async Task TestProviderConnectionAsync_ReturnsFailure_WhenDecryptionFails()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_testTenantId, "Test Provider", providerId);
        provider.UpdateApiToken("encrypted_token");

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _mockEncryptionService
            .Setup(x => x.Decrypt("encrypted_token"))
            .Throws(new InvalidOperationException("Decryption failed"));

        // Act
        var result = await _service.TestProviderConnectionAsync(providerId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Connection test failed", result.Message);
    }

    [Fact]
    public async Task TestProviderConnectionAsync_ThrowsUnauthorizedException_WhenProviderBelongsToDifferentTenant()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(_otherTenantId, "Other Tenant Provider", providerId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.TestProviderConnectionAsync(providerId));

        Assert.Contains("does not belong to the current tenant", exception.Message);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task AllMethods_ThrowUnauthorizedException_WhenNoTenantIdAvailable()
    {
        // Arrange: Current user has no tenant
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act & Assert: GetProvidersForTenantAsync
        var exception1 = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetProvidersForTenantAsync());
        Assert.Contains("not authenticated", exception1.Message);

        // Act & Assert: CreateApiKeyProviderAsync
        var dto = new CreateApiKeyProviderDto
        {
            Name = "Test",
            ProviderType = "ZAi",
            ApiKey = "key",
            ApiBaseUrl = "https://api.z.ai",
            DefaultModel = "model"
        };
        var exception2 = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateApiKeyProviderAsync(dto));
        Assert.Contains("not authenticated", exception2.Message);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test TenantLlmProvider for testing purposes
    /// </summary>
    private static TenantLlmProvider CreateTestProvider(Guid tenantId, string name, Guid? id = null)
    {
        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenantId: tenantId,
            name: name,
            defaultModel: "claude-sonnet-4-5-20250929"
        );

        // Use reflection to set the ID if provided (for testing purposes)
        if (id.HasValue)
        {
            var idProperty = typeof(TenantLlmProvider).GetProperty(nameof(TenantLlmProvider.Id));
            idProperty?.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion
}
