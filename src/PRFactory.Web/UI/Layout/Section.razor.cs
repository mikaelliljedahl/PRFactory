using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.UI.Layout;

/// <summary>
/// Logical page section with optional collapse/expand functionality.
/// </summary>
public partial class Section
{
    private bool isCollapsed;

    /// <summary>
    /// Optional section title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Optional Bootstrap icon name (without "bi-" prefix).
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Whether the section can be collapsed.
    /// </summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>
    /// Whether the section is initially collapsed.
    /// </summary>
    [Parameter]
    public bool InitiallyCollapsed { get; set; }

    /// <summary>
    /// The content to display inside the section.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        isCollapsed = InitiallyCollapsed;
    }

    private void ToggleCollapse()
    {
        if (Collapsible)
        {
            isCollapsed = !isCollapsed;
        }
    }
}
