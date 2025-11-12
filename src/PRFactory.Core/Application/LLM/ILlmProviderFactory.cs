using System;
using System.Collections.Generic;

namespace PRFactory.Core.Application.LLM;

/// <summary>
/// Factory for creating LLM provider instances.
/// Supports provider selection, fallback logic, and health checks.
/// </summary>
public interface ILlmProviderFactory
{
    /// <summary>
    /// Create provider by name
    /// </summary>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <returns>LLM provider instance</returns>
    /// <exception cref="NotSupportedException">If provider is not supported</exception>
    ILlmProvider CreateProvider(string providerName);

    /// <summary>
    /// Get default provider from configuration
    /// </summary>
    /// <returns>Default LLM provider instance</returns>
    ILlmProvider GetDefaultProvider();

    /// <summary>
    /// Get provider with fallback logic
    /// </summary>
    /// <param name="primaryProvider">Primary provider to try first</param>
    /// <param name="fallbackProvider">Fallback provider if primary fails (optional, uses configured fallback if null)</param>
    /// <returns>Healthy LLM provider instance</returns>
    ILlmProvider GetProviderWithFallback(string primaryProvider, string? fallbackProvider = null);

    /// <summary>
    /// List all available providers
    /// </summary>
    /// <returns>List of provider names</returns>
    List<string> GetAvailableProviders();
}
