using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using PRFactory.Web.UI.Buttons;

namespace PRFactory.Web.Tests.UI.Buttons;

/// <summary>
/// Tests for LoadingButton component
/// </summary>
public class LoadingButtonTests : TestContext
{
    [Fact]
    public void Render_WithText_DisplaysText()
    {
        // Arrange
        var expectedText = "Click Me";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, expectedText));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_ShowsSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Processing..."));

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
        Assert.Contains("spinner-border-sm", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_DisplaysLoadingText()
    {
        // Arrange
        var loadingText = "Processing...";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, loadingText));

        // Assert
        Assert.Contains(loadingText, cut.Markup);
    }

    [Fact]
    public void Render_WhenNotLoading_DoesNotShowSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Text, "Click Me"));

        // Assert
        Assert.DoesNotContain("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotLoadingWithIcon_ShowsIcon()
    {
        // Arrange
        var icon = "save";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Icon, icon)
            .Add(p => p.Text, "Save"));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_DoesNotShowIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Icon, "save")
            .Add(p => p.LoadingText, "Saving..."));

        // Assert
        Assert.DoesNotContain("bi-save", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_IsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenDisabled_IsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenNotLoadingAndNotDisabled_IsEnabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Disabled, false));

        // Assert
        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_WhenEnabledAndNotLoading_InvokesOnClick()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Click")
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

        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, (MouseEventArgs _) => clicked = true));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.False(clicked);
    }

    [Fact]
    public void Click_WhenLoading_DoesNotInvokeOnClick()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
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
    [InlineData(ButtonVariant.OutlineDanger, "btn-outline-danger")]
    public void Render_WithVariant_AppliesCorrectClass(ButtonVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
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
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Size, size));

        // Assert
        var button = cut.Find("button");
        Assert.Contains(expectedClass, button.ClassName);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysChildContent()
    {
        // Arrange
        var childContent = "<span>Custom Content</span>";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .AddChildContent(childContent));

        // Assert
        Assert.Contains("Custom Content", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotLoading_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .AddChildContent("<strong>Bold Text</strong>"));

        // Assert
        Assert.Contains("<strong>Bold Text</strong>", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_DoesNotRenderChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Loading")
            .AddChildContent("<strong>Bold Text</strong>"));

        // Assert
        Assert.DoesNotContain("Bold Text", cut.Markup);
    }

    [Theory]
    [InlineData("button")]
    [InlineData("submit")]
    [InlineData("reset")]
    public void Render_WithButtonType_SetsTypeAttribute(string buttonType)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.ButtonType, buttonType));

        // Assert
        var button = cut.Find("button");
        Assert.Equal(buttonType, button.GetAttribute("type"));
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var additionalClass = "my-custom-class";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var button = cut.Find("button");
        Assert.Contains(additionalClass, button.ClassName);
    }
}
