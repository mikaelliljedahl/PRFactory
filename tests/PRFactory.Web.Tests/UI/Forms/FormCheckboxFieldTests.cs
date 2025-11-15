using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormCheckboxField component
/// </summary>
public class FormCheckboxFieldTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "I agree to terms";

        // Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Fact]
    public void Render_WithValueTrue_CheckboxIsChecked()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.Value, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WithValueFalse_CheckboxIsUnchecked()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.Value, false));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.False(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Required Checkbox")
            .Add(p => p.Required, true));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WhenRequired_SetsRequiredAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Required Checkbox")
            .Add(p => p.Required, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.True(checkbox.HasAttribute("required"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Check this box to agree";

        // Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WithHelpTooltip_RendersContextualHelp()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.HelpTooltipText, "Tooltip help"));

        // Assert
        Assert.Contains("contextual-help", cut.Markup);
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Disabled Checkbox")
            .Add(p => p.Disabled, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.True(checkbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Invalid Checkbox")
            .Add(p => p.IsInvalid, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.Contains("is-invalid", checkbox.ClassName);
    }

    [Fact]
    public void Render_HasFormCheckClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox"));

        // Assert
        Assert.Contains("form-check", cut.Markup);
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.Contains("form-check-input", checkbox.ClassName);
    }

    [Fact]
    public void OnChange_ToChecked_TriggersValueChangedWithTrue()
    {
        // Arrange
        bool? capturedValue = null;

        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.Value, false)
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Change(true);

        // Assert
        Assert.True(capturedValue);
    }

    [Fact]
    public void OnChange_ToUnchecked_TriggersValueChangedWithFalse()
    {
        // Arrange
        bool? capturedValue = null;

        var cut = RenderComponent<FormCheckboxField>(parameters => parameters
            .Add(p => p.Label, "Checkbox")
            .Add(p => p.Value, true)
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Change(false);

        // Assert
        Assert.False(capturedValue);
    }
}
