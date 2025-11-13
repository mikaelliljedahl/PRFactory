using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Help;
using Xunit;

namespace PRFactory.Tests.UI.Help;

public class ContextualHelpTests : ComponentTestBase
{
    [Fact]
    public void Render_WithHelpText_DisplaysIcon()
    {
        var cut = RenderComponent<ContextualHelp>(p => p.Add(x => x.HelpText, "This is help text"));
        Assert.Contains("bi-question-circle-fill", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        var cut = RenderComponent<ContextualHelp>(p => p
            .Add(x => x.HelpText, "Help text")
            .Add(x => x.Title, "Important Info"));
        Assert.Contains("Important Info", cut.Markup);
    }

    [Fact]
    public void Render_WithLearnMoreUrl_DisplaysLink()
    {
        var cut = RenderComponent<ContextualHelp>(p => p
            .Add(x => x.HelpText, "Help text")
            .Add(x => x.LearnMoreUrl, "https://example.com"));
        Assert.Contains("Learn More", cut.Markup);
        Assert.Contains("https://example.com", cut.Markup);
    }

    [Fact]
    public void Render_DefaultPosition_IsTop()
    {
        var cut = RenderComponent<ContextualHelp>(p => p.Add(x => x.HelpText, "Help"));
        Assert.Contains("contextual-help-tooltip-top", cut.Markup);
    }

    [Fact]
    public void Render_DefaultIconSize_IsMedium()
    {
        var cut = RenderComponent<ContextualHelp>(p => p.Add(x => x.HelpText, "Help"));
        Assert.Contains("contextual-help-icon-medium", cut.Markup);
    }
}
