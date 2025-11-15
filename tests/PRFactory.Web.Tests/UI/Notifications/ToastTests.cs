using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.UI.Notifications;
using PRFactory.Web.Services;

namespace PRFactory.Web.Tests.UI.Notifications;

/// <summary>
/// Tests for Toast component
/// </summary>
public class ToastTests : TestContext
{
    [Fact]
    public void Toast_RendersToastWithMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Success")
            .Add(p => p.Message, "Operation completed")
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("Success", cut.Markup);
        Assert.Contains("Operation completed", cut.Markup);
        Assert.Contains("toast show", cut.Markup);
    }

    [Fact]
    public void Toast_AppliesSuccessTypeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Success")
            .Add(p => p.Type, ToastType.Success)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("bg-success", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Toast_AppliesErrorTypeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Error")
            .Add(p => p.Type, ToastType.Error)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Toast_AppliesWarningTypeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Warning")
            .Add(p => p.Type, ToastType.Warning)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("bg-warning", cut.Markup);
    }

    [Fact]
    public void Toast_AppliesInfoTypeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Info")
            .Add(p => p.Type, ToastType.Info)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("bg-info", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Toast_ShowsIconWhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Success")
            .Add(p => p.Icon, "check-circle")
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void Toast_ShowsCloseButton()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Dismissible, true)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void Toast_OnDismissCallbackInvokedWhenDismissed()
    {
        // Arrange
        var dismissInvoked = false;
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Dismissible, true)
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, () => dismissInvoked = true)));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.True(dismissInvoked);
    }

    [Fact]
    public void Toast_DoesNotRenderWhenNotVisible()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.IsVisible, false));

        // Assert
        Assert.DoesNotContain("toast show", cut.Markup);
    }

    [Fact]
    public void Toast_ShowsTimestampWhenEnabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowTimestamp, true)
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("just now", cut.Markup);
    }

    [Fact]
    public void Toast_RendersChildContentInBody()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.IsVisible, true)
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Custom content");
                builder.CloseElement();
            }));

        // Assert
        Assert.Contains("Custom content", cut.Markup);
    }
}
