using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Alerts;
using Xunit;

namespace PRFactory.Tests.UI.Alerts;

public class DemoModeBannerTests : ComponentTestBase
{
    [Fact]
    public void Render_WithShowBannerFalse_RendersNothing()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, false));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithShowBannerTrue_DisplaysBanner()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("demo-mode-banner", cut.Markup);
        Assert.Contains("🎭 Demo Mode:", cut.Markup);
    }

    [Fact]
    public void Render_WithoutAdditionalMessage_DisplaysDefaultMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("This is not production data.", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalMessage_DisplaysAdditionalMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true)
            .Add(p => p.AdditionalMessage, "Custom message here"));

        // Assert
        Assert.Contains("Custom message here", cut.Markup);
        Assert.DoesNotContain("This is not production data.", cut.Markup);
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
    public void CloseButton_WhenClicked_HidesBanner()
    {
        // Arrange
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_HasInfoIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("bi-info-circle", cut.Markup);
    }

    [Fact]
    public void Render_HasContainerFluid()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("container-fluid", cut.Markup);
    }

    [Fact]
    public void Render_HasZeroBottomMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("mb-0", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectAlertRole()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("role=\"alert\"", cut.Markup);
    }

    [Fact]
    public void Render_CloseButtonHasAriaLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<DemoModeBanner>(parameters => parameters
            .Add(p => p.ShowBanner, true));

        // Assert
        Assert.Contains("aria-label=\"Close\"", cut.Markup);
    }

}
