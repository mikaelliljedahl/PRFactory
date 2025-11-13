using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Pages.Auth;
using Xunit;

namespace PRFactory.Tests.Pages.Auth;

public class PersonalAccountNotSupportedTests : PageTestBase
{
    [Fact]
    public void Render_DisplaysErrorMessage()
    {
        // Act
        var cut = RenderComponent<PersonalAccountNotSupported>();

        // Assert
        Assert.Contains("not supported", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysWorkAccountInstructions()
    {
        // Act
        var cut = RenderComponent<PersonalAccountNotSupported>();

        // Assert
        Assert.Contains("work", cut.Markup);
    }
}
