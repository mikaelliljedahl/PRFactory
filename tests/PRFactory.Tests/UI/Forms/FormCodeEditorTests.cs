using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Forms;
using Xunit;

namespace PRFactory.Tests.UI.Forms;

public class FormCodeEditorTests : ComponentTestBase
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        var cut = RenderComponent<FormCodeEditor>(p => p.Add(x => x.Label, "Code"));
        Assert.Contains("Code", cut.Markup);
        Assert.Contains("<textarea", cut.Markup);
    }

    [Fact]
    public void Render_HasCodeEditorClass()
    {
        var cut = RenderComponent<FormCodeEditor>(p => p.Add(x => x.Label, "Code"));
        Assert.Contains("code-editor", cut.Markup);
    }

    [Fact]
    public void Render_DefaultRows_Is15()
    {
        var cut = RenderComponent<FormCodeEditor>(p => p.Add(x => x.Label, "Code"));
        Assert.Contains("rows=\"15\"", cut.Markup);
    }

    [Fact]
    public void Render_SpellcheckDisabled()
    {
        var cut = RenderComponent<FormCodeEditor>(p => p.Add(x => x.Label, "Code"));
        Assert.Contains("spellcheck=\"false\"", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabled_DisablesTextarea()
    {
        var cut = RenderComponent<FormCodeEditor>(p => p
            .Add(x => x.Label, "Code")
            .Add(x => x.Disabled, true));
        Assert.Contains("disabled", cut.Markup);
    }
}
