using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Dialogs;
using Xunit;

namespace PRFactory.Tests.UI.Dialogs;

public class ModalTests : ComponentTestBase
{
    [Fact]
    public void Render_WhenNotVisible_RendersNothing()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, false));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WhenVisible_DisplaysModal()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal"));

        // Assert
        Assert.Contains("modal fade show", cut.Markup);
        Assert.Contains("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "My Modal"));

        // Assert
        Assert.Contains("My Modal", cut.Markup);
        Assert.Contains("modal-title", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Settings")
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("bi-gear", cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Info")
            .Add(p => p.Message, "This is a message"));

        // Assert
        Assert.Contains("This is a message", cut.Markup);
    }

    [Fact]
    public void Render_WithBodyContent_DisplaysBodyContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Custom")
            .Add(p => p.BodyContent, builder =>
            {
                builder.AddMarkupContent(0, "<div id=\"custom-body\">Custom body</div>");
            }));

        // Assert
        Assert.Contains("custom-body", cut.Markup);
        Assert.Contains("Custom body", cut.Markup);
    }

    [Fact]
    public void Render_WithBodyContent_IgnoresMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Message, "This should be ignored")
            .Add(p => p.BodyContent, builder =>
            {
                builder.AddMarkupContent(0, "<div>Body content</div>");
            }));

        // Assert
        Assert.DoesNotContain("This should be ignored", cut.Markup);
        Assert.Contains("Body content", cut.Markup);
    }

    [Theory]
    [InlineData(ModalSize.Small, "modal-sm")]
    [InlineData(ModalSize.Large, "modal-lg")]
    [InlineData(ModalSize.ExtraLarge, "modal-xl")]
    public void Render_WithSize_AppliesCorrectSizeClass(ModalSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithMediumSize_DoesNotAddSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Size, ModalSize.Medium));

        // Assert
        Assert.DoesNotContain("modal-sm", cut.Markup);
        Assert.DoesNotContain("modal-lg", cut.Markup);
        Assert.DoesNotContain("modal-xl", cut.Markup);
    }

    [Theory]
    [InlineData(ModalVariant.Primary, "bg-primary text-white")]
    [InlineData(ModalVariant.Success, "bg-success text-white")]
    [InlineData(ModalVariant.Danger, "bg-danger text-white")]
    [InlineData(ModalVariant.Warning, "bg-warning text-dark")]
    [InlineData(ModalVariant.Info, "bg-info text-white")]
    public void Render_WithVariant_AppliesCorrectHeaderClass(ModalVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, variant));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_CenteredByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Contains("modal-dialog-centered", cut.Markup);
    }

    [Fact]
    public void Render_WithCenteredFalse_DoesNotCenterModal()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Centered, false));

        // Assert
        Assert.DoesNotContain("modal-dialog-centered", cut.Markup);
    }

    [Fact]
    public void Render_ShowsCloseButtonByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void Render_WithShowCloseButtonFalse_HidesCloseButton()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowCloseButton, false));

        // Assert
        Assert.DoesNotContain("btn-close", cut.Markup);
    }

    [Fact]
    public void CloseButton_WhenClicked_InvokesOnCloseCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void CloseButton_WhenClicked_HidesModal()
    {
        // Arrange
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test"));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithShowDefaultButtonsTrue_DisplaysButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true));

        // Assert
        Assert.Contains("modal-footer", cut.Markup);
        Assert.Contains("OK", cut.Markup);
        Assert.Contains("Cancel", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomButtonText_DisplaysCustomText()
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
    public void Render_WithShowCancelButtonFalse_HidesCancelButton()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.ShowCancelButton, false));

        // Assert
        Assert.DoesNotContain("Cancel", cut.Markup);
        Assert.Contains("OK", cut.Markup);
    }

    [Fact]
    public void ConfirmButton_WhenClicked_InvokesOnConfirmCallback()
    {
        // Arrange
        var confirmInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => confirmInvoked = true)));

        // Act
        var buttons = cut.FindAll("button");
        var confirmButton = buttons.FirstOrDefault(b => b.TextContent.Contains("OK"));
        Assert.NotNull(confirmButton);
        confirmButton.Click();

        // Assert
        Assert.True(confirmInvoked);
    }

    [Fact]
    public void ConfirmButton_WhenClicked_ClosesModal()
    {
        // Arrange
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true));

        // Act
        var buttons = cut.FindAll("button");
        var confirmButton = buttons.FirstOrDefault(b => b.TextContent.Contains("OK"));
        Assert.NotNull(confirmButton);
        confirmButton.Click();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void CancelButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var cancelInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        // Act
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
        cancelButton.Click();

        // Assert
        Assert.True(cancelInvoked);
    }

    [Fact]
    public void CancelButton_WhenClicked_ClosesModal()
    {
        // Arrange
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true));

        // Act
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
        cancelButton.Click();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithFooterContent_DisplaysCustomFooter()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddMarkupContent(0, "<div id=\"custom-footer\">Custom footer</div>");
            }));

        // Assert
        Assert.Contains("modal-footer", cut.Markup);
        Assert.Contains("custom-footer", cut.Markup);
        Assert.Contains("Custom footer", cut.Markup);
    }

    [Fact]
    public void Render_WithFooterContent_IgnoresDefaultButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddMarkupContent(0, "<div>Custom footer</div>");
            }));

        // Assert
        Assert.DoesNotContain("OK", cut.Markup);
        Assert.DoesNotContain("Cancel", cut.Markup);
        Assert.Contains("Custom footer", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Contains("role=\"dialog\"", cut.Markup);
        Assert.Contains("aria-labelledby=\"modalTitle\"", cut.Markup);
    }

    [Fact]
    public void Render_WithNonDefaultVariant_UsesWhiteCloseButton()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, ModalVariant.Primary));

        // Assert
        Assert.Contains("btn-close-white", cut.Markup);
    }

    [Fact]
    public void Render_ConfirmButtonMatchesVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowDefaultButtons, true)
            .Add(p => p.Variant, ModalVariant.Success));

        // Assert
        Assert.Contains("btn-success", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTitleAndNoCloseButton_DoesNotRenderHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ShowCloseButton, false)
            .Add(p => p.Message, "Just a message"));

        // Assert
        Assert.DoesNotContain("modal-header", cut.Markup);
    }
}
