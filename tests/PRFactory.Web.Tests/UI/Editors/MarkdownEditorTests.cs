using Bunit;
using PRFactory.Web.UI.Editors;
using Xunit;

namespace PRFactory.Web.Tests.UI.Editors;

public class MarkdownEditorTests : TestContext
{
    [Fact]
    public void Render_WithInitialValue_DisplaysValue()
    {
        // Arrange
        var initialValue = "# Hello World";

        // Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Value, initialValue));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(initialValue, textarea.GetAttribute("value"));
    }

    [Fact]
    public void Render_WithNoValue_DisplaysEmpty()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>();

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(string.Empty, textarea.GetAttribute("value"));
    }

    [Fact]
    public void Render_ShowToolbarTrue_DisplaysToolbar()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.ShowToolbar, true));

        // Assert
        Assert.NotNull(cut.FindComponent<MarkdownToolbar>());
    }

    [Fact]
    public void Render_ShowToolbarFalse_HidesToolbar()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.ShowToolbar, false));

        // Assert
        Assert.Throws<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<MarkdownToolbar>());
    }

    [Theory]
    [InlineData(ViewMode.Split, "view-split")]
    [InlineData(ViewMode.EditorOnly, "view-editor-only")]
    [InlineData(ViewMode.PreviewOnly, "view-preview-only")]
    [InlineData(ViewMode.Fullscreen, "view-fullscreen")]
    public void ViewMode_AppliesCorrectCssClass(ViewMode mode, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.InitialViewMode, mode));

        // Assert
        var editor = cut.Find(".markdown-editor");
        Assert.Contains(expectedClass, editor.ClassName);
    }

    [Fact]
    public void ViewMode_Split_ShowsBothEditorAndPreview()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.InitialViewMode, ViewMode.Split));

        // Assert
        Assert.NotNull(cut.Find(".editor-pane"));
        Assert.NotNull(cut.FindComponent<MarkdownPreview>());
    }

    [Fact]
    public void ViewMode_EditorOnly_ShowsOnlyEditor()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.InitialViewMode, ViewMode.EditorOnly));

        // Assert
        Assert.NotNull(cut.Find(".editor-pane"));
        Assert.Throws<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<MarkdownPreview>());
    }

    [Fact]
    public void ViewMode_PreviewOnly_ShowsOnlyPreview()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.InitialViewMode, ViewMode.PreviewOnly));

        // Assert
        Assert.NotNull(cut.FindComponent<MarkdownPreview>());
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".editor-pane"));
    }

    [Fact]
    public void LineNumbers_UpdatesBasedOnContent()
    {
        // Arrange
        var multilineContent = "Line 1\nLine 2\nLine 3";

        // Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Value, multilineContent));

        // Assert
        var lineNumbers = cut.Find(".line-numbers");
        Assert.Contains("3", lineNumbers.InnerHtml);
    }

    [Fact]
    public void Height_AppliesCustomHeight()
    {
        // Arrange
        var customHeight = "700px";

        // Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Height, customHeight));

        // Assert
        var container = cut.Find(".editor-container");
        Assert.Contains(customHeight, container.GetAttribute("style"));
    }

    [Fact]
    public void Preview_DisplaysEmptyStateWhenNoContent()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, string.Empty));

        // Assert
        Assert.Contains("Preview will appear here", cut.Markup);
    }

    [Fact]
    public void Preview_RendersMarkdownCorrectly()
    {
        // Arrange
        var markdown = "# Heading\n\n**Bold text**";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<h1", cut.Markup);
        Assert.Contains("Heading", cut.Markup);
        Assert.Contains("<strong>", cut.Markup);
        Assert.Contains("Bold text", cut.Markup);
    }

    [Fact]
    public void Preview_HandlesInvalidMarkdown()
    {
        // Arrange
        var invalidMarkdown = "```\nUnclosed code block";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, invalidMarkdown));

        // Assert - Should not throw, should render something
        Assert.NotNull(cut.Markup);
    }
}

public class MarkdownCommandTests
{
    [Fact]
    public void WrapSelection_Bold_AddsAsterisks()
    {
        // Arrange
        var text = "Hello World";
        var cursor = 6;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.Bold });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.Contains("**", newValue);
    }

    [Fact]
    public void InsertAtLineStart_Heading1_AddsHash()
    {
        // Arrange
        var text = "Hello World";
        var cursor = 0;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.Heading1 });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.StartsWith("# ", newValue);
    }

    [Fact]
    public void InsertList_UnorderedList_AddsDash()
    {
        // Arrange
        var text = "Hello World";
        var cursor = 0;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.UnorderedList });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.StartsWith("- ", newValue);
    }

    [Fact]
    public void InsertList_OrderedList_AddsNumber()
    {
        // Arrange
        var text = "Hello World";
        var cursor = 0;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.OrderedList });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.StartsWith("1. ", newValue);
    }

    [Fact]
    public void InsertCodeBlock_AddsTripleBackticks()
    {
        // Arrange
        var text = "Hello World";
        var cursor = 6;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.CodeBlock });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.Contains("```", newValue);
    }

    [Fact]
    public void InsertTable_AddsTableMarkdown()
    {
        // Arrange
        var text = "";
        var cursor = 0;

        // Act
        var editor = new MarkdownEditor();
        var method = typeof(MarkdownEditor).GetMethod("ApplyMarkdownCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(editor, new object[] { text, cursor, ToolbarCommand.Table });

        // Assert
        Assert.NotNull(result);
        var (newValue, newCursor) = ((string, int))result;
        Assert.Contains("Header", newValue);
        Assert.Contains("|", newValue);
        Assert.Contains("-", newValue);
    }
}
