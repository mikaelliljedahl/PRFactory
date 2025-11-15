namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for rendering git diffs as HTML.
/// </summary>
public interface IDiffRenderService
{
    /// <summary>
    /// Renders a git patch as HTML.
    /// </summary>
    /// <param name="diffPatch">Git patch content</param>
    /// <param name="viewMode">Unified or SideBySide</param>
    /// <returns>HTML string (safe for @((MarkupString)html))</returns>
    string RenderDiffAsHtml(string diffPatch, DiffViewMode viewMode = DiffViewMode.Unified);

    /// <summary>
    /// Parses file information from a git patch.
    /// </summary>
    /// <param name="diffPatch">Git patch content</param>
    /// <returns>List of changed files with stats</returns>
    List<FileChangeInfo> ParseFileChanges(string diffPatch);
}

/// <summary>
/// Diff view mode options.
/// </summary>
public enum DiffViewMode
{
    /// <summary>
    /// Unified diff view (additions and deletions in single column).
    /// </summary>
    Unified,

    /// <summary>
    /// Side-by-side diff view (old and new versions in separate columns).
    /// </summary>
    SideBySide
}

/// <summary>
/// Information about a file change in a diff.
/// </summary>
public class FileChangeInfo
{
    /// <summary>
    /// Path to the changed file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Type of change (Added, Modified, Deleted, Renamed).
    /// </summary>
    public required FileChangeType ChangeType { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines deleted.
    /// </summary>
    public int LinesDeleted { get; init; }
}

/// <summary>
/// Type of file change.
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// File was added.
    /// </summary>
    Added,

    /// <summary>
    /// File was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// File was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// File was renamed.
    /// </summary>
    Renamed
}
