using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Command;

/// <summary>
/// Execute shell commands with timeout and whitelist validation.
/// </summary>
public class ExecuteShellTool : ToolBase
{
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "dotnet", "npm", "node", "git", "make", "cmake", "mvn", "gradle",
        "yarn", "pnpm", "cargo", "rustc", "go", "python", "python3", "pip", "pip3"
    };

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "ExecuteShell";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Execute shell commands with timeout (whitelisted commands only)";

    /// <summary>
    /// Create a new ExecuteShellTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public ExecuteShellTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the shell command
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Command output (stdout and stderr)</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var command = context.GetParameter<string>("command");
        var workingDirectory = context.GetOptionalParameter<string>("workingDirectory", ".") ?? ".";
        var timeoutSeconds = context.GetOptionalParameter<int>("timeout", 30);

        // 1. Validate working directory
        var fullWorkingDir = PathValidator.ValidateAndResolve(workingDirectory, context.WorkspacePath);
        if (!Directory.Exists(fullWorkingDir))
        {
            throw new DirectoryNotFoundException($"Working directory '{workingDirectory}' does not exist");
        }

        // 2. Validate timeout
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        if (timeout > MaxTimeout)
        {
            throw new ArgumentException($"Timeout {timeout} exceeds maximum {MaxTimeout}");
        }

        // 3. Parse and validate command
        var (executable, arguments) = ParseCommand(command);
        ValidateCommand(executable);

        // 4. Execute command with timeout
        var result = await ExecuteCommandAsync(executable, arguments, fullWorkingDir, timeout);

        _logger.LogInformation(
            "Executed command '{Command}' with exit code {ExitCode} for tenant {TenantId}",
            command, result.ExitCode, context.TenantId);

        return FormatOutput(result);
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("command"))
            throw new ArgumentException("Parameter 'command' is required");

        var command = context.GetParameter<string>("command");
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Parse command into executable and arguments
    /// </summary>
    private static (string executable, string arguments) ParseCommand(string command)
    {
        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Command is empty");
        }

        var executable = parts[0];
        var arguments = parts.Length > 1 ? parts[1] : string.Empty;

        return (executable, arguments);
    }

    /// <summary>
    /// Validate command against whitelist
    /// </summary>
    private static void ValidateCommand(string executable)
    {
        // Remove path and extension for validation
        var commandName = Path.GetFileNameWithoutExtension(executable);

        if (!AllowedCommands.Contains(commandName))
        {
            throw new UnauthorizedAccessException(
                $"Command '{commandName}' is not whitelisted. " +
                $"Allowed commands: {string.Join(", ", AllowedCommands)}");
        }

        // Prevent path traversal in executable
        if (executable.Contains("..") || Path.IsPathRooted(executable))
        {
            throw new ArgumentException("Executable path cannot contain '..' or be rooted");
        }
    }

    /// <summary>
    /// Execute command and capture output
    /// </summary>
    private async Task<CommandResult> ExecuteCommandAsync(
        string executable,
        string arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process { StartInfo = processInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await process.WaitForExitAsync(timeout);
        if (!completed)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore kill errors
            }

            throw new ToolTimeoutException(Name, timeout);
        }

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }

    /// <summary>
    /// Format command output
    /// </summary>
    private static string FormatOutput(CommandResult result)
    {
        var output = new StringBuilder();
        output.AppendLine($"Exit Code: {result.ExitCode}");
        output.AppendLine();

        if (!string.IsNullOrEmpty(result.StandardOutput))
        {
            output.AppendLine("Standard Output:");
            output.AppendLine(result.StandardOutput);
        }

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            output.AppendLine("Standard Error:");
            output.AppendLine(result.StandardError);
        }

        return output.ToString();
    }

    private class CommandResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}

/// <summary>
/// Extension methods for Process
/// </summary>
internal static class ProcessExtensions
{
    /// <summary>
    /// Wait for process to exit with timeout
    /// </summary>
    public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
