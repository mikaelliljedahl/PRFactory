using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Buttons;
using Xunit;

namespace PRFactory.Tests.UI.Buttons;

public class IconButtonTests : ComponentTestBase
{
    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("bi-gear", cut.Markup);
    }

    [Fact]
    public void Render_WithText_DisplaysTextWithMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Text, "Settings"));

        // Assert
        Assert.Contains("Settings", cut.Markup);
        Assert.Contains("ms-2", cut.Markup);
    }

    [Fact]
    public void Render_WithoutText_DoesNotDisplayTextSpan()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Assert
        var spans = cut.FindAll("span");
        Assert.Empty(spans);
    }

    [Fact]
    public void Render_WithTooltip_SetsTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Tooltip, "Click to configure"));

        // Assert
        Assert.Contains("title=\"Click to configure\"", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabledTrue_IsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Disabled, true));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }

    [Theory]
    [InlineData(ButtonVariant.Primary, "btn-primary")]
    [InlineData(ButtonVariant.Secondary, "btn-secondary")]
    [InlineData(ButtonVariant.Success, "btn-success")]
    [InlineData(ButtonVariant.Danger, "btn-danger")]
    [InlineData(ButtonVariant.Warning, "btn-warning")]
    [InlineData(ButtonVariant.Info, "btn-info")]
    [InlineData(ButtonVariant.Light, "btn-light")]
    [InlineData(ButtonVariant.Dark, "btn-dark")]
    [InlineData(ButtonVariant.OutlinePrimary, "btn-outline-primary")]
    [InlineData(ButtonVariant.OutlineSecondary, "btn-outline-secondary")]
    [InlineData(ButtonVariant.OutlineSuccess, "btn-outline-success")]
    [InlineData(ButtonVariant.OutlineDanger, "btn-outline-danger")]
    [InlineData(ButtonVariant.OutlineWarning, "btn-outline-warning")]
    [InlineData(ButtonVariant.OutlineInfo, "btn-outline-info")]
    public void Render_WithVariant_ShowsCorrectCssClass(ButtonVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Variant, variant));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Theory]
    [InlineData(ButtonSize.Small, "btn-sm")]
    [InlineData(ButtonSize.Large, "btn-lg")]
    public void Render_WithSize_ShowsCorrectCssClass(ButtonSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithNormalSize_DoesNotAddSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Size, ButtonSize.Normal));

        // Assert
        Assert.DoesNotContain("btn-sm", cut.Markup);
        Assert.DoesNotContain("btn-lg", cut.Markup);
    }

    [Fact]
    public void Render_WithButtonType_SetsCorrectType()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.ButtonType, "submit"));

        // Assert
        Assert.Contains("type=\"submit\"", cut.Markup);
    }

    [Fact]
    public void Render_DefaultButtonType_IsButton()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("type=\"button\"", cut.Markup);
    }

    [Fact]
    public void Button_WhenClicked_InvokesOnClickCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => callbackInvoked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Button_WhenDisabled_DoesNotInvokeOnClickCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => callbackInvoked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void Render_WithAdditionalClass_AddsCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.AdditionalClass, "custom-class"));

        // Assert
        Assert.Contains("custom-class", cut.Markup);
    }

    [Fact]
    public void Render_WithoutOnClickCallback_DoesNotThrowOnClick()
    {
        // Arrange
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Act & Assert (should not throw)
        var button = cut.Find("button");
        button.Click();
    }

    [Fact]
    public void Render_DefaultVariant_IsPrimary()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("btn-primary", cut.Markup);
    }

    [Fact]
    public void Render_HasBaseButtonClass()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("class=\"btn", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_DisplaysCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "save")
            .Add(p => p.Text, "Save")
            .Add(p => p.Tooltip, "Save changes")
            .Add(p => p.Variant, ButtonVariant.Success)
            .Add(p => p.Size, ButtonSize.Large)
            .Add(p => p.AdditionalClass, "mt-3"));

        // Assert
        Assert.Contains("bi-save", cut.Markup);
        Assert.Contains("Save", cut.Markup);
        Assert.Contains("title=\"Save changes\"", cut.Markup);
        Assert.Contains("btn-success", cut.Markup);
        Assert.Contains("btn-lg", cut.Markup);
        Assert.Contains("mt-3", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyText_DoesNotDisplayTextSpan()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "gear")
            .Add(p => p.Text, ""));

        // Assert
        var spans = cut.FindAll("span");
        Assert.Empty(spans);
    }
}
