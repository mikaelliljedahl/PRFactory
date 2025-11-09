# Core Workflow Engine Architecture

## Overview

The Core Workflow Engine orchestrates the entire lifecycle of a ticket from initial trigger through refinement, planning, implementation, and PR creation. It manages state transitions, coordinates between external services, and ensures reliable execution through job scheduling and retry mechanisms.

## Key Responsibilities

- **State Management**: Track ticket progress through workflow states
- **Orchestration**: Coordinate multi-step processes across Jira, Git, and Claude
- **Job Scheduling**: Queue and execute background tasks
- **Error Handling**: Retry logic, failure recovery, circuit breakers
- **Event Publishing**: Notify interested parties of state changes
- **Audit Trail**: Log all operations for compliance and debugging

## State Machine

### Workflow States

```csharp
public enum WorkflowState
{
    // Initial states
    Triggered,              // Ticket detected with @claude or label

    // Refinement phase
    Analyzing,              // AI analyzing codebase
    QuestionsPosted,        // Questions posted to Jira
    AwaitingAnswers,        // Waiting for developer response
    AnswersReceived,        // Developer provided answers

    // Planning phase
    Planning,               // AI generating implementation plan
    PlanPosted,             // Plan committed to branch and posted to Jira
    PlanUnderReview,        // Developer reviewing plan
    PlanApproved,           // Developer approved plan
    PlanRejected,           // Developer rejected plan (back to Planning)

    // Implementation phase (optional)
    Implementing,           // AI implementing code
    ImplementationFailed,   // Implementation encountered errors
    PRCreated,              // Pull request created

    // Terminal states
    InReview,               // PR under human review
    Completed,              // PR merged
    Cancelled,              // User cancelled
    Failed                  // Unrecoverable failure
}
```

### State Transition Rules

```csharp
public class WorkflowStateTransitions
{
    private static readonly Dictionary<WorkflowState, List<WorkflowState>> ValidTransitions = new()
    {
        [WorkflowState.Triggered] = new() { WorkflowState.Analyzing, WorkflowState.Failed },

        [WorkflowState.Analyzing] = new() {
            WorkflowState.QuestionsPosted,
            WorkflowState.Failed
        },

        [WorkflowState.QuestionsPosted] = new() {
            WorkflowState.AwaitingAnswers
        },

        [WorkflowState.AwaitingAnswers] = new() {
            WorkflowState.AnswersReceived,
            WorkflowState.Cancelled
        },

        [WorkflowState.AnswersReceived] = new() {
            WorkflowState.Planning
        },

        [WorkflowState.Planning] = new() {
            WorkflowState.PlanPosted,
            WorkflowState.Failed
        },

        [WorkflowState.PlanPosted] = new() {
            WorkflowState.PlanUnderReview
        },

        [WorkflowState.PlanUnderReview] = new() {
            WorkflowState.PlanApproved,
            WorkflowState.PlanRejected,
            WorkflowState.Cancelled
        },

        [WorkflowState.PlanRejected] = new() {
            WorkflowState.Planning  // Refinement loop
        },

        [WorkflowState.PlanApproved] = new() {
            WorkflowState.Implementing,
            WorkflowState.Completed  // If manual implementation
        },

        [WorkflowState.Implementing] = new() {
            WorkflowState.PRCreated,
            WorkflowState.ImplementationFailed
        },

        [WorkflowState.ImplementationFailed] = new() {
            WorkflowState.Implementing,  // Retry
            WorkflowState.Failed
        },

        [WorkflowState.PRCreated] = new() {
            WorkflowState.InReview
        },

        [WorkflowState.InReview] = new() {
            WorkflowState.Completed,
            WorkflowState.Implementing  // Changes requested
        }
    };

    public static bool CanTransition(WorkflowState from, WorkflowState to)
    {
        return ValidTransitions.TryGetValue(from, out var allowed)
            && allowed.Contains(to);
    }
}
```

## Domain Model

### Core Entities

