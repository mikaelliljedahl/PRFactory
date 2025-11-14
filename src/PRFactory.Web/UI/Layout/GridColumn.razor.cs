using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.UI.Layout;

/// <summary>
/// Semantic wrapper for Bootstrap grid columns.
/// </summary>
public partial class GridColumn
{
    /// <summary>
    /// Column width (1-12). Defaults to 12 (full width).
    /// </summary>
    [Parameter]
    public int Width { get; set; } = 12;

    /// <summary>
    /// The content to display inside the column.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Additional CSS classes to apply to the column.
    /// </summary>
    [Parameter]
    public string AdditionalClass { get; set; } = string.Empty;
}
