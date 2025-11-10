using System.Reflection;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Tests.Builders;

/// <summary>
/// Fluent builder for creating Ticket entities in tests with sensible defaults
/// </summary>
public class TicketBuilder
{
    private Guid? _id;
    private string _ticketKey = "TEST-123";
    private Guid _tenantId = Guid.NewGuid();
    private Guid _repositoryId = Guid.NewGuid();
    private string _ticketSystem = "Jira";
    private TicketSource _source = TicketSource.WebUI;
    private WorkflowState _state = WorkflowState.Triggered;
    private string? _title;
    private string? _description;
    private List<Question> _questions = new();
    private List<Answer> _answers = new();
    private string? _planBranchName;
    private string? _planMarkdownPath;
    private string? _implementationBranchName;
    private string? _pullRequestUrl;
    private int? _pullRequestNumber;
    private string? _externalTicketId;
    private int _requiredApprovalCount = 1;

    public TicketBuilder()
    {
    }

    public TicketBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TicketBuilder WithTicketKey(string ticketKey)
    {
        _ticketKey = ticketKey;
        return this;
    }

    public TicketBuilder WithTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public TicketBuilder WithRepositoryId(Guid repositoryId)
    {
        _repositoryId = repositoryId;
        return this;
    }

    public TicketBuilder WithTicketSystem(string ticketSystem)
    {
        _ticketSystem = ticketSystem;
        return this;
    }

    public TicketBuilder WithSource(TicketSource source)
    {
        _source = source;
        return this;
    }

    public TicketBuilder WithState(WorkflowState state)
    {
        _state = state;
        return this;
    }

    public TicketBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TicketBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TicketBuilder WithQuestions(int count)
    {
        _questions.Clear();
        for (int i = 0; i < count; i++)
        {
            _questions.Add(Question.Create($"Question {i + 1}?", "requirements"));
        }
        return this;
    }

    public TicketBuilder WithQuestion(Question question)
    {
        _questions.Add(question);
        return this;
    }

    public TicketBuilder WithAnswers(params (string questionId, string text)[] answers)
    {
        foreach (var (questionId, text) in answers)
        {
            _answers.Add(new Answer(questionId, text, DateTime.UtcNow));
        }
        return this;
    }

    public TicketBuilder WithPlanBranch(string branchName, string? markdownPath = null)
    {
        _planBranchName = branchName;
        _planMarkdownPath = markdownPath;
        return this;
    }

    public TicketBuilder WithImplementationBranch(string branchName)
    {
        _implementationBranchName = branchName;
        return this;
    }

    public TicketBuilder WithPullRequest(string url, int number)
    {
        _pullRequestUrl = url;
        _pullRequestNumber = number;
        return this;
    }

    public TicketBuilder WithExternalTicketId(string externalId)
    {
        _externalTicketId = externalId;
        return this;
    }

    public TicketBuilder WithRequiredApprovalCount(int count)
    {
        _requiredApprovalCount = count;
        return this;
    }

    public Ticket Build()
    {
        var ticket = Ticket.Create(_ticketKey, _tenantId, _repositoryId, _ticketSystem, _source);

        // Set custom ID if provided (for test scenarios where we need a specific ID)
        if (_id.HasValue)
        {
            // Access the private setter using reflection
            var idProperty = typeof(Ticket).GetProperty("Id");
            idProperty?.SetValue(ticket, _id.Value);
        }

        if (!string.IsNullOrEmpty(_title) || !string.IsNullOrEmpty(_description))
        {
            ticket.UpdateTicketInfo(_title ?? "Default Title", _description ?? "Default Description");
        }

        if (_state != WorkflowState.Triggered)
        {
            TransitionToState(ticket, _state);
        }

        foreach (var question in _questions)
        {
            ticket.AddQuestion(question);
        }

        foreach (var answer in _answers)
        {
            ticket.AddAnswer(answer.QuestionId, answer.Text);
        }

        if (!string.IsNullOrEmpty(_planBranchName))
        {
            ticket.SetPlanBranch(_planBranchName, _planMarkdownPath);
        }

        if (!string.IsNullOrEmpty(_implementationBranchName))
        {
            ticket.SetImplementationBranch(_implementationBranchName);
        }

        if (!string.IsNullOrEmpty(_pullRequestUrl) && _pullRequestNumber.HasValue)
        {
            ticket.SetPullRequest(_pullRequestUrl, _pullRequestNumber.Value);
        }

        if (!string.IsNullOrEmpty(_externalTicketId))
        {
            ticket.SetExternalTicketId(_externalTicketId, TicketSource.Jira);
        }

        return ticket;
    }

    private void TransitionToState(Ticket ticket, WorkflowState targetState)
    {
        var statePath = GetStatePath(WorkflowState.Triggered, targetState);
        foreach (var state in statePath)
        {
            if (ticket.State != state)
            {
                var result = ticket.TransitionTo(state);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Failed to transition ticket to {state}: {result.ErrorMessage}. Current state: {ticket.State}");
                }
            }
        }
    }

    private List<WorkflowState> GetStatePath(WorkflowState from, WorkflowState to)
    {
        // Simple path mapping for common test scenarios
        var paths = new Dictionary<WorkflowState, List<WorkflowState>>
        {
            [WorkflowState.Triggered] = new() { },
            [WorkflowState.Analyzing] = new() { WorkflowState.Analyzing },
            [WorkflowState.TicketUpdateGenerated] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated },
            [WorkflowState.TicketUpdateUnderReview] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview },
            [WorkflowState.TicketUpdateApproved] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved },
            [WorkflowState.TicketUpdatePosted] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted },
            [WorkflowState.QuestionsPosted] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted },
            [WorkflowState.AwaitingAnswers] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted, WorkflowState.AwaitingAnswers },
            [WorkflowState.AnswersReceived] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted, WorkflowState.AwaitingAnswers, WorkflowState.AnswersReceived },
            [WorkflowState.Planning] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning },
            [WorkflowState.PlanPosted] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted },
            [WorkflowState.PlanUnderReview] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview },
            [WorkflowState.PlanApproved] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved },
            [WorkflowState.Implementing] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing },
            [WorkflowState.PRCreated] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.PRCreated },
            [WorkflowState.InReview] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.PRCreated, WorkflowState.InReview },
            [WorkflowState.Completed] = new() { WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Completed },
            [WorkflowState.Failed] = new() { WorkflowState.Analyzing, WorkflowState.Failed },
            [WorkflowState.Cancelled] = new() { WorkflowState.Analyzing, WorkflowState.Cancelled }
        };

        return paths.TryGetValue(to, out var path) ? path : new List<WorkflowState>();
    }
}
