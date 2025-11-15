using Bunit;
using Xunit;
using PRFactory.Web.UI.Alerts;

namespace PRFactory.Web.Tests.UI.Alerts;

/// <summary>
/// Tests for DemoModeBanner component
/// </summary>
public class DemoModeBannerTests : TestContext
{
    [Fact]
    public void Render_WhenShowBannerTrue_DisplaysBanner()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("Demo Mode", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowBannerFalse_DoesNotDisplayBanner()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, false));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_DisplaysDemoModeMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("Demo Mode", cut.Markup);
        Assert.Contains("sample data", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysDefaultMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("This is not production data", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalMessage_DisplaysAdditionalMessage()
    {
        // Arrange
        var additionalMessage = "Features are limited in demo mode.";

        // Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true)
            .Add(p => p.AdditionalMessage, additionalMessage));

        // Assert
        Assert.Contains(additionalMessage, cut.Markup);
        Assert.DoesNotContain("This is not production data", cut.Markup);
    }

    [Fact]
    public void Render_HasInfoAlertClass()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("alert-info", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("bi-info-circle", cut.Markup);
    }

    [Fact]
    public void Render_IsDismissible()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("alert-dismissible", cut.Markup);
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void DismissButton_Click_HidesBanner()
    {
        // Arrange
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Act
        var dismissButton = cut.Find(".btn-close");
        dismissButton.Click();

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_HasDemoModeBannerClass()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("demo-mode-banner", cut.Markup);
    }

    [Fact]
    public void Render_UsesContainerFluid()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("container-fluid", cut.Markup);
    }
}
