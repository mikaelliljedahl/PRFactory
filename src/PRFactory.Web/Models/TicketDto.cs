using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Models;

/// <summary>
/// Data transfer object for displaying ticket information in the UI
/// </summary>
public class TicketDto
{
    // CSS class constants for state badge styling
    private const string BadgeBgInfo = "badge bg-info";
    private const string BadgeBgPrimary = "badge bg-primary";
    private const string BadgeBgDanger = "badge bg-danger";
    private const string BadgeBgSuccess = "badge bg-success";
    private const string BadgeBgSecondary = "badge bg-secondary";
    private const string BadgeBgWarning = "badge bg-warning";
    /// <summary>
    /// Unique identifier for the ticket in PRFactory
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ticket key from the source system (e.g., "PROJ-123" for Jira)
    /// </summary>
    public string TicketKey { get; set; } = string.Empty;

    /// <summary>
    /// Ticket title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ticket description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current workflow state
    /// </summary>
    public WorkflowState State { get; set; }

    /// <summary>
    /// Indicates where the ticket originated (WebUI, Jira, etc.)
    /// </summary>
    public TicketSource Source { get; set; }

    /// <summary>
    /// The ID of the repository this ticket belongs to
    /// </summary>
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// The name of the repository
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// When the ticket was created in PRFactory
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the ticket was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the ticket workflow completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// URL of the created pull request
    /// </summary>
    public string? PullRequestUrl { get; set; }

    /// <summary>
    /// Pull request number from the git platform
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// Name of the branch where the implementation plan was committed
    /// </summary>
    public string? PlanBranchName { get; set; }

    /// <summary>
    /// Path to the plan markdown file in the repository
    /// </summary>
    public string? PlanMarkdownPath { get; set; }

    /// <summary>
    /// Last error message if any operation failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Implementation plan with multi-artifact support and version history
    /// </summary>
    public Core.Application.DTOs.PlanDto? Plan { get; set; }

    /// <summary>
    /// Returns a friendly display name for the workflow state
    /// </summary>
    public string StateBadgeClass => State switch
    {
        WorkflowState.Triggered => BadgeBgSecondary,
        WorkflowState.Analyzing => BadgeBgInfo,
        WorkflowState.TicketUpdateGenerated => BadgeBgInfo,
        WorkflowState.TicketUpdateUnderReview => BadgeBgPrimary,
        WorkflowState.TicketUpdateRejected => BadgeBgDanger,
        WorkflowState.TicketUpdateApproved => BadgeBgSuccess,
        WorkflowState.TicketUpdatePosted => BadgeBgSuccess,
        WorkflowState.QuestionsPosted => BadgeBgWarning,
        WorkflowState.AwaitingAnswers => BadgeBgWarning,
        WorkflowState.AnswersReceived => BadgeBgInfo,
        WorkflowState.Planning => BadgeBgInfo,
        WorkflowState.PlanPosted => BadgeBgPrimary,
        WorkflowState.PlanUnderReview => BadgeBgPrimary,
        WorkflowState.PlanApproved => BadgeBgSuccess,
        WorkflowState.PlanRejected => BadgeBgDanger,
        WorkflowState.Implementing => BadgeBgInfo,
        WorkflowState.ImplementationFailed => BadgeBgDanger,
        WorkflowState.PRCreated => BadgeBgSuccess,
        WorkflowState.InReview => BadgeBgPrimary,
        WorkflowState.Completed => BadgeBgSuccess,
        WorkflowState.Cancelled => BadgeBgSecondary,
        WorkflowState.Failed => BadgeBgDanger,
        _ => BadgeBgSecondary
    };

    /// <summary>
    /// Returns the state as a display-friendly string
    /// </summary>
    public string StateDisplay => State switch
    {
        WorkflowState.Triggered => "Triggered",
        WorkflowState.Analyzing => "Analyzing",
        WorkflowState.TicketUpdateGenerated => "Ticket Update Generated",
        WorkflowState.TicketUpdateUnderReview => "Ticket Update Under Review",
        WorkflowState.TicketUpdateRejected => "Ticket Update Rejected",
        WorkflowState.TicketUpdateApproved => "Ticket Update Approved",
        WorkflowState.TicketUpdatePosted => "Ticket Update Posted",
        WorkflowState.QuestionsPosted => "Questions Posted",
        WorkflowState.AwaitingAnswers => "Awaiting Answers",
        WorkflowState.AnswersReceived => "Answers Received",
        WorkflowState.Planning => "Planning",
        WorkflowState.PlanPosted => "Plan Posted",
        WorkflowState.PlanUnderReview => "Plan Under Review",
        WorkflowState.PlanApproved => "Plan Approved",
        WorkflowState.PlanRejected => "Plan Rejected",
        WorkflowState.Implementing => "Implementing",
        WorkflowState.ImplementationFailed => "Implementation Failed",
        WorkflowState.PRCreated => "PR Created",
        WorkflowState.InReview => "In Review",
        WorkflowState.Completed => "Completed",
        WorkflowState.Cancelled => "Cancelled",
        WorkflowState.Failed => "Failed",
        _ => State.ToString()
    };

    /// <summary>
    /// Returns the source as a display-friendly string
    /// </summary>
    public string SourceDisplay => Source switch
    {
        TicketSource.WebUI => "Web UI",
        TicketSource.Jira => "Jira",
        TicketSource.AzureDevOps => "Azure DevOps",
        TicketSource.GitHubIssues => "GitHub Issues",
        _ => Source.ToString()
    };
}
