using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for WorkflowEvent entity operations.
/// </summary>
public class WorkflowEventRepository : IWorkflowEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowEventRepository> _logger;

    public WorkflowEventRepository(
        ApplicationDbContext context,
        ILogger<WorkflowEventRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkflowEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<WorkflowEvent>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowEvents
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<WorkflowEvent> Events, int TotalCount)> GetPagedAsync(
        WorkflowEventQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowEvents.AsQueryable();

        // Apply filters
        if (queryParameters.TicketId.HasValue)
        {
            query = query.Where(e => e.TicketId == queryParameters.TicketId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.EventType))
        {
            query = query.Where(e => e.EventType == queryParameters.EventType);
        }

        if (queryParameters.StartDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= queryParameters.StartDate.Value);
        }

        if (queryParameters.EndDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= queryParameters.EndDate.Value);
        }

        // Get total count before search text filtering (for efficiency)
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and materialize before text search (pattern matching not supported in SQL)
        var events = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        // Apply text search in memory (pattern matching requires materialized data)
        if (!string.IsNullOrWhiteSpace(queryParameters.SearchText))
        {
            events = events.Where(e =>
                e.EventType.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e is WorkflowStateChanged wsc && wsc.Reason != null && wsc.Reason.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e is QuestionAdded qa && qa.Question.Text.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e is AnswerAdded aa && aa.AnswerText.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e is PlanCreated pc && pc.BranchName.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e is PullRequestCreated prc && prc.PullRequestUrl.Contains(queryParameters.SearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return (events, totalCount);
    }

    public async Task<List<WorkflowEvent>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowEvents
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowEvent>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowEvents
            .Where(e => e.EventType == eventType)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetEventTypeCountsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowEvents.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= endDate.Value);
        }

        var counts = await query
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.EventType, x => x.Count);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowEvents.CountAsync(cancellationToken);
    }

    public async Task<WorkflowEvent> AddAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        _context.WorkflowEvents.Add(workflowEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created workflow event {EventId} of type {EventType} for ticket {TicketId}",
            workflowEvent.Id, workflowEvent.EventType, workflowEvent.TicketId);

        return workflowEvent;
    }

    public async Task AddRangeAsync(IEnumerable<WorkflowEvent> workflowEvents, CancellationToken cancellationToken = default)
    {
        var eventsList = workflowEvents.ToList();
        _context.WorkflowEvents.AddRange(eventsList);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} workflow events", eventsList.Count);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldEvents = await _context.WorkflowEvents
            .Where(e => e.OccurredAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldEvents.Any())
        {
            _context.WorkflowEvents.RemoveRange(oldEvents);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} workflow events older than {CutoffDate}",
                oldEvents.Count, cutoffDate);
        }
    }
}
