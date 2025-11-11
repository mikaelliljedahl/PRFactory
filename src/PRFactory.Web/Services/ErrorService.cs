using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of error service.
/// Uses direct application service injection (Blazor Server architecture).
/// This is a facade service that converts between domain entities and DTOs.
/// </summary>
public class ErrorService : IErrorService
{
    private readonly ILogger<ErrorService> _logger;
    private readonly IErrorApplicationService _errorApplicationService;

    public ErrorService(
        ILogger<ErrorService> logger,
        IErrorApplicationService errorApplicationService)
    {
        _logger = logger;
        _errorApplicationService = errorApplicationService;
    }

    public async Task<(List<ErrorDto> Items, int TotalCount)> GetErrorsAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        ErrorSeverity? severity = null,
        string? entityType = null,
        bool? isResolved = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new ErrorQueryParameters(
                tenantId,
                page,
                pageSize,
                severity,
                entityType,
                isResolved,
                fromDate,
                toDate,
                searchTerm);

            var (items, totalCount) = await _errorApplicationService.GetErrorsAsync(
                queryParams, ct);

            var dtos = items.Select(MapToDto).ToList();
            return (dtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching errors for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<ErrorDto?> GetErrorByIdAsync(Guid errorId, CancellationToken ct = default)
    {
        try
        {
            var error = await _errorApplicationService.GetErrorByIdAsync(errorId, ct);
            return error != null ? MapToDto(error) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching error {ErrorId}", errorId);
            throw;
        }
    }

    public async Task<List<ErrorDto>> GetErrorsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        try
        {
            var errors = await _errorApplicationService.GetErrorsByEntityAsync(entityType, entityId, ct);
            return errors.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching errors for entity {EntityType}:{EntityId}", entityType, entityId);
            throw;
        }
    }

    public async Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            return await _errorApplicationService.GetUnresolvedCountAsync(tenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unresolved count for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<ErrorStatisticsDto> GetStatisticsAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var stats = await _errorApplicationService.GetStatisticsAsync(tenantId, ct);
            return new ErrorStatisticsDto
            {
                TotalErrors = stats.TotalErrors,
                UnresolvedErrors = stats.UnresolvedErrors,
                ResolvedErrors = stats.ResolvedErrors,
                CriticalErrors = stats.CriticalErrors,
                HighErrors = stats.HighErrors,
                MediumErrors = stats.MediumErrors,
                LowErrors = stats.LowErrors,
                ErrorsByEntityType = stats.ErrorsByEntityType,
                ErrorsByDate = stats.ErrorsByDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task MarkErrorResolvedAsync(
        Guid errorId,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken ct = default)
    {
        try
        {
            await _errorApplicationService.MarkErrorResolvedAsync(errorId, resolvedBy, resolutionNotes, ct);
            _logger.LogInformation("Marked error {ErrorId} as resolved", errorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking error {ErrorId} as resolved", errorId);
            throw;
        }
    }

    public async Task BulkMarkErrorsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken ct = default)
    {
        try
        {
            await _errorApplicationService.BulkMarkErrorsResolvedAsync(errorIds, resolvedBy, resolutionNotes, ct);
            _logger.LogInformation("Bulk marked {Count} errors as resolved", errorIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk marking errors as resolved");
            throw;
        }
    }

    public async Task<bool> RetryFailedOperationAsync(Guid errorId, CancellationToken ct = default)
    {
        try
        {
            var result = await _errorApplicationService.RetryFailedOperationAsync(errorId, ct);
            _logger.LogInformation("Retry operation for error {ErrorId} {Result}", errorId, result ? "succeeded" : "failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying operation for error {ErrorId}", errorId);
            throw;
        }
    }

    private static ErrorDto MapToDto(Domain.Entities.ErrorLog error)
    {
        return new ErrorDto
        {
            Id = error.Id,
            TenantId = error.TenantId,
            Severity = error.Severity,
            Message = error.Message,
            StackTrace = error.StackTrace,
            EntityType = error.EntityType,
            EntityId = error.EntityId,
            ContextData = error.ContextData,
            IsResolved = error.IsResolved,
            ResolvedAt = error.ResolvedAt,
            ResolvedBy = error.ResolvedBy,
            ResolutionNotes = error.ResolutionNotes,
            CreatedAt = error.CreatedAt
        };
    }
}
