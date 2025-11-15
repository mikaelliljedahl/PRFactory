using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormTextField component
/// </summary>
public class FormTextFieldTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "Username";

        // Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("email")]
    [InlineData("number")]
    [InlineData("url")]
    public void Render_WithInputType_AppliesCorrectType(string inputType)
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Test Field")
            .Add(p => p.InputType, inputType));

        // Assert
        var input = cut.Find("input");
        Assert.Equal(inputType, input.GetAttribute("type"));
    }

    [Fact]
    public void Render_WithValue_DisplaysValue()
    {
        // Arrange
        var expectedValue = "test@example.com";

        // Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.Value, expectedValue));

        // Assert
        var input = cut.Find("input");
        Assert.Equal(expectedValue, input.GetAttribute("value"));
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Required Field")
            .Add(p => p.Required, true));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WhenRequired_SetsRequiredAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Required Field")
            .Add(p => p.Required, true));

        // Assert
        var input = cut.Find("input");
        Assert.True(input.HasAttribute("required"));
    }

    [Fact]
    public void Render_WithPlaceholder_DisplaysPlaceholder()
    {
        // Arrange
        var placeholder = "Enter your email";

        // Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.Placeholder, placeholder));

        // Assert
        var input = cut.Find("input");
        Assert.Equal(placeholder, input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "This is a helpful hint";

        // Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Field")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Disabled Field")
            .Add(p => p.Disabled, true));

        // Assert
        var input = cut.Find("input");
        Assert.True(input.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Invalid Field")
            .Add(p => p.IsInvalid, true));

        // Assert
        var input = cut.Find("input");
        Assert.Contains("is-invalid", input.ClassName);
    }

    [Fact]
    public void Render_WithCustomId_UsesCustomId()
    {
        // Arrange
        var customId = "my-custom-id";

        // Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Field")
            .Add(p => p.Id, customId));

        // Assert
        var input = cut.Find("input");
        Assert.Equal(customId, input.Id);
    }

    [Fact]
    public void Render_WithoutCustomId_GeneratesIdFromLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "My Field Name"));

        // Assert
        var input = cut.Find("input");
        Assert.Equal("field-my-field-name", input.Id);
    }

    [Fact]
    public void Render_WithHelpTooltip_RendersContextualHelp()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Field")
            .Add(p => p.HelpTooltipText, "Tooltip help"));

        // Assert
        Assert.Contains("contextual-help", cut.Markup);
    }

    [Fact]
    public void OnInput_TriggersValueChanged()
    {
        // Arrange
        var newValue = "new text value";
        string? capturedValue = null;

        var cut = RenderComponent<FormTextField>(parameters => parameters
            .Add(p => p.Label, "Field")
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var input = cut.Find("input");
        input.Input(newValue);

        // Assert
        Assert.Equal(newValue, capturedValue);
    }
}
