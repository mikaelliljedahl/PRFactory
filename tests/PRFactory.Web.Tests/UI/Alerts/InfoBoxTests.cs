using Bunit;
using PRFactory.Web.UI.Alerts;
using Xunit;

namespace PRFactory.Web.Tests.UI.Alerts;

public class InfoBoxTests : TestContext
{
    [Fact]
    public void Render_AppliesAlertInfoClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>();

        // Assert
        var alert = cut.Find(".alert");
        Assert.Contains("alert-info", alert.ClassName);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .Add(p => p.Title, "Important Information"));

        // Assert
        var heading = cut.Find(".alert-heading");
        Assert.Equal("Important Information", heading.TextContent.Trim());
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .Add(p => p.Title, "Info")
            .Add(p => p.Icon, "info-circle"));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("bi-info-circle", icon.ClassName);
    }

    [Fact]
    public void Render_WithoutTitle_DoesNotDisplayHeading()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Empty(cut.FindAll(".alert-heading"));
    }

    [Fact]
    public void Render_WithoutIcon_DoesNotDisplayIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .Add(p => p.Title, "Info"));

        // Assert
        Assert.Empty(cut.FindAll("i"));
    }

    [Fact]
    public void Render_WithChildContent_DisplaysContent()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .AddChildContent("<p>Test content</p>"));

        // Assert
        var content = cut.Find(".info-box-content");
        Assert.Contains("Test content", content.InnerHtml);
    }

    [Fact]
    public void Render_WithList_DisplaysList()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .Add(p => p.Title, "Prerequisites")
            .AddChildContent("<ul><li>Item 1</li><li>Item 2</li></ul>"));

        // Assert
        var list = cut.Find("ul");
        Assert.NotNull(list);
        Assert.Equal(2, cut.FindAll("li").Count);
    }

    [Fact]
    public void Render_TitleAndIcon_DisplaysInCorrectOrder()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .Add(p => p.Title, "Info Title")
            .Add(p => p.Icon, "check-circle"));

        // Assert
        var heading = cut.Find(".alert-heading");
        var icon = heading.QuerySelector("i");
        Assert.NotNull(icon);
        Assert.Contains("bi-check-circle", icon.ClassName);
    }

    [Fact]
    public void Render_WithComplexContent_PreservesStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .AddChildContent("<div><p>Paragraph 1</p><p>Paragraph 2</p></div>"));

        // Assert
        Assert.Equal(2, cut.FindAll("p").Count);
    }

    [Fact]
    public void Render_InfoBoxContent_HasCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<InfoBox>(parameters => parameters
            .AddChildContent("<span>Test</span>"));

        // Assert
        var content = cut.Find(".info-box-content");
        Assert.NotNull(content);
    }
}
