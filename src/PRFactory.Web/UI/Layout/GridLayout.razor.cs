using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.UI.Layout;

/// <summary>
/// Semantic wrapper for Bootstrap grid rows.
/// </summary>
public partial class GridLayout
{
    /// <summary>
    /// The content to display inside the grid row.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Additional CSS classes to apply to the row.
    /// </summary>
    [Parameter]
    public string AdditionalClass { get; set; } = string.Empty;
}