```csharp
// PRFactory.Core/Domain/Entities/Ticket.cs
public class Ticket
{
    public Guid Id { get; private set; }
    public string TicketKey { get; private set; }  // e.g., "PROJ-123" (Jira) or "12345" (Azure DevOps)
    public string TicketSystem { get; private set; }  // "Jira" or "AzureDevOps"
    public Guid TenantId { get; private set; }
    public Guid RepositoryId { get; private set; }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public WorkflowState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Refinement data
    public List<Question> Questions { get; private set; } = new();
    public List<Answer> Answers { get; private set; } = new();

    // Planning data
    public string? PlanBranchName { get; private set; }
    public string? PlanMarkdownPath { get; private set; }
    public DateTime? PlanApprovedAt { get; private set; }

    // Implementation data
    public string? ImplementationBranchName { get; private set; }
    public string? PullRequestUrl { get; private set; }
    public int? PullRequestNumber { get; private set; }

    // Metadata
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation
    public Repository Repository { get; private set; }
    public Tenant Tenant { get; private set; }
    public List<WorkflowEvent> Events { get; private set; } = new();
    public List<AuditLog> AuditLogs { get; private set; } = new();

    // Methods
    public Result TransitionTo(WorkflowState newState, string? reason = null)
    {
        if (!WorkflowStateTransitions.CanTransition(State, newState))
        {
            return Result.Failure($"Invalid transition from {State} to {newState}");
        }

        var previousState = State;
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new WorkflowStateChanged(Id, previousState, newState, reason));

        return Result.Success();
    }

    public void AddQuestion(Question question)
    {
        Questions.Add(question);
        AddEvent(new QuestionAdded(Id, question));
    }

    public void AddAnswer(string questionId, string answer)
    {
        var question = Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null) return;

        Answers.Add(new Answer(questionId, answer, DateTime.UtcNow));
        AddEvent(new AnswerAdded(Id, questionId, answer));
    }

    private void AddEvent(WorkflowEvent @event)
    {
        Events.Add(@event);
    }
}

public class Question
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; }
    public string Category { get; set; }  // "requirements", "technical", "testing"
    public DateTime CreatedAt { get; set; }
}

public class Answer
{
    public Answer(string questionId, string text, DateTime answeredAt)
    {
        QuestionId = questionId;
        Text = text;
        AnsweredAt = answeredAt;
    }

    public string QuestionId { get; }
    public string Text { get; }
    public DateTime AnsweredAt { get; }
}
```

```csharp
// PRFactory.Core/Domain/Entities/Repository.cs
public class Repository
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public string Name { get; private set; }
    public string GitPlatform { get; private set; }  // "GitHub", "Bitbucket", "AzureDevOps"
    public string CloneUrl { get; private set; }
    public string DefaultBranch { get; private set; } = "main";

    public string AccessToken { get; private set; }  // Encrypted

    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; }
    public List<Ticket> Tickets { get; private set; } = new();
}
```

```csharp
// PRFactory.Core/Domain/Entities/Tenant.cs
public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string JiraUrl { get; private set; }
    public string JiraApiToken { get; private set; }  // Encrypted
    public string ClaudeApiKey { get; private set; }  // Encrypted

    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    // Configuration
    public TenantConfiguration Configuration { get; private set; } = new();

    // Navigation
    public List<Repository> Repositories { get; private set; } = new();
    public List<Ticket> Tickets { get; private set; } = new();
}

public class TenantConfiguration
{
    public bool AutoImplementAfterPlanApproval { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";
    public int MaxTokensPerRequest { get; set; } = 8000;
}
```

```csharp
// PRFactory.Core/Domain/Entities/WorkflowEvent.cs
public abstract class WorkflowEvent
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid TicketId { get; protected set; }
    public DateTime OccurredAt { get; protected set; } = DateTime.UtcNow;
    public string EventType { get; protected set; }
}

public class WorkflowStateChanged : WorkflowEvent
{
    public WorkflowStateChanged(Guid ticketId, WorkflowState from, WorkflowState to, string? reason)
    {
        TicketId = ticketId;
        From = from;
        To = to;
        Reason = reason;
        EventType = nameof(WorkflowStateChanged);
    }

    public WorkflowState From { get; }
    public WorkflowState To { get; }
    public string? Reason { get; }
}

public class QuestionAdded : WorkflowEvent
{
    public QuestionAdded(Guid ticketId, Question question)
    {
        TicketId = ticketId;
        Question = question;
        EventType = nameof(QuestionAdded);
    }

    public Question Question { get; }
}

// ... more event types
```

## Application Services

### Ticket Service

