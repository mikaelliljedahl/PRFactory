using Bunit;
using Xunit;
using PRFactory.Web.UI.Dialogs;
using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Tests.UI.Dialogs;

/// <summary>
/// Tests for Modal component
/// </summary>
public class ModalTests : TestContext
{
    [Fact]
    public void Modal_WhenVisible_RendersModal()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("modal fade show", cut.Markup);
        Assert.Contains("Test Modal", cut.Markup);
        Assert.Contains("Test message", cut.Markup);
    }

    [Fact]
    public void Modal_WhenNotVisible_DoesNotRender()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, false));

        // Assert
        Assert.DoesNotContain("modal fade show", cut.Markup);
    }

    [Fact]
    public void Modal_ShowsTitleWithIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Alert")
            .Add(p => p.Icon, "exclamation-triangle"));

        // Assert
        Assert.Contains("Alert", cut.Markup);
        Assert.Contains("bi-exclamation-triangle", cut.Markup);
    }

    [Fact]
    public void Modal_RendersBodyContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.BodyContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Custom body content");
                builder.CloseElement();
            }));

        // Assert
        Assert.Contains("Custom body content", cut.Markup);
    }

    [Fact]
    public void Modal_RendersFooterContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.FooterContent, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddContent(1, "Custom Button");
                builder.CloseElement();
            }));

        // Assert
        Assert.Contains("Custom Button", cut.Markup);
    }

    [Fact]
    public void Modal_ShowsCloseButtonWhenEnabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowCloseButton, true));

        // Assert
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void Modal_OnCloseCallbackInvokedWhenClosed()
    {
        // Arrange
        var closeInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowCloseButton, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeInvoked = true)));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.True(closeInvoked);
    }

    [Fact]
    public void Modal_AppliesSmallSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Size, ModalSize.Small));

        // Assert
        Assert.Contains("modal-sm", cut.Markup);
    }

    [Fact]
    public void Modal_AppliesLargeSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Size, ModalSize.Large));

        // Assert
        Assert.Contains("modal-lg", cut.Markup);
    }

    [Fact]
    public void Modal_AppliesExtraLargeSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Size, ModalSize.ExtraLarge));

        // Assert
        Assert.Contains("modal-xl", cut.Markup);
    }

    [Fact]
    public void Modal_AppliesPrimaryVariantClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, ModalVariant.Primary));

        // Assert
        Assert.Contains("bg-primary", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Modal_AppliesDangerVariantClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, ModalVariant.Danger));

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Modal_CentersModalWhenCenteredTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Centered, true));

        // Assert
        Assert.Contains("modal-dialog-centered", cut.Markup);
    }

    [Fact]
    public void Modal_ShowsDefaultButtonsWhenEnabled()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.ConfirmButtonText, "Yes")
            .Add(p => p.CancelButtonText, "No"));

        // Assert
        Assert.Contains("Yes", cut.Markup);
        Assert.Contains("No", cut.Markup);
    }

    [Fact]
    public void Modal_ConfirmButtonInvokesOnConfirmAndCloses()
    {
        // Arrange
        var confirmInvoked = false;
        var closeInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => confirmInvoked = true))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeInvoked = true)));

        // Act
        var confirmButton = cut.Find(".btn-primary");
        confirmButton.Click();

        // Assert
        Assert.True(confirmInvoked);
        Assert.True(closeInvoked);
    }

    [Fact]
    public void Modal_CancelButtonInvokesOnCancelAndCloses()
    {
        // Arrange
        var cancelInvoked = false;
        var closeInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeInvoked = true)));

        // Act
        var cancelButton = cut.Find(".btn-secondary");
        cancelButton.Click();

        // Assert
        Assert.True(cancelInvoked);
        Assert.True(closeInvoked);
    }

    [Fact]
    public void Modal_RendersModalBackdrop()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Contains("modal-backdrop fade show", cut.Markup);
    }

    [Fact]
    public void Modal_HidesCancelButtonWhenShowCancelButtonFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.ShowCancelButton, false)
            .Add(p => p.ConfirmButtonText, "OK")
            .Add(p => p.CancelButtonText, "Cancel"));

        // Assert
        Assert.Contains("OK", cut.Markup);
        Assert.DoesNotContain("Cancel", cut.Markup);
    }
}
