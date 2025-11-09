using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using System.Text;
using System.Text.Json;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of workflow event service.
/// Uses direct repository injection (Blazor Server architecture).
/// This is a facade service that converts between domain entities and DTOs.
/// </summary>
public class WorkflowEventService : IWorkflowEventService
{
    private readonly IWorkflowEventRepository _workflowEventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<WorkflowEventService> _logger;

    public WorkflowEventService(
        IWorkflowEventRepository workflowEventRepository,
        ITicketRepository ticketRepository,
        ILogger<WorkflowEventService> logger)
    {
        _workflowEventRepository = workflowEventRepository ?? throw new ArgumentNullException(nameof(workflowEventRepository));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<WorkflowEventDto>> GetEventsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null,
        CancellationToken ct = default)
    {
        try
        {
            var (events, totalCount) = await _workflowEventRepository.GetPagedAsync(
                pageNumber,
                pageSize,
                ticketId,
                eventType,
                startDate,
                endDate,
                searchText,
                ct);

            // Get ticket keys for all events
            var ticketIds = events.Select(e => e.TicketId).Distinct().ToList();
            var tickets = new Dictionary<Guid, Ticket>();
            foreach (var id in ticketIds)
            {
                var ticket = await _ticketRepository.GetByIdAsync(id, ct);
                if (ticket != null)
                {
                    tickets[id] = ticket;
                }
            }

            var eventDtos = events.Select(e => MapToDto(e, tickets.GetValueOrDefault(e.TicketId))).ToList();

            return new PagedResult<WorkflowEventDto>(eventDtos, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow events");
            throw;
        }
    }

    public async Task<WorkflowEventDto?> GetEventByIdAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var workflowEvent = await _workflowEventRepository.GetByIdAsync(eventId, ct);
            if (workflowEvent == null)
            {
                return null;
            }

            var ticket = await _ticketRepository.GetByIdAsync(workflowEvent.TicketId, ct);
            return MapToDto(workflowEvent, ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow event {EventId}", eventId);
            throw;
        }
    }

    public async Task<EventStatisticsDto> GetStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var eventTypeCounts = await _workflowEventRepository.GetEventTypeCountsAsync(startDate, endDate, ct);
            var totalEvents = eventTypeCounts.Values.Sum();

            // Get state change events for success/error counts
            var stateChanges = await _workflowEventRepository.GetByEventTypeAsync(nameof(WorkflowStateChanged), ct);

            if (startDate.HasValue)
            {
                stateChanges = stateChanges.Where(e => e.OccurredAt >= startDate.Value).ToList();
            }

            if (endDate.HasValue)
            {
                stateChanges = stateChanges.Where(e => e.OccurredAt <= endDate.Value).ToList();
            }

            var successCount = stateChanges
                .OfType<WorkflowStateChanged>()
                .Count(e => e.To == WorkflowState.Completed);

            var errorCount = stateChanges
                .OfType<WorkflowStateChanged>()
                .Count(e => e.To == WorkflowState.Failed || e.To == WorkflowState.ImplementationFailed);

            var successRate = totalEvents > 0 ? (double)successCount / totalEvents * 100 : 0;

            // Calculate average duration for completed workflows
            var completedWorkflows = stateChanges
                .OfType<WorkflowStateChanged>()
                .Where(e => e.To == WorkflowState.Completed)
                .ToList();

            double averageDuration = 0;
            if (completedWorkflows.Any())
            {
                var durations = new List<double>();
                foreach (var completedEvent in completedWorkflows)
                {
                    var ticket = await _ticketRepository.GetByIdAsync(completedEvent.TicketId, ct);
                    if (ticket?.CreatedAt != null && ticket.CompletedAt.HasValue)
                    {
                        durations.Add((ticket.CompletedAt.Value - ticket.CreatedAt).TotalSeconds);
                    }
                }

                if (durations.Any())
                {
                    averageDuration = durations.Average();
                }
            }

