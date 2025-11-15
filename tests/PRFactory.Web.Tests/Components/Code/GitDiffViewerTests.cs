using Bunit;
using Moq;
using Xunit;
using PRFactory.Web.Components.Code;
using PRFactory.Core.Application.Services;

namespace PRFactory.Web.Tests.Components.Code;

/// <summary>
/// Tests for GitDiffViewer component
/// </summary>
public class GitDiffViewerTests : TestContext
{
    private readonly Mock<IDiffRenderService> _mockDiffRenderService;

    public GitDiffViewerTests()
    {
        _mockDiffRenderService = new Mock<IDiffRenderService>();
        Services.AddSingleton(_mockDiffRenderService.Object);
    }

    [Fact]
    public void GitDiffViewer_RendersEmptyState_WhenDiffContentEmpty()
    {
        // Arrange
        var diffContent = string.Empty;

        // Act
        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Assert
        Assert.Contains("No changes to display", cut.Markup);
    }

    [Fact]
    public void GitDiffViewer_RendersDiffContent_WhenProvided()
    {
        // Arrange
        var diffContent = "diff --git a/file.cs b/file.cs\n+added line\n-deleted line";
        var renderedHtml = "<div class='diff-table'>Rendered diff</div>";

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.Unified))
            .Returns(renderedHtml);

        _mockDiffRenderService
            .Setup(x => x.ParseFileChanges(diffContent))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "file.cs",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 1,
                    LinesDeleted = 1
                }
            });

        // Act
        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Assert
        Assert.Contains("Rendered diff", cut.Markup);
        Assert.DoesNotContain("No changes to display", cut.Markup);
    }

    [Fact]
    public void GitDiffViewer_DisplaysFileStats_WhenAvailable()
    {
        // Arrange
        var diffContent = "diff --git a/file.cs b/file.cs\n+added line\n-deleted line";
        var renderedHtml = "<div class='diff-table'>Rendered diff</div>";

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.Unified))
            .Returns(renderedHtml);

        _mockDiffRenderService
            .Setup(x => x.ParseFileChanges(diffContent))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "file1.cs",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 5,
                    LinesDeleted = 3
                },
                new FileChangeInfo
                {
                    FilePath = "file2.cs",
                    ChangeType = FileChangeType.Added,
                    LinesAdded = 10,
                    LinesDeleted = 0
                }
            });

        // Act
        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Assert - should show 2 files, 15 lines added (5+10), 3 lines deleted
        Assert.Contains("2 file(s)", cut.Markup);
        Assert.Contains("15", cut.Markup); // total lines added
        Assert.Contains("3", cut.Markup); // total lines deleted
    }

    [Fact]
    public void GitDiffViewer_ViewModeToggle_UpdatesRendering()
    {
        // Arrange
        var diffContent = "diff --git a/file.cs b/file.cs\n+added line";
        var unifiedHtml = "<div class='unified'>Unified view</div>";
        var sideBySideHtml = "<div class='side-by-side'>Side by side view</div>";

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.Unified))
            .Returns(unifiedHtml);

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.SideBySide))
            .Returns(sideBySideHtml);

        _mockDiffRenderService
            .Setup(x => x.ParseFileChanges(diffContent))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "file.cs",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 1,
                    LinesDeleted = 0
                }
            });

        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Assert initial state (Unified)
        Assert.Contains("Unified view", cut.Markup);

        // Act - click Side by Side button
        var sideBySideButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Side by Side"));
        Assert.NotNull(sideBySideButton);
        sideBySideButton.Click();

        // Assert - should now show side by side view
        Assert.Contains("Side by side view", cut.Markup);
    }

    [Fact]
    public void GitDiffViewer_DefaultsToUnifiedView()
    {
        // Arrange
        var diffContent = "diff --git a/file.cs b/file.cs\n+added line";
        var renderedHtml = "<div class='diff-table'>Rendered diff</div>";

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.Unified))
            .Returns(renderedHtml);

        _mockDiffRenderService
            .Setup(x => x.ParseFileChanges(diffContent))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "file.cs",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 1,
                    LinesDeleted = 0
                }
            });

        // Act
        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Assert - Unified button should have btn-primary class (active)
        var unifiedButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Unified"));
        Assert.NotNull(unifiedButton);
        Assert.Contains("btn-primary", unifiedButton.ClassName);

        // Side by Side button should have btn-outline-primary class (inactive)
        var sideBySideButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Side by Side"));
        Assert.NotNull(sideBySideButton);
        Assert.Contains("btn-outline-primary", sideBySideButton.ClassName);
    }

    [Fact]
    public void GitDiffViewer_ClickSideBySideButton_ChangesMode()
    {
        // Arrange
        var diffContent = "diff --git a/file.cs b/file.cs\n+added line";
        var unifiedHtml = "<div class='unified'>Unified</div>";
        var sideBySideHtml = "<div class='side-by-side'>Side by side</div>";

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.Unified))
            .Returns(unifiedHtml);

        _mockDiffRenderService
            .Setup(x => x.RenderDiffAsHtml(diffContent, DiffViewMode.SideBySide))
            .Returns(sideBySideHtml);

        _mockDiffRenderService
            .Setup(x => x.ParseFileChanges(diffContent))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "file.cs",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 1,
                    LinesDeleted = 0
                }
            });

        var cut = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, diffContent));

        // Act - click Side by Side button
        var sideBySideButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Side by Side"));
        Assert.NotNull(sideBySideButton);
        sideBySideButton.Click();

        // Assert - re-query buttons to get updated state
        var sideBySideButtonAfter = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Side by Side"));
        Assert.NotNull(sideBySideButtonAfter);
        Assert.Contains("btn-primary", sideBySideButtonAfter.ClassName);

        // Unified button should now be inactive
        var unifiedButtonAfter = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Unified"));
        Assert.NotNull(unifiedButtonAfter);
        Assert.Contains("btn-outline-primary", unifiedButtonAfter.ClassName);

        // Verify service was called with SideBySide mode
        _mockDiffRenderService.Verify(
            x => x.RenderDiffAsHtml(diffContent, DiffViewMode.SideBySide),
            Times.Once);
    }
}
