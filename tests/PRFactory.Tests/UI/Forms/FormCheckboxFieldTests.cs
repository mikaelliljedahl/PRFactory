using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormCheckboxFieldTests : ComponentTestBase
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        var cut = RenderComponent<FormCheckboxField>(p => p.Add(x => x.Label, "I agree"));
        Assert.Contains("I agree", cut.Markup);
        Assert.Contains("type=\"checkbox\"", cut.Markup);
    }

    [Fact]
    public void Render_WithValueTrue_IsChecked()
    {
        var cut = RenderComponent<FormCheckboxField>(p => p
            .Add(x => x.Label, "Enabled")
            .Add(x => x.Value, true));
        Assert.Contains("checked", cut.Markup);
    }

    [Fact]
    public void Render_WithRequired_ShowsAsterisk()
    {
        var cut = RenderComponent<FormCheckboxField>(p => p
            .Add(x => x.Label, "Accept terms")
            .Add(x => x.Required, true));
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabled_DisablesCheckbox()
    {
        var cut = RenderComponent<FormCheckboxField>(p => p
            .Add(x => x.Label, "Enabled")
            .Add(x => x.Disabled, true));
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void Render_HasFormCheckClass()
    {
        var cut = RenderComponent<FormCheckboxField>(p => p.Add(x => x.Label, "Test"));
        Assert.Contains("form-check", cut.Markup);
    }
}
