using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for Claude Code CLI running in headless mode.
///
/// <para><strong>Claude Code CLI Interface Assumptions:</strong></para>
/// <para>This adapter assumes the Claude Code CLI supports the following interface:</para>
///
/// <code>
/// claude --headless --prompt "Your prompt here"
/// claude --headless --project-path "/path/to/project" --prompt "Your prompt here"
/// </code>
///
/// <para><strong>Expected CLI Flags:</strong></para>
/// <list type="bullet">
/// <item><description><c>--headless</c>: Run CLI in non-interactive mode without opening the desktop application</description></item>
/// <item><description><c>--prompt</c>: The prompt text to send to Claude (required)</description></item>
/// <item><description><c>--project-path</c>: Optional path to a project directory for full codebase context</description></item>
/// </list>
///
/// <para><strong>Expected Output Format:</strong></para>
/// <para>The CLI should output the response on stdout in plain text format. Optionally, it may include:</para>
/// <list type="bullet">
/// <item><description>File operations in JSON format: <c>{"operation": "create|update|delete", "filePath": "...", "content": "..."}</c></description></item>
/// <item><description>Metadata lines like <c>Tokens used: 1234</c> or <c>Model: claude-sonnet-4-5</c></description></item>
/// </list>
///
/// <para><strong>Example CLI Commands:</strong></para>
/// <code>
/// # Basic prompt
/// claude --headless --prompt "Explain this code"
///
/// # With project context
/// claude --headless --project-path "/home/user/myproject" --prompt "Refactor this function"
/// </code>
///
/// <para><strong>Exit Codes:</strong></para>
/// <list type="bullet">
/// <item><description><c>0</c>: Success</description></item>
/// <item><description>Non-zero: Error (error details should be on stderr)</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// This is a hypothetical CLI interface. If the actual Claude Code CLI has a different interface,
/// this adapter will need to be updated to match the actual CLI command structure.
/// </para>
/// <para>
/// For production use, verify the Claude Code CLI documentation and update this adapter accordingly.
/// </para>
/// </remarks>
public class ClaudeCodeCliAdapter : ICliAgent
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<ClaudeCodeCliAdapter> _logger;
    private readonly ClaudeCodeCliOptions _options;

    public string AgentName => "Claude Code CLI";
    public bool SupportsStreaming => true;

    public ClaudeCodeCliAdapter(
        IProcessExecutor processExecutor,
        ILogger<ClaudeCodeCliAdapter> logger,
        IOptions<ClaudeCodeCliOptions> options)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the capabilities of Claude Code CLI
    /// </summary>
    public CliAgentCapabilities GetCapabilities()
    {
        return CliAgentCapabilities.ForClaudeCode();
    }

    /// <summary>
    /// Executes a prompt using Claude Code CLI in headless mode
    /// </summary>
    public async Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (_options.EnableVerboseLogging)
        {
            _logger.LogInformation("Executing prompt with Claude Code CLI");
            _logger.LogDebug("Prompt: {Prompt}", prompt);
        }
        else
        {
            _logger.LogInformation("Executing prompt with Claude Code CLI");
        }

        // Build arguments for headless mode
        var arguments = BuildArguments(prompt, projectPath: null);

        // Execute the CLI command
        var result = await _processExecutor.ExecuteAsync(
            _options.ExecutablePath,
            arguments,
            workingDirectory: null,
            timeoutSeconds: _options.DefaultTimeoutSeconds,
            cancellationToken: cancellationToken);

        return ParseCliResponse(result);
    }

    /// <summary>
    /// Executes a prompt with full project context
    /// </summary>
    public async Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be empty", nameof(projectPath));

        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        _logger.LogInformation(
            "Executing prompt with Claude Code CLI with project context: {ProjectPath}",
            projectPath);

        // Build arguments with project context
        var arguments = BuildArguments(prompt, projectPath);

        // Execute the CLI command
        var result = await _processExecutor.ExecuteAsync(
            _options.ExecutablePath,
            arguments,
            workingDirectory: projectPath,
            timeoutSeconds: _options.ProjectContextTimeoutSeconds,
            cancellationToken: cancellationToken);

        return ParseCliResponse(result);
    }

    /// <summary>
    /// Executes a prompt with streaming output
    /// </summary>
    public async Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (onOutputReceived == null)
            throw new ArgumentNullException(nameof(onOutputReceived));

        _logger.LogInformation("Executing streaming prompt with Claude Code CLI");

        var arguments = BuildArguments(prompt, projectPath: null);

        // Execute with streaming
        var result = await _processExecutor.ExecuteStreamingAsync(
            _options.ExecutablePath,
            arguments,
            onOutputReceived: onOutputReceived,
            onErrorReceived: errorLine => _logger.LogWarning("Claude CLI error: {Error}", errorLine),
            workingDirectory: null,
            timeoutSeconds: _options.StreamingTimeoutSeconds,
            cancellationToken: cancellationToken);

        return ParseCliResponse(result);
    }

    /// <summary>
    /// Builds command line arguments for Claude CLI.
    /// Returns an argument list which is safer than string concatenation.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="projectPath">Optional project path for context</param>
    /// <returns>List of command line arguments (no escaping needed)</returns>
    private IEnumerable<string> BuildArguments(string prompt, string? projectPath)
    {
        var args = new List<string>();

        // Use headless mode
        args.Add("--headless");

        // Add project path if provided
        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            args.Add("--project-path");
            args.Add(projectPath);
        }

        // Add the prompt (no escaping needed with ArgumentList)
        args.Add("--prompt");
        args.Add(prompt);

        return args;
    }

    /// <summary>
    /// Parses the CLI response into a CliAgentResponse
    /// </summary>
    private CliAgentResponse ParseCliResponse(ProcessExecutionResult result)
    {
        var response = new CliAgentResponse
        {
            Success = result.Success,
            ExitCode = result.ExitCode,
            Metadata = new Dictionary<string, object>
            {
                ["Duration"] = result.Duration.TotalSeconds,
                ["AgentName"] = AgentName
            }
        };

        if (!result.Success)
        {
            response.ErrorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? $"Claude CLI failed with exit code {result.ExitCode}"
                : result.Error;

            _logger.LogError(
                "Claude CLI execution failed with exit code {ExitCode}: {Error}",
                result.ExitCode,
                response.ErrorMessage);

            return response;
        }

        // Try to parse the output
        response.Content = result.Output;

        // Try to extract file operations from the output
        // Claude Code CLI may output file operations in a specific format
        response.FileOperations = ExtractFileOperations(result.Output);

        // Extract metadata if available
        ExtractMetadata(result.Output, response.Metadata);

        _logger.LogInformation(
            "Claude CLI execution completed successfully in {Duration}s",
            result.Duration.TotalSeconds);

        return response;
    }

    /// <summary>
    /// Attempts to extract file operations from Claude's output
    /// </summary>
    private List<FileOperation> ExtractFileOperations(string output)
    {
        var operations = new List<FileOperation>();

        // This is a placeholder implementation
        // In a real implementation, you would parse Claude's output format
        // which might include markers for file operations like:
        // [FILE_CREATE: path/to/file.cs]
        // [FILE_UPDATE: path/to/file.cs]
        // [FILE_DELETE: path/to/file.cs]

        try
        {
            // Try to find JSON-formatted file operations
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("{") && line.Contains("\"operation\""))
                {
                    try
                    {
                        var operation = JsonSerializer.Deserialize<FileOperationDto>(line);
                        if (operation != null && !string.IsNullOrWhiteSpace(operation.Operation))
                        {
                            operations.Add(new FileOperation
                            {
                                OperationType = operation.Operation,
                                FilePath = operation.FilePath ?? string.Empty,
                                Content = operation.Content
                            });
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for individual lines
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting file operations from output");
        }

        return operations;
    }

    /// <summary>
    /// Extracts metadata from Claude's output
    /// </summary>
    private void ExtractMetadata(string output, Dictionary<string, object> metadata)
    {
        try
        {
            // Look for common metadata patterns in the output
            // For example: "Tokens used: 1234"
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("Tokens used:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var tokens))
                    {
                        metadata["TokensUsed"] = tokens;
                    }
                }
                else if (line.Contains("Model:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        metadata["Model"] = parts[1].Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting metadata from output");
        }
    }

    /// <summary>
    /// DTO for deserializing file operations from JSON
    /// </summary>
    private class FileOperationDto
    {
        public string? Operation { get; set; }
        public string? FilePath { get; set; }
        public string? Content { get; set; }
    }
}
