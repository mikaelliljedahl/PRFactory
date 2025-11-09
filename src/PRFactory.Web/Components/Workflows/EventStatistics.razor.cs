using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Workflows;

public partial class EventStatistics
{
    [Parameter, EditorRequired]
    public EventStatisticsDto Statistics { get; set; } = new();

    private string FormatDuration(double seconds)
    {
        if (seconds == 0) return "N/A";

        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalMinutes < 1)
            return "< 1 min";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes} min";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";

        return $"{(int)timeSpan.TotalDays} days";
    }

    private string GetEventTypeProgressColor(string eventType)
    {
        return eventType switch
        {
            "WorkflowStateChanged" => "bg-primary",
            "QuestionAdded" => "bg-info",
            "AnswerAdded" => "bg-success",
            "PlanCreated" => "bg-warning",
            "PullRequestCreated" => "bg-success",
            _ => "bg-secondary"
        };
    }
}
