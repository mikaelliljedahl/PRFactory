using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tickets;

public partial class WorkflowTimeline
{
    [Parameter, EditorRequired]
    public TicketDto Ticket { get; set; } = null!;

    [Parameter]
    public List<WorkflowEventDto>? Events { get; set; }

    private string FormatTimestamp(DateTime timestamp)
    {
        var now = DateTime.UtcNow;
        var diff = now - timestamp;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} days ago";

        return timestamp.ToString("MMM dd, yyyy 'at' h:mm tt");
    }

    private string GetEventMarkerClass(WorkflowEventDto evt)
    {
        if (evt.EventType == "WorkflowStateChanged" && evt.ToState.HasValue)
        {
            return GetStateMarkerClass(evt.ToState.Value);
        }

        return "timeline-marker-info";
    }

    private string GetStateMarkerClass(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Completed => "timeline-marker-success",
            WorkflowState.Failed => "timeline-marker-danger",
            WorkflowState.Cancelled => "timeline-marker-secondary",
            WorkflowState.PlanApproved => "timeline-marker-success",
            WorkflowState.PlanRejected => "timeline-marker-danger",
            WorkflowState.ImplementationFailed => "timeline-marker-danger",
            WorkflowState.AwaitingAnswers => "timeline-marker-warning",
            WorkflowState.PlanUnderReview => "timeline-marker-warning",
            _ => "timeline-marker-info"
        };
    }

    private string GetEventIcon(WorkflowEventDto evt)
    {
        return evt.EventType switch
        {
            "WorkflowStateChanged" => "bi bi-arrow-right-circle",
            "QuestionAdded" => "bi bi-question-circle",
            "AnswerAdded" => "bi bi-chat-left-text",
            "PlanCreated" => "bi bi-file-earmark-text",
            "PullRequestCreated" => "bi bi-git",
            _ => "bi bi-circle"
        };
    }

    private string GetStateIcon(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Triggered => "bi bi-play-circle",
            WorkflowState.Analyzing => "bi bi-search",
            WorkflowState.QuestionsPosted => "bi bi-question-circle",
            WorkflowState.AwaitingAnswers => "bi bi-hourglass-split",
            WorkflowState.AnswersReceived => "bi bi-check-circle",
            WorkflowState.Planning => "bi bi-pencil-square",
            WorkflowState.PlanPosted => "bi bi-file-earmark-text",
            WorkflowState.PlanUnderReview => "bi bi-eye",
            WorkflowState.PlanApproved => "bi bi-check-circle",
            WorkflowState.PlanRejected => "bi bi-x-circle",
            WorkflowState.Implementing => "bi bi-gear",
            WorkflowState.ImplementationFailed => "bi bi-exclamation-triangle",
            WorkflowState.PRCreated => "bi bi-git",
            WorkflowState.InReview => "bi bi-eye",
            WorkflowState.Completed => "bi bi-check-circle",
            WorkflowState.Cancelled => "bi bi-x-circle",
            WorkflowState.Failed => "bi bi-exclamation-triangle",
            _ => "bi bi-circle"
        };
    }

    private string GetStateDisplay(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Triggered => "Triggered",
            WorkflowState.Analyzing => "Analyzing",
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
            _ => state.ToString()
        };
    }

    private string GetStateBadgeClass(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Completed => "bg-success",
            WorkflowState.Failed => "bg-danger",
            WorkflowState.Cancelled => "bg-secondary",
            WorkflowState.PlanApproved => "bg-success",
            WorkflowState.PlanRejected => "bg-danger",
            WorkflowState.ImplementationFailed => "bg-danger",
            WorkflowState.AwaitingAnswers => "bg-warning",
            WorkflowState.PlanUnderReview => "bg-warning",
            _ => "bg-info"
        };
    }
}
