using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.CodeDiff;

/// <summary>
/// Service for rendering git diffs as HTML.
/// </summary>
public class DiffRenderService : IDiffRenderService
{
    private readonly ILogger<DiffRenderService> _logger;
    private readonly Differ _differ;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffRenderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public DiffRenderService(ILogger<DiffRenderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _differ = new Differ();
    }

    /// <inheritdoc/>
    public string RenderDiffAsHtml(string diffPatch, DiffViewMode viewMode = DiffViewMode.Unified)
    {
        if (string.IsNullOrEmpty(diffPatch))
        {
            return "<p class='text-muted'>No changes to display.</p>";
        }

        _logger.LogDebug("Rendering diff as HTML ({ViewMode}): {Size} bytes", viewMode, diffPatch.Length);

        try
        {
            // Parse git patch into file sections
            var fileSections = ParseGitPatch(diffPatch);

            var html = new StringBuilder();
            html.Append("<div class='diff-container'>");

            foreach (var fileSection in fileSections)
            {
                html.Append(RenderFileSection(fileSection, viewMode));
            }

            html.Append("</div>");

            return html.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering diff as HTML");
            return $"<div class='alert alert-danger'>Error rendering diff: {HttpUtility.HtmlEncode(ex.Message)}</div>";
        }
    }

    /// <inheritdoc/>
    public List<FileChangeInfo> ParseFileChanges(string diffPatch)
    {
        var files = new List<FileChangeInfo>();

        if (string.IsNullOrEmpty(diffPatch))
            return files;

        var fileSections = ParseGitPatch(diffPatch);

        foreach (var section in fileSections)
        {
            var changeType = DetermineChangeType(section);
            var filePath = GetPrimaryFilePath(section, changeType);

            files.Add(new FileChangeInfo
            {
                FilePath = filePath,
                ChangeType = changeType,
                LinesAdded = section.AddedLines.Count,
                LinesDeleted = section.DeletedLines.Count
            });
        }

        return files;
    }

    private List<FileDiffSection> ParseGitPatch(string diffPatch)
    {
        var sections = new List<FileDiffSection>();
        var lines = diffPatch.Split('\n');

        FileDiffSection? currentSection = null;
        var currentHunk = new List<string>();

        foreach (var rawLine in lines)
        {
            // Trim carriage returns from Windows line endings
            var line = rawLine.TrimEnd('\r');

            // New file section: "diff --git a/file.txt b/file.txt"
            if (line.StartsWith("diff --git "))
            {
                if (currentSection != null)
                {
                    if (currentHunk.Any())
                        currentSection.Hunks.Add(currentHunk.ToArray());
                    sections.Add(currentSection);
                }

                currentSection = new FileDiffSection();
                currentHunk = new List<string>();

                var match = Regex.Match(line, @"diff --git a/(.+?) b/(.+)");
                if (match.Success)
                {
                    currentSection.OldFilePath = match.Groups[1].Value;
                    currentSection.NewFilePath = match.Groups[2].Value;
                }
            }
            // File metadata - use these lines to confirm paths (handles /dev/null correctly)
            else if (line.StartsWith("--- ") && currentSection != null)
            {
                var oldPath = line.Substring(4).Trim(); // "--- a/file.txt" -> "a/file.txt"
                if (oldPath.StartsWith("a/"))
                    oldPath = oldPath.Substring(2); // Remove "a/" prefix
                if (!string.IsNullOrEmpty(oldPath))
                    currentSection.OldFilePath = oldPath;
            }
            else if (line.StartsWith("+++ ") && currentSection != null)
            {
                var newPath = line.Substring(4).Trim(); // "+++ b/file.txt" -> "b/file.txt"
                if (newPath.StartsWith("b/"))
                    newPath = newPath.Substring(2); // Remove "b/" prefix
                if (!string.IsNullOrEmpty(newPath))
                    currentSection.NewFilePath = newPath;
            }
            // Hunk header: "@@ -1,5 +1,7 @@"
            else if (line.StartsWith("@@ "))
            {
                if (currentHunk.Any())
                {
                    currentSection?.Hunks.Add(currentHunk.ToArray());
                    currentHunk = new List<string>();
                }
                currentHunk.Add(line);
            }
            // Diff content lines
            else if (currentSection != null)
            {
                currentHunk.Add(line);

                if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    currentSection.AddedLines.Add(line.Substring(1));
                }
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                {
                    currentSection.DeletedLines.Add(line.Substring(1));
                }
            }
        }

