using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.UI.Alerts;

/// <summary>
/// Display information lists or content with an icon and title.
/// </summary>
public partial class InfoBox
{
    /// <summary>
    /// Optional title for the info box.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Optional Bootstrap icon name (without "bi-" prefix).
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// The content to display inside the info box.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
