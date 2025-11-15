using Bunit;
using Xunit;
using PRFactory.Web.UI.Editors;

namespace PRFactory.Web.Tests.UI.Editors;

/// <summary>
/// Tests for MarkdownPreview component
/// </summary>
public class MarkdownPreviewTests : TestContext
{
    [Fact]
    public void MarkdownPreview_RendersMarkdownContentAsHtml()
    {
        // Arrange
        var markdown = "# Hello World\n\nThis is a paragraph.";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<h1", cut.Markup);
        Assert.Contains("Hello World", cut.Markup);
        Assert.Contains("<p>", cut.Markup);
        Assert.Contains("This is a paragraph", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_HandlesNullMarkdownGracefully()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, (string)null!));

        // Assert
        Assert.Contains("Preview will appear here", cut.Markup);
        Assert.Contains("empty-state", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_HandlesEmptyMarkdownGracefully()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, string.Empty));

        // Assert
        Assert.Contains("Preview will appear here", cut.Markup);
        Assert.Contains("empty-state", cut.Markup);
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsHeadingsCorrectly()
    {
        // Arrange
        var markdown = "# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<h1", cut.Markup);
        Assert.Contains("<h2", cut.Markup);
        Assert.Contains("<h3", cut.Markup);
        Assert.Contains("<h4", cut.Markup);
        Assert.Contains("<h5", cut.Markup);
        Assert.Contains("<h6", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsListsCorrectly()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<ul>", cut.Markup);
        Assert.Contains("<li>", cut.Markup);
        Assert.Contains("Item 1", cut.Markup);
        Assert.Contains("Item 2", cut.Markup);
        Assert.Contains("Item 3", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsCodeBlocksCorrectly()
    {
        // Arrange
        var markdown = "```csharp\nvar x = 10;\n```";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<code", cut.Markup);
        Assert.Contains("var x = 10;", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsLinksCorrectly()
    {
        // Arrange
        var markdown = "[Click here](https://example.com)";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<a", cut.Markup);
        Assert.Contains("href=\"https://example.com\"", cut.Markup);
        Assert.Contains("Click here", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsBoldAndItalicCorrectly()
    {
        // Arrange
        var markdown = "**bold** *italic* ~~strikethrough~~";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<strong>", cut.Markup);
        Assert.Contains("bold", cut.Markup);
        Assert.Contains("<em>", cut.Markup);
        Assert.Contains("italic", cut.Markup);
    }

    [Fact]
    public void MarkdownPreview_ConvertsOrderedListCorrectly()
    {
        // Arrange
        var markdown = "1. First\n2. Second\n3. Third";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        Assert.Contains("<ol>", cut.Markup);
        Assert.Contains("<li>", cut.Markup);
        Assert.Contains("First", cut.Markup);
        Assert.Contains("Second", cut.Markup);
        Assert.Contains("Third", cut.Markup);
    }
}
