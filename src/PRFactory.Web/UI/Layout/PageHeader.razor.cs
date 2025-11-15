using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.UI.Layout;

/// <summary>
/// Consistent page header component with title, icon, subtitle, and action buttons.
/// </summary>
public partial class PageHeader
{
    /// <summary>
    /// The main page title.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional Bootstrap icon name (without "bi-" prefix).
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Optional subtitle or description.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Optional action buttons or links to display in the header.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }
}
