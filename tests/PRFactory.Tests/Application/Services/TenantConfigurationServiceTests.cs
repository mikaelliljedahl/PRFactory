using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for TenantConfigurationService covering all business logic paths
/// </summary>
public class TenantConfigurationServiceTests
{
    private readonly Mock<ILogger<TenantConfigurationService>> _mockLogger;
    private readonly Mock<ITenantRepository> _mockTenantRepo;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;

    public TenantConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<TenantConfigurationService>>();
        _mockTenantRepo = new Mock<ITenantRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
    }

    private TenantConfigurationService CreateService()
    {
        return new TenantConfigurationService(
            _mockLogger.Object,
            _mockTenantRepo.Object,
            _mockCurrentUserService.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TenantConfigurationService(
                null!,
                _mockTenantRepo.Object,
                _mockCurrentUserService.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTenantRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TenantConfigurationService(
                _mockLogger.Object,
                null!,
                _mockCurrentUserService.Object));

        Assert.Equal("tenantRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCurrentUserService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TenantConfigurationService(
                _mockLogger.Object,
                _mockTenantRepo.Object,
                null!));

        Assert.Equal("currentUserService", exception.ParamName);
    }

    #endregion

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_WithValidTenant_ReturnsConfigurationDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .WithConfiguration(config =>
            {
                config.AutoImplementAfterPlanApproval = true;
                config.MaxRetries = 5;
                config.ClaudeModel = "claude-sonnet-4-5-20250929";
                config.MaxTokensPerRequest = 10000;
                config.ApiTimeoutSeconds = 120;
                config.EnableVerboseLogging = true;
                config.EnableAutoCodeReview = true;
                config.MaxCodeReviewIterations = 5;
                config.AutoApproveIfNoIssues = true;
            })
            .Build();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AutoImplementAfterPlanApproval);
        Assert.Equal(5, result.MaxRetries);
        Assert.Equal("claude-sonnet-4-5-20250929", result.ClaudeModel);
        Assert.Equal(10000, result.MaxTokensPerRequest);
        Assert.Equal(120, result.ApiTimeoutSeconds);
        Assert.True(result.EnableVerboseLogging);
        Assert.True(result.EnableAutoCodeReview);
        Assert.Equal(5, result.MaxCodeReviewIterations);
        Assert.True(result.AutoApproveIfNoIssues);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNoAuthenticatedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetConfigurationAsync());

        Assert.Equal("Current tenant ID cannot be determined. User may not be authenticated.", exception.Message);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithTenantNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetConfigurationAsync());

        Assert.Equal($"Tenant {tenantId} not found", exception.Message);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithDefaultConfiguration_ReturnsDefaultValues()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert - verify default values
        Assert.NotNull(result);
        Assert.False(result.AutoImplementAfterPlanApproval);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal("claude-sonnet-4-5-20250929", result.ClaudeModel);
        Assert.Equal(8000, result.MaxTokensPerRequest);
        Assert.Equal(300, result.ApiTimeoutSeconds);
        Assert.False(result.EnableVerboseLogging);
        Assert.False(result.EnableAutoCodeReview);
        Assert.Equal(3, result.MaxCodeReviewIterations);
        Assert.False(result.AutoApproveIfNoIssues);
        Assert.True(result.RequireHumanApprovalAfterReview);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithCustomPromptTemplates_ReturnsTemplates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var customTemplates = new Dictionary<string, string>
        {
            { "refinement", "Custom refinement prompt" },
            { "planning", "Custom planning prompt" }
        };

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .WithConfiguration(config =>
            {
                config.CustomPromptTemplates = customTemplates;
            })
            .Build();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CustomPromptTemplates);
        Assert.Equal(2, result.CustomPromptTemplates.Count);
        Assert.Equal("Custom refinement prompt", result.CustomPromptTemplates["refinement"]);
        Assert.Equal("Custom planning prompt", result.CustomPromptTemplates["planning"]);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithLlmProviderIds_ReturnsProviderIds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var codeReviewProviderId = Guid.NewGuid();
        var implementationProviderId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .WithConfiguration(config =>
            {
                config.CodeReviewLlmProviderId = codeReviewProviderId;
                config.ImplementationLlmProviderId = implementationProviderId;
            })
            .Build();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(codeReviewProviderId, result.CodeReviewLlmProviderId);
        Assert.Equal(implementationProviderId, result.ImplementationLlmProviderId);
    }

    #endregion

    #region UpdateConfigurationAsync Tests

    [Fact]
    public async Task UpdateConfigurationAsync_WithValidConfiguration_UpdatesTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        var dto = new TenantConfigurationDto
        {
            AutoImplementAfterPlanApproval = true,
            MaxRetries = 5,
            ClaudeModel = "claude-opus-4",
            MaxTokensPerRequest = 15000,
            ApiTimeoutSeconds = 180,
            EnableVerboseLogging = true,
            EnableAutoCodeReview = true,
            MaxCodeReviewIterations = 5,
            AutoApproveIfNoIssues = true,
            RequireHumanApprovalAfterReview = false
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert - verify tenant configuration was updated
        Assert.True(tenant.Configuration.AutoImplementAfterPlanApproval);
        Assert.Equal(5, tenant.Configuration.MaxRetries);
        Assert.Equal("claude-opus-4", tenant.Configuration.ClaudeModel);
        Assert.Equal(15000, tenant.Configuration.MaxTokensPerRequest);
        Assert.Equal(180, tenant.Configuration.ApiTimeoutSeconds);
        Assert.True(tenant.Configuration.EnableVerboseLogging);
        Assert.True(tenant.Configuration.EnableAutoCodeReview);
        Assert.Equal(5, tenant.Configuration.MaxCodeReviewIterations);
        Assert.True(tenant.Configuration.AutoApproveIfNoIssues);
        Assert.False(tenant.Configuration.RequireHumanApprovalAfterReview);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateConfigurationAsync(null!));

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithNoAuthenticatedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new TenantConfigurationDto { MaxRetries = 5 };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Equal("Current tenant ID cannot be determined. User may not be authenticated.", exception.Message);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithTenantNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var dto = new TenantConfigurationDto { MaxRetries = 5 };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Equal($"Tenant {tenantId} not found", exception.Message);

        _mockCurrentUserService.Verify(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public async Task UpdateConfigurationAsync_WithInvalidMaxRetries_ThrowsArgumentException(int maxRetries)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto { MaxRetries = maxRetries };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("MaxRetries must be between 1 and 10", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    [InlineData(1000)]
    public async Task UpdateConfigurationAsync_WithInvalidApiTimeout_ThrowsArgumentException(int apiTimeout)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            ApiTimeoutSeconds = apiTimeout
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("ApiTimeoutSeconds must be between 30 and 600", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200001)]
    [InlineData(500000)]
    public async Task UpdateConfigurationAsync_WithInvalidMaxTokens_ThrowsArgumentException(int maxTokens)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            MaxTokensPerRequest = maxTokens
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("MaxTokensPerRequest must be between 1,000 and 200,000", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(50)]
    public async Task UpdateConfigurationAsync_WithInvalidMaxCodeReviewIterations_ThrowsArgumentException(int maxIterations)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            MaxCodeReviewIterations = maxIterations
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("MaxCodeReviewIterations must be between 1 and 10", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateConfigurationAsync_WithEmptyClaudeModel_ThrowsArgumentException(string? claudeModel)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            ClaudeModel = claudeModel!
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("ClaudeModel cannot be empty", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithTooLongClaudeModel_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            ClaudeModel = new string('a', 101) // 101 characters
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        Assert.Contains("ClaudeModel cannot exceed 100 characters", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 15,              // Invalid: > 10
            ApiTimeoutSeconds = 10,       // Invalid: < 30
            MaxTokensPerRequest = 500,    // Invalid: < 1000
            MaxCodeReviewIterations = 20, // Invalid: > 10
            ClaudeModel = ""              // Invalid: empty
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateConfigurationAsync(dto));

        // Verify all validation errors are included
        Assert.Contains("MaxRetries must be between 1 and 10", exception.Message);
        Assert.Contains("ApiTimeoutSeconds must be between 30 and 600", exception.Message);
        Assert.Contains("MaxTokensPerRequest must be between 1,000 and 200,000", exception.Message);
        Assert.Contains("MaxCodeReviewIterations must be between 1 and 10", exception.Message);
        Assert.Contains("ClaudeModel cannot be empty", exception.Message);

        _mockTenantRepo.Verify(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task UpdateConfigurationAsync_WithValidMaxRetries_Succeeds(int maxRetries)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto { MaxRetries = maxRetries };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert - no exception thrown
        Assert.Equal(maxRetries, tenant.Configuration.MaxRetries);
        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(300)]
    [InlineData(600)]
    public async Task UpdateConfigurationAsync_WithValidApiTimeout_Succeeds(int apiTimeout)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            ApiTimeoutSeconds = apiTimeout
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert - no exception thrown
        Assert.Equal(apiTimeout, tenant.Configuration.ApiTimeoutSeconds);
        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task GetConfigurationAsync_OnlyAccessesCurrentTenantConfiguration()
    {
        // Arrange
        var currentTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var currentTenant = new TenantBuilder()
            .WithId(currentTenantId)
            .WithName("Current Tenant")
            .WithConfiguration(config => config.MaxRetries = 5)
            .Build();

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentTenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(currentTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentTenant);

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert - verify only current tenant was accessed
        Assert.Equal(5, result.MaxRetries);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(currentTenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(otherTenantId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_OnlyUpdatesCurrentTenantConfiguration()
    {
        // Arrange
        var currentTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var currentTenant = new TenantBuilder()
            .WithId(currentTenantId)
            .WithName("Current Tenant")
            .Build();

        var dto = new TenantConfigurationDto { MaxRetries = 7 };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentTenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(currentTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentTenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(currentTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert - verify only current tenant was updated
        Assert.Equal(7, currentTenant.Configuration.MaxRetries);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(currentTenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.UpdateAsync(currentTenant, It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantRepo.Verify(x => x.GetByIdAsync(otherTenantId, It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public async Task UpdateConfigurationAsync_WithCustomPromptTemplates_UpdatesTemplates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var customTemplates = new Dictionary<string, string>
        {
            { "refinement", "Custom refinement prompt" },
            { "planning", "Custom planning prompt" },
            { "implementation", "Custom implementation prompt" }
        };

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            CustomPromptTemplates = customTemplates
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(3, tenant.Configuration.CustomPromptTemplates.Count);
        Assert.Equal("Custom refinement prompt", tenant.Configuration.CustomPromptTemplates["refinement"]);
        Assert.Equal("Custom planning prompt", tenant.Configuration.CustomPromptTemplates["planning"]);
        Assert.Equal("Custom implementation prompt", tenant.Configuration.CustomPromptTemplates["implementation"]);

        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithAllowedRepositories_UpdatesRepositories()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            AllowedRepositories = ["repo1", "repo2", "repo3"]
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(3, tenant.Configuration.AllowedRepositories.Length);
        Assert.Contains("repo1", tenant.Configuration.AllowedRepositories);
        Assert.Contains("repo2", tenant.Configuration.AllowedRepositories);
        Assert.Contains("repo3", tenant.Configuration.AllowedRepositories);

        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithAllLlmProviderIds_UpdatesAllProviders()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantBuilder().WithId(tenantId).Build();

        var codeReviewProviderId = Guid.NewGuid();
        var implementationProviderId = Guid.NewGuid();
        var planningProviderId = Guid.NewGuid();
        var analysisProviderId = Guid.NewGuid();

        var dto = new TenantConfigurationDto
        {
            MaxRetries = 3,
            CodeReviewLlmProviderId = codeReviewProviderId,
            ImplementationLlmProviderId = implementationProviderId,
            PlanningLlmProviderId = planningProviderId,
            AnalysisLlmProviderId = analysisProviderId
        };

        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockTenantRepo
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepo
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(codeReviewProviderId, tenant.Configuration.CodeReviewLlmProviderId);
        Assert.Equal(implementationProviderId, tenant.Configuration.ImplementationLlmProviderId);
        Assert.Equal(planningProviderId, tenant.Configuration.PlanningLlmProviderId);
        Assert.Equal(analysisProviderId, tenant.Configuration.AnalysisLlmProviderId);

        _mockTenantRepo.Verify(x => x.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
