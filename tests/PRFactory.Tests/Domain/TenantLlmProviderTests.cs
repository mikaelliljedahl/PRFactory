using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Tests.Domain;

public class TenantLlmProviderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string ValidName = "Test Provider";
    private const string ValidApiBaseUrl = "https://api.test.com";
    private const string ValidApiToken = "encrypted-token-123";
    private const string ValidModel = "claude-sonnet-4-5-20250929";

    #region CreateApiKeyProvider Tests

    [Fact]
    public void CreateApiKeyProvider_WithValidInputs_ReturnsValidProvider()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);

        // Act
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Assert
        Assert.NotNull(provider);
        Assert.NotEqual(Guid.Empty, provider.Id);
        Assert.Equal(_tenantId, provider.TenantId);
        Assert.Equal(ValidName, provider.Name);
        Assert.Equal(LlmProviderType.ZAi, provider.ProviderType);
        Assert.False(provider.UsesOAuth);
        Assert.Equal(ValidApiToken, provider.EncryptedApiToken);
        Assert.Equal(ValidApiBaseUrl, provider.ApiBaseUrl);
        Assert.Equal(300000, provider.TimeoutMs);
        Assert.Equal(ValidModel, provider.DefaultModel);
        Assert.False(provider.DisableNonEssentialTraffic);
        Assert.Null(provider.ModelOverrides);
        Assert.True(provider.IsActive);
        Assert.False(provider.IsDefault);
        Assert.True(Math.Abs((provider.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(provider.UpdatedAt);
        Assert.Null(provider.OAuthTokenRefreshedAt);
    }

    [Fact]
    public void CreateApiKeyProvider_WithCustomTimeout_SetsTimeout()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            timeoutMs: 600000);

        // Act
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Assert
        Assert.Equal(600000, provider.TimeoutMs);
    }

    [Fact]
    public void CreateApiKeyProvider_WithDisableNonEssentialTraffic_SetsFlag()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            disableNonEssentialTraffic: true);

        // Act
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Assert
        Assert.True(provider.DisableNonEssentialTraffic);
    }

    [Fact]
    public void CreateApiKeyProvider_WithModelOverrides_SetsOverrides()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            ["small_fast_model"] = "MiniMax-M2",
            ["default_sonnet_model"] = "MiniMax-M2"
        };

        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            modelOverrides: modelOverrides);

        // Act
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Assert
        Assert.NotNull(provider.ModelOverrides);
        Assert.Equal(2, provider.ModelOverrides.Count);
        Assert.Equal("MiniMax-M2", provider.ModelOverrides["small_fast_model"]);
        Assert.Equal("MiniMax-M2", provider.ModelOverrides["default_sonnet_model"]);
    }

    [Fact]
    public void CreateApiKeyProvider_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiKeyProviderConfiguration(
                Guid.Empty,
                ValidName,
                LlmProviderType.ZAi,
                ValidApiBaseUrl,
                ValidApiToken,
                ValidModel));

        Assert.Contains("Tenant ID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateApiKeyProvider_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiKeyProviderConfiguration(
                _tenantId,
                invalidName!,
                LlmProviderType.ZAi,
                ValidApiBaseUrl,
                ValidApiToken,
                ValidModel));

        Assert.Contains("Name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateApiKeyProvider_WithInvalidApiBaseUrl_ThrowsArgumentException(string? invalidUrl)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiKeyProviderConfiguration(
                _tenantId,
                ValidName,
                LlmProviderType.ZAi,
                invalidUrl!,
                ValidApiToken,
                ValidModel));

        Assert.Contains("API base URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateApiKeyProvider_WithInvalidApiToken_ThrowsArgumentException(string? invalidToken)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiKeyProviderConfiguration(
                _tenantId,
                ValidName,
                LlmProviderType.ZAi,
                ValidApiBaseUrl,
                invalidToken!,
                ValidModel));

        Assert.Contains("API token", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateApiKeyProvider_WithInvalidDefaultModel_ThrowsArgumentException(string? invalidModel)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiKeyProviderConfiguration(
                _tenantId,
                ValidName,
                LlmProviderType.ZAi,
                ValidApiBaseUrl,
                ValidApiToken,
                invalidModel!));

        Assert.Contains("Default model", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CreateOAuthProvider Tests

    [Fact]
    public void CreateOAuthProvider_WithValidInputs_ReturnsValidProvider()
    {
        // Act
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, ValidName);

        // Assert
        Assert.NotNull(provider);
        Assert.NotEqual(Guid.Empty, provider.Id);
        Assert.Equal(_tenantId, provider.TenantId);
        Assert.Equal(ValidName, provider.Name);
        Assert.Equal(LlmProviderType.AnthropicNative, provider.ProviderType);
        Assert.True(provider.UsesOAuth);
        Assert.Null(provider.EncryptedApiToken);
        Assert.Null(provider.ApiBaseUrl);
        Assert.Equal(300000, provider.TimeoutMs);
        Assert.Equal("claude-sonnet-4-5-20250929", provider.DefaultModel);
        Assert.False(provider.DisableNonEssentialTraffic);
        Assert.True(provider.IsActive);
        Assert.False(provider.IsDefault);
        Assert.True(Math.Abs((provider.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(provider.UpdatedAt);
        Assert.Null(provider.OAuthTokenRefreshedAt);
    }

    [Fact]
    public void CreateOAuthProvider_WithCustomModel_SetsModel()
    {
        // Act
        var provider = TenantLlmProvider.CreateOAuthProvider(
            _tenantId,
            ValidName,
            defaultModel: "claude-opus-4-5-20250929");

        // Assert
        Assert.Equal("claude-opus-4-5-20250929", provider.DefaultModel);
    }

    [Fact]
    public void CreateOAuthProvider_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            TenantLlmProvider.CreateOAuthProvider(Guid.Empty, ValidName));

        Assert.Contains("Tenant ID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateOAuthProvider_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            TenantLlmProvider.CreateOAuthProvider(_tenantId, invalidName!));

        Assert.Contains("Name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateApiToken Tests

    [Fact]
    public void UpdateApiToken_WithValidToken_UpdatesToken()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string newToken = "new-encrypted-token";

        // Act
        provider.UpdateApiToken(newToken);

        // Assert
        Assert.Equal(newToken, provider.EncryptedApiToken);
        Assert.NotNull(provider.UpdatedAt);
        Assert.Null(provider.OAuthTokenRefreshedAt);
    }

    [Fact]
    public void UpdateApiToken_WithOAuthToken_UpdatesTokenAndRefreshTime()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, ValidName);
        const string oauthToken = "oauth-access-token";

        // Act
        provider.UpdateApiToken(oauthToken, isOAuthToken: true);

        // Assert
        Assert.Equal(oauthToken, provider.EncryptedApiToken);
        Assert.NotNull(provider.UpdatedAt);
        Assert.NotNull(provider.OAuthTokenRefreshedAt);
        Assert.True(Math.Abs((provider.OAuthTokenRefreshedAt!.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateApiToken_WithInvalidToken_ThrowsArgumentException(string? invalidToken)
    {
        // Arrange
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, ValidName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => provider.UpdateApiToken(invalidToken!));

        Assert.Contains("API token", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public void UpdateConfiguration_WithNewName_UpdatesName()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string newName = "Updated Provider";

        // Act
        provider.UpdateConfiguration(name: newName);

        // Assert
        Assert.Equal(newName, provider.Name);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithNewApiBaseUrl_UpdatesUrl()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string newUrl = "https://api.newprovider.com";

        // Act
        provider.UpdateConfiguration(apiBaseUrl: newUrl);

        // Assert
        Assert.Equal(newUrl, provider.ApiBaseUrl);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithNewDefaultModel_UpdatesModel()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string newModel = "gpt-4o";

        // Act
        provider.UpdateConfiguration(defaultModel: newModel);

        // Assert
        Assert.Equal(newModel, provider.DefaultModel);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithNewTimeout_UpdatesTimeout()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Act
        provider.UpdateConfiguration(timeoutMs: 600000);

        // Assert
        Assert.Equal(600000, provider.TimeoutMs);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithDisableNonEssentialTraffic_UpdatesFlag()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Act
        provider.UpdateConfiguration(disableNonEssentialTraffic: true);

        // Assert
        Assert.True(provider.DisableNonEssentialTraffic);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithModelOverrides_UpdatesOverrides()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        var newOverrides = new Dictionary<string, string>
        {
            ["model_tier_1"] = "MiniMax-M2-Pro",
            ["model_tier_2"] = "MiniMax-M2-Lite"
        };

        // Act
        provider.UpdateConfiguration(modelOverrides: newOverrides);

        // Assert
        Assert.NotNull(provider.ModelOverrides);
        Assert.Equal(2, provider.ModelOverrides.Count);
        Assert.Equal("MiniMax-M2-Pro", provider.ModelOverrides["model_tier_1"]);
        Assert.Equal("MiniMax-M2-Lite", provider.ModelOverrides["model_tier_2"]);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithMultipleFields_UpdatesAll()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string newName = "Multi-Update Provider";
        const string newUrl = "https://api.multi.com";
        const string newModel = "new-model";
        const int newTimeout = 450000;
        var newOverrides = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        provider.UpdateConfiguration(
            name: newName,
            apiBaseUrl: newUrl,
            defaultModel: newModel,
            timeoutMs: newTimeout,
            disableNonEssentialTraffic: true,
            modelOverrides: newOverrides);

        // Assert
        Assert.Equal(newName, provider.Name);
        Assert.Equal(newUrl, provider.ApiBaseUrl);
        Assert.Equal(newModel, provider.DefaultModel);
        Assert.Equal(newTimeout, provider.TimeoutMs);
        Assert.True(provider.DisableNonEssentialTraffic);
        Assert.NotNull(provider.ModelOverrides);
        Assert.Single(provider.ModelOverrides);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithNoChanges_StillUpdatesTimestamp()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        Assert.Null(provider.UpdatedAt);

        // Act
        provider.UpdateConfiguration();

        // Assert
        Assert.NotNull(provider.UpdatedAt);
    }

    #endregion

    #region SetAsDefault / RemoveAsDefault Tests

    [Fact]
    public void SetAsDefault_SetsIsDefaultToTrue()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        Assert.False(provider.IsDefault);

        // Act
        provider.SetAsDefault();

        // Assert
        Assert.True(provider.IsDefault);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void RemoveAsDefault_SetsIsDefaultToFalse()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        provider.SetAsDefault();
        Assert.True(provider.IsDefault);

        // Act
        provider.RemoveAsDefault();

        // Assert
        Assert.False(provider.IsDefault);
        Assert.NotNull(provider.UpdatedAt);
    }

    #endregion

    #region Activate / Deactivate Tests

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        provider.Deactivate();
        Assert.False(provider.IsActive);

        // Act
        provider.Activate();

        // Assert
        Assert.True(provider.IsActive);
        Assert.NotNull(provider.UpdatedAt);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        Assert.True(provider.IsActive);

        // Act
        provider.Deactivate();

        // Assert
        Assert.False(provider.IsActive);
        Assert.NotNull(provider.UpdatedAt);
    }

    #endregion

    #region GenerateClaudeSettingsEnv Tests

    [Fact]
    public void GenerateClaudeSettingsEnv_WithBasicProvider_GeneratesBasicEnv()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string decryptedToken = "decrypted-api-key-123";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.NotNull(env);
        Assert.Equal(3, env.Count);
        Assert.Equal(decryptedToken, env["ANTHROPIC_AUTH_TOKEN"]);
        Assert.Equal(ValidApiBaseUrl, env["ANTHROPIC_BASE_URL"]);
        Assert.Equal("300000", env["API_TIMEOUT_MS"]);
    }

    [Fact]
    public void GenerateClaudeSettingsEnv_WithNativeAnthropic_OmitsBaseUrl()
    {
        // Arrange
        var provider = TenantLlmProvider.CreateOAuthProvider(_tenantId, ValidName);
        provider.UpdateApiToken("oauth-token", isOAuthToken: true);

        const string decryptedToken = "decrypted-oauth-token";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.NotNull(env);
        Assert.Equal(2, env.Count);
        Assert.Equal(decryptedToken, env["ANTHROPIC_AUTH_TOKEN"]);
        Assert.Equal("300000", env["API_TIMEOUT_MS"]);
        Assert.False(env.ContainsKey("ANTHROPIC_BASE_URL"));
    }

    [Fact]
    public void GenerateClaudeSettingsEnv_WithDisableNonEssentialTraffic_IncludesFlag()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            disableNonEssentialTraffic: true);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string decryptedToken = "decrypted-api-key";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.True(env.ContainsKey("CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"));
        Assert.Equal("1", env["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"]);
    }

    [Fact]
    public void GenerateClaudeSettingsEnv_WithModelOverrides_IncludesOverrides()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            ["small_fast_model"] = "MiniMax-M2",
            ["default_sonnet_model"] = "MiniMax-M2",
            ["extended_model"] = "MiniMax-M2-Extended"
        };

        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            modelOverrides: modelOverrides);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string decryptedToken = "decrypted-api-key";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.Equal(6, env.Count); // 3 base + 3 overrides
        Assert.Equal("MiniMax-M2", env["small_fast_model"]);
        Assert.Equal("MiniMax-M2", env["default_sonnet_model"]);
        Assert.Equal("MiniMax-M2-Extended", env["extended_model"]);
    }

    [Fact]
    public void GenerateClaudeSettingsEnv_WithCustomTimeout_UsesCustomTimeout()
    {
        // Arrange
        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.ZAi,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            timeoutMs: 600000);
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string decryptedToken = "decrypted-api-key";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.Equal("600000", env["API_TIMEOUT_MS"]);
    }

    [Fact]
    public void GenerateClaudeSettingsEnv_WithAllFeatures_GeneratesCompleteEnv()
    {
        // Arrange
        var modelOverrides = new Dictionary<string, string>
        {
            ["model_1"] = "override-1",
            ["model_2"] = "override-2"
        };

        var config = new ApiKeyProviderConfiguration(
            _tenantId,
            ValidName,
            LlmProviderType.MinimaxM2,
            ValidApiBaseUrl,
            ValidApiToken,
            ValidModel,
            timeoutMs: 450000,
            disableNonEssentialTraffic: true,
            modelOverrides: modelOverrides);

        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        const string decryptedToken = "full-featured-token";

        // Act
        var env = provider.GenerateClaudeSettingsEnv(decryptedToken);

        // Assert
        Assert.Equal(6, env.Count); // 4 base + 2 overrides
        Assert.Equal(decryptedToken, env["ANTHROPIC_AUTH_TOKEN"]);
        Assert.Equal(ValidApiBaseUrl, env["ANTHROPIC_BASE_URL"]);
        Assert.Equal("450000", env["API_TIMEOUT_MS"]);
        Assert.Equal("1", env["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"]);
        Assert.Equal("override-1", env["model_1"]);
        Assert.Equal("override-2", env["model_2"]);
    }

    #endregion
}
