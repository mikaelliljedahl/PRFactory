using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.CodeDiff;

namespace PRFactory.Infrastructure.Tests.CodeDiff;

/// <summary>
/// Comprehensive unit tests for DiffRenderService with 85%+ coverage.
/// </summary>
public class DiffRenderServiceTests
{
    private readonly Mock<ILogger<DiffRenderService>> _mockLogger;
    private readonly DiffRenderService _service;

    public DiffRenderServiceTests()
    {
        _mockLogger = new Mock<ILogger<DiffRenderService>>();
        _service = new DiffRenderService(_mockLogger.Object);
    }

    #region RenderDiffAsHtml Tests

    [Fact]
    public void RenderDiffAsHtml_EmptyPatch_ReturnsMessage()
    {
        // Arrange
        var emptyPatch = "";

        // Act
        var result = _service.RenderDiffAsHtml(emptyPatch);

        // Assert
        Assert.Contains("No changes to display", result);
        Assert.Contains("text-muted", result);
    }

    [Fact]
    public void RenderDiffAsHtml_NullPatch_ReturnsMessage()
    {
        // Arrange
        string? nullPatch = null;

        // Act
        var result = _service.RenderDiffAsHtml(nullPatch!);

        // Assert
        Assert.Contains("No changes to display", result);
    }

    [Fact]
    public void RenderDiffAsHtml_SimplePatch_RendersCorrectly()
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

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("test.txt", result);
        Assert.Contains("added line", result);
        Assert.Contains("diff-container", result);
        Assert.Contains("file-diff-section", result);
    }

    [Fact]
    public void RenderDiffAsHtml_MultipleFiles_RendersAll()
    {
        // Arrange
        var patch = @"diff --git a/file1.txt b/file1.txt
--- a/file1.txt
+++ b/file1.txt
@@ -1 +1,2 @@
 line 1
+added to file1
diff --git a/file2.txt b/file2.txt
--- a/file2.txt
+++ b/file2.txt
@@ -1 +1,2 @@
 line 1
+added to file2";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("file1.txt", result);
        Assert.Contains("file2.txt", result);
        Assert.Contains("added to file1", result);
        Assert.Contains("added to file2", result);
    }

    [Fact]
    public void RenderDiffAsHtml_AddedLines_HasCorrectCssClass()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1 +1,2 @@
 line 1
+added line";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("diff-line-added", result);
        Assert.Contains("+added line", result);
    }

    [Fact]
    public void RenderDiffAsHtml_DeletedLines_HasCorrectCssClass()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,2 +1 @@
 line 1
-deleted line";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("diff-line-deleted", result);
        Assert.Contains("-deleted line", result);
    }

    [Fact]
    public void RenderDiffAsHtml_UnchangedLines_HasCorrectCssClass()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,3 @@
 unchanged line
-old line
+new line";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("diff-line-unchanged", result);
        Assert.Contains("unchanged line", result);
    }

    [Fact]
    public void RenderDiffAsHtml_HunkHeaders_RendersCorrectly()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,4 @@
 line 1
+added line";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("diff-line-hunk-header", result);
        Assert.Contains("@@ -1,3 +1,4 @@", result);
    }

    [Fact]
    public void RenderDiffAsHtml_EscapesHtmlContent()
    {
        // Arrange - XSS protection test
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1 +1,2 @@
 normal line
+<script>alert('xss')</script>";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.DoesNotContain("<script>alert('xss')</script>", result);
        Assert.Contains("&lt;script&gt;", result);
        Assert.Contains("&lt;/script&gt;", result);
    }

    [Fact]
    public void RenderDiffAsHtml_EscapesFilePath()
    {
        // Arrange - Test file path HTML encoding
        var patch = @"diff --git a/path<with>special&chars.txt b/path<with>special&chars.txt
--- a/path<with>special&chars.txt
+++ b/path<with>special&chars.txt
@@ -1 +1,2 @@
 line 1
+added line";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.DoesNotContain("<with>", result);
        Assert.Contains("&lt;with&gt;", result);
        Assert.Contains("&amp;", result);
    }

    [Fact]
    public void RenderDiffAsHtml_UnifiedViewMode_RendersCorrectly()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1 +1,2 @@
 line 1
