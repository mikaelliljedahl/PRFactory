using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormPasswordFieldTests : ComponentTestBase
{
    [Fact]
    public void Render_InitialType_IsPassword()
    {
        var cut = RenderComponent<FormPasswordField>(p => p.Add(x => x.Label, "Password"));
        Assert.Contains("type=\"password\"", cut.Markup);
    }

    [Fact]
    public void Render_HasToggleVisibilityButton()
    {
        var cut = RenderComponent<FormPasswordField>(p => p.Add(x => x.Label, "Password"));
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void ToggleButton_WhenClicked_ShowsPassword()
    {
        var cut = RenderComponent<FormPasswordField>(p => p.Add(x => x.Label, "Password"));
        var button = cut.Find("button");
        button.Click();
        Assert.Contains("type=\"text\"", cut.Markup);
        Assert.Contains("bi-eye-slash", cut.Markup);
    }

    [Fact]
    public void Render_WithShowStrengthIndicatorFalse_HidesStrength()
    {
        var cut = RenderComponent<FormPasswordField>(p => p
            .Add(x => x.Label, "Password")
            .Add(x => x.Value, "Test123!")
            .Add(x => x.ShowStrengthIndicator, false));
        Assert.DoesNotContain("Password Strength", cut.Markup);
    }

    [Fact]
    public void Render_WithShowStrengthIndicatorTrueAndValue_ShowsStrength()
    {
        var cut = RenderComponent<FormPasswordField>(p => p
            .Add(x => x.Label, "Password")
            .Add(x => x.Value, "Test123!")
            .Add(x => x.ShowStrengthIndicator, true));
        Assert.Contains("Password Strength", cut.Markup);
    }

    [Theory]
    [InlineData("test", "Weak")]
    [InlineData("Test123", "Good")]
    [InlineData("Test123!", "Strong")]
    public void PasswordStrength_CalculatesCorrectly(string password, string expectedText)
    {
        var cut = RenderComponent<FormPasswordField>(p => p
            .Add(x => x.Label, "Password")
            .Add(x => x.Value, password)
            .Add(x => x.ShowStrengthIndicator, true));
        Assert.Contains(expectedText, cut.Markup);
    }
}