```csharp
// PRFactory.Core/Services/TicketService.cs
public interface ITicketService
{
    Task<Result<Guid>> TriggerTicketAsync(string ticketKey, Guid tenantId, Guid repositoryId, CancellationToken ct = default);
    Task<Result> SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken ct = default);
    Task<Result> ApprovePlanAsync(Guid ticketId, CancellationToken ct = default);
    Task<Result> RejectPlanAsync(Guid ticketId, string reason, CancellationToken ct = default);
    Task<Result<TicketStatusDto>> GetTicketStatusAsync(Guid ticketId, CancellationToken ct = default);
}

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        IWorkflowEngine workflowEngine,
        IBackgroundJobClient jobClient,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _workflowEngine = workflowEngine;
        _jobClient = jobClient;
        _logger = logger;
    }

    public async Task<Result<Guid>> TriggerTicketAsync(
        string ticketKey,
        Guid tenantId,
        Guid repositoryId,
        CancellationToken ct = default)
    {
        // Create ticket entity
        var ticket = Ticket.Create(ticketKey, tenantId, repositoryId);

        // Transition to Analyzing state
        await _workflowEngine.TransitionAsync(ticket, WorkflowState.Analyzing);

        // Persist
        await _ticketRepository.AddAsync(ticket, ct);

        // Schedule background job for analysis
        _jobClient.Enqueue<RefineTicketJob>(job => job.ExecuteAsync(ticket.Id, CancellationToken.None));

        _logger.LogInformation("Triggered ticket {TicketKey} with ID {TicketId}",
            ticketKey, ticket.Id);

        return Result.Success(ticket.Id);
    }

    public async Task<Result> SubmitAnswersAsync(
        Guid ticketId,
        Dictionary<string, string> answers,
        CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) return Result.Failure("Ticket not found");

        // Add answers
        foreach (var (questionId, answerText) in answers)
        {
            ticket.AddAnswer(questionId, answerText);
        }

        // Transition state
        await _workflowEngine.TransitionAsync(ticket, WorkflowState.AnswersReceived);

        await _ticketRepository.UpdateAsync(ticket, ct);

        // Schedule planning job
        _jobClient.Enqueue<GeneratePlanJob>(job => job.ExecuteAsync(ticket.Id, CancellationToken.None));

        return Result.Success();
    }

    public async Task<Result> ApprovePlanAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) return Result.Failure("Ticket not found");

        await _workflowEngine.TransitionAsync(ticket, WorkflowState.PlanApproved);
        await _ticketRepository.UpdateAsync(ticket, ct);

        // Optionally enqueue implementation job
        // _jobClient.Enqueue<ImplementPlanJob>(job => job.ExecuteAsync(ticket.Id, CancellationToken.None));

        return Result.Success();
    }

    public async Task<Result> RejectPlanAsync(Guid ticketId, string reason, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) return Result.Failure("Ticket not found");

        await _workflowEngine.TransitionAsync(ticket, WorkflowState.PlanRejected, reason);
        await _ticketRepository.UpdateAsync(ticket, ct);

        // Re-enqueue planning job
        _jobClient.Enqueue<GeneratePlanJob>(job => job.ExecuteAsync(ticket.Id, CancellationToken.None));

        return Result.Success();
    }

    public async Task<Result<TicketStatusDto>> GetTicketStatusAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) return Result.Failure<TicketStatusDto>("Ticket not found");

        var dto = new TicketStatusDto
        {
            Id = ticket.Id,
            TicketKey = ticket.TicketKey,
            State = ticket.State.ToString(),
            Questions = ticket.Questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Category = q.Category,
                Answer = ticket.Answers.FirstOrDefault(a => a.QuestionId == q.Id)?.Text
            }).ToList(),
            PlanBranchName = ticket.PlanBranchName,
            PullRequestUrl = ticket.PullRequestUrl,
            UpdatedAt = ticket.UpdatedAt
        };

        return Result.Success(dto);
    }
}
```

## Background Jobs (Hangfire)

### Job Definitions

```csharp
// PRFactory.Worker/Jobs/RefineTicketJob.cs
public class RefineTicketJob
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IRepositoryService _repositoryService;
    private readonly IClaudeService _claudeService;
    private readonly IJiraService _jiraService;
    private readonly ILogger<RefineTicketJob> _logger;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ExecuteAsync(Guid ticketId, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) throw new InvalidOperationException($"Ticket {ticketId} not found");

        try
        {
            _logger.LogInformation("Starting refinement for ticket {JiraKey}", ticket.JiraKey);

            // 1. Clone repository
            var repoPath = await _repositoryService.EnsureClonedAsync(ticket.RepositoryId, ct);

            // 2. Analyze codebase with Claude
            var analysisResult = await _claudeService.AnalyzeCodebaseAsync(
                ticket,
                repoPath,
                ct
            );

            // 3. Generate questions
            var questions = await _claudeService.GenerateQuestionsAsync(
                ticket,
                analysisResult,
                ct
            );

            foreach (var question in questions)
            {
                ticket.AddQuestion(question);
            }

            // 4. Post questions to Jira
            await _jiraService.PostCommentAsync(
                ticket.JiraKey,
                FormatQuestionsAsComment(questions),
                ct
            );

            // 5. Transition state
            ticket.TransitionTo(WorkflowState.QuestionsPosted);
            ticket.TransitionTo(WorkflowState.AwaitingAnswers);

            await _ticketRepository.UpdateAsync(ticket, ct);

            _logger.LogInformation("Refinement completed for ticket {JiraKey}", ticket.JiraKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refinement failed for ticket {JiraKey}", ticket.JiraKey);

            ticket.LastError = ex.Message;
            ticket.RetryCount++;

            if (ticket.RetryCount >= 3)
            {
                ticket.TransitionTo(WorkflowState.Failed, $"Max retries exceeded: {ex.Message}");
            }

            await _ticketRepository.UpdateAsync(ticket, ct);
            throw;
        }
    }

    private string FormatQuestionsAsComment(List<Question> questions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🤖 Claude AI - Clarifying Questions");
        sb.AppendLine();
        sb.AppendLine("I've analyzed the codebase and have some questions to ensure the implementation meets your needs:");
        sb.AppendLine();

        foreach (var question in questions.OrderBy(q => q.Category))
        {
            sb.AppendLine($"**{question.Category}**: {question.Text}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*Please reply to this comment with @claude and your answers.*");

        return sb.ToString();
    }
}
```

