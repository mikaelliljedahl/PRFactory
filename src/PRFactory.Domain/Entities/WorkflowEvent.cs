using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Base class for all workflow events that occur during ticket processing.
/// </summary>
public abstract class WorkflowEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    /// <summary>
    /// The ticket this event is associated with
    /// </summary>
    public Guid TicketId { get; protected init; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; protected init; } = DateTime.UtcNow;

    /// <summary>
    /// The type of event (derived class name)
    /// </summary>
    public string EventType { get; protected init; } = string.Empty;
}

/// <summary>
/// Event raised when a ticket transitions from one workflow state to another.
/// </summary>
public class WorkflowStateChanged : WorkflowEvent
{
    /// <summary>
    /// The previous state
    /// </summary>
    public WorkflowState From { get; init; }

    /// <summary>
    /// The new state
    /// </summary>
    public WorkflowState To { get; init; }

    /// <summary>
    /// Optional reason for the transition
    /// </summary>
    public string? Reason { get; init; }

    // EF Core constructor
    private WorkflowStateChanged() { }

    /// <summary>
    /// Creates a new workflow state change event
    /// </summary>
    public WorkflowStateChanged(Guid ticketId, WorkflowState from, WorkflowState to, string? reason = null)
    {
        TicketId = ticketId;
        From = from;
        To = to;
        Reason = reason;
        EventType = nameof(WorkflowStateChanged);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event raised when a question is added to a ticket.
/// </summary>
public class QuestionAdded : WorkflowEvent
{
    /// <summary>
    /// The question that was added
    /// </summary>
    public Question Question { get; init; } = null!;

    // EF Core constructor
    private QuestionAdded() { }

    /// <summary>
    /// Creates a new question added event
    /// </summary>
    public QuestionAdded(Guid ticketId, Question question)
    {
        ArgumentNullException.ThrowIfNull(question);

        TicketId = ticketId;
        Question = question;
        EventType = nameof(QuestionAdded);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event raised when an answer is provided to a question.
/// </summary>
public class AnswerAdded : WorkflowEvent
{
    /// <summary>
    /// The ID of the question that was answered
    /// </summary>
    public string QuestionId { get; init; } = string.Empty;

    /// <summary>
    /// The answer text
    /// </summary>
    public string AnswerText { get; init; } = string.Empty;

    // EF Core constructor
    private AnswerAdded() { }

    /// <summary>
    /// Creates a new answer added event
    /// </summary>
    public AnswerAdded(Guid ticketId, string questionId, string answerText)
    {
        if (string.IsNullOrWhiteSpace(questionId))
            throw new ArgumentException("Question ID cannot be empty", nameof(questionId));

        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        TicketId = ticketId;
        QuestionId = questionId;
        AnswerText = answerText;
        EventType = nameof(AnswerAdded);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event raised when an implementation plan is created.
/// </summary>
public class PlanCreated : WorkflowEvent
{
    /// <summary>
    /// The branch name where the plan was committed
    /// </summary>
    public string BranchName { get; init; } = string.Empty;

    // EF Core constructor
    private PlanCreated() { }

    /// <summary>
    /// Creates a new plan created event
    /// </summary>
    public PlanCreated(Guid ticketId, string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));

        TicketId = ticketId;
        BranchName = branchName;
        EventType = nameof(PlanCreated);
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event raised when a pull request is created.
/// </summary>
public class PullRequestCreated : WorkflowEvent
{
    /// <summary>
    /// The URL of the created pull request
    /// </summary>
    public string PullRequestUrl { get; init; } = string.Empty;

    /// <summary>
    /// The pull request number
    /// </summary>
    public int PullRequestNumber { get; init; }

    // EF Core constructor
    private PullRequestCreated() { }

    /// <summary>
    /// Creates a new pull request created event
    /// </summary>
    public PullRequestCreated(Guid ticketId, string pullRequestUrl, int pullRequestNumber)
    {
        if (string.IsNullOrWhiteSpace(pullRequestUrl))
            throw new ArgumentException("Pull request URL cannot be empty", nameof(pullRequestUrl));

        TicketId = ticketId;
        PullRequestUrl = pullRequestUrl;
        PullRequestNumber = pullRequestNumber;
        EventType = nameof(PullRequestCreated);
        OccurredAt = DateTime.UtcNow;
    }
}
