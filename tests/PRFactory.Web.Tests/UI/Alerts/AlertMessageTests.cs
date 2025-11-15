using Bunit;
using Xunit;
using PRFactory.Web.UI.Alerts;

namespace PRFactory.Web.Tests.UI.Alerts;

/// <summary>
/// Tests for AlertMessage component
/// </summary>
public class AlertMessageTests : TestContext
{
    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange
        var message = "This is an alert message";

        // Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains(message, cut.Markup);
    }

    [Fact]
    public void Render_WithNullMessage_DoesNotRender()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, null));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_WithEmptyMessage_DoesNotRender()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, ""));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Theory]
    [InlineData(AlertType.Success, "alert-success")]
    [InlineData(AlertType.Warning, "alert-warning")]
    [InlineData(AlertType.Danger, "alert-danger")]
    [InlineData(AlertType.Info, "alert-info")]
    [InlineData(AlertType.Primary, "alert-primary")]
    [InlineData(AlertType.Secondary, "alert-secondary")]
    public void Render_WithAlertType_AppliesCorrectClass(AlertType type, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test")
            .Add(p => p.Type, type));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_UsesInfoType()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test"));

        // Assert
        Assert.Contains("alert-info", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange
        var icon = "check-circle";

        // Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Success!")
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_WithoutIcon_DoesNotShowIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test"));

        // Assert
        Assert.DoesNotContain("bi-", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange
        var title = "Alert Title";

        // Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Message")
            .Add(p => p.Title, title));

        // Assert
        Assert.Contains(title, cut.Markup);
        Assert.Contains("<strong>", cut.Markup);
    }

    [Fact]
    public void Render_WhenDismissibleTrue_ShowsDismissButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Dismissible alert")
            .Add(p => p.Dismissible, true));

        // Assert
        Assert.Contains("btn-close", cut.Markup);
        Assert.Contains("alert-dismissible", cut.Markup);
    }

    [Fact]
    public void Render_WhenDismissibleFalse_DoesNotShowDismissButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Non-dismissible alert")
            .Add(p => p.Dismissible, false));

        // Assert
        Assert.DoesNotContain("btn-close", cut.Markup);
        Assert.DoesNotContain("alert-dismissible", cut.Markup);
    }

    [Fact]
    public void DismissButton_Click_HidesAlert()
    {
        // Arrange
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Dismissible alert")
            .Add(p => p.Dismissible, true));

        // Act
        var dismissButton = cut.Find(".btn-close");
        dismissButton.Click();

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void DismissButton_Click_InvokesOnDismiss()
    {
        // Arrange
        var dismissed = false;

        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Dismissible alert")
            .Add(p => p.Dismissible, true)
            .Add(p => p.OnDismiss, () => dismissed = true));

        // Act
        var dismissButton = cut.Find(".btn-close");
        dismissButton.Click();

        // Assert
        Assert.True(dismissed);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Alert message")
            .AddChildContent("<strong>Additional content</strong>"));

        // Assert
        Assert.Contains("Additional content", cut.Markup);
    }

    [Fact]
    public void Render_AlwaysHasAlertClass()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test"));

        // Assert
        Assert.Contains("alert", cut.Markup);
    }

    [Fact]
    public void Render_HasRoleAlert()
    {
        // Arrange & Act
        var cut = RenderComponent<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Test"));

        // Assert
        Assert.Contains("role=\"alert\"", cut.Markup);
    }
}
