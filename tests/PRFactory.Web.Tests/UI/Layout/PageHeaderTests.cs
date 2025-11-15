using Bunit;
using PRFactory.Web.UI.Layout;
using Xunit;

namespace PRFactory.Web.Tests.UI.Layout;

public class PageHeaderTests : TestContext
{
    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test Title"));

        // Assert
        var h1 = cut.Find("h1");
        Assert.Equal("Test Title", h1.TextContent.Trim());
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Icon, "gear"));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("bi-gear", icon.ClassName);
    }

    [Fact]
    public void Render_WithoutIcon_DoesNotDisplayIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Empty(cut.FindAll("i"));
    }

    [Fact]
    public void Render_WithSubtitle_DisplaysSubtitle()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Subtitle, "Test subtitle"));

        // Assert
        var subtitle = cut.Find(".page-header-subtitle");
        Assert.Equal("Test subtitle", subtitle.TextContent.Trim());
    }

    [Fact]
    public void Render_WithoutSubtitle_DoesNotDisplaySubtitle()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Empty(cut.FindAll(".page-header-subtitle"));
    }

    [Fact]
    public void Render_WithActions_DisplaysActions()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Actions, builder => builder.AddMarkupContent(0, "<button>Test Action</button>")));

        // Assert
        var actionsDiv = cut.Find(".page-header-actions");
        Assert.Contains("Test Action", actionsDiv.TextContent);
    }

    [Fact]
    public void Render_WithoutActions_DoesNotDisplayActionsDiv()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Empty(cut.FindAll(".page-header-actions"));
    }

    [Fact]
    public void Render_AppliesPageHeaderClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var pageHeader = cut.Find(".page-header");
        Assert.NotNull(pageHeader);
    }

    [Fact]
    public void Render_TitleAndIcon_DisplaysInCorrectOrder()
    {
        // Arrange & Act
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test Title")
            .Add(p => p.Icon, "ticket"));

        // Assert
        var titleDiv = cut.Find(".page-header-title");
        var icon = titleDiv.QuerySelector("i");
        var h1 = titleDiv.QuerySelector("h1");

        Assert.NotNull(icon);
        Assert.NotNull(h1);
        Assert.Contains("bi-ticket", icon.ClassName);
    }
}
