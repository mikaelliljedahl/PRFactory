using Bunit;
using Xunit;
using PRFactory.Web.UI.Help;

namespace PRFactory.Web.Tests.UI.Help;

/// <summary>
/// Tests for ContextualHelp component
/// </summary>
public class ContextualHelpTests : TestContext
{
    [Fact]
    public void Render_WithHelpText_DisplaysHelpIcon()
    {
        // Arrange
        var helpText = "This is helpful information";

        // Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, helpText));

        // Assert
        Assert.Contains("bi-question-circle-fill", cut.Markup);
    }

    [Fact]
    public void Render_WithHelpText_HasTooltip()
    {
        // Arrange
        var helpText = "This is helpful information";

        // Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, helpText));

        // Assert
        Assert.Contains(helpText, cut.Markup);
        Assert.Contains("contextual-help-tooltip", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange
        var title = "Help Title";
        var helpText = "Help content";

        // Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, helpText)
            .Add(p => p.Title, title));

        // Assert
        Assert.Contains(title, cut.Markup);
        Assert.Contains("contextual-help-tooltip-title", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTitle_DoesNotShowTitleSection()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.DoesNotContain("contextual-help-tooltip-title", cut.Markup);
    }

    [Fact]
    public void Render_WithLearnMoreUrl_ShowsLearnMoreLink()
    {
        // Arrange
        var learnMoreUrl = "https://docs.example.com/help";
        var helpText = "Help text";

        // Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, helpText)
            .Add(p => p.LearnMoreUrl, learnMoreUrl));

        // Assert
        Assert.Contains("Learn More", cut.Markup);
        var link = cut.Find("a");
        Assert.Equal(learnMoreUrl, link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void Render_WithoutLearnMoreUrl_DoesNotShowLearnMoreLink()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.DoesNotContain("Learn More", cut.Markup);
    }

    [Theory]
    [InlineData("top", "contextual-help-tooltip-top")]
    [InlineData("bottom", "contextual-help-tooltip-bottom")]
    [InlineData("left", "contextual-help-tooltip-left")]
    [InlineData("right", "contextual-help-tooltip-right")]
    public void Render_WithPosition_AppliesPositionClass(string position, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text")
            .Add(p => p.Position, position));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_PositionIsTop()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.Contains("contextual-help-tooltip-top", cut.Markup);
    }

    [Theory]
    [InlineData("small", "contextual-help-icon-small")]
    [InlineData("medium", "contextual-help-icon-medium")]
    [InlineData("large", "contextual-help-icon-large")]
    public void Render_WithIconSize_AppliesIconSizeClass(string iconSize, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text")
            .Add(p => p.IconSize, iconSize));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_IconSizeIsMedium()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.Contains("contextual-help-icon-medium", cut.Markup);
    }

    [Fact]
    public void Render_HasTooltipBody()
    {
        // Arrange
        var helpText = "Help content";

        // Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, helpText));

        // Assert
        Assert.Contains("contextual-help-tooltip-body", cut.Markup);
        Assert.Contains(helpText, cut.Markup);
    }

    [Fact]
    public void Render_HasTooltipArrow()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.Contains("contextual-help-tooltip-arrow", cut.Markup);
    }

    [Fact]
    public void Render_HasContextualHelpWrapper()
    {
        // Arrange & Act
        var cut = RenderComponent<ContextualHelp>(parameters => parameters
            .Add(p => p.HelpText, "Help text"));

        // Assert
        Assert.Contains("contextual-help-wrapper", cut.Markup);
    }
}
