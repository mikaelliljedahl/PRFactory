using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using PRFactory.Web.UI.Buttons;

namespace PRFactory.Web.Tests.UI.Buttons;

/// <summary>
/// Tests for IconButton component
/// </summary>
public class IconButtonTests : TestContext
{
    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange
        var icon = "trash";

        // Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_WithIconAndText_DisplaysBoth()
    {
        // Arrange
        var icon = "save";
        var text = "Save";

        // Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, icon)
            .Add(p => p.Text, text));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
        Assert.Contains(text, cut.Markup);
    }

    [Fact]
    public void Render_WithoutText_OnlyShowsIcon()
    {
        // Arrange
        var icon = "edit";

        // Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
        Assert.DoesNotContain("<span class=\"ms-2\">", cut.Markup);
    }

    [Fact]
    public void Render_WithTooltip_SetsTitle()
    {
        // Arrange
        var tooltip = "Delete this item";

        // Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "trash")
            .Add(p => p.Tooltip, tooltip));

        // Assert
        var button = cut.Find("button");
        Assert.Equal(tooltip, button.GetAttribute("title"));
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.Disabled, true));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenNotDisabled_IsEnabled()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.Disabled, false));

        // Assert
        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_WhenEnabled_InvokesOnClick()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.OnClick, (MouseEventArgs _) => clicked = true));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void Click_WhenDisabled_DoesNotInvokeOnClick()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, (MouseEventArgs _) => clicked = true));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.False(clicked);
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
    public void Render_WithVariant_AppliesCorrectClass(ButtonVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.Variant, variant));

        // Assert
        var button = cut.Find("button");
        Assert.Contains(expectedClass, button.ClassName);
    }

    [Theory]
    [InlineData(ButtonSize.Small, "btn-sm")]
    [InlineData(ButtonSize.Normal, "btn")]
    [InlineData(ButtonSize.Large, "btn-lg")]
    public void Render_WithSize_AppliesCorrectClass(ButtonSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.Size, size));

        // Assert
        var button = cut.Find("button");
        Assert.Contains(expectedClass, button.ClassName);
    }

    [Theory]
    [InlineData("button")]
    [InlineData("submit")]
    [InlineData("reset")]
    public void Render_WithButtonType_SetsTypeAttribute(string buttonType)
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.ButtonType, buttonType));

        // Assert
        var button = cut.Find("button");
        Assert.Equal(buttonType, button.GetAttribute("type"));
    }

    [Fact]
    public void Render_ByDefault_TypeIsButton()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var additionalClass = "my-custom-class";

        // Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit")
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var button = cut.Find("button");
        Assert.Contains(additionalClass, button.ClassName);
    }

    [Fact]
    public void Render_AlwaysHasBtnClass()
    {
        // Arrange & Act
        var cut = RenderComponent<IconButton>(parameters => parameters
            .Add(p => p.Icon, "edit"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn", button.ClassName);
    }
}
