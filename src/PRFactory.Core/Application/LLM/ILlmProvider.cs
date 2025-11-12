using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Core.Application.LLM;

/// <summary>
/// Abstraction for LLM (Large Language Model) providers.
/// Supports multiple providers: Anthropic Claude, OpenAI GPT, Google Gemini, etc.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Provider name (e.g., "Anthropic", "OpenAI", "Google")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Supported models for this provider
    /// </summary>
    List<string> SupportedModels { get; }

    /// <summary>
    /// Execute a prompt and return response
    /// </summary>
    /// <param name="prompt">User prompt/message</param>
    /// <param name="systemPrompt">System prompt (optional)</param>
    /// <param name="options">LLM execution options (model, max tokens, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>LLM response with content and usage metrics</returns>
    Task<LlmResponse> SendMessageAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute with streaming response
    /// </summary>
    /// <param name="prompt">User prompt/message</param>
    /// <param name="systemPrompt">System prompt (optional)</param>
    /// <param name="options">LLM execution options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Streaming response</returns>
    Task<LlmStreamingResponse> SendMessageStreamAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute with project context (provide codebase path)
    /// </summary>
    /// <param name="prompt">User prompt/message</param>
    /// <param name="projectPath">Path to project/codebase</param>
    /// <param name="systemPrompt">System prompt (optional)</param>
    /// <param name="options">LLM execution options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>LLM response with content and usage metrics</returns>
    Task<LlmResponse> SendMessageWithContextAsync(
        string prompt,
        string projectPath,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Check if provider is healthy (CLI installed, authenticated)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Provider health status</returns>
    Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>
/// Options for LLM execution
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// Model name (e.g., "claude-sonnet-4-5", "gpt-4o", "gemini-pro")
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Maximum tokens in response
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature for response generation (0.0-1.0)
    /// Lower = more deterministic, Higher = more creative
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Timeout in seconds for the LLM request
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Response from LLM provider
/// </summary>
public class LlmResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response content from the LLM
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Usage metrics (tokens, latency)
    /// </summary>
    public LlmUsageMetrics Usage { get; set; } = new();
}

/// <summary>
/// Usage metrics for LLM requests
/// </summary>
public class LlmUsageMetrics
{
    /// <summary>
    /// Number of input tokens
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output)
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Latency for the request
    /// </summary>
    public TimeSpan Latency { get; set; }
}

/// <summary>
/// Streaming response from LLM provider
/// </summary>
public class LlmStreamingResponse
{
    /// <summary>
    /// Whether streaming started successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Stream of content chunks
    /// </summary>
    public IAsyncEnumerable<string>? ContentStream { get; set; }

    /// <summary>
    /// Error message if streaming failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Usage metrics (populated after streaming completes)
    /// </summary>
    public LlmUsageMetrics Usage { get; set; } = new();
}

/// <summary>
/// Health status of an LLM provider
/// </summary>
public class ProviderHealthStatus
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Status message describing the health state
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Whether the provider CLI/SDK is installed
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Whether the provider is authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }
}
