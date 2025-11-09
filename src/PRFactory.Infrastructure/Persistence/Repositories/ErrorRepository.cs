using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ErrorLog entity operations.
/// </summary>
public class ErrorRepository : IErrorRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ErrorRepository> _logger;

    public ErrorRepository(
        ApplicationDbContext context,
        ILogger<ErrorRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorLog>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(List<ErrorLog> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        ErrorSeverity? severity = null,
        string? entityType = null,
        bool? isResolved = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ErrorLog>()
            .Where(e => e.TenantId == tenantId);

        // Apply filters
        if (severity.HasValue)
            query = query.Where(e => e.Severity == severity.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(e => e.EntityType == entityType);

        if (isResolved.HasValue)
            query = query.Where(e => e.IsResolved == isResolved.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e =>
                e.Message.Contains(searchTerm) ||
                (e.StackTrace != null && e.StackTrace.Contains(searchTerm)) ||
                (e.ResolutionNotes != null && e.ResolutionNotes.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<ErrorLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorLog>()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErrorLog>()
            .CountAsync(e => e.TenantId == tenantId && !e.IsResolved, cancellationToken);
    }

    public async Task<ErrorStatistics> GetStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var errors = await _context.Set<ErrorLog>()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var statistics = new ErrorStatistics
        {
            TotalErrors = errors.Count,
            UnresolvedErrors = errors.Count(e => !e.IsResolved),
            ResolvedErrors = errors.Count(e => e.IsResolved),
            CriticalErrors = errors.Count(e => e.Severity == ErrorSeverity.Critical),
            HighErrors = errors.Count(e => e.Severity == ErrorSeverity.High),
            MediumErrors = errors.Count(e => e.Severity == ErrorSeverity.Medium),
            LowErrors = errors.Count(e => e.Severity == ErrorSeverity.Low),
            ErrorsByEntityType = errors
                .Where(e => !string.IsNullOrEmpty(e.EntityType))
                .GroupBy(e => e.EntityType!)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByDate = errors
                .GroupBy(e => e.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return statistics;
    }

    public async Task<ErrorLog> AddAsync(ErrorLog errorLog, CancellationToken cancellationToken = default)
    {
        _context.Set<ErrorLog>().Add(errorLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created error log {ErrorId} with severity {Severity}", errorLog.Id, errorLog.Severity);

        return errorLog;
    }

    public async Task UpdateAsync(ErrorLog errorLog, CancellationToken cancellationToken = default)
    {
        _context.Set<ErrorLog>().Update(errorLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated error log {ErrorId}", errorLog.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var errorLog = await GetByIdAsync(id, cancellationToken);
        if (errorLog == null)
        {
            _logger.LogWarning("Attempted to delete non-existent error log {ErrorId}", id);
            return;
        }

        _context.Set<ErrorLog>().Remove(errorLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted error log {ErrorId}", id);
    }

    public async Task BulkMarkAsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default)
    {
        var errors = await _context.Set<ErrorLog>()
            .Where(e => errorIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var error in errors)
        {
            error.MarkAsResolved(resolvedBy, resolutionNotes);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk resolved {Count} errors", errors.Count);
    }
}
