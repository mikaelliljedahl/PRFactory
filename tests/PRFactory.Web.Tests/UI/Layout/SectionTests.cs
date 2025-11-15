using Bunit;
using PRFactory.Web.UI.Layout;
using Xunit;

namespace PRFactory.Web.Tests.UI.Layout;

public class SectionTests : TestContext
{
    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test Section"));

        // Assert
        var title = cut.Find(".section-title");
        Assert.Equal("Test Section", title.TextContent.Trim());
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Icon, "gear"));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("bi-gear", icon.ClassName);
    }

    [Fact]
    public void Render_WithoutTitle_DoesNotDisplayHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Empty(cut.FindAll(".section-header"));
    }

    [Fact]
    public void Render_WithChildContent_DisplaysContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .AddChildContent("<p>Test content</p>"));

        // Assert
        var content = cut.Find(".section-content");
        Assert.Contains("Test content", content.InnerHtml);
    }

    [Fact]
    public void Render_Collapsible_DisplaysChevronIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, true));

        // Assert
        var chevron = cut.Find("i.bi-chevron-up, i.bi-chevron-down");
        Assert.NotNull(chevron);
    }

    [Fact]
    public void Render_NotCollapsible_DoesNotDisplayChevronIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, false));

        // Assert
        Assert.Empty(cut.FindAll("i.bi-chevron-up"));
        Assert.Empty(cut.FindAll("i.bi-chevron-down"));
    }

    [Fact]
    public void Render_InitiallyCollapsed_HidesContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, true)
            .Add(p => p.InitiallyCollapsed, true)
            .AddChildContent("<p>Hidden content</p>"));

        // Assert
        Assert.Empty(cut.FindAll(".section-content"));
    }

    [Fact]
    public void Render_InitiallyNotCollapsed_ShowsContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, true)
            .Add(p => p.InitiallyCollapsed, false)
            .AddChildContent("<p>Visible content</p>"));

        // Assert
        var content = cut.Find(".section-content");
        Assert.NotNull(content);
    }

    [Fact]
    public void Click_OnCollapsibleHeader_TogglesContent()
    {
        // Arrange
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, true)
            .AddChildContent("<p>Content</p>"));

        var header = cut.Find(".section-header");

        // Act - Click to collapse
        header.Click();
        cut.Render();

        // Assert - Content should be hidden
        Assert.Empty(cut.FindAll(".section-content"));

        // Act - Click to expand
        header.Click();
        cut.Render();

        // Assert - Content should be visible
        Assert.NotEmpty(cut.FindAll(".section-content"));
    }

    [Fact]
    public void Click_OnNonCollapsibleHeader_DoesNothing()
    {
        // Arrange
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, false)
            .AddChildContent("<p>Content</p>"));

        var header = cut.Find(".section-header");

        // Act
        header.Click();
        cut.Render();

        // Assert - Content should still be visible
        var content = cut.Find(".section-content");
        Assert.NotNull(content);
    }

    [Fact]
    public void Render_Collapsible_HasCollapsibleClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, true));

        // Assert
        var section = cut.Find(".section");
        Assert.Contains("section-collapsible", section.ClassName);
    }

    [Fact]
    public void Render_NotCollapsible_DoesNotHaveCollapsibleClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Section>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Collapsible, false));

        // Assert
        var section = cut.Find(".section");
        Assert.DoesNotContain("section-collapsible", section.ClassName);
    }
}
