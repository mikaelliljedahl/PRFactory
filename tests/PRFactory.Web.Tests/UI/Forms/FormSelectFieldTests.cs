using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormSelectField component
/// </summary>
public class FormSelectFieldTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "Country";

        // Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Fact]
    public void Render_WithChildContent_RendersOptions()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .AddChildContent("<option value=\"1\">Option 1</option><option value=\"2\">Option 2</option>"));

        // Assert
        Assert.Contains("Option 1", cut.Markup);
        Assert.Contains("Option 2", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultOptionText_RendersDefaultOption()
    {
        // Arrange
        var defaultText = "-- Select an option --";

        // Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .Add(p => p.DefaultOptionText, defaultText));

        // Assert
        Assert.Contains(defaultText, cut.Markup);
        // Check that the default option is the first option in the select
        var select = cut.Find("select");
        var firstOption = select.QuerySelector("option");
        Assert.NotNull(firstOption);
        Assert.Contains(defaultText, firstOption.TextContent);
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
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
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Required Field")
            .Add(p => p.Required, true));

        // Assert
        var select = cut.Find("select");
        Assert.True(select.HasAttribute("required"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Choose from the list";

        // Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WithHelpTooltip_RendersContextualHelp()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .Add(p => p.HelpTooltipText, "Tooltip help"));

        // Assert
        Assert.Contains("contextual-help", cut.Markup);
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Disabled Field")
            .Add(p => p.Disabled, true));

        // Assert
        var select = cut.Find("select");
        Assert.True(select.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Invalid Field")
            .Add(p => p.IsInvalid, true));

        // Assert
        var select = cut.Find("select");
        Assert.Contains("is-invalid", select.ClassName);
    }

    [Fact]
    public void Render_UsesFormSelectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select"));

        // Assert
        var select = cut.Find("select");
        Assert.Contains("form-select", select.ClassName);
    }

    [Fact]
    public void OnChange_TriggersValueChanged_ForStringType()
    {
        // Arrange
        string? capturedValue = null;

        var cut = RenderComponent<FormSelectField<string>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .Add(p => p.ValueChanged, value => capturedValue = value)
            .AddChildContent("<option value=\"test\">Test</option>"));

        // Act
        var select = cut.Find("select");
        select.Change("test");

        // Assert
        Assert.Equal("test", capturedValue);
    }

    [Fact]
    public void OnChange_TriggersValueChanged_ForIntType()
    {
        // Arrange
        int? capturedValue = null;

        var cut = RenderComponent<FormSelectField<int>>(parameters => parameters
            .Add(p => p.Label, "Select")
            .Add(p => p.ValueChanged, value => capturedValue = value)
            .AddChildContent("<option value=\"42\">Forty Two</option>"));

        // Act
        var select = cut.Find("select");
        select.Change("42");

        // Assert
        Assert.Equal(42, capturedValue);
    }
}
