using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Command;

/// <summary>
/// Execute .NET test suite with optional filter.
/// </summary>
public class RunTestsTool : ToolBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "RunTests";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Execute .NET test suite with optional filter";

    /// <summary>
    /// Create a new RunTestsTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public RunTestsTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the tests
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Test results</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var testFilter = context.GetOptionalParameter<string>("testFilter", null);

        // 1. Validate repository path
        var fullRepoPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullRepoPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Build dotnet test command
        var arguments = "test --no-build --verbosity normal";
        if (!string.IsNullOrEmpty(testFilter))
        {
            // Sanitize filter to prevent injection
            if (testFilter.Contains("\"") || testFilter.Contains(";") || testFilter.Contains("&"))
            {
                throw new ArgumentException("Test filter contains invalid characters");
            }
            arguments += $" --filter \"{testFilter}\"";
        }

        // 3. Execute tests with timeout
        var result = await ExecuteWithTimeoutAsync(
            () => ExecuteCommandAsync("dotnet", arguments, fullRepoPath),
            TestTimeout
        );

        _logger.LogInformation(
            "Executed tests in {RepositoryPath} with exit code {ExitCode} for tenant {TenantId}",
            repositoryPath, result.ExitCode, context.TenantId);

        return FormatTestOutput(result);
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("repositoryPath"))
            throw new ArgumentException("Parameter 'repositoryPath' is required");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Execute command and capture output
    /// </summary>
    private static async Task<CommandResult> ExecuteCommandAsync(
        string executable,
        string arguments,
        string workingDirectory)
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

        await process.WaitForExitAsync();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }

    /// <summary>
    /// Format test output
    /// </summary>
    private static string FormatTestOutput(CommandResult result)
    {
        var output = new StringBuilder();

        // Parse test results from output
        var passed = 0;
        var failed = 0;
        var skipped = 0;

        var lines = result.StandardOutput.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Passed!"))
            {
                // Extract counts from summary line
                var parts = line.Split(',');
                foreach (var part in parts)
                {
                    if (part.Contains("Passed:"))
                    {
                        var numStr = new string(part.Where(char.IsDigit).ToArray());
                        if (int.TryParse(numStr, out var num)) passed = num;
                    }
                    else if (part.Contains("Failed:"))
                    {
                        var numStr = new string(part.Where(char.IsDigit).ToArray());
                        if (int.TryParse(numStr, out var num)) failed = num;
                    }
                    else if (part.Contains("Skipped:"))
                    {
                        var numStr = new string(part.Where(char.IsDigit).ToArray());
                        if (int.TryParse(numStr, out var num)) skipped = num;
                    }
                }
            }
        }

        output.AppendLine("Test Results");
        output.AppendLine("============");
        output.AppendLine($"Exit Code: {result.ExitCode}");
        output.AppendLine($"Passed: {passed}");
        output.AppendLine($"Failed: {failed}");
        output.AppendLine($"Skipped: {skipped}");
        output.AppendLine();

        if (result.ExitCode == 0)
        {
            output.AppendLine("All tests passed!");
        }
        else
        {
            output.AppendLine("Some tests failed. See output below for details.");
        }

        output.AppendLine();
        output.AppendLine("Full Output:");
        output.AppendLine("------------");
        output.AppendLine(result.StandardOutput);

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            output.AppendLine();
            output.AppendLine("Errors:");
            output.AppendLine("-------");
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
