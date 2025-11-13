using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Configuration;
using PRFactory.Infrastructure.Execution;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// LLM provider adapter for Google Gemini CLI.
///
/// IMPORTANT: This implementation assumes a Gemini CLI with similar interface to Claude Code CLI.
/// As of implementation date, Google does not provide an official standalone Gemini CLI tool.
///
/// Options for production use:
/// 1. Use Google AI SDK with direct API calls instead of CLI (recommended)
/// 2. Create a custom CLI wrapper around Google AI API
/// 3. Use gcloud CLI with AI commands (if available)
/// 4. Wait for official Gemini CLI release
///
/// Installation (if/when available):
/// - Via npm: npm install -g @google/gemini-cli
/// - Via gcloud: gcloud components install gemini-cli
/// - Verify: gemini --version
/// </summary>
public partial class GeminiCliAdapter : ILlmProvider
{
    private static readonly string[] VersionArgs = { "--version" };
    private static readonly string[] AuthStatusArgs = { "auth", "status" };

    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<GeminiCliAdapter> _logger;
    private readonly ProviderOptions _options;

    public GeminiCliAdapter(
        IProcessExecutor processExecutor,
        ILogger<GeminiCliAdapter> logger,
        IOptions<LlmProvidersOptions> llmOptions)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var providerOptions = llmOptions?.Value?.Providers?.GetValueOrDefault("google");
        _options = providerOptions ?? new ProviderOptions
        {
            Enabled = false,
            CliPath = "gemini",
            DefaultModel = "gemini-pro",
            TimeoutSeconds = 300,
            MaxTokens = 8000
        };
    }

    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "Google (Gemini CLI)";

    /// <summary>
    /// Supported Gemini models
    /// </summary>
    public List<string> SupportedModels => new()
    {
        "gemini-pro",
        "gemini-pro-vision",
        "gemini-ultra",
        "gemini-1.5-pro",
        "gemini-1.5-flash"
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
        if (!_options.Enabled)
        {
            return new LlmResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = "Gemini provider is not enabled in configuration. " +
                              "Note: Google does not currently provide an official Gemini CLI. " +
                              "Consider using Google AI SDK with direct API calls instead."
            };
        }

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing prompt with Gemini CLI (Provider: {Provider})", ProviderName);

        // Build command arguments (assumed CLI interface)
        var args = new List<string>
        {
            "--headless",
            "--model", options?.Model ?? _options.DefaultModel,
            "--prompt", prompt
        };

        // Add system prompt if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // Add max tokens if specified
        if (options?.MaxTokens.HasValue == true)
        {
            args.Add("--max-tokens");
            args.Add(options.MaxTokens.Value.ToString());
        }

        // Execute CLI command
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.TimeoutSeconds;
        var result = await _processExecutor.ExecuteAsync(
            _options.CliPath,
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
        if (!_options.Enabled)
        {
            return new LlmStreamingResponse
            {
                Success = false,
                ContentStream = null,
                ErrorMessage = "Gemini provider is not enabled in configuration."
            };
        }

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing streaming prompt with Gemini CLI");

        // Build command arguments
        var args = new List<string>
        {
            "--headless",
            "--stream",
            "--model", options?.Model ?? _options.DefaultModel,
            "--prompt", prompt
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // Collect streaming output
        var contentChunks = new List<string>();
        var errorOutput = new List<string>();

        // Execute with streaming
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.TimeoutSeconds;
        var result = await _processExecutor.ExecuteStreamingAsync(
            _options.CliPath,
            args,
            onOutputReceived: chunk => contentChunks.Add(chunk),
            onErrorReceived: error => errorOutput.Add(error),
            workingDirectory: null,
            timeoutSeconds: timeoutSeconds,
            cancellationToken: ct);

        var latency = DateTime.UtcNow - startTime;

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
        if (!_options.Enabled)
        {
            return new LlmResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = "Gemini provider is not enabled in configuration."
            };
        }

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be empty", nameof(projectPath));

        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Executing prompt with Gemini CLI with project context: {ProjectPath}",
            projectPath);

        // Build command arguments with project context
        var args = new List<string>
        {
            "--headless",
            "--model", options?.Model ?? _options.DefaultModel,
            "--project-path", projectPath,
            "--prompt", prompt
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            args.Insert(1, "--system");
            args.Insert(2, systemPrompt);
        }

        // Execute CLI command
        var timeoutSeconds = options?.TimeoutSeconds ?? _options.TimeoutSeconds;
        var result = await _processExecutor.ExecuteAsync(
            _options.CliPath,
            args,
            workingDirectory: projectPath,
            environmentVariables: null,
            timeoutSeconds: timeoutSeconds,
            cancellationToken: ct);

        var latency = DateTime.UtcNow - startTime;

        return ParseCliResponse(result, latency);
    }

    /// <summary>
    /// Check if provider is healthy (CLI installed, authenticated)
    /// </summary>
    public async Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return new ProviderHealthStatus
            {
                IsHealthy = false,
                IsInstalled = false,
                IsAuthenticated = false,
                StatusMessage = "Gemini provider is disabled in configuration"
            };
        }

        try
        {
            // Check if CLI is installed
            var versionResult = await _processExecutor.ExecuteAsync(
                _options.CliPath,
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
                    StatusMessage = $"Gemini CLI not installed or not in PATH. " +
                                  $"Note: Google does not currently provide an official Gemini CLI. " +
                                  $"See adapter documentation for alternatives."
                };
            }

            // Check if authenticated (command may vary)
            var authResult = await _processExecutor.ExecuteAsync(
                _options.CliPath,
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
                    StatusMessage = "Gemini CLI not authenticated. Run: gemini auth login"
                };
            }

            return new ProviderHealthStatus
            {
                IsHealthy = true,
                IsInstalled = true,
                IsAuthenticated = true,
                StatusMessage = $"Gemini CLI ready (Version: {versionResult.Output.Trim()})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Gemini CLI health");

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
    private LlmResponse ParseCliResponse(ProcessExecutionResult result, TimeSpan latency)
    {
        if (!result.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? $"Gemini CLI failed with exit code {result.ExitCode}"
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
    private LlmUsageMetrics ExtractUsageMetrics(string output, TimeSpan latency)
    {
        var metrics = new LlmUsageMetrics
        {
            Latency = latency
        };

        try
        {
            // Look for token usage patterns in the output
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

            var totalTokenMatch = TotalTokenPattern().Match(output);
            if (totalTokenMatch.Success && int.TryParse(totalTokenMatch.Groups[1].Value, out var totalTokens))
            {
                metrics.TotalTokens = totalTokens;
            }

            if (metrics.TotalTokens == 0 && (metrics.InputTokens > 0 || metrics.OutputTokens > 0))
            {
                metrics.TotalTokens = metrics.InputTokens + metrics.OutputTokens;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting token metrics from Gemini CLI output");
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
            await Task.Delay(1);
        }
    }
}
