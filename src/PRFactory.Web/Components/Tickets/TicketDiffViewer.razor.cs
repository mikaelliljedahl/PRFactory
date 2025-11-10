using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tickets;

public partial class TicketDiffViewer
{
    [Parameter, EditorRequired]
    public TicketDto OriginalTicket { get; set; } = new();

    [Parameter, EditorRequired]
    public TicketUpdateDto TicketUpdate { get; set; } = new();

    private bool HasTitleChanged()
    {
        return !string.Equals(OriginalTicket.Title, TicketUpdate.UpdatedTitle, StringComparison.Ordinal);
    }

    private bool HasDescriptionChanged()
    {
        return !string.Equals(OriginalTicket.Description, TicketUpdate.UpdatedDescription, StringComparison.Ordinal);
    }

    private static string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        try
        {
            return Markdig.Markdown.ToHtml(markdown);
        }
        catch
        {
            // If markdown parsing fails, return the raw text
            return System.Web.HttpUtility.HtmlEncode(markdown);
        }
    }
}
