using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRFactory.Core.Application.LLM;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// LLM provider adapter for Claude Code CLI.
/// Implements ILlmProvider interface for multi-provider support.
/// </summary>
public partial class ClaudeCodeCliLlmProvider : ILlmProvider
{
    private static readonly string[] VersionArgs = { "--version" };
    private static readonly string[] AuthStatusArgs = { "auth", "status" };

    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<ClaudeCodeCliLlmProvider> _logger;
    private readonly ClaudeCodeCliOptions _options;

    public ClaudeCodeCliLlmProvider(
        IProcessExecutor processExecutor,
        ILogger<ClaudeCodeCliLlmProvider> logger,
        IOptions<ClaudeCodeCliOptions> options)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "Anthropic (Claude Code CLI)";

    /// <summary>
    /// Supported Claude models
    /// </summary>
    public List<string> SupportedModels => new()
    {
        "claude-sonnet-4-5-20250929",
        "claude-opus-4-20250514",
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022"
    };

    /// <summary>
    /// Execute a prompt and return response
    /// </summary>
    public async Task<LlmResponse> SendMessageAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing prompt with Claude Code CLI (Provider: {Provider})", ProviderName);

        // Build command arguments
        var args = new List<string> { "--headless", "--prompt", prompt };

        // Add system prompt if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // Execute CLI command
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;
        var result = await _processExecutor.ExecuteAsync(
            _options.ExecutablePath,
            args,
            workingDirectory: null,
            environmentVariables: null,
            timeoutSeconds: timeoutSeconds,
            cancellationToken: ct);

        var latency = DateTime.UtcNow - startTime;

