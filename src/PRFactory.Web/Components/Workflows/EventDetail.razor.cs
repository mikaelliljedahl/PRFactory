using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Workflows;

public partial class EventDetail
{
    [Parameter]
    public WorkflowEventDto? Event { get; set; }

    private string GetSeverityBadgeClass()
    {
        if (Event == null) return "bg-secondary";

        return Event.Severity switch
        {
            EventSeverity.Success => "bg-success",
            EventSeverity.Error => "bg-danger",
            EventSeverity.Warning => "bg-warning text-dark",
            EventSeverity.Info => "bg-info",
            _ => "bg-secondary"
        };
    }

    private string FormatMetadataKey(string key)
    {
        // Convert camelCase to Title Case
        if (string.IsNullOrEmpty(key)) return key;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToUpper(key[0]));

        for (int i = 1; i < key.Length; i++)
        {
            if (char.IsUpper(key[i]))
            {
                result.Append(' ');
            }
            result.Append(key[i]);
        }

        return result.ToString();
    }
}
