using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating TicketDto instances for testing
/// </summary>
public class TicketDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _ticketKey = "TEST-123";
    private string _title = "Test Ticket";
    private string _description = "Test ticket description";
    private WorkflowState _state = WorkflowState.Triggered;
    private TicketSource _source = TicketSource.WebUI;
    private Guid _repositoryId = Guid.NewGuid();
    private string? _repositoryName = "Test Repository";
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = null;
    private DateTime? _completedAt = null;
    private string? _pullRequestUrl = null;
    private int? _pullRequestNumber = null;
    private string? _planBranchName = null;
    private string? _planMarkdownPath = null;
    private string? _lastError = null;

    public TicketDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TicketDtoBuilder WithTicketKey(string ticketKey)
    {
        _ticketKey = ticketKey;
        return this;
    }

    public TicketDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TicketDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TicketDtoBuilder WithState(WorkflowState state)
    {
        _state = state;
        return this;
    }

    public TicketDtoBuilder WithSource(TicketSource source)
    {
        _source = source;
        return this;
    }

    public TicketDtoBuilder WithRepositoryId(Guid repositoryId)
    {
        _repositoryId = repositoryId;
        return this;
    }

    public TicketDtoBuilder WithRepositoryName(string repositoryName)
    {
        _repositoryName = repositoryName;
        return this;
    }

    public TicketDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TicketDtoBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public TicketDtoBuilder WithCompletedAt(DateTime? completedAt)
    {
        _completedAt = completedAt;
        return this;
    }

    public TicketDtoBuilder WithPullRequest(string url, int number)
    {
        _pullRequestUrl = url;
        _pullRequestNumber = number;
        return this;
    }

    public TicketDtoBuilder WithPlanBranch(string branchName, string markdownPath)
    {
        _planBranchName = branchName;
        _planMarkdownPath = markdownPath;
        return this;
    }

    public TicketDtoBuilder WithLastError(string error)
    {
        _lastError = error;
        return this;
    }

    public TicketDto Build()
    {
        return new TicketDto
        {
            Id = _id,
            TicketKey = _ticketKey,
            Title = _title,
            Description = _description,
            State = _state,
            Source = _source,
            RepositoryId = _repositoryId,
            RepositoryName = _repositoryName,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            CompletedAt = _completedAt,
            PullRequestUrl = _pullRequestUrl,
            PullRequestNumber = _pullRequestNumber,
            PlanBranchName = _planBranchName,
            PlanMarkdownPath = _planMarkdownPath,
            LastError = _lastError
        };
    }

    /// <summary>
    /// Creates a ticket in the "TicketUpdateUnderReview" state with a ticket update
    /// </summary>
    public static TicketDtoBuilder InReviewState()
    {
        return new TicketDtoBuilder()
            .WithState(WorkflowState.TicketUpdateUnderReview)
            .WithUpdatedAt(DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a ticket in the "PlanUnderReview" state with a plan
    /// </summary>
    public static TicketDtoBuilder InPlanReviewState()
    {
        return new TicketDtoBuilder()
            .WithState(WorkflowState.PlanUnderReview)
            .WithUpdatedAt(DateTime.UtcNow)
            .WithPlanBranch("plan/TEST-123", "docs/plan.md");
    }

    /// <summary>
    /// Creates a completed ticket with a pull request
    /// </summary>
    public static TicketDtoBuilder Completed()
    {
        return new TicketDtoBuilder()
            .WithState(WorkflowState.Completed)
            .WithCompletedAt(DateTime.UtcNow)
            .WithPullRequest("https://github.com/test/repo/pull/42", 42);
    }

    /// <summary>
    /// Creates a failed ticket with an error
    /// </summary>
    public static TicketDtoBuilder Failed()
    {
        return new TicketDtoBuilder()
            .WithState(WorkflowState.Failed)
            .WithLastError("Test error message");
    }
}
