using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class ProgressBarTests : ComponentTestBase
{
    [Fact]
    public void Render_WithValue_DisplaysProgressBar()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50));

        // Assert
        Assert.Contains("progress", cut.Markup);
        Assert.Contains("progress-bar", cut.Markup);
        Assert.Contains("width: 50%", cut.Markup);
    }

    [Fact]
    public void Render_DefaultValue_IsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>();

        // Assert
        Assert.Contains("width: 0%", cut.Markup);
    }

    [Fact]
    public void Render_WithShowLabelTrue_DisplaysPercentageLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 75)
            .Add(p => p.ShowLabel, true));

        // Assert
        Assert.Contains("75%", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomLabel_DisplaysCustomLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Label, "50 of 100"));

        // Assert
        Assert.Contains("50 of 100", cut.Markup);
    }

    [Fact]
    public void Render_ShowLabelTakesPrecedenceOverCustomLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 60)
            .Add(p => p.ShowLabel, true)
            .Add(p => p.Label, "Custom"));

        // Assert
        Assert.Contains("60%", cut.Markup);
        Assert.DoesNotContain("Custom", cut.Markup);
    }

    [Theory]
    [InlineData(ProgressVariant.Primary, "bg-primary")]
    [InlineData(ProgressVariant.Success, "bg-success")]
    [InlineData(ProgressVariant.Info, "bg-info")]
    [InlineData(ProgressVariant.Warning, "bg-warning")]
    [InlineData(ProgressVariant.Danger, "bg-danger")]
    [InlineData(ProgressVariant.Secondary, "bg-secondary")]
    public void Render_WithVariant_AppliesCorrectColorClass(ProgressVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Variant, variant));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_DefaultVariant_IsPrimary()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50));

        // Assert
        Assert.Contains("bg-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithStripedTrue_AddsStripedClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Striped, true));

        // Assert
        Assert.Contains("progress-bar-striped", cut.Markup);
    }

    [Fact]
    public void Render_WithAnimatedTrue_AddsAnimatedClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.Animated, true));

        // Assert
        Assert.Contains("progress-bar-animated", cut.Markup);
    }

    [Fact]
    public void Render_WithAnimatedTrue_AlsoAddsStripedClass()
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
    public void Render_WithCustomHeight_AppliesHeightStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.HeightPx, "30px"));

        // Assert
        Assert.Contains("height: 30px", cut.Markup);
    }

    [Fact]
    public void Render_DefaultHeight_Is20px()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50));

        // Assert
        Assert.Contains("height: 20px", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50)
            .Add(p => p.AdditionalClass, "custom-progress"));

        // Assert
        Assert.Contains("custom-progress", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 75));

        // Assert
        Assert.Contains("role=\"progressbar\"", cut.Markup);
        Assert.Contains("aria-valuenow=\"75\"", cut.Markup);
        Assert.Contains("aria-valuemin=\"0\"", cut.Markup);
        Assert.Contains("aria-valuemax=\"100\"", cut.Markup);
    }

    [Fact]
    public void Render_WithZeroValue_DisplaysEmptyBar()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 0));

        // Assert
        Assert.Contains("width: 0%", cut.Markup);
    }

    [Fact]
    public void Render_WithHundredValue_DisplaysFullBar()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 100));

        // Assert
        Assert.Contains("width: 100%", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_AppliesAllStyles()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 75)
            .Add(p => p.ShowLabel, true)
            .Add(p => p.Variant, ProgressVariant.Success)
            .Add(p => p.Striped, true)
            .Add(p => p.Animated, true)
            .Add(p => p.HeightPx, "25px")
            .Add(p => p.AdditionalClass, "mt-3"));

        // Assert
        Assert.Contains("75%", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
        Assert.Contains("progress-bar-striped", cut.Markup);
        Assert.Contains("progress-bar-animated", cut.Markup);
        Assert.Contains("height: 25px", cut.Markup);
        Assert.Contains("mt-3", cut.Markup);
    }
}
