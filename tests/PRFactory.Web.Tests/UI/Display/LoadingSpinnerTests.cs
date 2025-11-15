using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for LoadingSpinner component
/// </summary>
public class LoadingSpinnerTests : TestContext
{
    [Fact]
    public void Render_WhenIsLoadingTrue_ShowsSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WhenIsLoadingFalse_DoesNotShowSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.DoesNotContain("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_IsLoading()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange
        var message = "Loading data...";

        // Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains(message, cut.Markup);
    }

    [Fact]
    public void Render_WhenCentered_AppliesTextCenterClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Centered, true));

        // Assert
        Assert.Contains("text-center", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotCentered_DoesNotApplyTextCenterClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Centered, false));

        // Assert
        Assert.DoesNotContain("text-center", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_IsCentered()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("text-center", cut.Markup);
    }

    [Theory]
    [InlineData(SpinnerColor.Primary, "text-primary")]
    [InlineData(SpinnerColor.Secondary, "text-secondary")]
    [InlineData(SpinnerColor.Success, "text-success")]
    [InlineData(SpinnerColor.Danger, "text-danger")]
    [InlineData(SpinnerColor.Warning, "text-warning")]
    [InlineData(SpinnerColor.Info, "text-info")]
    public void Render_WithColor_AppliesCorrectClass(SpinnerColor color, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Color, color));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Theory]
    [InlineData(SpinnerSize.Small, "width: 1rem; height: 1rem;")]
    [InlineData(SpinnerSize.Normal, "spinner-border")]
    [InlineData(SpinnerSize.Large, "width: 3rem; height: 3rem;")]
    public void Render_WithSize_AppliesCorrectStyle(SpinnerSize size, string expectedContent)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedContent, cut.Markup);
    }

    [Fact]
    public void Render_HasLoadingTextInVisuallyHidden()
    {
        // Arrange
        var loadingText = "Please wait";

        // Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, loadingText));

        // Assert
        Assert.Contains("visually-hidden", cut.Markup);
        Assert.Contains(loadingText, cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_HasLoadingTextAsLoadingDotDotDot()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("Loading...", cut.Markup);
    }
}
