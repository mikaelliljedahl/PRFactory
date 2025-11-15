using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;

namespace PRFactory.Web.Components.Code;

/// <summary>
/// Component for rendering git diffs with DiffPlex.
/// Supports unified and side-by-side view modes.
/// </summary>
public partial class GitDiffViewer
{
    [Parameter, EditorRequired]
    public string DiffContent { get; set; } = string.Empty;

    [Inject]
    private IDiffRenderService DiffRenderer { get; set; } = null!;

    private DiffViewMode ViewMode { get; set; } = DiffViewMode.Unified;
    private string RenderedDiff { get; set; } = string.Empty;
    private List<FileChangeInfo>? FileStats { get; set; }

    private int TotalLinesAdded => FileStats?.Sum(f => f.LinesAdded) ?? 0;
    private int TotalLinesDeleted => FileStats?.Sum(f => f.LinesDeleted) ?? 0;

    protected override void OnParametersSet()
    {
        RenderDiff();
    }

    private void SetViewMode(DiffViewMode mode)
    {
        if (ViewMode == mode)
            return;

        ViewMode = mode;
        RenderDiff();
        StateHasChanged();
    }

    private void RenderDiff()
    {
        if (string.IsNullOrEmpty(DiffContent))
        {
            RenderedDiff = string.Empty;
            FileStats = null;
            return;
        }

        try
        {
            // Render diff HTML
            RenderedDiff = DiffRenderer.RenderDiffAsHtml(DiffContent, ViewMode);

            // Parse file stats
            FileStats = DiffRenderer.ParseFileChanges(DiffContent);
        }
        catch (Exception ex)
        {
            RenderedDiff = $"<div class='alert alert-danger'>Error rendering diff: {ex.Message}</div>";
            FileStats = null;
        }
    }
}
