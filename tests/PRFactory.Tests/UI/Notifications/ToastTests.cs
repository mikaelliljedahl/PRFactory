using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Notifications;
using Xunit;

namespace PRFactory.Tests.UI.Notifications;

public class ToastTests : ComponentTestBase
{
    [Fact]
    public void Render_WhenVisible_DisplaysToast()
    {
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Notification")
            .Add(x => x.IsVisible, true));
        Assert.Contains("toast", cut.Markup);
        Assert.Contains("Notification", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotVisible_HidesToast()
    {
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Test")
            .Add(x => x.IsVisible, false));
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Success")
            .Add(x => x.Message, "Operation completed"));
        Assert.Contains("Operation completed", cut.Markup);
    }

    [Theory]
    [InlineData(ToastType.Success, "bg-success")]
    [InlineData(ToastType.Error, "bg-danger")]
    [InlineData(ToastType.Warning, "bg-warning")]
    [InlineData(ToastType.Info, "bg-info")]
    [InlineData(ToastType.Primary, "bg-primary")]
    public void Render_WithType_AppliesCorrectClass(ToastType type, string expectedClass)
    {
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Test")
            .Add(x => x.Type, type));
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithDismissibleTrue_ShowsCloseButton()
    {
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Test")
            .Add(x => x.Dismissible, true));
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void CloseButton_WhenClicked_InvokesOnDismiss()
    {
        var dismissed = false;
        var cut = RenderComponent<Toast>(p => p
            .Add(x => x.Title, "Test")
            .Add(x => x.Dismissible, true)
            .Add(x => x.OnDismiss, EventCallback.Factory.Create(this, () => dismissed = true)));
        var button = cut.Find(".btn-close");
        button.Click();
        Assert.True(dismissed);
    }
}
