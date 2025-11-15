using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormTextAreaField component
/// </summary>
public class FormTextAreaFieldTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "Description";

        // Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Fact]
    public void Render_WithValue_DisplaysValue()
    {
        // Arrange
        var expectedValue = "This is a long description";

        // Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.Value, expectedValue));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains(expectedValue, textarea.TextContent);
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
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
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Required Field")
            .Add(p => p.Required, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.True(textarea.HasAttribute("required"));
    }

    [Fact]
    public void Render_WithPlaceholder_DisplaysPlaceholder()
    {
        // Arrange
        var placeholder = "Enter a description";

        // Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.Placeholder, placeholder));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(placeholder, textarea.GetAttribute("placeholder"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Provide a detailed description";

        // Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WithHelpTooltip_RendersContextualHelp()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.HelpTooltipText, "Tooltip help"));

        // Assert
        Assert.Contains("contextual-help", cut.Markup);
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Disabled Field")
            .Add(p => p.Disabled, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.True(textarea.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Invalid Field")
            .Add(p => p.IsInvalid, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("is-invalid", textarea.ClassName);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Render_WithRowsParameter_SetsRowsAttribute(int rows)
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.Rows, rows));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(rows.ToString(), textarea.GetAttribute("rows"));
    }

    [Fact]
    public void Render_ByDefault_HasThreeRows()
    {
        // Arrange & Act
        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description"));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal("3", textarea.GetAttribute("rows"));
    }

    [Fact]
    public void OnInput_TriggersValueChanged()
    {
        // Arrange
        var newValue = "New textarea content";
        string? capturedValue = null;

        var cut = RenderComponent<FormTextAreaField>(parameters => parameters
            .Add(p => p.Label, "Description")
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var textarea = cut.Find("textarea");
        textarea.Input(newValue);

        // Assert
        Assert.Equal(newValue, capturedValue);
    }
}
