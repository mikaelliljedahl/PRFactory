using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for ProgressBar component
/// </summary>
public class ProgressBarTests : TestContext
{
    [Fact]
    public void Render_WithValue_DisplaysCorrectWidth()
    {
        // Arrange
        var value = 75;

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, value));

        // Assert
        Assert.Contains($"width: {value}%", cut.Markup);
    }

    [Fact]
    public void Render_WithValue_SetsAriaAttributes()
    {
        // Arrange
        var value = 60;

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, value));

        // Assert
        var progressBar = cut.Find(".progress-bar");
        Assert.Equal(value.ToString(), progressBar.GetAttribute("aria-valuenow"));
        Assert.Equal("0", progressBar.GetAttribute("aria-valuemin"));
        Assert.Equal("100", progressBar.GetAttribute("aria-valuemax"));
    }

    [Fact]
    public void Render_WhenShowLabelTrue_DisplaysPercentage()
    {
        // Arrange
        var value = 45;

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ShowLabel, true));

        // Assert
        Assert.Contains($"{value}%", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowLabelFalseAndNoCustomLabel_DoesNotShowLabel()
    {
        // Arrange
        var value = 45;

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ShowLabel, false));

        // Assert
        var progressBar = cut.Find(".progress-bar");
        Assert.DoesNotContain("%", progressBar.InnerHtml);
    }

    [Fact]
    public void Render_WithCustomLabel_DisplaysCustomLabel()
    {
        // Arrange
        var customLabel = "3 of 10 complete";

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 30)
            .Add(p => p.Label, customLabel));

        // Assert
        Assert.Contains(customLabel, cut.Markup);
    }

    [Theory]
    [InlineData(ProgressVariant.Primary, "bg-primary")]
    [InlineData(ProgressVariant.Success, "bg-success")]
    [InlineData(ProgressVariant.Info, "bg-info")]
    [InlineData(ProgressVariant.Warning, "bg-warning")]
    [InlineData(ProgressVariant.Danger, "bg-danger")]
    [InlineData(ProgressVariant.Secondary, "bg-secondary")]
    public void Render_WithVariant_AppliesCorrectClass(ProgressVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Variant, variant));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WhenStripedTrue_AppliesStripedClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Striped, true));

        // Assert
        Assert.Contains("progress-bar-striped", cut.Markup);
    }

    [Fact]
    public void Render_WhenAnimatedTrue_AppliesStripedAndAnimatedClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Animated, true));

        // Assert
        Assert.Contains("progress-bar-striped", cut.Markup);
        Assert.Contains("progress-bar-animated", cut.Markup);
    }

    [Fact]
    public void Render_WhenAnimatedFalseAndStripedFalse_DoesNotApplyStripedClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Striped, false)
            .Add(p => p.Animated, false));

        // Assert
        Assert.DoesNotContain("progress-bar-striped", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomHeight_AppliesHeightStyle()
    {
        // Arrange
        var height = "30px";

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.HeightPx, height));

        // Assert
        Assert.Contains($"height: {height}", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_Has20pxHeight()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50));

        // Assert
        Assert.Contains("height: 20px", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var additionalClass = "my-custom-progress";

        // Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var progress = cut.Find(".progress");
        Assert.Contains(additionalClass, progress.ClassName);
    }

    [Fact]
    public void Render_ByDefault_ValueIsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>();

        // Assert
        Assert.Contains("width: 0%", cut.Markup);
    }
}