        // Parse response
        return ParseCliResponse(result, latency);
    }

    /// <summary>
    /// Execute with streaming response
    /// </summary>
    public async Task<LlmStreamingResponse> SendMessageStreamAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing streaming prompt with Claude Code CLI");

        // Build command arguments
        var args = new List<string> { "--headless", "--prompt", prompt };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // For streaming, we'll collect output chunks
        var contentChunks = new List<string>();
        var errorOutput = new List<string>();

        // Execute with streaming
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.StreamingTimeoutSeconds;
        var result = await _processExecutor.ExecuteStreamingAsync(
            _options.ExecutablePath,
            args,
            onOutputReceived: chunk => contentChunks.Add(chunk),
            onErrorReceived: error => errorOutput.Add(error),
            workingDirectory: null,
            timeoutSeconds: timeoutSeconds,
            cancellationToken: ct);

        var latency = DateTime.UtcNow - startTime;

        // Return streaming response
        return new LlmStreamingResponse
        {
            Success = result.Success,
            ContentStream = ConvertToAsyncEnumerable(contentChunks),
            ErrorMessage = result.Success ? null : string.Join("\n", errorOutput),
            Usage = ExtractUsageMetrics(result.Output, latency)
        };
    }

    /// <summary>
    /// Execute with project context (provide codebase path)
    /// </summary>
    public async Task<LlmResponse> SendMessageWithContextAsync(
        string prompt,
        string projectPath,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be empty", nameof(projectPath));

        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Executing prompt with Claude Code CLI with project context: {ProjectPath}",
            projectPath);

        // Build command arguments with project context
        var args = new List<string>
        {
            "--headless",
            "--project-path", projectPath,
            "--prompt", prompt
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // Execute CLI command
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.ProjectContextTimeoutSeconds;
        var result = await _processExecutor.ExecuteAsync(
            _options.ExecutablePath,
            args,
            workingDirectory: projectPath,
            environmentVariables: null,
            timeoutSeconds: timeoutSeconds,
            cancellationToken: ct);

        var latency = DateTime.UtcNow - startTime;

        // Parse response
        return ParseCliResponse(result, latency);
    }

    /// <summary>
    /// Check if provider is healthy (CLI installed, authenticated)
    /// </summary>
    public async Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if CLI is installed
            var versionResult = await _processExecutor.ExecuteAsync(
                _options.ExecutablePath,
                VersionArgs,
                workingDirectory: null,
                timeoutSeconds: 5,
                cancellationToken: ct);

            if (!versionResult.Success)
            {
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    IsInstalled = false,
                    IsAuthenticated = false,
                    StatusMessage = "Claude Code CLI not installed or not in PATH"
                };
            }

            // Check if authenticated
            var authResult = await _processExecutor.ExecuteAsync(
                _options.ExecutablePath,
                AuthStatusArgs,
                workingDirectory: null,
                timeoutSeconds: 5,
                cancellationToken: ct);

            if (!authResult.Success)
            {
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    IsInstalled = true,
                    IsAuthenticated = false,
                    StatusMessage = "Claude Code CLI not authenticated. Run: claude auth login"
                };
            }

            return new ProviderHealthStatus
            {
                IsHealthy = true,
                IsInstalled = true,
                IsAuthenticated = true,
                StatusMessage = $"Claude Code CLI ready (Version: {versionResult.Output.Trim()})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Claude Code CLI health");

            return new ProviderHealthStatus
            {
                IsHealthy = false,
                IsInstalled = false,
                IsAuthenticated = false,
                StatusMessage = $"Error checking health: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Parses the CLI response into an LlmResponse
    /// </summary>
    private static LlmResponse ParseCliResponse(ProcessExecutionResult result, TimeSpan latency)
    {
        if (!result.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? $"Claude CLI failed with exit code {result.ExitCode}"
                : result.Error;

            return new LlmResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = errorMessage,
                Usage = new LlmUsageMetrics { Latency = latency }
            };
        }

        var usage = ExtractUsageMetrics(result.Output, latency);

        return new LlmResponse
        {
            Success = true,
            Content = result.Output,
            ErrorMessage = null,
            Usage = usage
        };
    }

    /// <summary>
    /// Extracts usage metrics from CLI output
    /// </summary>
    private static LlmUsageMetrics ExtractUsageMetrics(string output, TimeSpan latency)
    {
        var metrics = new LlmUsageMetrics
        {
            Latency = latency
        };

        try
        {
            // Look for token usage patterns in the output
            // Example patterns: "Tokens used: 1234", "Input tokens: 100", "Output tokens: 50"

            var inputTokenMatch = InputTokenPattern().Match(output);
            if (inputTokenMatch.Success && int.TryParse(inputTokenMatch.Groups[1].Value, out var inputTokens))
            {
                metrics.InputTokens = inputTokens;
            }

            var outputTokenMatch = OutputTokenPattern().Match(output);
            if (outputTokenMatch.Success && int.TryParse(outputTokenMatch.Groups[1].Value, out var outputTokens))
            {
                metrics.OutputTokens = outputTokens;
            }

            // If total tokens are provided
            var totalTokenMatch = TotalTokenPattern().Match(output);
            if (totalTokenMatch.Success && int.TryParse(totalTokenMatch.Groups[1].Value, out var totalTokens))
            {
                metrics.TotalTokens = totalTokens;
            }

            // Calculate total if not provided
            if (metrics.TotalTokens == 0 && (metrics.InputTokens > 0 || metrics.OutputTokens > 0))
            {
                metrics.TotalTokens = metrics.InputTokens + metrics.OutputTokens;
            }
        }
        catch (Exception)
        {
        }

        return metrics;
    }

    [GeneratedRegex(@"Input tokens:\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex InputTokenPattern();

    [GeneratedRegex(@"Output tokens:\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex OutputTokenPattern();

    [GeneratedRegex(@"(?:Total tokens|Tokens used):\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalTokenPattern();

    /// <summary>
    /// Converts a list of strings to an async enumerable for streaming
    /// </summary>
    private static async IAsyncEnumerable<string> ConvertToAsyncEnumerable(List<string> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Delay(1); // Small delay to maintain async behavior
        }
    }
}
