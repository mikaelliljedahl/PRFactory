using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a ticket from a ticket system (Jira, Azure DevOps) that is being processed by PRFactory.
/// This is the main aggregate root for the workflow.
/// </summary>
public class Ticket
{
    /// <summary>
    /// Unique identifier for the ticket in PRFactory
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket key from the source system (e.g., "PROJ-123" for Jira, "12345" for Azure DevOps)
    /// </summary>
    public string TicketKey { get; private set; } = string.Empty;

    /// <summary>
    /// The ticket system type (e.g., "Jira", "AzureDevOps")
    /// </summary>
    public string TicketSystem { get; private set; } = "Jira";

    /// <summary>
    /// Indicates where the ticket originated (WebUI, Jira, etc.)
    /// </summary>
    public TicketSource Source { get; private set; }

    /// <summary>
    /// The ID of the ticket in the external system (e.g., Jira issue key)
    /// Null for tickets created directly in the Web UI
    /// </summary>
    public string? ExternalTicketId { get; private set; }

    /// <summary>
    /// When the ticket was last synchronized with the external system
    /// Null for tickets created in the Web UI or not yet synced
    /// </summary>
    public DateTime? LastSyncedAt { get; private set; }

    /// <summary>
    /// The tenant this ticket belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The repository this ticket will be implemented in
    /// </summary>
    public Guid RepositoryId { get; private set; }

    /// <summary>
    /// Ticket title from the source system
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Ticket description from the source system
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Current workflow state
    /// </summary>
    public WorkflowState State { get; private set; }

    /// <summary>
    /// When the ticket was created in PRFactory
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the ticket was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// When the ticket workflow completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Questions generated during refinement phase
    /// </summary>
    public List<Question> Questions { get; private set; } = new();

    /// <summary>
    /// Answers provided by the developer
    /// </summary>
    public List<Answer> Answers { get; private set; } = new();

    /// <summary>
    /// Name of the branch where the implementation plan was committed
    /// </summary>
    public string? PlanBranchName { get; private set; }

    /// <summary>
    /// Path to the plan markdown file in the repository
    /// </summary>
    public string? PlanMarkdownPath { get; private set; }

    /// <summary>
    /// When the plan was approved
    /// </summary>
    public DateTime? PlanApprovedAt { get; private set; }

    /// <summary>
    /// Name of the branch where implementation was performed
    /// </summary>
    public string? ImplementationBranchName { get; private set; }

    /// <summary>
    /// URL of the created pull request
    /// </summary>
    public string? PullRequestUrl { get; private set; }

    /// <summary>
    /// Pull request number from the git platform
    /// </summary>
    public int? PullRequestNumber { get; private set; }

    /// <summary>
    /// Number of times the workflow has been retried after failures
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Last error message if any operation failed
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    /// <summary>
    /// Number of required reviewer approvals needed before plan can be approved
    /// Default: 1 (backward compatible with single-user workflow)
    /// </summary>
    public int RequiredApprovalCount { get; private set; } = 1;

