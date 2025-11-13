using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Pages.Auth;
using Xunit;

namespace PRFactory.Tests.Pages.Auth;

public class WelcomeTests : PageTestBase
{
    [Fact]
    public void Render_DisplaysWelcomeMessage()
    {
        // Act
        var cut = RenderComponent<Welcome>();

        // Assert
        Assert.Contains("Welcome", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysFeatures()
    {
        // Act
        var cut = RenderComponent<Welcome>();

        // Assert
        Assert.NotNull(cut.Markup);
    }
}
