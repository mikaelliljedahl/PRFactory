using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormSelectFieldTests : ComponentTestBase
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        var cut = RenderComponent<FormSelectField<string>>(p => p.Add(x => x.Label, "Country"));
        Assert.Contains("Country", cut.Markup);
        Assert.Contains("<select", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultOptionText_ShowsDefaultOption()
    {
        var cut = RenderComponent<FormSelectField<string>>(p => p
            .Add(x => x.Label, "Country")
            .Add(x => x.DefaultOptionText, "Select a country"));
        Assert.Contains("Select a country", cut.Markup);
        Assert.Contains("<option value=\"\">", cut.Markup);
    }

    [Fact]
    public void Render_WithRequired_ShowsAsterisk()
    {
        var cut = RenderComponent<FormSelectField<string>>(p => p
            .Add(x => x.Label, "Country")
            .Add(x => x.Required, true));
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabled_DisablesSelect()
    {
        var cut = RenderComponent<FormSelectField<string>>(p => p
            .Add(x => x.Label, "Country")
            .Add(x => x.Disabled, true));
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void Render_HasFormSelectClass()
    {
        var cut = RenderComponent<FormSelectField<string>>(p => p.Add(x => x.Label, "Country"));
        Assert.Contains("form-select", cut.Markup);
    }
}