    /// <summary>
    /// Navigation property to the repository
    /// </summary>
    public Repository? Repository { get; private set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Events that occurred during the ticket lifecycle
    /// </summary>
    public List<WorkflowEvent> Events { get; private set; } = new();

    /// <summary>
    /// Ticket updates generated during the refinement workflow
    /// </summary>
    public List<TicketUpdate> TicketUpdates { get; private set; } = new();

    /// <summary>
    /// Plan reviews by individual reviewers (team review feature)
    /// </summary>
    public List<PlanReview> PlanReviews { get; private set; } = new();

    /// <summary>
    /// Comments made during plan review discussions
    /// </summary>
    public List<ReviewComment> ReviewComments { get; private set; } = new();

    private Ticket() { }

    /// <summary>
    /// Creates a new ticket
    /// </summary>
    public static Ticket Create(
        string ticketKey,
        Guid tenantId,
        Guid repositoryId,
        string ticketSystem = "Jira",
        TicketSource source = TicketSource.WebUI)
    {
        if (string.IsNullOrWhiteSpace(ticketKey))
            throw new ArgumentException("Ticket key cannot be empty", nameof(ticketKey));

        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (repositoryId == Guid.Empty)
            throw new ArgumentException("Repository ID cannot be empty", nameof(repositoryId));

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketKey = ticketKey,
            TicketSystem = ticketSystem,
            Source = source,
            TenantId = tenantId,
            RepositoryId = repositoryId,
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        return ticket;
    }

    /// <summary>
    /// Updates the ticket with information from the source system
    /// </summary>
    public void UpdateTicketInfo(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title;
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the ticket to a new workflow state
    /// </summary>
    public TicketResult TransitionTo(WorkflowState newState, string? reason = null)
    {
        // Validate transition
        if (!CanTransitionTo(newState))
        {
            return TicketResult.Failure($"Invalid transition from {State} to {newState}");
        }

        var previousState = State;
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        // Set completed timestamp for terminal states
        if (newState == WorkflowState.Completed || newState == WorkflowState.Cancelled || newState == WorkflowState.Failed)
        {
            CompletedAt = DateTime.UtcNow;
        }

        // Add event
        AddEvent(new WorkflowStateChanged(Id, previousState, newState, reason));

        return TicketResult.Success();
    }

    /// <summary>
    /// Checks if the ticket can transition to the specified state
    /// </summary>
    public bool CanTransitionTo(WorkflowState newState)
    {
        var validTransitions = GetValidTransitions();
        return validTransitions.Contains(newState);
    }

    /// <summary>
    /// Gets the list of valid next states for the current state
    /// </summary>
    public List<WorkflowState> GetValidTransitions()
    {
        return State switch
        {
            WorkflowState.Triggered => new() { WorkflowState.Analyzing, WorkflowState.Failed, WorkflowState.Cancelled },
            WorkflowState.Analyzing => new() { WorkflowState.TicketUpdateGenerated, WorkflowState.Failed },
            WorkflowState.TicketUpdateGenerated => new() { WorkflowState.TicketUpdateUnderReview },
            WorkflowState.TicketUpdateUnderReview => new() { WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdateRejected, WorkflowState.Cancelled },
            WorkflowState.TicketUpdateRejected => new() { WorkflowState.Analyzing },
            WorkflowState.TicketUpdateApproved => new() { WorkflowState.TicketUpdatePosted },
            WorkflowState.TicketUpdatePosted => new() { WorkflowState.QuestionsPosted, WorkflowState.Planning },
            WorkflowState.QuestionsPosted => new() { WorkflowState.AwaitingAnswers },
            WorkflowState.AwaitingAnswers => new() { WorkflowState.AnswersReceived, WorkflowState.Cancelled },
            WorkflowState.AnswersReceived => new() { WorkflowState.Planning },
            WorkflowState.Planning => new() { WorkflowState.PlanPosted, WorkflowState.Failed },
            WorkflowState.PlanPosted => new() { WorkflowState.PlanUnderReview },
            WorkflowState.PlanUnderReview => new() { WorkflowState.PlanApproved, WorkflowState.PlanRejected, WorkflowState.Cancelled },
            WorkflowState.PlanRejected => new() { WorkflowState.Planning },
            WorkflowState.PlanApproved => new() { WorkflowState.Implementing, WorkflowState.Completed },
            WorkflowState.Implementing => new() { WorkflowState.PRCreated, WorkflowState.ImplementationFailed },
            WorkflowState.ImplementationFailed => new() { WorkflowState.Implementing, WorkflowState.Failed },
            WorkflowState.PRCreated => new() { WorkflowState.InReview },
            WorkflowState.InReview => new() { WorkflowState.Completed, WorkflowState.Implementing },
            WorkflowState.Completed => new() { },
            WorkflowState.Cancelled => new() { },
            WorkflowState.Failed => new() { },
            _ => new()
        };
    }

    /// <summary>
    /// Adds a question to the ticket
    /// </summary>
    public void AddQuestion(Question question)
    {
        if (question == null)
            throw new ArgumentNullException(nameof(question));

        Questions.Add(question);
        AddEvent(new QuestionAdded(Id, question));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an answer to a specific question
    /// </summary>
    public TicketResult AddAnswer(string questionId, string answerText)
    {
        if (string.IsNullOrWhiteSpace(questionId))
            return TicketResult.Failure("Question ID cannot be empty");

        if (string.IsNullOrWhiteSpace(answerText))
            return TicketResult.Failure("Answer text cannot be empty");

        var question = Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return TicketResult.Failure($"Question with ID {questionId} not found");

        var answer = new Answer(questionId, answerText, DateTime.UtcNow);
        Answers.Add(answer);
        AddEvent(new AnswerAdded(Id, questionId, answerText));
        UpdatedAt = DateTime.UtcNow;

        return TicketResult.Success();
    }

    /// <summary>
    /// Sets the plan branch information
    /// </summary>
    public void SetPlanBranch(string branchName, string? planMarkdownPath = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));

        PlanBranchName = branchName;
        PlanMarkdownPath = planMarkdownPath;
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new PlanCreated(Id, branchName));
    }

    /// <summary>
    /// Marks the plan as approved (validates multi-reviewer logic if team review is enabled)
    /// </summary>
    public void ApprovePlan()
    {
        // For team review: validate that sufficient approvals have been received
        if (PlanReviews.Any() && !HasSufficientApprovals())
        {
            var approvedCount = PlanReviews.Count(r => r.IsRequired && r.Status == ReviewStatus.Approved);
            throw new InvalidOperationException(
                $"Cannot approve plan: Insufficient approvals. Required: {RequiredApprovalCount}, Received: {approvedCount}");
        }

        PlanApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the implementation branch information
    /// </summary>
    public void SetImplementationBranch(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));

