using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Navigation;
using Xunit;

namespace PRFactory.Tests.UI.Navigation;

public class BreadcrumbsTests : ComponentTestBase
{
    [Fact]
    public void Render_WithItems_DisplaysBreadcrumbs()
    {
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Href = "/" },
            new BreadcrumbItem { Text = "Tickets", Href = "/tickets" },
            new BreadcrumbItem { Text = "Detail" }
        };

        var cut = RenderComponent<Breadcrumbs>(p => p.Add(x => x.Items, items));
        Assert.Contains("Home", cut.Markup);
        Assert.Contains("Tickets", cut.Markup);
        Assert.Contains("Detail", cut.Markup);
    }

    [Fact]
    public void Render_LastItem_IsActive()
    {
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Href = "/" },
            new BreadcrumbItem { Text = "Current" }
        };

        var cut = RenderComponent<Breadcrumbs>(p => p.Add(x => x.Items, items));
        Assert.Contains("breadcrumb-item active", cut.Markup);
        Assert.Contains("aria-current=\"page\"", cut.Markup);
    }

    [Fact]
    public void Render_WithIcons_DisplaysIcons()
    {
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Href = "/", Icon = "house" },
            new BreadcrumbItem { Text = "Settings", Icon = "gear" }
        };

        var cut = RenderComponent<Breadcrumbs>(p => p.Add(x => x.Items, items));
        Assert.Contains("bi-house", cut.Markup);
        Assert.Contains("bi-gear", cut.Markup);
    }

    [Fact]
    public void Render_HasBreadcrumbClass()
    {
        var items = new List<BreadcrumbItem> { new BreadcrumbItem { Text = "Home" } };
        var cut = RenderComponent<Breadcrumbs>(p => p.Add(x => x.Items, items));
        Assert.Contains("class=\"breadcrumb", cut.Markup);
    }
}
