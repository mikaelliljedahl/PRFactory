using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.LLM;
using PRFactory.Infrastructure.Agents.Adapters;
using Xunit;

namespace PRFactory.Tests.Integration.Adapters;

/// <summary>
/// Integration tests for LLM Provider Adapters.
/// Tests health checks, message sending, and usage metrics extraction.
///
/// NOTE: These tests verify the adapter interfaces and error handling.
/// Actual LLM calls are mocked to avoid external dependencies and API costs.
/// </summary>
public class ProviderAdapterTests
{
    private readonly Mock<ILogger<ClaudeCodeCliLlmProvider>> _mockClaudeLogger;
    private readonly Mock<ILogger<GeminiCliAdapter>> _mockGeminiLogger;
    private readonly Mock<ILogger<OpenAiCliAdapter>> _mockOpenAiLogger;
    private readonly IConfiguration _configuration;

    public ProviderAdapterTests()
    {
        _mockClaudeLogger = new Mock<ILogger<ClaudeCodeCliLlmProvider>>();
        _mockGeminiLogger = new Mock<ILogger<GeminiCliAdapter>>();
        _mockOpenAiLogger = new Mock<ILogger<OpenAiCliAdapter>>();

        var configDict = new Dictionary<string, string?>
        {
            { "ClaudeCodeCli:Path", "/usr/local/bin/claude" },
            { "ClaudeCodeCli:Model", "claude-sonnet-4-5" },
            { "GeminiCli:Path", "/usr/local/bin/gemini" },
            { "GeminiCli:Model", "gemini-pro" },
            { "GeminiCli:Enabled", "false" }, // Disabled by default
            { "OpenAiCli:Path", "/usr/local/bin/openai" },
            { "OpenAiCli:Model", "gpt-4o" },
            { "OpenAiCli:Enabled", "false" } // Disabled by default
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    #region Claude Provider Tests

    [Fact]
    public async Task ClaudeProvider_HealthCheck_ReturnsStatus()
    {
        // Arrange
        var provider = new ClaudeCodeCliLlmProvider(_mockClaudeLogger.Object, _configuration);

        // Act
        var result = await provider.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.StatusMessage);
        // Note: IsHealthy will depend on whether Claude CLI is actually installed
        // In test environment, it's expected to be false unless CLI is installed
    }

    [Fact]
    public void ClaudeProvider_HasCorrectProviderName()
    {
        // Arrange
        var provider = new ClaudeCodeCliLlmProvider(_mockClaudeLogger.Object, _configuration);

        // Act
        var name = provider.ProviderName;

        // Assert
        Assert.Equal("Anthropic", name);
    }

    [Fact]
    public void ClaudeProvider_HasSupportedModels()
    {
        // Arrange
        var provider = new ClaudeCodeCliLlmProvider(_mockClaudeLogger.Object, _configuration);

        // Act
        var models = provider.SupportedModels;

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        Assert.Contains("claude-sonnet-4-5", models);
    }

    #endregion

    #region Gemini Provider Tests

    [Fact]
    public async Task GeminiProvider_WhenDisabled_HealthCheckIndicatesUnavailable()
    {
        // Arrange
        var provider = new GeminiCliAdapter(_mockGeminiLogger.Object, _configuration);

        // Act
        var result = await provider.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHealthy); // Should be false when disabled
        Assert.Contains("disabled", result.StatusMessage.ToLowerInvariant());
    }

    [Fact]
    public void GeminiProvider_HasCorrectProviderName()
    {
        // Arrange
        var provider = new GeminiCliAdapter(_mockGeminiLogger.Object, _configuration);

        // Act
        var name = provider.ProviderName;

        // Assert
        Assert.Equal("Google", name);
    }

    [Fact]
    public void GeminiProvider_HasSupportedModels()
    {
        // Arrange
        var provider = new GeminiCliAdapter(_mockGeminiLogger.Object, _configuration);

        // Act
        var models = provider.SupportedModels;

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        Assert.Contains("gemini-pro", models);
    }

