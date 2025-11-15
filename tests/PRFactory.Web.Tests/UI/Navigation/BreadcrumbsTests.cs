using Bunit;
using Xunit;
using PRFactory.Web.UI.Navigation;

namespace PRFactory.Web.Tests.UI.Navigation;

/// <summary>
/// Tests for Breadcrumbs component
/// </summary>
public class BreadcrumbsTests : TestContext
{
    private List<BreadcrumbItem> CreateTestBreadcrumbs()
    {
        return new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Href = "/", Icon = "house" },
            new BreadcrumbItem { Text = "Projects", Href = "/projects" },
            new BreadcrumbItem { Text = "Current Project" }
        };
    }

    [Fact]
    public void Render_WithItems_DisplaysBreadcrumbs()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        Assert.Contains("Home", cut.Markup);
        Assert.Contains("Projects", cut.Markup);
        Assert.Contains("Current Project", cut.Markup);
    }

    [Fact]
    public void Render_WithIcons_DisplaysIcons()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        Assert.Contains("bi-house", cut.Markup);
    }

    [Fact]
    public void Render_LastItem_IsActive()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var activeBreadcrumb = cut.FindAll(".breadcrumb-item.active");
        Assert.Single(activeBreadcrumb);
        Assert.Contains("Current Project", activeBreadcrumb[0].TextContent);
    }

    [Fact]
    public void Render_LastItem_HasAriaCurrentPage()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var activeItem = cut.Find(".breadcrumb-item.active");
        Assert.Equal("page", activeItem.GetAttribute("aria-current"));
    }

    [Fact]
    public void Render_LastItem_IsNotLink()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var lastItem = cut.FindAll(".breadcrumb-item").Last();
        var links = lastItem.QuerySelectorAll("a");
        Assert.Empty(links);
    }

    [Fact]
    public void Render_NonLastItems_AreLinks()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var links = cut.FindAll("a");
        Assert.Equal(2, links.Count); // First two items have links
        Assert.Equal("/", links[0].GetAttribute("href"));
        Assert.Equal("/projects", links[1].GetAttribute("href"));
    }

    [Fact]
    public void Render_ItemWithoutHref_DoesNotRenderAsLink()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home" },
            new BreadcrumbItem { Text = "Current" }
        };

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var nonActiveItems = cut.FindAll(".breadcrumb-item:not(.active)");
        var firstItem = nonActiveItems[0];
        var links = firstItem.QuerySelectorAll("a");
        Assert.Empty(links);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();
        var additionalClass = "my-custom-breadcrumbs";

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var breadcrumbList = cut.Find(".breadcrumb");
        Assert.Contains(additionalClass, breadcrumbList.ClassName);
    }

    [Fact]
    public void Render_HasBreadcrumbClass()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var breadcrumbList = cut.Find(".breadcrumb");
        Assert.NotNull(breadcrumbList);
    }

    [Fact]
    public void Render_HasAriaBreadcrumbLabel()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var nav = cut.Find("nav");
        Assert.Equal("breadcrumb", nav.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_WithEmptyItems_ShowsEmptyBreadcrumbs()
    {
        // Arrange
        var items = new List<BreadcrumbItem>();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var breadcrumbItems = cut.FindAll(".breadcrumb-item");
        Assert.Empty(breadcrumbItems);
    }

    [Fact]
    public void Render_AllItems_AreBreadcrumbItems()
    {
        // Arrange
        var items = CreateTestBreadcrumbs();

        // Act
        var cut = RenderComponent<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var breadcrumbItems = cut.FindAll(".breadcrumb-item");
        Assert.Equal(items.Count, breadcrumbItems.Count);
    }
}
