using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Tests.Builders;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Web.Services;

/// <summary>
/// Integration tests for AgentConfigurationService.
/// Tests configuration retrieval, provider listing, configuration saving, and validation.
/// </summary>
public class AgentConfigurationServiceTests
{
    private readonly Mock<ILogger<AgentConfigurationService>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ITenantApplicationService> _mockTenantAppService;
    private readonly Mock<ApplicationDbContext> _mockDbContext;

    public AgentConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AgentConfigurationService>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockTenantAppService = new Mock<ITenantApplicationService>();
        _mockDbContext = new Mock<ApplicationDbContext>();
    }

    private AgentConfigurationService CreateService()
    {
        return new AgentConfigurationService(
            _mockLogger.Object,
            _mockTenantContext.Object,
            _mockTenantAppService.Object,
            _mockDbContext.Object);
    }

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_ReturnsConfiguration()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableCodeReview = true
        });

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.EnableCodeReview);

        _mockTenantContext.Verify(
            c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mockTenantAppService.Verify(
            s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNonExistentTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetConfigurationAsync());

        Assert.Contains($"Tenant {tenantId} not found", exception.Message);
    }

    #endregion

    #region GetConfigurationByTenantIdAsync Tests

    [Fact]
    public async Task GetConfigurationByTenantIdAsync_WithValidTenant_ReturnsConfiguration()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableCodeReview = true
        });

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await service.GetConfigurationByTenantIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.EnableCodeReview);
        Assert.Equal(3, result.MaxCodeReviewIterations); // Default value
        Assert.False(result.AutoApproveIfNoIssues); // Default value
        Assert.True(result.RequireHumanApprovalAfterReview); // Default value
    }

    #endregion

    #region GetAvailableProvidersAsync Tests

    [Fact]
    public async Task GetAvailableProvidersAsync_ReturnsProviders()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        // Mock tenant with LLM providers
        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        var anthropicProvider = TenantLlmProvider.CreateOAuthProvider(
            tenantId: tenantId,
            name: "Anthropic",
            defaultModel: "claude-sonnet-4-5-20250929");

        var openAiConfig = new PRFactory.Domain.ValueObjects.ApiKeyProviderConfiguration(
            tenantId: tenantId,
            name: "OpenAI",
            providerType: PRFactory.Domain.Entities.LlmProviderType.Custom,
            apiBaseUrl: "https://api.openai.com/v1",
            encryptedApiToken: "encrypted-key",
            defaultModel: "gpt-4o");
        var openAiProvider = TenantLlmProvider.CreateApiKeyProvider(openAiConfig);

        var googleConfig = new PRFactory.Domain.ValueObjects.ApiKeyProviderConfiguration(
            tenantId: tenantId,
            name: "Google",
            providerType: PRFactory.Domain.Entities.LlmProviderType.Custom,
            apiBaseUrl: "https://generativelanguage.googleapis.com/v1",
            encryptedApiToken: "encrypted-key",
            defaultModel: "gemini-2.0-flash-exp");
        var googleProvider = TenantLlmProvider.CreateApiKeyProvider(googleConfig);

        tenant.AddLlmProvider(anthropicProvider);
        tenant.AddLlmProvider(openAiProvider);
        tenant.AddLlmProvider(googleProvider);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await service.GetAvailableProvidersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        Assert.Contains(result, p => p.Name == "Anthropic" && p.IsActive);
        Assert.Contains(result, p => p.Name == "OpenAI" && p.IsActive);
        Assert.Contains(result, p => p.Name == "Google" && p.IsActive);
    }

    #endregion

    #region SaveConfigurationAsync Tests

    [Fact]
    public async Task SaveConfigurationAsync_UpdatesConfiguration()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableCodeReview = false
        });

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantAppService
            .Setup(s => s.UpdateTenantConfigurationAsync(
                tenantId,
                It.IsAny<TenantConfiguration>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var newConfiguration = new AgentConfigurationDto
        {
            TenantId = tenantId,
            EnableCodeReview = true,
            MaxCodeReviewIterations = 5,
            AutoApproveIfNoIssues = true,
            RequireHumanApprovalAfterReview = false
        };

        // Act
        var result = await service.SaveConfigurationAsync(newConfiguration);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EnableCodeReview);

        _mockTenantAppService.Verify(
            s => s.UpdateTenantConfigurationAsync(
                tenantId,
                It.Is<TenantConfiguration>(c => c.EnableCodeReview == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ValidateConfigurationAsync Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfig_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenantId: tenantId,
            name: "Anthropic",
            defaultModel: "claude-sonnet-4-5-20250929");

        var providerId = provider.Id;
        tenant.AddLlmProvider(provider);

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var configuration = new AgentConfigurationDto
        {
            TenantId = tenantId,
            CodeReviewAgentProviderId = providerId,
            EnableCodeReview = true
        };

        // Act
        var (isValid, errors) = await service.ValidateConfigurationAsync(configuration);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidProviderId_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        // No providers added to tenant

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var invalidProviderId = Guid.NewGuid();
        var configuration = new AgentConfigurationDto
        {
            TenantId = tenantId,
            CodeReviewAgentProviderId = invalidProviderId,
            EnableCodeReview = true
        };

        // Act
        var (isValid, errors) = await service.ValidateConfigurationAsync(configuration);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("provider") && e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithDisabledProvider_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenantId: tenantId,
            name: "Anthropic",
            defaultModel: "claude-sonnet-4-5-20250929");
        provider.Deactivate(); // Make provider inactive

        var providerId = provider.Id;
        tenant.AddLlmProvider(provider);

        _mockTenantContext
            .Setup(c => c.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantAppService
            .Setup(s => s.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var configuration = new AgentConfigurationDto
        {
            TenantId = tenantId,
            CodeReviewAgentProviderId = providerId,
            EnableCodeReview = true
        };

        // Act
        var (isValid, errors) = await service.ValidateConfigurationAsync(configuration);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("inactive") || e.Contains("disabled"));
    }

    #endregion
}