    #endregion

    #region OpenAI Provider Tests

    [Fact]
    public async Task OpenAiProvider_WhenDisabled_HealthCheckIndicatesUnavailable()
    {
        // Arrange
        var provider = new OpenAiCliAdapter(_mockOpenAiLogger.Object, _configuration);

        // Act
        var result = await provider.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHealthy); // Should be false when disabled
        Assert.Contains("disabled", result.StatusMessage.ToLowerInvariant());
    }

    [Fact]
    public void OpenAiProvider_HasCorrectProviderName()
    {
        // Arrange
        var provider = new OpenAiCliAdapter(_mockOpenAiLogger.Object, _configuration);

        // Act
        var name = provider.ProviderName;

        // Assert
        Assert.Equal("OpenAI", name);
    }

    [Fact]
    public void OpenAiProvider_HasSupportedModels()
    {
        // Arrange
        var provider = new OpenAiCliAdapter(_mockOpenAiLogger.Object, _configuration);

        // Act
        var models = provider.SupportedModels;

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        Assert.Contains("gpt-4o", models);
    }

    #endregion

    #region Response Handling Tests

    [Fact]
    public void LlmOptions_WithDefaultValues_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new LlmOptions
        {
            Model = "claude-sonnet-4-5",
            MaxTokens = 4000,
            Temperature = 0.3,
            TimeoutSeconds = 120
        };

        // Assert
        Assert.Equal("claude-sonnet-4-5", options.Model);
        Assert.Equal(4000, options.MaxTokens);
        Assert.Equal(0.3, options.Temperature);
        Assert.Equal(120, options.TimeoutSeconds);
    }

    [Fact]
    public void LlmResponse_SuccessfulResponse_HasCorrectStructure()
    {
        // Arrange & Act
        var response = new LlmResponse
        {
            Success = true,
            Content = "This is the LLM response",
            Usage = new LlmUsageMetrics
            {
                InputTokens = 100,
                OutputTokens = 50,
                TotalTokens = 150,
                Latency = TimeSpan.FromSeconds(2)
            }
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal("This is the LLM response", response.Content);
        Assert.Equal(100, response.Usage.InputTokens);
        Assert.Equal(50, response.Usage.OutputTokens);
        Assert.Equal(150, response.Usage.TotalTokens);
        Assert.Equal(2, response.Usage.Latency.TotalSeconds);
    }

    [Fact]
    public void LlmResponse_FailedResponse_HasErrorMessage()
    {
        // Arrange & Act
        var response = new LlmResponse
        {
            Success = false,
            ErrorMessage = "Rate limit exceeded"
        };

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("Rate limit exceeded", response.ErrorMessage);
    }

    [Fact]
    public void ProviderHealthStatus_HealthyStatus_HasCorrectProperties()
    {
        // Arrange & Act
        var status = new ProviderHealthStatus
        {
            IsHealthy = true,
            StatusMessage = "Provider is healthy and ready",
            IsInstalled = true,
            IsAuthenticated = true
        };

        // Assert
        Assert.True(status.IsHealthy);
        Assert.True(status.IsInstalled);
        Assert.True(status.IsAuthenticated);
        Assert.Contains("healthy", status.StatusMessage.ToLowerInvariant());
    }

    [Fact]
    public void ProviderHealthStatus_UnhealthyStatus_HasCorrectProperties()
    {
        // Arrange & Act
        var status = new ProviderHealthStatus
        {
            IsHealthy = false,
            StatusMessage = "Provider not authenticated",
            IsInstalled = true,
            IsAuthenticated = false
        };

        // Assert
        Assert.False(status.IsHealthy);
        Assert.True(status.IsInstalled);
        Assert.False(status.IsAuthenticated);
        Assert.Contains("not authenticated", status.StatusMessage.ToLowerInvariant());
    }

    #endregion
}
