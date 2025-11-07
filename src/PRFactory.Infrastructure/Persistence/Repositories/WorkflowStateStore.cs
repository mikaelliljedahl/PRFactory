using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Persistence.Entities;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for workflow state persistence.
/// Implements IWorkflowStateStore interface from WorkflowOrchestrator.
/// </summary>
public class WorkflowStateStore : IWorkflowStateStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowStateStore> _logger;

    public WorkflowStateStore(
        ApplicationDbContext context,
        ILogger<WorkflowStateStore> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Save or update workflow state
    /// </summary>
    public async Task SaveStateAsync(WorkflowState state)
    {
        var entity = await _context.WorkflowStates
            .FirstOrDefaultAsync(w => w.WorkflowId == state.WorkflowId);

        if (entity == null)
        {
            // Create new entity
            entity = MapToEntity(state);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.WorkflowStates.Add(entity);

            _logger.LogInformation(
                "Creating workflow state for WorkflowId {WorkflowId}, TicketId {TicketId}, Status {Status}",
                state.WorkflowId, state.TicketId, state.Status);
        }
        else
        {
            // Update existing entity
            UpdateEntity(entity, state);
            entity.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updating workflow state for WorkflowId {WorkflowId}, Status {Status}, Graph {Graph}",
                state.WorkflowId, state.Status, state.CurrentGraph);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get workflow state by ticket ID
    /// </summary>
    public async Task<WorkflowState> GetByTicketIdAsync(Guid ticketId)
    {
        var entity = await _context.WorkflowStates
            .Where(w => w.TicketId == ticketId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            _logger.LogWarning("No workflow state found for TicketId {TicketId}", ticketId);
            return null;
        }

        return MapToModel(entity);
    }

    /// <summary>
    /// Get workflow state by workflow ID
    /// </summary>
    public async Task<WorkflowState> GetByWorkflowIdAsync(Guid workflowId)
    {
        var entity = await _context.WorkflowStates
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

        if (entity == null)
        {
            _logger.LogWarning("No workflow state found for WorkflowId {WorkflowId}", workflowId);
            return null;
        }

        return MapToModel(entity);
    }

    /// <summary>
    /// Update only the status and optionally error message of a workflow
    /// </summary>
    public async Task UpdateStatusAsync(Guid workflowId, WorkflowStatus status, string? error = null)
    {
        var entity = await _context.WorkflowStates
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

        if (entity == null)
        {
            _logger.LogWarning(
                "Attempted to update status for non-existent WorkflowId {WorkflowId}",
                workflowId);
            return;
        }

        entity.Status = status.ToString();
        entity.ErrorMessage = error;
        entity.UpdatedAt = DateTime.UtcNow;

        // Set CompletedAt for terminal states
        if (status == WorkflowStatus.Completed ||
            status == WorkflowStatus.Failed ||
            status == WorkflowStatus.Cancelled)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated workflow status for WorkflowId {WorkflowId} to {Status}",
            workflowId, status);
    }

    /// <summary>
    /// Map WorkflowState model to WorkflowStateEntity
    /// </summary>
    private WorkflowStateEntity MapToEntity(WorkflowState state)
    {
        return new WorkflowStateEntity
        {
            WorkflowId = state.WorkflowId,
            TicketId = state.TicketId,
            CurrentGraph = state.CurrentGraph,
            CurrentState = state.CurrentState ?? string.Empty,
            Status = state.Status.ToString(),
            StartedAt = state.StartedAt,
            CompletedAt = state.CompletedAt,
            ErrorMessage = state.ErrorMessage
        };
    }

    /// <summary>
    /// Update existing entity with values from model
    /// </summary>
    private void UpdateEntity(WorkflowStateEntity entity, WorkflowState state)
    {
        entity.TicketId = state.TicketId;
        entity.CurrentGraph = state.CurrentGraph;
        entity.CurrentState = state.CurrentState ?? string.Empty;
        entity.Status = state.Status.ToString();
        entity.StartedAt = state.StartedAt;
        entity.CompletedAt = state.CompletedAt;
        entity.ErrorMessage = state.ErrorMessage;
    }

    /// <summary>
    /// Map WorkflowStateEntity to WorkflowState model
    /// </summary>
    private WorkflowState MapToModel(WorkflowStateEntity entity)
    {
        return new WorkflowState
        {
            WorkflowId = entity.WorkflowId,
            TicketId = entity.TicketId,
            CurrentGraph = entity.CurrentGraph,
            CurrentState = entity.CurrentState,
            Status = Enum.Parse<WorkflowStatus>(entity.Status),
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ErrorMessage = entity.ErrorMessage
        };
    }
}
