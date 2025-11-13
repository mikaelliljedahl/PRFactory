using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Pages.Auth;
using Xunit;

namespace PRFactory.Tests.Pages.Auth;

public class LoginTests : PageTestBase
{
    [Fact]
    public void Render_DisplaysLoginForm()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        Assert.Contains("Welcome to PRFactory", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEntraIdButton()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        Assert.Contains("Microsoft", cut.Markup);
    }
}