        ImplementationBranchName = branchName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the pull request information
    /// </summary>
    public void SetPullRequest(string pullRequestUrl, int pullRequestNumber)
    {
        if (string.IsNullOrWhiteSpace(pullRequestUrl))
            throw new ArgumentException("Pull request URL cannot be empty", nameof(pullRequestUrl));

        PullRequestUrl = pullRequestUrl;
        PullRequestNumber = pullRequestNumber;
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new PullRequestCreated(Id, pullRequestUrl, pullRequestNumber));
    }

    /// <summary>
    /// Records an error that occurred during processing
    /// </summary>
    public void RecordError(string error)
    {
        LastError = error;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the error state
    /// </summary>
    public void ClearError()
    {
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the external ticket ID and source for tickets synced from external systems
    /// </summary>
    public void SetExternalTicketId(string externalTicketId, TicketSource source)
    {
        if (string.IsNullOrWhiteSpace(externalTicketId))
            throw new ArgumentException("External ticket ID cannot be empty", nameof(externalTicketId));

        if (source == TicketSource.WebUI)
            throw new ArgumentException("Cannot set external ticket ID for Web UI created tickets", nameof(source));

        ExternalTicketId = externalTicketId;
        Source = source;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the ticket as synchronized with the external system
    /// </summary>
    public void MarkAsSynced()
    {
        LastSyncedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets a metadata value
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a metadata value
    /// </summary>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        return default;
    }

    /// <summary>
    /// Assigns reviewers to the plan (team review feature)
    /// </summary>
    /// <param name="requiredReviewerIds">List of user IDs who must approve the plan</param>
    /// <param name="optionalReviewerIds">List of user IDs who can optionally review the plan</param>
    public void AssignReviewers(List<Guid> requiredReviewerIds, List<Guid>? optionalReviewerIds = null)
    {
        if (State != WorkflowState.PlanPosted && State != WorkflowState.PlanUnderReview)
            throw new InvalidOperationException($"Cannot assign reviewers when state is {State}. Plan must be posted first.");

        if (requiredReviewerIds == null || requiredReviewerIds.Count == 0)
            throw new ArgumentException("At least one required reviewer must be specified", nameof(requiredReviewerIds));

        // Clear existing reviews (in case of re-assignment)
        PlanReviews.Clear();

        // Add required reviewers
        foreach (var reviewerId in requiredReviewerIds)
        {
            PlanReviews.Add(new PlanReview(Id, reviewerId, isRequired: true));
        }

        // Add optional reviewers
        if (optionalReviewerIds != null)
        {
            foreach (var reviewerId in optionalReviewerIds)
            {
                PlanReviews.Add(new PlanReview(Id, reviewerId, isRequired: false));
            }
        }

        RequiredApprovalCount = requiredReviewerIds.Count;
        UpdatedAt = DateTime.UtcNow;

        // Transition to PlanUnderReview if not already
        if (State == WorkflowState.PlanPosted)
        {
            TransitionTo(WorkflowState.PlanUnderReview, "Reviewers assigned");
        }
    }

    /// <summary>
    /// Checks if the plan has received sufficient approvals from required reviewers
    /// </summary>
    public bool HasSufficientApprovals()
    {
        // If no reviewers assigned, fall back to single-user workflow (backward compatible)
        if (!PlanReviews.Any())
            return true;

        var requiredReviews = PlanReviews.Where(r => r.IsRequired).ToList();
        var approvedCount = requiredReviews.Count(r => r.Status == ReviewStatus.Approved);

        return approvedCount >= RequiredApprovalCount;
    }

    /// <summary>
    /// Checks if any reviewer has rejected the plan
    /// </summary>
    public bool HasRejections()
    {
        return PlanReviews.Any(r =>
            r.Status == ReviewStatus.RejectedForRefinement ||
            r.Status == ReviewStatus.RejectedForRegeneration);
    }

    /// <summary>
    /// Gets the rejection details from the first rejection found
    /// </summary>
    /// <returns>Tuple of (reason, regenerateCompletely) or null if no rejections</returns>
    public (string Reason, bool RegenerateCompletely)? GetRejectionDetails()
    {
        var rejection = PlanReviews.FirstOrDefault(r =>
            r.Status == ReviewStatus.RejectedForRefinement ||
            r.Status == ReviewStatus.RejectedForRegeneration);

        if (rejection == null)
            return null;

        return (rejection.Decision ?? "No reason provided",
                rejection.Status == ReviewStatus.RejectedForRegeneration);
    }

    /// <summary>
    /// Resets all plan reviews to pending status when a new plan is generated
    /// Called when the plan is regenerated/refined and needs to be reviewed again
    /// </summary>
    public void ResetReviewsForNewPlan()
    {
        foreach (var review in PlanReviews)
        {
            review.ResetForNewPlan();
        }

        PlanApprovedAt = null; // Clear previous approval timestamp
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an event to the ticket's event history
    /// </summary>
    private void AddEvent(WorkflowEvent @event)
    {
        Events.Add(@event);
    }
}

/// <summary>
/// Result type for ticket operations
/// </summary>
public class TicketResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }

    private TicketResult() { }

    public static TicketResult Success() => new() { IsSuccess = true };
    public static TicketResult Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
