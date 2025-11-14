using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Alerts;
using Xunit;

namespace PRFactory.Tests.UI.Alerts;

public class AlertMessageTests : ComponentTestBase
{
    [Fact]
    public void Render_WithNullMessage_RendersNothing()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyMessage_RendersNothing()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, ""));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("Test message", cut.Markup);
    }

    [Theory]
    [InlineData(AlertType.Success, "alert-success")]
    [InlineData(AlertType.Warning, "alert-warning")]
    [InlineData(AlertType.Danger, "alert-danger")]
    [InlineData(AlertType.Info, "alert-info")]
    [InlineData(AlertType.Primary, "alert-primary")]
    [InlineData(AlertType.Secondary, "alert-secondary")]
    public void Render_WithType_ShowsCorrectCssClass(AlertType type, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test")
            .Add(p => p.Type, type));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Title, "Important"));

        // Assert
        Assert.Contains("<strong>Important:</strong>", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Icon, "check-circle"));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void Render_WithDismissibleTrue_ShowsCloseButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true));

        // Assert
        Assert.Contains("alert-dismissible", cut.Markup);
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void Render_WithDismissibleFalse_HidesCloseButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, false));

        // Assert
        Assert.DoesNotContain("alert-dismissible", cut.Markup);
        Assert.DoesNotContain("btn-close", cut.Markup);
    }

    [Fact]
    public void Render_WithChildContent_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<span id=\"child-content\">Child</span>");
            }));

        // Assert
        Assert.Contains("child-content", cut.Markup);
        Assert.Contains("Child", cut.Markup);
    }

    [Fact]
    public void CloseButton_WhenClicked_HidesAlert()
    {
        // Arrange
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void CloseButton_WhenClicked_InvokesOnDismissCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true)
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Render_WithoutOnDismissCallback_DoesNotThrowOnClose()
    {
        // Arrange
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert - No exception thrown
        Assert.NotNull(cut);
    }

    [Fact]
    public void Render_DefaultType_IsInfo()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("alert-info", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_DisplaysAllElements()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Title, "Warning")
            .Add(p => p.Type, AlertType.Warning)
            .Add(p => p.Icon, "exclamation-triangle")
            .Add(p => p.Dismissible, true)
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<span>Extra content</span>");
            }));

        // Assert
        Assert.Contains("alert-warning", cut.Markup);
        Assert.Contains("<strong>Warning:</strong>", cut.Markup);
        Assert.Contains("Test message", cut.Markup);
        Assert.Contains("bi-exclamation-triangle", cut.Markup);
        Assert.Contains("btn-close", cut.Markup);
        Assert.Contains("Extra content", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectAlertRole()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("role=\"alert\"", cut.Markup);
    }

    [Fact]
    public void Render_CloseButtonHasAriaLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true));

        // Assert
        Assert.Contains("aria-label=\"Close\"", cut.Markup);
    }
}
