using System;

namespace PRFactory.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity for persisting workflow state to database.
/// Maps to WorkflowState model in WorkflowOrchestrator.
/// </summary>
public class WorkflowStateEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique workflow identifier
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Associated ticket ID
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Current graph being executed (RefinementGraph, PlanningGraph, ImplementationGraph)
    /// </summary>
    public string CurrentGraph { get; set; } = string.Empty;

    /// <summary>
    /// Current state within the graph
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Workflow status (Running, Suspended, Completed, Failed, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the workflow started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the workflow completed (if completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if workflow failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
