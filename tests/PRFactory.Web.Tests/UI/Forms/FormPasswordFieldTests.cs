using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormPasswordField component
/// </summary>
public class FormPasswordFieldTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "Password";

        // Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Fact]
    public void Render_ByDefault_InputTypeIsPassword()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password"));

        // Assert
        var input = cut.Find("input");
        Assert.Equal("password", input.GetAttribute("type"));
    }

    [Fact]
    public void Render_HasTogglePasswordButton()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password"));

        // Assert
        var button = cut.Find("button");
        Assert.NotNull(button);
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void TogglePasswordButton_Click_ShowsPassword()
    {
        // Arrange
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Value, "secret123"));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        var input = cut.Find("input");
        Assert.Equal("text", input.GetAttribute("type"));
        Assert.Contains("bi-eye-slash", cut.Markup);
    }

    [Fact]
    public void TogglePasswordButton_ClickTwice_HidesPassword()
    {
        // Arrange
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password"));

        var button = cut.Find("button");

        // Act
        button.Click(); // Show
        button.Click(); // Hide

        // Assert
        var input = cut.Find("input");
        Assert.Equal("password", input.GetAttribute("type"));
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Required, true));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithPlaceholder_DisplaysPlaceholder()
    {
        // Arrange
        var placeholder = "Enter your password";

        // Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Placeholder, placeholder));

        // Assert
        var input = cut.Find("input");
        Assert.Equal(placeholder, input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Password must be at least 8 characters";

        // Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WhenDisabled_DisablesInputAndButton()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Disabled, true));

        // Assert
        var input = cut.Find("input");
        var button = cut.Find("button");
        Assert.True(input.HasAttribute("disabled"));
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.IsInvalid, true));

        // Assert
        var input = cut.Find("input");
        Assert.Contains("is-invalid", input.ClassName);
    }

    [Fact]
    public void Render_WithShowStrengthIndicatorAndValue_ShowsStrengthMeter()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Value, "Password123!")
            .Add(p => p.ShowStrengthIndicator, true));

        // Assert
        Assert.Contains("Password Strength:", cut.Markup);
        Assert.Contains("progress-bar", cut.Markup);
    }

    [Fact]
    public void Render_WithShowStrengthIndicatorButNoValue_DoesNotShowStrengthMeter()
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.ShowStrengthIndicator, true));

        // Assert
        Assert.DoesNotContain("Password Strength:", cut.Markup);
    }

    [Theory]
    [InlineData("weak", "bg-danger")] // Short password (strength 1)
    [InlineData("password", "bg-warning")] // Only lowercase (strength 2: length + lowercase)
    [InlineData("Password1", "bg-success")] // Upper, lower, number, length >= 8 (strength 4)
    [InlineData("Password123!", "bg-success")] // All criteria met (strength 4)
    public void Render_WithDifferentPasswordStrengths_ShowsCorrectColor(string password, string expectedColorClass)
    {
        // Arrange & Act
        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Value, password)
            .Add(p => p.ShowStrengthIndicator, true));

        // Assert
        Assert.Contains(expectedColorClass, cut.Markup);
    }

    [Fact]
    public void OnInput_TriggersValueChanged()
    {
        // Arrange
        var newValue = "newPassword123";
        string? capturedValue = null;

        var cut = RenderComponent<FormPasswordField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var input = cut.Find("input");
        input.Input(newValue);

        // Assert
        Assert.Equal(newValue, capturedValue);
    }
}
