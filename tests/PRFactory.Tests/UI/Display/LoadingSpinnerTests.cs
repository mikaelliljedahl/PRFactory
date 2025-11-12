using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class LoadingSpinnerTests : ComponentTestBase
{
    [Fact]
    public void Render_WhenIsLoadingTrue_DisplaysSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WhenIsLoadingFalse_RendersNothing()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_DefaultIsLoading_IsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, "Please wait..."));

        // Assert
        Assert.Contains("Please wait...", cut.Markup);
    }

    [Fact]
    public void Render_WithoutMessage_DoesNotDisplayMessageElement()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        var paragraphs = cut.FindAll("p");
        Assert.Empty(paragraphs);
    }

    [Theory]
    [InlineData(SpinnerSize.Small, "width: 1rem; height: 1rem;")]
    [InlineData(SpinnerSize.Large, "width: 3rem; height: 3rem;")]
    public void Render_WithSize_AppliesCorrectStyle(SpinnerSize size, string expectedStyle)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedStyle, cut.Markup);
    }

    [Fact]
    public void Render_WithNormalSize_DoesNotApplyStyleAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Size, SpinnerSize.Normal));

        // Assert
        var spinnerDiv = cut.Find(".spinner-border");
        var styleAttr = spinnerDiv.GetAttribute("style");
        Assert.DoesNotContain("width:", styleAttr ?? "");
        Assert.DoesNotContain("height:", styleAttr ?? "");
    }

    [Theory]
    [InlineData(SpinnerColor.Primary, "text-primary")]
    [InlineData(SpinnerColor.Secondary, "text-secondary")]
    [InlineData(SpinnerColor.Success, "text-success")]
    [InlineData(SpinnerColor.Danger, "text-danger")]
    [InlineData(SpinnerColor.Warning, "text-warning")]
    [InlineData(SpinnerColor.Info, "text-info")]
    public void Render_WithColor_AppliesCorrectColorClass(SpinnerColor color, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Color, color));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_DefaultColor_IsPrimary()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("text-primary", cut.Markup);
    }

    [Fact]
    public void Render_CenteredByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("text-center", cut.Markup);
        Assert.Contains("py-5", cut.Markup);
    }

    [Fact]
    public void Render_WithCenteredFalse_UsesInlineBlock()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Centered, false));

        // Assert
        Assert.Contains("d-inline-block", cut.Markup);
        Assert.DoesNotContain("text-center", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("role=\"status\"", cut.Markup);
        Assert.Contains("visually-hidden", cut.Markup);
    }

    [Fact]
    public void Render_DefaultLoadingText_IsLoading()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("Loading...", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomLoadingText_DisplaysCustomText()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Processing..."));

        // Assert
        Assert.Contains("Processing...", cut.Markup);
        Assert.DoesNotContain("Loading...", cut.Markup);
    }

    [Fact]
    public void Render_MessageWithCentered_HasCorrectMarginTop()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, "Please wait")
            .Add(p => p.Centered, true));

        // Assert
        Assert.Contains("mt-3", cut.Markup);
    }

    [Fact]
    public void Render_MessageWithoutCentered_HasCorrectMarginStart()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, "Please wait")
            .Add(p => p.Centered, false));

        // Assert
        Assert.Contains("ms-2", cut.Markup);
    }

    [Fact]
    public void Render_MessageAlwaysHasTextMuted()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, "Please wait"));

        // Assert
        Assert.Contains("text-muted", cut.Markup);
    }
}
