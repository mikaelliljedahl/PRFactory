using Bunit;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Tests.Web.Components.Settings;

public class ModelOverridesEditorTests : TestContext
{
    [Fact]
    public void ModelOverridesEditor_RendersTextarea()
    {
        // Act
        var cut = RenderComponent<ModelOverridesEditor>();

        // Assert
        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
    }

    [Fact]
    public void ModelOverridesEditor_RendersHelpText_WhenProvided()
    {
        // Arrange
        var helpText = "This is help text";

        // Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.HelpText, helpText));

        // Assert
        Assert.Contains(helpText, cut.Markup);
    }

    [Fact]
    public void ModelOverridesEditor_ShowsExample()
    {
        // Act
        var cut = RenderComponent<ModelOverridesEditor>();

        // Assert
        Assert.Contains("Example:", cut.Markup);
        Assert.Contains("claude-3-5-sonnet", cut.Markup);
    }

    [Fact]
    public void ModelOverridesEditor_InitializesWithEmptyValue_WhenNoValueProvided()
    {
        // Act
        var cut = RenderComponent<ModelOverridesEditor>();

        // Assert
        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
        // Should have default example JSON
        Assert.Contains("claude-3-5-sonnet", textarea.TextContent);
    }

    [Fact]
    public void ModelOverridesEditor_InitializesWithProvidedValue()
    {
        // Arrange
        var initialValue = new Dictionary<string, string>
        {
            { "model1", "provider-model1" },
            { "model2", "provider-model2" }
        };

        // Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, initialValue));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("model1", textarea.TextContent);
        Assert.Contains("provider-model1", textarea.TextContent);
    }
}
