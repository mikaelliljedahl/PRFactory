using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Command;

/// <summary>
/// Build .NET project with specified configuration.
/// </summary>
public class BuildProjectTool : ToolBase
{
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(10);
    private static readonly HashSet<string> ValidConfigurations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Debug", "Release"
    };

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "BuildProject";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Build .NET project with specified configuration (Debug/Release)";

    /// <summary>
    /// Create a new BuildProjectTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public BuildProjectTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the build
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Build output</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var configuration = context.GetOptionalParameter<string>("configuration", "Release") ?? "Release";

        // 1. Validate repository path
        var fullRepoPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullRepoPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Validate configuration
        if (!ValidConfigurations.Contains(configuration))
        {
            throw new ArgumentException(
                $"Invalid configuration '{configuration}'. Valid options: {string.Join(", ", ValidConfigurations)}");
        }

        // 3. Build dotnet build command
        var arguments = $"build --configuration {configuration} --verbosity normal";

        // 4. Execute build with timeout
        var result = await ExecuteWithTimeoutAsync(
            () => ExecuteCommandAsync("dotnet", arguments, fullRepoPath),
            BuildTimeout
        );

        _logger.LogInformation(
            "Built project in {RepositoryPath} with configuration {Configuration} (exit code {ExitCode}) for tenant {TenantId}",
            repositoryPath, configuration, result.ExitCode, context.TenantId);

        return FormatBuildOutput(result, configuration);
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
    /// Format build output
    /// </summary>
    private static string FormatBuildOutput(CommandResult result, string configuration)
    {
        var output = new StringBuilder();

        // Parse build results
        var warnings = 0;
        var errors = 0;

        var lines = result.StandardOutput.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Build succeeded.") || line.Contains("Build FAILED."))
            {
                // Extract warning/error counts
                var warningMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+) Warning");
                if (warningMatch.Success)
                {
                    int.TryParse(warningMatch.Groups[1].Value, out warnings);
                }

                var errorMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+) Error");
                if (errorMatch.Success)
                {
                    int.TryParse(errorMatch.Groups[1].Value, out errors);
                }
            }
        }

        output.AppendLine($"Build Results ({configuration})");
        output.AppendLine("===============");
        output.AppendLine($"Exit Code: {result.ExitCode}");
        output.AppendLine($"Warnings: {warnings}");
        output.AppendLine($"Errors: {errors}");
        output.AppendLine();

        if (result.ExitCode == 0)
        {
            output.AppendLine("Build succeeded!");
        }
        else
        {
            output.AppendLine("Build failed. See output below for details.");
        }

        output.AppendLine();
        output.AppendLine("Build Output:");
        output.AppendLine("-------------");
        output.AppendLine(result.StandardOutput);

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            output.AppendLine();
            output.AppendLine("Build Errors:");
            output.AppendLine("-------------");
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