+added line";

        // Act
        var result = _service.RenderDiffAsHtml(patch, DiffViewMode.Unified);

        // Assert
        Assert.Contains("diff-unified", result);
        Assert.Contains("diff-table", result);
    }

    [Fact]
    public void RenderDiffAsHtml_SideBySideViewMode_RendersCorrectly()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1 +1,2 @@
 line 1
+added line";

        // Act
        var result = _service.RenderDiffAsHtml(patch, DiffViewMode.SideBySide);

        // Assert
        // Side-by-side uses same rendering as unified for MVP
        Assert.Contains("diff-table", result);
    }

    [Fact]
    public void RenderDiffAsHtml_FileStats_DisplaysCorrectly()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,2 +1,3 @@
 line 1
+added line
-deleted line
 line 2";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("file-stats", result);
        Assert.Contains("+1", result); // 1 added line
        Assert.Contains("-1", result); // 1 deleted line
    }

    #endregion

    #region ParseFileChanges Tests

    [Fact]
    public void ParseFileChanges_EmptyPatch_ReturnsEmptyList()
    {
        // Arrange
        var emptyPatch = "";

        // Act
        var result = _service.ParseFileChanges(emptyPatch);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFileChanges_NullPatch_ReturnsEmptyList()
    {
        // Arrange
        string? nullPatch = null;

        // Act
        var result = _service.ParseFileChanges(nullPatch!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFileChanges_SingleFile_ReturnsCorrectStats()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,4 @@
 line 1
+added line
-deleted line
 line 2";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal("test.txt", result[0].FilePath);
        Assert.Equal(1, result[0].LinesAdded);
        Assert.Equal(1, result[0].LinesDeleted);
    }

    [Fact]
    public void ParseFileChanges_MultipleFiles_CountsCorrectly()
    {
        // Arrange
        var patch = @"diff --git a/file1.txt b/file1.txt
--- a/file1.txt
+++ b/file1.txt
@@ -1 +1,3 @@
 line 1
+added line 1
+added line 2
diff --git a/file2.txt b/file2.txt
--- a/file2.txt
+++ b/file2.txt
@@ -1,3 +1 @@
-deleted line 1
-deleted line 2
 line 1";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Equal(2, result.Count);

        var file1 = result[0];
        Assert.Equal("file1.txt", file1.FilePath);
        Assert.Equal(2, file1.LinesAdded);
        Assert.Equal(0, file1.LinesDeleted);

        var file2 = result[1];
        Assert.Equal("file2.txt", file2.FilePath);
        Assert.Equal(0, file2.LinesAdded);
        Assert.Equal(2, file2.LinesDeleted);
    }

    [Fact]
    public void ParseFileChanges_AddedFile_DetectsChangeType()
    {
        // Arrange
        var patch = @"diff --git a/new-file.txt b/new-file.txt
--- a/dev/null
+++ b/new-file.txt
@@ -0,0 +1,2 @@
+line 1
+line 2";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal("new-file.txt", result[0].FilePath);
        Assert.Equal(FileChangeType.Added, result[0].ChangeType);
        Assert.Equal(2, result[0].LinesAdded);
    }

    [Fact]
    public void ParseFileChanges_DeletedFile_DetectsChangeType()
    {
        // Arrange
        var patch = @"diff --git a/old-file.txt b/old-file.txt
--- a/old-file.txt
+++ b/dev/null
@@ -1,2 +0,0 @@
-line 1
-line 2";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal("old-file.txt", result[0].FilePath);
        Assert.Equal(FileChangeType.Deleted, result[0].ChangeType);
        Assert.Equal(2, result[0].LinesDeleted);
    }

    [Fact]
    public void ParseFileChanges_RenamedFile_DetectsChangeType()
    {
        // Arrange
        var patch = @"diff --git a/old-name.txt b/new-name.txt
--- a/old-name.txt
+++ b/new-name.txt
@@ -1 +1 @@
 line 1";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal("new-name.txt", result[0].FilePath);
        Assert.Equal(FileChangeType.Renamed, result[0].ChangeType);
    }

    [Fact]
    public void ParseFileChanges_ModifiedFile_DetectsChangeType()
    {
        // Arrange
        var patch = @"diff --git a/file.txt b/file.txt
--- a/file.txt
+++ b/file.txt
@@ -1,2 +1,2 @@
 line 1
-old line 2
+new line 2";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal("file.txt", result[0].FilePath);
        Assert.Equal(FileChangeType.Modified, result[0].ChangeType);
    }

    [Fact]
    public void ParseFileChanges_MultipleHunks_CountsAllLines()
    {
        // Arrange
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,4 @@
 line 1
+added line in hunk 1
 line 2
 line 3
@@ -10,2 +11,3 @@
 line 10
+added line in hunk 2
 line 11";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].LinesAdded);
        Assert.Equal(0, result[0].LinesDeleted);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void RenderDiffAsHtml_MalformedPatch_ReturnsErrorDiv()
    {
        // Arrange - Intentionally malformed patch that might cause exceptions
        var malformedPatch = "This is not a valid git patch at all!";

        // Act
        var result = _service.RenderDiffAsHtml(malformedPatch);

        // Assert
        // Service should handle gracefully and return empty container or error
        Assert.Contains("diff-container", result);
    }

    [Fact]
    public void RenderDiffAsHtml_PatchWithNoHunks_RendersFileHeader()
    {
        // Arrange
        var patch = @"diff --git a/empty-file.txt b/empty-file.txt
--- a/empty-file.txt
+++ b/empty-file.txt";

        // Act
        var result = _service.RenderDiffAsHtml(patch);

        // Assert
        Assert.Contains("empty-file.txt", result);
        Assert.Contains("file-diff-section", result);
    }

    [Fact]
    public void ParseFileChanges_PatchWithOnlyMetadata_ReturnsFileWithZeroStats()
    {
        // Arrange
        var patch = @"diff --git a/file.txt b/file.txt
--- a/file.txt
+++ b/file.txt";

        // Act
        var result = _service.ParseFileChanges(patch);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].LinesAdded);
        Assert.Equal(0, result[0].LinesDeleted);
    }

    [Fact]
    public void RenderDiffAsHtml_FileIconsRenderedCorrectly()
    {
        // Arrange
        var addedPatch = @"diff --git a/new.txt b/new.txt
--- a/dev/null
+++ b/new.txt
@@ -0,0 +1 @@
+new file";

        // Act
        var result = _service.RenderDiffAsHtml(addedPatch);

        // Assert
        Assert.Contains("bi-file-plus", result); // Added file icon
        Assert.Contains("file-added", result); // Added file class
    }

    [Fact]
    public void RenderDiffAsHtml_DeletedFileIconsRenderedCorrectly()
    {
        // Arrange
        var deletedPatch = @"diff --git a/old.txt b/old.txt
--- a/old.txt
+++ b/dev/null
@@ -1 +0,0 @@
-old file";

        // Act
        var result = _service.RenderDiffAsHtml(deletedPatch);

        // Assert
        Assert.Contains("bi-file-minus", result); // Deleted file icon
        Assert.Contains("file-deleted", result); // Deleted file class
    }

    [Fact]
    public void RenderDiffAsHtml_RenamedFileIconsRenderedCorrectly()
    {
        // Arrange
        var renamedPatch = @"diff --git a/old-name.txt b/new-name.txt
--- a/old-name.txt
+++ b/new-name.txt
@@ -1 +1 @@
 content";

        // Act
        var result = _service.RenderDiffAsHtml(renamedPatch);

        // Assert
        Assert.Contains("bi-arrow-left-right", result); // Renamed file icon
        Assert.Contains("file-renamed", result); // Renamed file class
    }

    [Fact]
    public void RenderDiffAsHtml_ModifiedFileIconsRenderedCorrectly()
    {
        // Arrange
        var modifiedPatch = @"diff --git a/file.txt b/file.txt
--- a/file.txt
+++ b/file.txt
@@ -1 +1 @@
-old
+new";

        // Act
        var result = _service.RenderDiffAsHtml(modifiedPatch);

        // Assert
        Assert.Contains("bi-file-diff", result); // Modified file icon
        Assert.Contains("file-modified", result); // Modified file class
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiffRenderService(null!));
    }

    #endregion
}
