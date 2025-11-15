# Phase 3: DiffPlex Integration

**Status**: Not Started
**Estimated Effort**: 6-8 hours
**Dependencies**: None (can develop in parallel with Phase 1-2)
**Risk Level**: Medium (git patch parsing complexity)

## Objective

Integrate DiffPlex library for server-side diff rendering. Parse git patch format and generate HTML for unified and side-by-side views.

## Tasks

### Task 3.1: Install DiffPlex NuGet Package

**File**: `/src/PRFactory.Web/PRFactory.Web.csproj`

```bash
dotnet add package DiffPlex --version 1.7.2
```

**Verify** package reference added:
```xml
<PackageReference Include="DiffPlex" Version="1.7.2" />
```

### Task 3.2: Create `IDiffRenderService` Interface

**File**: `/src/PRFactory.Core/Application/Services/IDiffRenderService.cs`

```csharp
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

public enum DiffViewMode
{
    Unified,
    SideBySide
}

public class FileChangeInfo
{
    public required string FilePath { get; init; }
    public required FileChangeType ChangeType { get; init; }
    public int LinesAdded { get; init; }
    public int LinesDeleted { get; init; }
}

public enum FileChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed
}
```

### Task 3.3: Implement `DiffRenderService`

**File**: `/src/PRFactory.Infrastructure/CodeDiff/DiffRenderService.cs`

```csharp
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.CodeDiff;

public class DiffRenderService : IDiffRenderService
{
    private readonly ILogger<DiffRenderService> _logger;
    private readonly Differ _differ;

    public DiffRenderService(ILogger<DiffRenderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _differ = new Differ();
    }

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

    public List<FileChangeInfo> ParseFileChanges(string diffPatch)
    {
        var files = new List<FileChangeInfo>();

        if (string.IsNullOrEmpty(diffPatch))
            return files;

        var fileSections = ParseGitPatch(diffPatch);

        foreach (var section in fileSections)
        {
            files.Add(new FileChangeInfo
            {
                FilePath = section.NewFilePath ?? section.OldFilePath ?? "unknown",
                ChangeType = DetermineChangeType(section),
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

        foreach (var line in lines)
        {
            // New file section: "diff --git a/file.txt b/file.txt"
            if (line.StartsWith("diff --git "))
            {
                if (currentSection != null)
                {
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
            // File metadata
            else if (line.StartsWith("--- ") || line.StartsWith("+++ "))
            {
                // Skip metadata (already captured from diff --git line)
                continue;
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

        html.Append($"<div class='file-diff-section'>");
        html.Append($"<div class='file-header {changeClass}'>");
        html.Append($"<i class='bi bi-{changeIcon}'></i> ");
        html.Append($"<span class='file-path'>{HttpUtility.HtmlEncode(filePath)}</span>");
        html.Append($"<span class='file-stats'>+{section.AddedLines.Count} -{section.DeletedLines.Count}</span>");
        html.Append($"</div>");

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
        if (section.OldFilePath == "/dev/null")
            return FileChangeType.Added;
        if (section.NewFilePath == "/dev/null")
            return FileChangeType.Deleted;
        if (section.OldFilePath != section.NewFilePath)
            return FileChangeType.Renamed;
        return FileChangeType.Modified;
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
```

### Task 3.4: Register Service in DI

**File**: `/src/PRFactory.Infrastructure/DependencyInjection.cs`

```csharp
using PRFactory.Infrastructure.CodeDiff;

// Add to AddInfrastructure() method:
services.AddScoped<IDiffRenderService, DiffRenderService>();
```

## Acceptance Criteria

- [ ] DiffPlex NuGet package installed
- [ ] `IDiffRenderService` interface created
- [ ] `DiffRenderService` parses git patches correctly
- [ ] HTML rendering produces valid, HTML-encoded output
- [ ] File change stats calculated (lines added/deleted)
- [ ] Service registered in DI container
- [ ] Error handling for malformed patches

## Testing

```csharp
[Fact]
public void RenderDiffAsHtml_ParsesSimplePatch()
{
    // Arrange
    var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,4 @@
 line 1
+added line
 line 2
 line 3";

    var service = new DiffRenderService(Mock.Of<ILogger<DiffRenderService>>());

    // Act
    var html = service.RenderDiffAsHtml(patch, DiffViewMode.Unified);

    // Assert
    Assert.Contains("test.txt", html);
    Assert.Contains("added line", html);
    Assert.Contains("diff-line-added", html);
}
```

## Next Steps

- **Phase 4**: Create Blazor UI components using this service
