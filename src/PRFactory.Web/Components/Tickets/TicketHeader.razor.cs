using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tickets;

public partial class TicketHeader
{
    [Parameter, EditorRequired]
    public TicketDto Ticket { get; set; } = null!;

    private string FormatDescription(string description)
    {
        // Simple markdown-like formatting
        // Convert newlines to <br> and preserve paragraphs
        var formatted = System.Net.WebUtility.HtmlEncode(description);
        formatted = formatted.Replace("\n\n", "</p><p>");
        formatted = formatted.Replace("\n", "<br>");
        return $"<p>{formatted}</p>";
    }
}
