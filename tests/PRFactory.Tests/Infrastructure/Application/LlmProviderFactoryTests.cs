using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Configuration;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Application;

/// <summary>
/// Comprehensive tests for LlmProviderFactory covering:
/// - Provider creation by name (anthropic, openai, google)
/// - Default provider retrieval
/// - Provider fallback logic with health checks
/// - Available providers listing
/// - Error handling for unsupported providers
/// </summary>
public class LlmProviderFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly LlmProvidersOptions _options;

    public LlmProviderFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _options = new LlmProvidersOptions
        {
            DefaultProvider = "anthropic",
            FallbackProvider = "google"
        };
    }

    private LlmProviderFactory CreateFactory(LlmProvidersOptions? customOptions = null)
    {
        var options = customOptions ?? _options;
        var optionsWrapper = Options.Create(options);
        return new LlmProviderFactory(_mockServiceProvider.Object, optionsWrapper);
    }

    #region CreateProvider Tests

    [Fact]
    public void CreateProvider_WithAnthropicName_ReturnsClaudeProvider()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.CreateProvider("anthropic");

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)),
            Times.Once);
    }

    [Fact]
    public void CreateProvider_WithClaudeAlias_ReturnsClaudeProvider()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.CreateProvider("claude");

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)),
            Times.Once);
    }

    [Fact]
    public void CreateProvider_WithOpenAiName_ReturnsOpenAiProvider()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("OpenAI");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.OpenAiCliAdapter)))
            .Returns(mockProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.CreateProvider("openai");

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.OpenAiCliAdapter)),
            Times.Once);
    }

    [Fact]
    public void CreateProvider_WithGoogleName_ReturnsGeminiProvider()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Google");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter)))
            .Returns(mockProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.CreateProvider("google");

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter)),
            Times.Once);
    }

    [Fact]
    public void CreateProvider_WithInvalidName_ThrowsNotSupportedException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => factory.CreateProvider("invalid-provider"));

        Assert.Contains("Provider 'invalid-provider' is not supported", exception.Message);
        Assert.Contains("anthropic", exception.Message);
        Assert.Contains("google", exception.Message);
        Assert.Contains("openai", exception.Message);
    }

    #endregion

    #region GetDefaultProvider Tests

    [Fact]
    public void GetDefaultProvider_ReturnsConfiguredDefault()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)),
            Times.Once);
    }

    [Fact]
    public void GetDefaultProvider_WhenDefaultIsNull_ReturnsAnthropicProvider()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockProvider.Object);

        var customOptions = new LlmProvidersOptions { DefaultProvider = null };
        var factory = CreateFactory(customOptions);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.NotNull(result);
        _mockServiceProvider.Verify(
            sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)),
            Times.Once);
    }

    #endregion

    #region GetProviderWithFallback Tests

    [Fact]
    public void GetProviderWithFallback_WhenPrimaryHealthy_ReturnsPrimary()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<ILlmProvider>();
        mockPrimaryProvider.Setup(p => p.ProviderName).Returns("Anthropic");
        mockPrimaryProvider
            .Setup(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealthStatus
            {
                IsHealthy = true,
                StatusMessage = "Healthy"
            });

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockPrimaryProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.GetProviderWithFallback("anthropic");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Anthropic", result.ProviderName);
        mockPrimaryProvider.Verify(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetProviderWithFallback_WhenPrimaryUnhealthy_ReturnsFallback()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<ILlmProvider>();
        mockPrimaryProvider.Setup(p => p.ProviderName).Returns("Anthropic");
        mockPrimaryProvider
            .Setup(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealthStatus
            {
                IsHealthy = false,
                StatusMessage = "Unhealthy"
            });

        var mockFallbackProvider = new Mock<ILlmProvider>();
        mockFallbackProvider.Setup(p => p.ProviderName).Returns("Google");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Returns(mockPrimaryProvider.Object);

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter)))
            .Returns(mockFallbackProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.GetProviderWithFallback("anthropic");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Google", result.ProviderName);
        mockPrimaryProvider.Verify(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetProviderWithFallback_WhenPrimaryThrowsException_ReturnsFallback()
    {
        // Arrange
        var mockFallbackProvider = new Mock<ILlmProvider>();
        mockFallbackProvider.Setup(p => p.ProviderName).Returns("Google");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider)))
            .Throws(new InvalidOperationException("Provider not available"));

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter)))
            .Returns(mockFallbackProvider.Object);

        var factory = CreateFactory();

        // Act
        var result = factory.GetProviderWithFallback("anthropic");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Google", result.ProviderName);
    }

    #endregion

    #region GetAvailableProviders Tests

    [Fact]
    public void GetAvailableProviders_ReturnsAllThreeProviders()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.GetAvailableProviders();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("anthropic", result);
        Assert.Contains("google", result);
        Assert.Contains("openai", result);
    }

    #endregion
}