        // Add final section
        if (currentSection != null)
        {
            if (currentHunk.Any())
                currentSection.Hunks.Add(currentHunk.ToArray());
            sections.Add(currentSection);
        }

        return sections;
    }

    private string RenderFileSection(FileDiffSection section, DiffViewMode viewMode)
    {
        var html = new StringBuilder();

        var filePath = section.NewFilePath ?? section.OldFilePath ?? "unknown";
        var changeType = DetermineChangeType(section);
        var changeIcon = GetChangeIcon(changeType);
        var changeClass = GetChangeClass(changeType);

        html.Append("<div class='file-diff-section'>");
        html.Append($"<div class='file-header {changeClass}'>");
        html.Append($"<i class='bi bi-{changeIcon}'></i> ");
        html.Append($"<span class='file-path'>{HttpUtility.HtmlEncode(filePath)}</span>");
        html.Append($"<span class='file-stats'>+{section.AddedLines.Count} -{section.DeletedLines.Count}</span>");
        html.Append("</div>");

        // Render hunks
        foreach (var hunk in section.Hunks)
        {
            html.Append(viewMode == DiffViewMode.SideBySide
                ? RenderHunkSideBySide(hunk)
                : RenderHunkUnified(hunk));
        }

        html.Append("</div>");

        return html.ToString();
    }

    private string RenderHunkUnified(string[] hunkLines)
    {
        var html = new StringBuilder();
        html.Append("<table class='diff-table diff-unified'>");

        foreach (var line in hunkLines)
        {
            var cssClass = GetLineCssClass(line);
            var lineContent = HttpUtility.HtmlEncode(line);

            html.Append($"<tr class='{cssClass}'>");
            html.Append($"<td class='diff-line-content'>{lineContent}</td>");
            html.Append("</tr>");
        }

        html.Append("</table>");
        return html.ToString();
    }

    private string RenderHunkSideBySide(string[] hunkLines)
    {
        // Simplified side-by-side (same as unified for MVP)
        // Full side-by-side requires diff alignment algorithm
        return RenderHunkUnified(hunkLines);
    }

    private string GetLineCssClass(string line)
    {
        if (line.StartsWith("+") && !line.StartsWith("+++"))
            return "diff-line-added";
        if (line.StartsWith("-") && !line.StartsWith("---"))
            return "diff-line-deleted";
        if (line.StartsWith("@@"))
            return "diff-line-hunk-header";
        return "diff-line-unchanged";
    }

    private FileChangeType DetermineChangeType(FileDiffSection section)
    {
        // Check for /dev/null or dev/null (paths from diff --git have a/ b/ prefixes stripped)
        if (section.OldFilePath == "/dev/null" || section.OldFilePath == "dev/null")
            return FileChangeType.Added;
        if (section.NewFilePath == "/dev/null" || section.NewFilePath == "dev/null")
            return FileChangeType.Deleted;
        if (section.OldFilePath != section.NewFilePath)
            return FileChangeType.Renamed;
        return FileChangeType.Modified;
    }

    private string GetPrimaryFilePath(FileDiffSection section, FileChangeType changeType)
    {
        // For deleted files, use old path; for added files, use new path
        return changeType switch
        {
            FileChangeType.Deleted => section.OldFilePath ?? "unknown",
            _ => section.NewFilePath ?? section.OldFilePath ?? "unknown"
        };
    }

    private string GetChangeIcon(FileChangeType changeType) => changeType switch
    {
        FileChangeType.Added => "file-plus",
        FileChangeType.Deleted => "file-minus",
        FileChangeType.Renamed => "arrow-left-right",
        FileChangeType.Modified => "file-diff",
        _ => "file"
    };

    private string GetChangeClass(FileChangeType changeType) => changeType switch
    {
        FileChangeType.Added => "file-added",
        FileChangeType.Deleted => "file-deleted",
        FileChangeType.Renamed => "file-renamed",
        FileChangeType.Modified => "file-modified",
        _ => ""
    };

    private class FileDiffSection
    {
        public string? OldFilePath { get; set; }
        public string? NewFilePath { get; set; }
        public List<string[]> Hunks { get; } = new();
        public List<string> AddedLines { get; } = new();
        public List<string> DeletedLines { get; } = new();
    }
}
