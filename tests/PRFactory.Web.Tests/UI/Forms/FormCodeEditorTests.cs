using Bunit;
using Xunit;
using PRFactory.Web.UI.Forms;

namespace PRFactory.Web.Tests.UI.Forms;

/// <summary>
/// Tests for FormCodeEditor component
/// </summary>
public class FormCodeEditorTests : TestContext
{
    [Fact]
    public void Render_WithLabel_DisplaysLabel()
    {
        // Arrange
        var expectedLabel = "Code";

        // Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, expectedLabel));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(expectedLabel, label.TextContent);
    }

    [Fact]
    public void Render_WithValue_DisplaysValue()
    {
        // Arrange
        var code = "function test() { return true; }";

        // Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code")
            .Add(p => p.Value, code));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains(code, textarea.TextContent);
    }

    [Fact]
    public void Render_AppliesCodeEditorClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code"));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("code-editor", textarea.ClassName);
    }

    [Fact]
    public void Render_AppliesFormControlClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code"));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("form-control", textarea.ClassName);
    }

    [Fact]
    public void Render_SpellcheckIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code"));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal("false", textarea.GetAttribute("spellcheck"));
    }

    [Fact]
    public void Render_WhenRequired_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Required Code")
            .Add(p => p.Required, true));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WhenRequired_SetsRequiredAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Required Code")
            .Add(p => p.Required, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.True(textarea.HasAttribute("required"));
    }

    [Fact]
    public void Render_WithPlaceholder_DisplaysPlaceholder()
    {
        // Arrange
        var placeholder = "// Enter code here";

        // Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code")
            .Add(p => p.Placeholder, placeholder));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(placeholder, textarea.GetAttribute("placeholder"));
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Enter valid JavaScript code";

        // Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code")
            .Add(p => p.HelpText, helpText));

        // Assert
        var small = cut.Find("small.form-text");
        Assert.Contains(helpText, small.TextContent);
    }

    [Fact]
    public void Render_WhenDisabled_SetsDisabledAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Disabled Code")
            .Add(p => p.Disabled, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.True(textarea.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenInvalid_AppliesInvalidClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Invalid Code")
            .Add(p => p.IsInvalid, true));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("is-invalid", textarea.ClassName);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void Render_WithRowsParameter_SetsRowsAttribute(int rows)
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code")
            .Add(p => p.Rows, rows));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(rows.ToString(), textarea.GetAttribute("rows"));
    }

    [Fact]
    public void Render_ByDefault_Has15Rows()
    {
        // Arrange & Act
        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code"));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal("15", textarea.GetAttribute("rows"));
    }

    [Fact]
    public void OnInput_TriggersValueChanged()
    {
        // Arrange
        var newCode = "const x = 42;";
        string? capturedValue = null;

        var cut = RenderComponent<FormCodeEditor>(parameters => parameters
            .Add(p => p.Label, "Code")
            .Add(p => p.ValueChanged, value => capturedValue = value));

        // Act
        var textarea = cut.Find("textarea");
        textarea.Input(newCode);

        // Assert
        Assert.Equal(newCode, capturedValue);
    }
}