            return new EventStatisticsDto
            {
                TotalEvents = totalEvents,
                ErrorCount = errorCount,
                SuccessCount = successCount,
                SuccessRate = successRate,
                AverageDurationSeconds = averageDuration,
                EventTypeCounts = eventTypeCounts,
                StartDate = startDate,
                EndDate = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow event statistics");
            throw;
        }
    }

    public async Task<List<string>> GetEventTypesAsync(CancellationToken ct = default)
    {
        try
        {
            var counts = await _workflowEventRepository.GetEventTypeCountsAsync(null, null, ct);
            return counts.Keys.OrderBy(k => k).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event types");
            throw;
        }
    }

    public async Task<byte[]> ExportToCsvAsync(
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var (events, _) = await _workflowEventRepository.GetPagedAsync(
                1,
                int.MaxValue,
                ticketId,
                eventType,
                startDate,
                endDate,
                null,
                ct);

            var csv = new StringBuilder();
            csv.AppendLine("Id,TicketId,EventType,OccurredAt,Description");

            foreach (var evt in events)
            {
                var description = GetEventDescription(evt);
                csv.AppendLine($"{evt.Id},{evt.TicketId},{evt.EventType},{evt.OccurredAt:O},\"{description.Replace("\"", "\"\"")}\"");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting events to CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportToJsonAsync(
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var (events, _) = await _workflowEventRepository.GetPagedAsync(
                1,
                int.MaxValue,
                ticketId,
                eventType,
                startDate,
                endDate,
                null,
                ct);

            var eventDtos = events.Select(e => MapToDto(e, null)).ToList();
            var json = JsonSerializer.Serialize(eventDtos, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting events to JSON");
            throw;
        }
    }

    /// <summary>
    /// Maps a WorkflowEvent entity to a WorkflowEventDto
    /// </summary>
    private WorkflowEventDto MapToDto(WorkflowEvent workflowEvent, Ticket? ticket)
    {
        var dto = new WorkflowEventDto
        {
            Id = workflowEvent.Id,
            TicketId = workflowEvent.TicketId,
            TicketKey = ticket?.TicketKey,
            OccurredAt = workflowEvent.OccurredAt,
            EventType = workflowEvent.EventType,
            Description = GetEventDescription(workflowEvent),
            Severity = GetEventSeverity(workflowEvent),
            Icon = GetEventIcon(workflowEvent),
            Metadata = GetEventMetadata(workflowEvent)
        };

        // Set type-specific properties
        if (workflowEvent is WorkflowStateChanged stateChanged)
        {
            dto.FromState = stateChanged.From;
            dto.ToState = stateChanged.To;
            dto.Reason = stateChanged.Reason;
        }

        // Serialize metadata to JSON
        if (dto.Metadata.Any())
        {
            dto.MetadataJson = JsonSerializer.Serialize(dto.Metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        return dto;
    }

    /// <summary>
    /// Gets a human-readable description for an event
    /// </summary>
    private string GetEventDescription(WorkflowEvent workflowEvent)
    {
        return workflowEvent switch
        {
            WorkflowStateChanged stateChanged =>
                $"State changed from {stateChanged.From} to {stateChanged.To}" +
                (string.IsNullOrEmpty(stateChanged.Reason) ? "" : $": {stateChanged.Reason}"),
            QuestionAdded questionAdded =>
                $"Question added: {questionAdded.Question.Text}",
            AnswerAdded answerAdded =>
                $"Answer provided for question {answerAdded.QuestionId}",
            PlanCreated planCreated =>
                $"Implementation plan created on branch {planCreated.BranchName}",
            PullRequestCreated prCreated =>
                $"Pull request #{prCreated.PullRequestNumber} created",
            _ => workflowEvent.EventType
        };
    }

    /// <summary>
    /// Gets the severity level for an event
    /// </summary>
    private EventSeverity GetEventSeverity(WorkflowEvent workflowEvent)
    {
        return workflowEvent switch
        {
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.Failed ||
                stateChanged.To == WorkflowState.ImplementationFailed =>
                EventSeverity.Error,
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.Completed =>
                EventSeverity.Success,
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.AwaitingAnswers ||
                stateChanged.To == WorkflowState.PlanUnderReview ||
                stateChanged.To == WorkflowState.TicketUpdateUnderReview =>
                EventSeverity.Warning,
            PullRequestCreated => EventSeverity.Success,
            PlanCreated => EventSeverity.Success,
            _ => EventSeverity.Info
        };
    }

    /// <summary>
    /// Gets the icon for an event
    /// </summary>
    private string GetEventIcon(WorkflowEvent workflowEvent)
    {
        return workflowEvent switch
        {
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.Failed ||
                stateChanged.To == WorkflowState.ImplementationFailed =>
                "x-circle",
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.Completed =>
                "check-circle",
            WorkflowStateChanged stateChanged when
                stateChanged.To == WorkflowState.AwaitingAnswers =>
                "question-circle",
            QuestionAdded => "question-circle",
            AnswerAdded => "chat-dots",
            PlanCreated => "file-text",
            PullRequestCreated => "git",
            _ => "circle"
        };
    }

    /// <summary>
    /// Gets metadata for an event
    /// </summary>
    private Dictionary<string, object> GetEventMetadata(WorkflowEvent workflowEvent)
    {
        var metadata = new Dictionary<string, object>();

        switch (workflowEvent)
        {
            case WorkflowStateChanged stateChanged:
                metadata["from"] = stateChanged.From.ToString();
                metadata["to"] = stateChanged.To.ToString();
                if (!string.IsNullOrEmpty(stateChanged.Reason))
                {
                    metadata["reason"] = stateChanged.Reason;
                }
                break;
            case QuestionAdded questionAdded:
                metadata["questionId"] = questionAdded.Question.Id;
                metadata["questionText"] = questionAdded.Question.Text;
                break;
            case AnswerAdded answerAdded:
                metadata["questionId"] = answerAdded.QuestionId;
                metadata["answerText"] = answerAdded.AnswerText;
                break;
            case PlanCreated planCreated:
                metadata["branchName"] = planCreated.BranchName;
                break;
            case PullRequestCreated prCreated:
                metadata["pullRequestUrl"] = prCreated.PullRequestUrl;
                metadata["pullRequestNumber"] = prCreated.PullRequestNumber;
                break;
        }

        return metadata;
    }
}
