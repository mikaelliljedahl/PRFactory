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
/// LLM provider adapter for OpenAI CLI.
///
/// IMPORTANT: This implementation assumes an OpenAI CLI with similar interface to Claude Code CLI.
/// As of implementation date, OpenAI does not provide an official standalone CLI tool.
///
/// Options for production use:
/// 1. Use OpenAI .NET SDK with direct API calls instead of CLI (recommended)
/// 2. Create a custom CLI wrapper around OpenAI API
/// 3. Use third-party CLIs like 'openai-cli' (npm package)
/// 4. Use 'chatgpt-cli' or similar community tools
///
/// Installation (third-party option):
/// - Via npm: npm install -g openai-cli
/// - Set API key: export OPENAI_API_KEY="your-key"
/// - Verify: openai --version
///
/// Recommended Alternative:
/// - Implement direct API integration using OpenAI .NET SDK (Betalgo.OpenAI or official SDK)
/// - This provides better control, error handling, and feature support
/// </summary>
public class OpenAiCliAdapter : ILlmProvider
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<OpenAiCliAdapter> _logger;
    private readonly ProviderOptions _options;

    public OpenAiCliAdapter(
        IProcessExecutor processExecutor,
        ILogger<OpenAiCliAdapter> logger,
        IOptions<LlmProvidersOptions> llmOptions)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var providerOptions = llmOptions?.Value?.Providers?.GetValueOrDefault("openai");
        _options = providerOptions ?? new ProviderOptions
        {
            Enabled = false,
            CliPath = "openai",
            DefaultModel = "gpt-4o",
            TimeoutSeconds = 300,
            MaxTokens = 4000
        };
    }

    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "OpenAI (CLI)";

    /// <summary>
    /// Supported OpenAI models
    /// </summary>
    public List<string> SupportedModels => new()
    {
        "gpt-4o",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo",
        "gpt-4o-mini"
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
                ErrorMessage = "OpenAI provider is not enabled in configuration. " +
                              "Note: OpenAI does not provide an official CLI. " +
                              "Consider using OpenAI .NET SDK with direct API calls instead."
            };
        }

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing prompt with OpenAI CLI (Provider: {Provider})", ProviderName);

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

        // Add temperature if specified
        if (options?.Temperature.HasValue == true)
        {
            args.Add("--temperature");
            args.Add(options.Temperature.Value.ToString("F1"));
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
                ErrorMessage = "OpenAI provider is not enabled in configuration."
            };
        }

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Executing streaming prompt with OpenAI CLI");

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
                ErrorMessage = "OpenAI provider is not enabled in configuration."
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
            "Executing prompt with OpenAI CLI with project context: {ProjectPath}",
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
                StatusMessage = "OpenAI provider is disabled in configuration"
            };
        }

        try
        {
            // Check if CLI is installed
            var versionResult = await _processExecutor.ExecuteAsync(
                _options.CliPath,
                new[] { "--version" },
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
                    StatusMessage = $"OpenAI CLI not installed or not in PATH. " +
                                  $"Note: OpenAI does not provide an official CLI. " +
                                  $"See adapter documentation for alternatives (OpenAI .NET SDK recommended)."
                };
            }

            // Check if authenticated (check for OPENAI_API_KEY environment variable)
            var hasApiKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            if (!hasApiKey)
            {
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    IsInstalled = true,
                    IsAuthenticated = false,
                    StatusMessage = "OpenAI CLI not authenticated. Set environment variable: OPENAI_API_KEY"
                };
            }

            return new ProviderHealthStatus
            {
                IsHealthy = true,
                IsInstalled = true,
                IsAuthenticated = true,
                StatusMessage = $"OpenAI CLI ready (Version: {versionResult.Output.Trim()})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OpenAI CLI health");

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
                ? $"OpenAI CLI failed with exit code {result.ExitCode}"
                : result.Error;

            _logger.LogError(
                "OpenAI CLI execution failed with exit code {ExitCode}: {Error}",
                result.ExitCode,
                errorMessage);

            return new LlmResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = errorMessage,
                Usage = new LlmUsageMetrics { Latency = latency }
            };
        }

        var usage = ExtractUsageMetrics(result.Output, latency);

        _logger.LogInformation(
            "OpenAI CLI execution completed successfully in {Latency}ms (Tokens: {InputTokens} in, {OutputTokens} out)",
            latency.TotalMilliseconds,
            usage.InputTokens,
            usage.OutputTokens);

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
            // OpenAI typically returns usage info in JSON format
            var inputTokenMatch = Regex.Match(output, @"(?:prompt_tokens|input_tokens).*?:\s*(\d+)", RegexOptions.IgnoreCase);
            if (inputTokenMatch.Success && int.TryParse(inputTokenMatch.Groups[1].Value, out var inputTokens))
            {
                metrics.InputTokens = inputTokens;
            }

            var outputTokenMatch = Regex.Match(output, @"(?:completion_tokens|output_tokens).*?:\s*(\d+)", RegexOptions.IgnoreCase);
            if (outputTokenMatch.Success && int.TryParse(outputTokenMatch.Groups[1].Value, out var outputTokens))
            {
                metrics.OutputTokens = outputTokens;
            }

            var totalTokenMatch = Regex.Match(output, @"total_tokens.*?:\s*(\d+)", RegexOptions.IgnoreCase);
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
            _logger.LogWarning(ex, "Error extracting token metrics from OpenAI CLI output");
        }

        return metrics;
    }

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
