using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.UI.Display;

public partial class EventTimeline
{
    [Parameter, EditorRequired]
    public List<WorkflowEventDto> Events { get; set; } = new();

    [Parameter]
    public bool ShowDetails { get; set; } = false;

    [Parameter]
    public EventCallback<WorkflowEventDto> OnEventClick { get; set; }

    [Parameter]
    public string AdditionalClass { get; set; } = "";

    private string GetTimelineItemClass(EventSeverity severity)
    {
        return $"severity-{severity.ToString().ToLower()}";
    }

    private string GetMarkerClass(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Success => "success",
            EventSeverity.Error => "error",
            EventSeverity.Warning => "warning",
            EventSeverity.Info => "info",
            _ => "info"
        };
    }
}
