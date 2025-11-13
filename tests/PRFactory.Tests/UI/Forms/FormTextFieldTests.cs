using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormTextFieldTests : ComponentTestBase
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        var cut = RenderComponent<FormTextField>(p => p.Add(x => x.Label, "Name"));
        Assert.Contains("Name", cut.Markup);
    }

    [Fact]
    public void Render_WithRequired_ShowsAsterisk()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.Required, true));
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithPlaceholder_SetsPlaceholder()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Name")
            .Add(x => x.Placeholder, "Enter name"));
        Assert.Contains("placeholder=\"Enter name\"", cut.Markup);
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Name")
            .Add(x => x.HelpText, "Your full name"));
        Assert.Contains("Your full name", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabled_DisablesInput()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Name")
            .Add(x => x.Disabled, true));
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void Render_WithIsInvalid_AddsInvalidClass()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Name")
            .Add(x => x.IsInvalid, true));
        Assert.Contains("is-invalid", cut.Markup);
    }

    [Fact]
    public void Render_DefaultInputType_IsText()
    {
        var cut = RenderComponent<FormTextField>(p => p.Add(x => x.Label, "Name"));
        Assert.Contains("type=\"text\"", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomInputType_SetsType()
    {
        var cut = RenderComponent<FormTextField>(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.InputType, "email"));
        Assert.Contains("type=\"email\"", cut.Markup);
    }
}
