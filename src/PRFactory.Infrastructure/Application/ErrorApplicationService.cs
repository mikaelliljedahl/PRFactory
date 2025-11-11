using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing errors.
/// This service encapsulates business logic and coordinates error logging, resolution, and retry operations.
/// </summary>
public class ErrorApplicationService : IErrorApplicationService
{
    private readonly ILogger<ErrorApplicationService> _logger;
    private readonly IErrorRepository _errorRepository;
    private readonly ITicketRepository _ticketRepository;

    public ErrorApplicationService(
        ILogger<ErrorApplicationService> logger,
        IErrorRepository errorRepository,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _errorRepository = errorRepository;
        _ticketRepository = ticketRepository;
    }

    /// <inheritdoc/>
    public async Task<(List<ErrorLog> Items, int TotalCount)> GetErrorsAsync(
        ErrorQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting errors for tenant {TenantId}, page {Page}, pageSize {PageSize}",
            queryParameters.TenantId, queryParameters.Page, queryParameters.PageSize);

        return await _errorRepository.GetByTenantAsync(queryParameters, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ErrorLog?> GetErrorByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting error {ErrorId}", id);

        return await _errorRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ErrorLog>> GetErrorsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting errors for entity {EntityType}:{EntityId}", entityType, entityId);

        return await _errorRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting unresolved error count for tenant {TenantId}", tenantId);

        return await _errorRepository.GetUnresolvedCountAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ErrorStatistics> GetStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting error statistics for tenant {TenantId}", tenantId);

        return await _errorRepository.GetStatisticsAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ErrorLog> LogErrorAsync(
        LogErrorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError("Logging error for tenant {TenantId}: {Message}", request.TenantId, request.Message);

        var errorLog = ErrorLog.Create(
            request.TenantId,
            request.Severity,
            request.Message,
            request.StackTrace,
            request.EntityType,
            request.EntityId,
            request.ContextData);

        return await _errorRepository.AddAsync(errorLog, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task MarkErrorResolvedAsync(
        Guid errorId,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking error {ErrorId} as resolved by {ResolvedBy}", errorId, resolvedBy ?? "system");

        var error = await _errorRepository.GetByIdAsync(errorId, cancellationToken);
        if (error == null)
        {
            throw new InvalidOperationException($"Error {errorId} not found");
        }

        if (error.IsResolved)
        {
            _logger.LogWarning("Error {ErrorId} is already resolved", errorId);
            return;
        }

        error.MarkAsResolved(resolvedBy, resolutionNotes);
        await _errorRepository.UpdateAsync(error, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BulkMarkErrorsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk marking {Count} errors as resolved by {ResolvedBy}", errorIds.Count, resolvedBy ?? "system");

        await _errorRepository.BulkMarkAsResolvedAsync(errorIds, resolvedBy, resolutionNotes, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> RetryFailedOperationAsync(Guid errorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying failed operation for error {ErrorId}", errorId);

        var error = await _errorRepository.GetByIdAsync(errorId, cancellationToken);
        if (error == null)
        {
            throw new InvalidOperationException($"Error {errorId} not found");
        }

        // Check if the error is associated with a ticket
        if (error.EntityType != "Ticket" || !error.EntityId.HasValue)
        {
            _logger.LogWarning("Error {ErrorId} is not associated with a ticket, cannot retry", errorId);
            return false;
        }

        var ticket = await _ticketRepository.GetByIdAsync(error.EntityId.Value, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found for error {ErrorId}, cannot retry", error.EntityId.Value, errorId);
            return false;
        }

        try
        {
            // Clear the error from the ticket
            ticket.ClearError();
            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            // Note: In a full implementation, you would trigger the workflow orchestrator here
            // For now, we just clear the error and log the retry attempt
            _logger.LogInformation("Successfully prepared ticket {TicketId} for retry", ticket.Id);

            // Mark the error as resolved with a note about the retry
            error.MarkAsResolved("system", $"Retry initiated for ticket {ticket.TicketKey}");
            await _errorRepository.UpdateAsync(error, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry operation for error {ErrorId}", errorId);
            return false;
        }
    }
}
