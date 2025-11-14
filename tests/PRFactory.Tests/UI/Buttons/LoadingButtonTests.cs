using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Buttons;
using Xunit;

namespace PRFactory.Tests.UI.Buttons;

public class LoadingButtonTests : ComponentTestBase
{
    [Fact]
    public void Render_WithText_DisplaysText()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Click me"));

        // Assert
        Assert.Contains("Click me", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotLoading_ShowsText()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Submit")
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.Contains("Submit", cut.Markup);
        Assert.DoesNotContain("spinner-border", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_ShowsSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Submit")
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
        Assert.Contains("spinner-border-sm", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_ShowsLoadingText()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Submit")
            .Add(p => p.LoadingText, "Processing...")
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("Processing...", cut.Markup);
        Assert.DoesNotContain("Submit", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Save")
            .Add(p => p.Icon, "save"));

        // Assert
        Assert.Contains("bi-save", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_HidesIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Save")
            .Add(p => p.Icon, "save")
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.DoesNotContain("bi-save", cut.Markup);
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
    public void Render_WithVariant_ShowsCorrectCssClass(ButtonVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
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
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithNormalSize_DoesNotAddSizeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Size, ButtonSize.Normal));

        // Assert
        Assert.DoesNotContain("btn-sm", cut.Markup);
        Assert.DoesNotContain("btn-lg", cut.Markup);
    }

    [Fact]
    public void Render_WithDisabledTrue_IsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Disabled, true));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_IsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void Render_WithButtonType_SetsCorrectType()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.ButtonType, "submit"));

        // Assert
        Assert.Contains("type=\"submit\"", cut.Markup);
    }

    [Fact]
    public void Render_DefaultButtonType_IsButton()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test"));

        // Assert
        Assert.Contains("type=\"button\"", cut.Markup);
    }

    [Fact]
    public void Button_WhenClicked_InvokesOnClickCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
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
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => callbackInvoked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void Button_WhenLoading_DoesNotInvokeOnClickCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.IsLoading, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => callbackInvoked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void Render_WithChildContent_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<span id=\"custom\">Custom</span>");
            }));

        // Assert
        Assert.Contains("custom", cut.Markup);
        Assert.Contains("Custom", cut.Markup);
    }

    [Fact]
    public void Render_WhenLoading_HidesChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<span id=\"custom\">Custom</span>");
            }));

        // Assert
        Assert.DoesNotContain("Custom", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalClass_AddsCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.AdditionalClass, "custom-class"));

        // Assert
        Assert.Contains("custom-class", cut.Markup);
    }

    [Fact]
    public void Render_WithoutOnClickCallback_DoesNotThrowOnClick()
    {
        // Arrange
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test"));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert - No exception thrown
        Assert.NotNull(cut);
    }

    [Fact]
    public void Render_DefaultVariant_IsPrimary()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Test"));

        // Assert
        Assert.Contains("btn-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_DisplaysCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Submit")
            .Add(p => p.LoadingText, "Saving...")
            .Add(p => p.Icon, "save")
            .Add(p => p.Variant, ButtonVariant.Success)
            .Add(p => p.Size, ButtonSize.Large)
            .Add(p => p.AdditionalClass, "mt-3"));

        // Assert
        Assert.Contains("Submit", cut.Markup);
        Assert.Contains("bi-save", cut.Markup);
        Assert.Contains("btn-success", cut.Markup);
        Assert.Contains("btn-lg", cut.Markup);
        Assert.Contains("mt-3", cut.Markup);
    }
}
