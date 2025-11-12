using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Notifications;
using Xunit;

namespace PRFactory.Tests.UI.Notifications;

public class ToastContainerTests : ComponentTestBase
{
    [Fact]
    public void Render_DisplaysContainer()
    {
        var cut = RenderComponent<ToastContainer>();
        Assert.Contains("toast-container", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectPositioning()
    {
        var cut = RenderComponent<ToastContainer>();
        Assert.Contains("position-fixed", cut.Markup);
        Assert.Contains("top-0", cut.Markup);
        Assert.Contains("end-0", cut.Markup);
    }
}
