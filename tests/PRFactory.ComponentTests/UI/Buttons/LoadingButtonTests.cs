using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PRFactory.Web.UI.Buttons;

namespace PRFactory.ComponentTests.UI.Buttons;

/// <summary>
/// Tests for the LoadingButton component using bUnit.
/// Tests rendering, click events, and loading states.
/// </summary>
public class LoadingButtonTests : TestContext
{
    [Fact]
    public void LoadingButton_WithText_RendersTextCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Click Me"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Click Me", button.TextContent);
    }

    [Fact]
    public void LoadingButton_WithChildContent_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .AddChildContent("<span>Child Content</span>"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Child Content", button.InnerHtml);
    }

    [Fact]
    public void LoadingButton_WithIcon_RendersIconElement()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Icon, "check-circle")
            .Add(p => p.Text, "Submit"));

        // Assert
        var icon = cut.Find("i.bi.bi-check-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void LoadingButton_IsLoading_ShowsSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Loading..."));

        // Assert
        var spinner = cut.Find("span.spinner-border");
        Assert.NotNull(spinner);
        Assert.Contains("Loading...", cut.Markup);
    }

    [Fact]
    public void LoadingButton_IsLoading_DoesNotShowText()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Click Me")
            .Add(p => p.LoadingText, "Processing...")
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.DoesNotContain("Click Me", cut.Markup);
        Assert.Contains("Processing...", cut.Markup);
    }

    [Fact]
    public void LoadingButton_IsLoading_ButtonIsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void LoadingButton_Disabled_ButtonIsDisabled()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void LoadingButton_Click_TriggersOnClickCallback()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, "Click Me")
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void LoadingButton_ClickWhileLoading_DoesNotTriggerCallback()
    {
        // Arrange
        var clickCount = 0;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.Equal(0, clickCount); // Should not increment
    }

    [Fact]
    public void LoadingButton_ClickWhileDisabled_DoesNotTriggerCallback()
    {
        // Arrange
        var clickCount = 0;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.Equal(0, clickCount); // Should not increment
    }

    [Fact]
    public void LoadingButton_PrimaryVariant_HasPrimaryClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Primary));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-primary", button.ClassName);
    }

    [Fact]
    public void LoadingButton_DangerVariant_HasDangerClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Danger));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-danger", button.ClassName);
    }

    [Fact]
    public void LoadingButton_SmallSize_HasSmallClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Small));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-sm", button.ClassName);
    }

    [Fact]
    public void LoadingButton_LargeSize_HasLargeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Large));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-lg", button.ClassName);
    }

    [Fact]
    public void LoadingButton_AdditionalClass_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.AdditionalClass, "custom-class"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("custom-class", button.ClassName);
    }

    [Fact]
    public void LoadingButton_ButtonType_SetsTypeAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.ButtonType, "submit"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("submit", button.GetAttribute("type"));
    }

    [Fact]
    public void LoadingButton_DefaultButtonType_IsButton()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>();

        // Assert
        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
    }
}
