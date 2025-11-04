namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Defines valid state transitions for the workflow state machine.
/// Ensures tickets can only move through allowed state transitions.
/// </summary>
public static class WorkflowStateTransitions
{
    private static readonly Dictionary<WorkflowState, List<WorkflowState>> ValidTransitions = new()
    {
        [WorkflowState.Triggered] = new()
        {
            WorkflowState.Analyzing,
            WorkflowState.Failed,
            WorkflowState.Cancelled
        },

        [WorkflowState.Analyzing] = new()
        {
            WorkflowState.QuestionsPosted,
            WorkflowState.Failed
        },

        [WorkflowState.QuestionsPosted] = new()
        {
            WorkflowState.AwaitingAnswers
        },

        [WorkflowState.AwaitingAnswers] = new()
        {
            WorkflowState.AnswersReceived,
            WorkflowState.Cancelled
        },

        [WorkflowState.AnswersReceived] = new()
        {
            WorkflowState.Planning
        },

        [WorkflowState.Planning] = new()
        {
            WorkflowState.PlanPosted,
            WorkflowState.Failed
        },

        [WorkflowState.PlanPosted] = new()
        {
            WorkflowState.PlanUnderReview
        },

        [WorkflowState.PlanUnderReview] = new()
        {
            WorkflowState.PlanApproved,
            WorkflowState.PlanRejected,
            WorkflowState.Cancelled
        },

        [WorkflowState.PlanRejected] = new()
        {
            WorkflowState.Planning
        },

        [WorkflowState.PlanApproved] = new()
        {
            WorkflowState.Implementing,
            WorkflowState.Completed  // Manual implementation path
        },

        [WorkflowState.Implementing] = new()
        {
            WorkflowState.PRCreated,
            WorkflowState.ImplementationFailed
        },

        [WorkflowState.ImplementationFailed] = new()
        {
            WorkflowState.Implementing,  // Retry
            WorkflowState.Failed
        },

        [WorkflowState.PRCreated] = new()
        {
            WorkflowState.InReview
        },

        [WorkflowState.InReview] = new()
        {
            WorkflowState.Completed,
            WorkflowState.Implementing  // Changes requested, back to implementation
        }
    };

    /// <summary>
    /// Checks if a transition from one state to another is valid.
    /// </summary>
    public static bool CanTransition(WorkflowState from, WorkflowState to)
    {
        return ValidTransitions.TryGetValue(from, out var allowedStates)
            && allowedStates.Contains(to);
    }

    /// <summary>
    /// Gets all valid next states for a given current state.
    /// </summary>
    public static IReadOnlyList<WorkflowState> GetValidNextStates(WorkflowState currentState)
    {
        return ValidTransitions.TryGetValue(currentState, out var states)
            ? states.AsReadOnly()
            : new List<WorkflowState>().AsReadOnly();
    }
}
