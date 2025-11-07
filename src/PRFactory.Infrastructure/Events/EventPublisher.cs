using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Persistence;

namespace PRFactory.Infrastructure.Events;

/// <summary>
/// Publishes workflow events to real-time clients and optionally persists to database.
/// Implements IEventPublisher interface from WorkflowOrchestrator.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IEventBroadcaster _eventBroadcaster;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        IEventBroadcaster eventBroadcaster,
        ApplicationDbContext dbContext,
        ILogger<EventPublisher> logger)
    {
        _eventBroadcaster = eventBroadcaster ?? throw new ArgumentNullException(nameof(eventBroadcaster));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to SignalR clients and optionally persists to database.
    /// Events are best-effort - errors are logged but not thrown.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        try
        {
            // Extract TicketId from event
            var ticketId = GetTicketIdFromEvent(@event);
            if (ticketId == null)
            {
                _logger.LogWarning("Event type {EventType} does not contain TicketId, skipping publication",
                    typeof(TEvent).Name);
                return;
            }

            // Get TenantId from database for multi-tenant isolation
            var tenantId = await GetTenantIdAsync(ticketId.Value);
            if (tenantId == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found, cannot publish event", ticketId.Value);
                return;
            }

            // Map event to SignalR DTO
            var eventDto = MapToDto(@event, ticketId.Value);

            // Broadcast to SignalR groups
            await BroadcastToSignalRAsync(tenantId.Value, ticketId.Value, eventDto);

            // Optionally persist to database
            await PersistEventAsync(@event, ticketId.Value);

            _logger.LogInformation(
                "Published event {EventType} for ticket {TicketId} to tenant {TenantId}",
                typeof(TEvent).Name, ticketId.Value, tenantId.Value);
        }
        catch (Exception ex)
        {
            // Events are best-effort - log but don't throw
            _logger.LogError(ex,
                "Failed to publish event {EventType}", typeof(TEvent).Name);
        }
    }

    /// <summary>
    /// Extracts TicketId from various event types using pattern matching
    /// </summary>
    private Guid? GetTicketIdFromEvent<TEvent>(TEvent @event) where TEvent : class
    {
        return @event switch
        {
            WorkflowSuspendedEvent suspended => suspended.TicketId,
            WorkflowCompletedEvent completed => completed.TicketId,
            WorkflowFailedEvent failed => failed.TicketId,
            WorkflowCancelledEvent cancelled => cancelled.TicketId,
            _ => null
        };
    }

    /// <summary>
    /// Gets TenantId from database by querying the Ticket entity
    /// </summary>
    private async Task<Guid?> GetTenantIdAsync(Guid ticketId)
    {
        try
        {
            var ticket = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Id == ticketId)
                .Select(t => new { t.TenantId })
                .FirstOrDefaultAsync();

            return ticket?.TenantId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve TenantId for ticket {TicketId}", ticketId);
            return null;
        }
    }

    /// <summary>
    /// Maps WorkflowOrchestrator events to WorkflowEventDto for SignalR
    /// </summary>
    private object MapToDto<TEvent>(TEvent @event, Guid ticketId) where TEvent : class
    {
        return @event switch
        {
            WorkflowSuspendedEvent suspended => new
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "WorkflowSuspended",
                OccurredAt = suspended.SuspendedAt,
                Description = $"Workflow suspended in {suspended.GraphId} at state: {suspended.State}",
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowId"] = suspended.WorkflowId,
                    ["GraphId"] = suspended.GraphId,
                    ["State"] = suspended.State
                }
            },

            WorkflowCompletedEvent completed => new
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "WorkflowCompleted",
                OccurredAt = completed.CompletedAt,
                Description = $"Workflow completed successfully in {completed.Duration.TotalMinutes:F1} minutes",
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowId"] = completed.WorkflowId,
                    ["Duration"] = completed.Duration.ToString()
                }
            },

            WorkflowFailedEvent failed => new
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "WorkflowFailed",
                OccurredAt = failed.FailedAt,
                Description = $"Workflow failed in {failed.GraphId}: {failed.Error}",
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowId"] = failed.WorkflowId,
                    ["GraphId"] = failed.GraphId,
                    ["Error"] = failed.Error ?? "Unknown error"
                }
            },

            WorkflowCancelledEvent cancelled => new
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "WorkflowCancelled",
                OccurredAt = cancelled.CancelledAt,
                Description = "Workflow was cancelled by user",
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowId"] = cancelled.WorkflowId
                }
            },

            _ => new
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = typeof(TEvent).Name,
                OccurredAt = DateTime.UtcNow,
                Description = $"Event: {typeof(TEvent).Name}",
                Metadata = new Dictionary<string, object>()
            }
        };
    }

    /// <summary>
    /// Broadcasts event to real-time clients in tenant and ticket groups
    /// </summary>
    private async Task BroadcastToSignalRAsync(Guid tenantId, Guid ticketId, object eventDto)
    {
        try
        {
            var tenantGroup = $"tenant-{tenantId}";
            var ticketGroup = $"ticket-{ticketId}";

            // Broadcast to both tenant and ticket groups
            await _eventBroadcaster.BroadcastAsync(
                new[] { tenantGroup, ticketGroup },
                "WorkflowEvent",
                eventDto);

            _logger.LogDebug(
                "Broadcasted event to groups: {TenantGroup}, {TicketGroup}",
                tenantGroup, ticketGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast event to real-time clients");
            throw;
        }
    }

    /// <summary>
    /// Optionally persists events to database as WorkflowEvent entities
    /// </summary>
    private async Task PersistEventAsync<TEvent>(TEvent @event, Guid ticketId) where TEvent : class
    {
        try
        {
            // Create domain event entities based on orchestrator events
            WorkflowEvent? domainEvent = @event switch
            {
                WorkflowSuspendedEvent suspended => new WorkflowSuspended(
                    ticketId,
                    suspended.GraphId,
                    suspended.State),

                WorkflowCompletedEvent completed => new WorkflowCompleted(
                    ticketId,
                    completed.Duration),

                WorkflowFailedEvent failed => new WorkflowFailed(
                    ticketId,
                    failed.GraphId,
                    failed.Error),

                WorkflowCancelledEvent cancelled => new WorkflowCancelled(ticketId),

                _ => null
            };

            if (domainEvent != null)
            {
                _dbContext.WorkflowEvents.Add(domainEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogDebug(
                    "Persisted {EventType} to database for ticket {TicketId}",
                    domainEvent.EventType, ticketId);
            }
        }
        catch (Exception ex)
        {
            // Persistence is optional - log but don't throw
            _logger.LogError(ex,
                "Failed to persist event {EventType} to database", typeof(TEvent).Name);
        }
    }
}

/// <summary>
/// Domain event for workflow suspension
/// </summary>
public class WorkflowSuspended : WorkflowEvent
{
    public string GraphId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;

    public WorkflowSuspended(Guid ticketId, string graphId, string state)
    {
        TicketId = ticketId;
        GraphId = graphId;
        State = state;
        EventType = nameof(WorkflowSuspended);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event for workflow completion
/// </summary>
public class WorkflowCompleted : WorkflowEvent
{
    public TimeSpan Duration { get; init; }

    public WorkflowCompleted(Guid ticketId, TimeSpan duration)
    {
        TicketId = ticketId;
        Duration = duration;
        EventType = nameof(WorkflowCompleted);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event for workflow failure
/// </summary>
public class WorkflowFailed : WorkflowEvent
{
    public string GraphId { get; init; } = string.Empty;
    public string? Error { get; init; }

    public WorkflowFailed(Guid ticketId, string graphId, string? error)
    {
        TicketId = ticketId;
        GraphId = graphId;
        Error = error;
        EventType = nameof(WorkflowFailed);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event for workflow cancellation
/// </summary>
public class WorkflowCancelled : WorkflowEvent
{
    public WorkflowCancelled(Guid ticketId)
    {
        TicketId = ticketId;
        EventType = nameof(WorkflowCancelled);
        OccurredAt = DateTime.UtcNow;
    }
}
