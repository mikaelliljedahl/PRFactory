namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents the various states a ticket can be in throughout its lifecycle.
/// </summary>
public enum WorkflowState
{
    /// <summary>
    /// Initial state: Ticket detected with @claude mention or label
    /// </summary>
    Triggered,

    /// <summary>
    /// AI is analyzing the codebase and ticket requirements
    /// </summary>
    Analyzing,

    /// <summary>
    /// Clarifying questions have been posted to the ticket system
    /// </summary>
    QuestionsPosted,

    /// <summary>
    /// Waiting for developer to provide answers to questions
    /// </summary>
    AwaitingAnswers,

    /// <summary>
    /// Developer has provided answers to the questions
    /// </summary>
    AnswersReceived,

    /// <summary>
    /// AI is generating an implementation plan
    /// </summary>
    Planning,

    /// <summary>
    /// Implementation plan has been committed to a branch and posted to ticket
    /// </summary>
    PlanPosted,

    /// <summary>
    /// Developer is reviewing the implementation plan
    /// </summary>
    PlanUnderReview,

    /// <summary>
    /// Developer has approved the implementation plan
    /// </summary>
    PlanApproved,

    /// <summary>
    /// Developer has rejected the plan (will return to Planning state)
    /// </summary>
    PlanRejected,

    /// <summary>
    /// AI is implementing the code based on the approved plan
    /// </summary>
    Implementing,

    /// <summary>
    /// Implementation encountered errors that need to be resolved
    /// </summary>
    ImplementationFailed,

    /// <summary>
    /// Pull request has been created with the implementation
    /// </summary>
    PRCreated,

    /// <summary>
    /// Pull request is under human review
    /// </summary>
    InReview,

    /// <summary>
    /// Pull request has been merged, ticket is complete
    /// </summary>
    Completed,

    /// <summary>
    /// User cancelled the workflow
    /// </summary>
    Cancelled,

    /// <summary>
    /// Unrecoverable failure occurred
    /// </summary>
    Failed
}
