using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Adapters;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using Xunit;

namespace PRFactory.Tests.Agents.Adapters;

/// <summary>
/// Tests for ClaudeCodeCliAdapter, focusing on environment variable building for tenant LLM providers
/// </summary>
[Collection("Database")]
public class ClaudeCodeCliAdapterTests : IDisposable
{
    private readonly Mock<IProcessExecutor> _mockProcessExecutor;
    private readonly Mock<ILogger<ClaudeCodeCliAdapter>> _mockLogger;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ClaudeCodeCliOptions _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ClaudeCodeCliAdapterTests()
    {
        _mockProcessExecutor = new Mock<IProcessExecutor>();
        _mockLogger = new Mock<ILogger<ClaudeCodeCliAdapter>>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _options = new ClaudeCodeCliOptions
        {
            ExecutablePath = "claude",
            DefaultTimeoutSeconds = 120,
            ProjectContextTimeoutSeconds = 300,
            StreamingTimeoutSeconds = 600,
            EnableVerboseLogging = false
        };

        // Create mocks for ApplicationDbContext dependencies
        var mockDbContextEncryptionService = new Mock<IEncryptionService>();
        var mockDbContextLogger = new Mock<ILogger<ApplicationDbContext>>();

        // Create in-memory database for testing
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(
            dbOptions,
            mockDbContextEncryptionService.Object,
            mockDbContextLogger.Object);

        // Setup service provider to return our db context
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ApplicationDbContext)))
            .Returns(_dbContext);

        // Setup default encryption service behavior
        _mockEncryptionService
            .Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns<string>(encrypted => $"decrypted-{encrypted}");

        // Setup default process executor behavior (successful execution)
        _mockProcessExecutor
            .Setup(p => p.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessExecutionResult
            {
                Success = true,
                ExitCode = 0,
                Output = "Test response from Claude",
                Error = string.Empty,
                Duration = TimeSpan.FromSeconds(1)
            });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private ClaudeCodeCliAdapter CreateAdapter()
    {
        return new ClaudeCodeCliAdapter(
            _mockProcessExecutor.Object,
            _mockLogger.Object,
            Options.Create(_options),
            _mockEncryptionService.Object,
            _mockServiceProvider.Object);
    }

    #region Environment Variable Building Tests

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithApiKeyProvider_BuildsCorrectEnvironmentVariables()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Z.ai Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-token-123",
            "claude-sonnet-4-5");

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Verify the process executor was called with correct environment variables
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            "claude",
            It.IsAny<IEnumerable<string>>(),
            null,
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("ANTHROPIC_AUTH_TOKEN") &&
                env["ANTHROPIC_AUTH_TOKEN"] == "decrypted-encrypted-token-123" &&
                env.ContainsKey("ANTHROPIC_BASE_URL") &&
                env["ANTHROPIC_BASE_URL"] == "https://api.z.ai" &&
                env.ContainsKey("API_TIMEOUT_MS") &&
                env["API_TIMEOUT_MS"] == "300000"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithOAuthProvider_OmitsBaseUrl()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, "Native Claude");
        provider.UpdateApiToken("encrypted-oauth-token", isOAuthToken: true);
        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Verify environment variables don't include base URL for native OAuth
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            "claude",
            It.IsAny<IEnumerable<string>>(),
            null,
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("ANTHROPIC_AUTH_TOKEN") &&
                env["ANTHROPIC_AUTH_TOKEN"] == "decrypted-encrypted-oauth-token" &&
                !env.ContainsKey("ANTHROPIC_BASE_URL") &&
                env.ContainsKey("API_TIMEOUT_MS")),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithCustomTimeout_UsesCustomTimeout()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Custom Provider",
            LlmProviderType.OpenRouter,
            "https://api.openrouter.ai",
            "encrypted-token",
            "gpt-4o",
            timeoutMs: 600000);

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("API_TIMEOUT_MS") &&
                env["API_TIMEOUT_MS"] == "600000"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithDisableNonEssentialTraffic_IncludesFlag()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Minimax Provider",
            LlmProviderType.MinimaxM2,
            "https://api.minimax.com",
            "encrypted-token",
            "MiniMax-M2",
            disableNonEssentialTraffic: true);

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC") &&
                env["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"] == "1"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithModelOverrides_IncludesOverrides()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            ["small_fast_model"] = "MiniMax-M2",
            ["default_sonnet_model"] = "MiniMax-M2",
            ["extended_model"] = "MiniMax-M2-Extended"
        };

        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Minimax with Overrides",
            LlmProviderType.MinimaxM2,
            "https://api.minimax.com",
            "encrypted-token",
            "MiniMax-M2",
            modelOverrides: modelOverrides);

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("small_fast_model") &&
                env["small_fast_model"] == "MiniMax-M2" &&
                env.ContainsKey("default_sonnet_model") &&
                env["default_sonnet_model"] == "MiniMax-M2" &&
                env.ContainsKey("extended_model") &&
                env["extended_model"] == "MiniMax-M2-Extended"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithSpecificProviderId_UsesSpecifiedProvider()
    {
        // Arrange
        var defaultProvider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Default Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-default-token",
            "claude-sonnet");

        defaultProvider.SetAsDefault();

        var specificProvider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Specific Provider",
            LlmProviderType.OpenRouter,
            "https://api.openrouter.ai",
            "encrypted-specific-token",
            "gpt-4o");

        await _dbContext.TenantLlmProviders.AddAsync(defaultProvider);
        await _dbContext.TenantLlmProviders.AddAsync(specificProvider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: specificProvider.Id,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("ANTHROPIC_AUTH_TOKEN") &&
                env["ANTHROPIC_AUTH_TOKEN"] == "decrypted-encrypted-specific-token" &&
                env["ANTHROPIC_BASE_URL"] == "https://api.openrouter.ai"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithNoProvider_UsesEmptyEnvironmentVariables()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        // Should still succeed but with empty environment variables
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env => env.Count == 0),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithInactiveProvider_ReturnsEmptyEnvironmentVariables()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Inactive Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-token",
            "claude-sonnet");

        provider.SetAsDefault();
        provider.Deactivate();

        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        // Should not use inactive provider even if it's marked as default
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env => env.Count == 0),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithNullEncryptedToken_SkipsAuthToken()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, "OAuth Provider (No Token)");
        provider.SetAsDefault();

        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        // Should not include auth token if it's null
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env => !env.ContainsKey("ANTHROPIC_AUTH_TOKEN")),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithProjectContextAndTenantAsync_WithProvider_BuildsEnvironmentVariables()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Project Context Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-token",
            "claude-sonnet");

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();
        var projectPath = Path.GetTempPath();

        // Act
        await adapter.ExecuteWithProjectContextAndTenantAsync(
            "Test prompt",
            projectPath,
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            "claude",
            It.Is<IEnumerable<string>>(args =>
                args.Contains("--headless") &&
                args.Contains("--project-path") &&
                args.Contains(projectPath)),
            projectPath,
            It.Is<Dictionary<string, string>>(env =>
                env.ContainsKey("ANTHROPIC_AUTH_TOKEN") &&
                env["ANTHROPIC_AUTH_TOKEN"] == "decrypted-encrypted-token"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithNullTenantId_UsesEmptyEnvironmentVariables()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            tenantId: null,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        // Should execute without environment variables when tenant is null
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            null,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithAllFeatures_BuildsCompleteEnvironmentVariables()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            ["model_1"] = "override-1",
            ["model_2"] = "override-2"
        };

        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Full Featured Provider",
            LlmProviderType.MinimaxM2,
            "https://api.minimax.com",
            "encrypted-full-token",
            "MiniMax-M2",
            timeoutMs: 450000,
            disableNonEssentialTraffic: true,
            modelOverrides: modelOverrides);

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env =>
                env["ANTHROPIC_AUTH_TOKEN"] == "decrypted-encrypted-full-token" &&
                env["ANTHROPIC_BASE_URL"] == "https://api.minimax.com" &&
                env["API_TIMEOUT_MS"] == "450000" &&
                env["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"] == "1" &&
                env["model_1"] == "override-1" &&
                env["model_2"] == "override-2"),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePromptWithTenantAsync_WithEncryptionServiceException_LogsErrorAndUsesEmptyEnvironment()
    {
        // Arrange
        _mockEncryptionService
            .Setup(e => e.Decrypt(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Decryption failed"));

        var provider = TenantLlmProvider.CreateApiKeyProvider(
            _tenantId,
            "Test Provider",
            LlmProviderType.ZAi,
            "https://api.z.ai",
            "encrypted-token",
            "claude-sonnet");

        provider.SetAsDefault();
        await _dbContext.TenantLlmProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();

        var adapter = CreateAdapter();

        // Act
        await adapter.ExecutePromptWithTenantAsync(
            "Test prompt",
            _tenantId,
            llmProviderId: null,
            CancellationToken.None);

        // Assert
        // Should still execute but with empty environment (catches exception)
        _mockProcessExecutor.Verify(p => p.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(env => env.Count == 0),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
