using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Configuration;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Factory for creating LLM provider instances.
/// Supports provider selection, fallback logic, and health checks.
/// </summary>
public class LlmProviderFactory : ILlmProviderFactory
{
    private const string AnthropicProviderName = "anthropic";

    private readonly IServiceProvider _serviceProvider;
    private readonly LlmProvidersOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmProviderFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="options">LLM provider configuration options</param>
    public LlmProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<LlmProvidersOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <summary>
    /// Create provider by name
    /// </summary>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <returns>LLM provider instance</returns>
    /// <exception cref="NotSupportedException">If provider is not supported</exception>
    public ILlmProvider CreateProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            // TODO: Implement LLM adapter providers
            // AnthropicProviderName or "claude" =>
            //     _serviceProvider.GetRequiredService<PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider>(),
            //
            // "google" or "gemini" =>
            //     _serviceProvider.GetRequiredService<PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter>(),
            //
            // "openai" or "gpt" =>
            //     _serviceProvider.GetRequiredService<PRFactory.Infrastructure.Agents.Adapters.OpenAiCliAdapter>(),

            _ => throw new NotSupportedException($"Provider '{providerName}' is not supported. " +
                $"Supported providers: {string.Join(", ", GetAvailableProviders())}")
        };
    }

    /// <summary>
    /// Get default provider from configuration
    /// </summary>
    /// <returns>Default LLM provider instance</returns>
    public ILlmProvider GetDefaultProvider()
    {
        var defaultProvider = _options.DefaultProvider ?? AnthropicProviderName;
        return CreateProvider(defaultProvider);
    }

    /// <summary>
    /// Get provider with fallback logic
    /// </summary>
    /// <param name="primaryProvider">Primary provider to try first</param>
    /// <param name="fallbackProvider">Fallback provider if primary fails (optional, uses configured fallback if null)</param>
    /// <returns>Healthy LLM provider instance</returns>
    public ILlmProvider GetProviderWithFallback(string primaryProvider, string? fallbackProvider = null)
    {
        try
        {
            var provider = CreateProvider(primaryProvider);

            // Check health (synchronously for simplicity in factory)
            var health = provider.CheckHealthAsync().GetAwaiter().GetResult();
            if (health.IsHealthy)
            {
                return provider;
            }
        }
        catch
        {
            // Primary failed, continue to fallback
        }

        // Use fallback or configured fallback
        var fallback = fallbackProvider ?? _options.FallbackProvider ?? AnthropicProviderName;
        return CreateProvider(fallback);
    }

    /// <summary>
    /// List all available providers
    /// </summary>
    /// <returns>List of provider names</returns>
    public List<string> GetAvailableProviders()
    {
        List<string> providers = [AnthropicProviderName, "google", "openai"];
        return providers;
    }
}