```csharp
// PRFactory.Worker/Jobs/GeneratePlanJob.cs
public class GeneratePlanJob
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IClaudeService _claudeService;
    private readonly IGitPlatformService _gitService;
    private readonly IJiraService _jiraService;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid ticketId, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket == null) throw new InvalidOperationException($"Ticket {ticketId} not found");

        try
        {
            ticket.TransitionTo(WorkflowState.Planning);
            await _ticketRepository.UpdateAsync(ticket, ct);

            // 1. Generate implementation plan with Claude
            var plan = await _claudeService.GenerateImplementationPlanAsync(ticket, ct);

            // 2. Create feature branch
            var branchName = $"feature/{ticket.JiraKey}-implementation-plan";
            ticket.PlanBranchName = branchName;

            await _gitService.CreateBranchAsync(ticket.RepositoryId, branchName, ct);

            // 3. Commit plan files
            await _gitService.CommitFilesAsync(
                ticket.RepositoryId,
                branchName,
                new Dictionary<string, string>
                {
                    ["IMPLEMENTATION_PLAN.md"] = plan.MainPlan,
                    ["docs/affected-files.md"] = plan.AffectedFiles,
                    ["docs/test-strategy.md"] = plan.TestStrategy
                },
                $"Add implementation plan for {ticket.JiraKey}",
                ct
            );

            // 4. Push branch
            await _gitService.PushBranchAsync(ticket.RepositoryId, branchName, ct);

            // 5. Post summary to Jira
            var summary = FormatPlanSummary(plan, branchName);
            await _jiraService.PostCommentAsync(ticket.JiraKey, summary, ct);

            // 6. Update state
            ticket.TransitionTo(WorkflowState.PlanPosted);
            ticket.TransitionTo(WorkflowState.PlanUnderReview);
            await _ticketRepository.UpdateAsync(ticket, ct);
        }
        catch (Exception ex)
        {
            ticket.LastError = ex.Message;
            await _ticketRepository.UpdateAsync(ticket, ct);
            throw;
        }
    }
}
```

### Job Configuration

```csharp
// Program.cs or Startup.cs
services.AddHangfire(config =>
{
    config
        .UsePostgreSqlStorage(connectionString)
        .UseRecommendedSerializerSettings()
        .UseFilter(new AutomaticRetryAttribute { Attempts = 3 })
        .UseFilter(new LogEverythingAttribute());
});

services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "low" };
});
```

## Error Handling & Resilience

### Polly Policies

```csharp
// PRFactory.Core/Application/Services/ResiliencePolicies.cs
public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry
                }
            );
    }

    public static IAsyncPolicy GetCircuitBreakerPolicy()
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1)
            );
    }
}
```

## Testing Strategy

### Unit Tests
- State transition logic
- Command/query handlers
- Domain entity behavior

### Integration Tests
- End-to-end workflow execution
- Database persistence
- Job execution

### Example Test
```csharp
[Fact]
public async Task TriggerTicket_ShouldCreateTicketAndEnqueueJob()
{
    // Arrange
    var ticketService = new TicketService(_mockTicketRepository.Object, _mockWorkflowEngine.Object, _mockJobClient.Object, _logger);

    // Act
    var result = await ticketService.TriggerTicketAsync("PROJ-123", tenantId, repoId);

    // Assert
    Assert.True(result.IsSuccess);
    var ticket = await _ticketRepository.GetByIdAsync(result.Value);
    Assert.Equal(WorkflowState.Analyzing, ticket.State);
    _mockJobClient.Verify(x => x.Enqueue<RefineTicketJob>(It.IsAny<Expression<Action<RefineTicketJob>>>()), Times.Once);
}
```

## Next Steps

This core engine forms the foundation. Review the integration-specific documents for details on:
- [Jira Integration](./jira-integration.md)
- [Git Platform Integration](./git-integration.md)
