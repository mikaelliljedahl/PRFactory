using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Core;

/// <summary>
/// Base class for all tools. Implements template method pattern with
/// security validations, logging, and error handling.
/// </summary>
public abstract class ToolBase : ITool
{
    /// <summary>
    /// Logger for tool execution
    /// </summary>
    protected readonly ILogger<ToolBase> _logger;

    /// <summary>
    /// Tenant context for multi-tenant isolation
    /// </summary>
    protected readonly ITenantContext _tenantContext;

    /// <summary>
    /// Unique tool name
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Create a new ToolBase instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    protected ToolBase(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Execute the tool with given context and parameters.
    /// This is the template method that implements the execution flow.
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Tool execution result</returns>
    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Validate execution context
            ValidateContext(context);

            // 2. Validate tenant isolation
            await ValidateTenantContextAsync(context);

            // 3. Validate input parameters (subclass can override)
            await ValidateInputAsync(context);

            // 4. Execute tool-specific logic (subclass implements this)
            var output = await ExecuteToolAsync(context);

            stopwatch.Stop();

            // 5. Log success
            _logger.LogInformation(
                "Tool {ToolName} executed successfully for tenant {TenantId} in {Duration}ms",
                Name, context.TenantId, stopwatch.ElapsedMilliseconds);

            return ToolExecutionResult.CreateSuccess(output, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failure (handle null context gracefully)
            var tenantId = context?.TenantId ?? Guid.Empty;
            _logger.LogError(ex,
                "Tool {ToolName} failed for tenant {TenantId} after {Duration}ms: {Error}",
                Name, tenantId, stopwatch.ElapsedMilliseconds, ex.Message);

            return ToolExecutionResult.CreateFailure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Execute the tool-specific logic. Subclasses must implement this.
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Tool output</returns>
    protected abstract Task<string> ExecuteToolAsync(ToolExecutionContext context);

    /// <summary>
    /// Validate input parameters. Subclasses can override for custom validation.
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected virtual Task ValidateInputAsync(ToolExecutionContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate the execution context has required fields
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <exception cref="ArgumentNullException">Context is null</exception>
    /// <exception cref="ArgumentException">Required field is missing</exception>
    private void ValidateContext(ToolExecutionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required", nameof(context));

        if (string.IsNullOrEmpty(context.WorkspacePath))
            throw new ArgumentException("WorkspacePath is required", nameof(context));
    }

    /// <summary>
    /// Validate that the current tenant context matches the execution context
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    /// <exception cref="UnauthorizedAccessException">Tenant mismatch</exception>
    private async Task ValidateTenantContextAsync(ToolExecutionContext context)
    {
        // Ensure current tenant context matches execution context
        var currentTenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (currentTenantId != context.TenantId)
        {
            throw new UnauthorizedAccessException(
                $"Tenant context mismatch: expected {currentTenantId}, " +
                $"got {context.TenantId}");
        }
    }

    /// <summary>
    /// Execute an operation with a timeout
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="timeout">Timeout duration</param>
    /// <returns>Operation result</returns>
    /// <exception cref="ToolTimeoutException">Operation timed out</exception>
    protected async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<Task<TResult>> operation,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new ToolTimeoutException(Name, timeout);
        }
    }
}
