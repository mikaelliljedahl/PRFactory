using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormTextAreaFieldTests : ComponentTestBase
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        var cut = RenderComponent<FormTextAreaField>(p => p.Add(x => x.Label, "Description"));
        Assert.Contains("Description", cut.Markup);
        Assert.Contains("<textarea", cut.Markup);
    }

    [Fact]
    public void Render_DefaultRows_Is3()
    {
        var cut = RenderComponent<FormTextAreaField>(p => p.Add(x => x.Label, "Notes"));
        Assert.Contains("rows=\"3\"", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomRows_SetsRows()
    {
        var cut = RenderComponent<FormTextAreaField>(p => p
            .Add(x => x.Label, "Notes")
            .Add(x => x.Rows, 10));
        Assert.Contains("rows=\"10\"", cut.Markup);
    }

    [Fact]
    public void Render_WithRequired_ShowsAsterisk()
    {
        var cut = RenderComponent<FormTextAreaField>(p => p
            .Add(x => x.Label, "Notes")
            .Add(x => x.Required, true));
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabled_DisablesTextarea()
    {
        var cut = RenderComponent<FormTextAreaField>(p => p
            .Add(x => x.Label, "Notes")
            .Add(x => x.Disabled, true));
        Assert.Contains("disabled", cut.Markup);
    }
}
