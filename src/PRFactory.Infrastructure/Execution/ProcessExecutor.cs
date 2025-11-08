using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Execution;

/// <summary>
/// Service for executing CLI commands safely with proper error handling,
/// timeout support, and streaming capabilities.
/// </summary>
public class ProcessExecutor : IProcessExecutor
{
    private readonly ILogger<ProcessExecutor> _logger;

    public ProcessExecutor(ILogger<ProcessExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a command and captures the complete output
    /// </summary>
    /// <param name="fileName">The executable to run</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="workingDirectory">Working directory for the process</param>
    /// <param name="timeoutSeconds">Timeout in seconds (null for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    public async Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        _logger.LogDebug(
            "Executing process: {FileName} {Arguments} in directory {WorkingDirectory}",
            fileName,
            arguments,
            workingDirectory ?? Directory.GetCurrentDirectory());

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        return await ExecuteCoreAsync(startInfo, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes a command with an argument list and captures the complete output.
    /// This is safer than string concatenation as it properly handles escaping.
    /// </summary>
    /// <param name="fileName">The executable to run</param>
    /// <param name="argumentList">Command line arguments as a list</param>
    /// <param name="workingDirectory">Working directory for the process</param>
    /// <param name="timeoutSeconds">Timeout in seconds (null for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    public async Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        IEnumerable<string> argumentList,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (argumentList == null)
            throw new ArgumentNullException(nameof(argumentList));

        _logger.LogDebug(
            "Executing process: {FileName} with {ArgumentCount} arguments in directory {WorkingDirectory}",
            fileName,
            argumentList.Count(),
            workingDirectory ?? Directory.GetCurrentDirectory());

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        // Use ArgumentList for safer argument handling (no manual escaping needed)
        foreach (var arg in argumentList)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return await ExecuteCoreAsync(startInfo, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Core execution logic shared by all ExecuteAsync overloads
    /// </summary>
    private async Task<ProcessExecutionResult> ExecuteCoreAsync(
        ProcessStartInfo startInfo,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var startTime = DateTime.UtcNow;

        using var process = new Process { StartInfo = startInfo };

        // Capture output and error streams
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _logger.LogDebug("Process started with PID: {ProcessId}", process.Id);

            // Wait for process to complete with timeout and cancellation support
            var completed = false;
            if (timeoutSeconds.HasValue)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(linkedCts.Token);
                    completed = true;
                }
                catch (OperationCanceledException)
                {
                    if (timeoutCts.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "Process {FileName} timed out after {TimeoutSeconds} seconds",
                            startInfo.FileName,
                            timeoutSeconds.Value);

                        KillProcessTree(process);

                        return new ProcessExecutionResult
                        {
                            Success = false,
                            ExitCode = -1,
                            Output = outputBuilder.ToString(),
                            Error = $"Process timed out after {timeoutSeconds.Value} seconds",
                            Duration = DateTime.UtcNow - startTime
                        };
                    }

                    throw;
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
                completed = true;
            }

            var duration = DateTime.UtcNow - startTime;
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            _logger.LogDebug(
                "Process {FileName} completed with exit code {ExitCode} in {Duration}ms",
                startInfo.FileName,
                process.ExitCode,
                duration.TotalMilliseconds);

            return new ProcessExecutionResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = output,
                Error = error,
                Duration = duration
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Process {FileName} was cancelled", startInfo.FileName);

            KillProcessTree(process);

            return new ProcessExecutionResult
            {
                Success = false,
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = "Process was cancelled",
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing process {FileName}", startInfo.FileName);

            return new ProcessExecutionResult
            {
                Success = false,
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = $"Error executing process: {ex.Message}",
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Executes a command with streaming output
    /// </summary>
    /// <param name="fileName">The executable to run</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="onOutputReceived">Callback invoked when output is received</param>
    /// <param name="onErrorReceived">Callback invoked when error output is received</param>
    /// <param name="workingDirectory">Working directory for the process</param>
    /// <param name="timeoutSeconds">Timeout in seconds (null for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    public async Task<ProcessExecutionResult> ExecuteStreamingAsync(
        string fileName,
        string arguments,
        Action<string> onOutputReceived,
        Action<string>? onErrorReceived = null,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (onOutputReceived == null)
            throw new ArgumentNullException(nameof(onOutputReceived));

        _logger.LogDebug(
            "Executing streaming process: {FileName} {Arguments}",
            fileName,
            arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        return await ExecuteStreamingCoreAsync(startInfo, onOutputReceived, onErrorReceived, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes a command with an argument list and streaming output.
    /// This is safer than string concatenation as it properly handles escaping.
    /// </summary>
    /// <param name="fileName">The executable to run</param>
    /// <param name="argumentList">Command line arguments as a list</param>
    /// <param name="onOutputReceived">Callback invoked when output is received</param>
    /// <param name="onErrorReceived">Callback invoked when error output is received</param>
    /// <param name="workingDirectory">Working directory for the process</param>
    /// <param name="timeoutSeconds">Timeout in seconds (null for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    public async Task<ProcessExecutionResult> ExecuteStreamingAsync(
        string fileName,
        IEnumerable<string> argumentList,
        Action<string> onOutputReceived,
        Action<string>? onErrorReceived = null,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (argumentList == null)
            throw new ArgumentNullException(nameof(argumentList));

        if (onOutputReceived == null)
            throw new ArgumentNullException(nameof(onOutputReceived));

        _logger.LogDebug(
            "Executing streaming process: {FileName} with {ArgumentCount} arguments",
            fileName,
            argumentList.Count());

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        // Use ArgumentList for safer argument handling (no manual escaping needed)
        foreach (var arg in argumentList)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return await ExecuteStreamingCoreAsync(startInfo, onOutputReceived, onErrorReceived, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Core streaming execution logic shared by all ExecuteStreamingAsync overloads
    /// </summary>
    private async Task<ProcessExecutionResult> ExecuteStreamingCoreAsync(
        ProcessStartInfo startInfo,
        Action<string> onOutputReceived,
        Action<string>? onErrorReceived,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var startTime = DateTime.UtcNow;

        using var process = new Process { StartInfo = startInfo };

        // Stream output as it arrives
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutputReceived(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                onErrorReceived?.Invoke(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _logger.LogDebug("Streaming process started with PID: {ProcessId}", process.Id);

            // Wait for completion
            if (timeoutSeconds.HasValue)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Streaming process timed out after {TimeoutSeconds} seconds", timeoutSeconds.Value);
                    KillProcessTree(process);

                    return new ProcessExecutionResult
                    {
                        Success = false,
                        ExitCode = -1,
                        Output = outputBuilder.ToString(),
                        Error = $"Process timed out after {timeoutSeconds.Value} seconds",
                        Duration = DateTime.UtcNow - startTime
                    };
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;

            return new ProcessExecutionResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                Duration = duration
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming process was cancelled");
            KillProcessTree(process);

            return new ProcessExecutionResult
            {
                Success = false,
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = "Process was cancelled",
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing streaming process");

            return new ProcessExecutionResult
            {
                Success = false,
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = $"Error executing process: {ex.Message}",
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Kills a process and all its child processes
    /// </summary>
    private void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                _logger.LogDebug("Killed process {ProcessId} and its children", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error killing process {ProcessId}", process.Id);
        }
    }
}

/// <summary>
/// Interface for process executor
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Executes a command and captures the complete output
    /// </summary>
    Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with an argument list and captures the complete output.
    /// This is safer than string concatenation as it properly handles escaping.
    /// </summary>
    Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        IEnumerable<string> argumentList,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with streaming output
    /// </summary>
    Task<ProcessExecutionResult> ExecuteStreamingAsync(
        string fileName,
        string arguments,
        Action<string> onOutputReceived,
        Action<string>? onErrorReceived = null,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with an argument list and streaming output.
    /// This is safer than string concatenation as it properly handles escaping.
    /// </summary>
    Task<ProcessExecutionResult> ExecuteStreamingAsync(
        string fileName,
        IEnumerable<string> argumentList,
        Action<string> onOutputReceived,
        Action<string>? onErrorReceived = null,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a process execution
/// </summary>
public class ProcessExecutionResult
{
    /// <summary>
    /// Indicates whether the process executed successfully (exit code 0)
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Exit code from the process
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output from the process
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Standard error from the process
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the process execution
    /// </summary>
    public TimeSpan Duration { get; init; }
}
