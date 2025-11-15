using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using PRFactory.Web.Components.Settings;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the ModelOverridesEditor component.
/// Verifies JSON validation, rendering, and callback functionality.
/// </summary>
public class ModelOverridesEditorTests : TestContext
{
    public ModelOverridesEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    [Fact]
    public void Render_DisplaysLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var label = cut.Find("label");
        Assert.NotNull(label);
        Assert.Contains("Model Overrides", label.TextContent);
    }

    [Fact]
    public void Render_DisplaysTextarea()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
        Assert.Equal("8", textarea.GetAttribute("rows"));
    }

    [Fact]
    public void Render_DisplaysExample()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var example = cut.Find("pre");
        Assert.NotNull(example);
        Assert.Contains("claude-3-5-sonnet", example.TextContent);
        Assert.Contains("claude-sonnet-3-5-20250101", example.TextContent);
    }

    [Fact]
    public void Render_WithHelpText_DisplaysHelpText()
    {
        // Arrange
        var helpText = "Map custom model names to provider-specific models";

        // Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.HelpText, helpText));

        // Assert
        var label = cut.Find("label");
        Assert.Contains(helpText, label.TextContent);
    }

    [Fact]
    public void Render_WithoutHelpText_DoesNotDisplayHelpText()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var label = cut.Find("label");
        var textAfterLabel = label.NextSibling;
        // Should not have help text
        var markup = cut.Markup;
        // Check that only one help text element exists (in the example, not in label)
        var helpTexts = cut.FindAll("small.text-muted");
        Assert.Single(helpTexts); // Only the "Example:" text
    }

    [Fact]
    public void Render_WithValue_DisplaysJsonInTextarea()
    {
        // Arrange
        var value = new Dictionary<string, string>
        {
            { "gpt-4", "gpt-4-turbo-2024-04-09" },
            { "gpt-3.5", "gpt-3.5-turbo-0125" }
        };

        // Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, value));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("gpt-4", textarea.TextContent);
        Assert.Contains("gpt-4-turbo-2024-04-09", textarea.TextContent);
    }

    [Fact]
    public void Render_WithoutValue_DisplaysDefaultExample()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("claude-3-5-sonnet", textarea.TextContent);
        Assert.Contains("claude-sonnet-3-5-20250101", textarea.TextContent);
    }

    [Fact]
    public void Render_TextareaHasMonospaceFont()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("font-monospace", textarea.GetAttribute("class"));
    }

    [Fact]
    public async Task Input_WithValidJson_ClearsValidationError()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = null;

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"gpt-4\": \"gpt-4-turbo\" }");

        // Assert
        var invalidFeedback = cut.FindAll(".invalid-feedback");
        // Valid JSON should not show error - element should not exist
        Assert.Empty(invalidFeedback);
    }

    [Fact]
    public async Task Input_WithInvalidJson_DisplaysValidationError()
    {
        // Arrange
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ invalid json }");

        // Assert
        var invalidFeedback = cut.FindAll(".invalid-feedback").FirstOrDefault();
        Assert.NotNull(invalidFeedback);
        Assert.Contains("Invalid JSON", invalidFeedback.TextContent);
    }

    [Fact]
    public async Task Input_WithValidJson_InvokesValueChangedCallback()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = null;

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"gpt-4\": \"gpt-4-turbo\" }");

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal("gpt-4-turbo", newValue!["gpt-4"]);
    }

    [Fact]
    public async Task Input_WithEmptyJson_SetsValueToNull()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = new Dictionary<string, string> { { "test", "value" } };

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, newValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input(string.Empty);

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.Null(newValue);
    }

    [Fact]
    public async Task Input_WithWhitespaceOnly_SetsValueToNull()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = new Dictionary<string, string> { { "test", "value" } };

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, newValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("   \n\t  ");

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.Null(newValue);
    }

    [Fact]
    public async Task Input_WithMultipleEntries_ParsesAllEntries()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = null;

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        var json = @"{
  ""gpt-4"": ""gpt-4-turbo-2024-04-09"",
  ""gpt-3.5"": ""gpt-3.5-turbo-0125"",
  ""claude-3"": ""claude-3-sonnet-20240229""
}";
        textarea.Input(json);

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal(3, newValue!.Count);
        Assert.Equal("gpt-4-turbo-2024-04-09", newValue["gpt-4"]);
        Assert.Equal("gpt-3.5-turbo-0125", newValue["gpt-3.5"]);
        Assert.Equal("claude-3-sonnet-20240229", newValue["claude-3"]);
    }

    [Fact]
    public async Task Input_WithMissingBrace_ShowsJsonError()
    {
        // Arrange
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"gpt-4\": \"gpt-4-turbo\"");

        // Assert
        var invalidFeedback = cut.FindAll(".invalid-feedback").FirstOrDefault();
        Assert.NotNull(invalidFeedback);
        Assert.Contains("Invalid JSON", invalidFeedback.TextContent);
    }

    [Fact]
    public async Task Input_WithInvalidJsonValue_ShowsError()
    {
        // Arrange
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"gpt-4\": }");

        // Assert
        var invalidFeedback = cut.FindAll(".invalid-feedback").FirstOrDefault();
        Assert.NotNull(invalidFeedback);
        Assert.Contains("Invalid JSON", invalidFeedback.TextContent);
    }

    [Fact]
    public async Task Input_ClearsErrorWhenFixedAfterError()
    {
        // Arrange
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var textarea = cut.Find("textarea");

        // Act - First invalid
        textarea.Input("{ invalid }");
        var feedback1 = cut.FindAll(".invalid-feedback").FirstOrDefault();
        Assert.NotNull(feedback1);
        Assert.Contains("Invalid JSON", feedback1.TextContent);

        // Act - Then valid
        textarea.Input("{ \"gpt-4\": \"turbo\" }");
        var feedback2 = cut.FindAll(".invalid-feedback");
        // After valid JSON, error element should be removed completely
        Assert.Empty(feedback2);
    }

    [Fact]
    public void Render_DisplaysExampleLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("<strong>Example:</strong>", markup);
    }

    [Fact]
    public void Render_DisplaysPreformattedExample()
    {
        // Arrange & Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var preCode = cut.Find("pre > code");
        Assert.NotNull(preCode);
        var parentElement = (IElement)preCode.Parent!;
        Assert.Contains("bg-light", parentElement.GetAttribute("class"));
    }

    [Fact]
    public async Task Input_ParsesJsonCorrectly()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = null;

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"model1\": \"provider-model-1\", \"model2\": \"provider-model-2\" }");

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal("provider-model-1", newValue!["model1"]);
        Assert.Equal("provider-model-2", newValue!["model2"]);
    }

    [Fact]
    public void Render_WithEmptyDictionary_DisplaysDefaultExample()
    {
        // Arrange
        var value = new Dictionary<string, string>();

        // Act
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, value));

        // Assert
        var textarea = cut.Find("textarea");
        // Empty dict should also display default example
        Assert.Contains("claude-3-5-sonnet", textarea.TextContent);
    }

    [Fact]
    public async Task Input_WithNestedJsonObject_ShowsError()
    {
        // Arrange
        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("{ \"gpt-4\": { \"name\": \"turbo\" } }");

        // Assert
        var invalidFeedback = cut.FindAll(".invalid-feedback").FirstOrDefault();
        Assert.NotNull(invalidFeedback);
        // JsonSerializer.Deserialize<Dictionary<string, string>> will fail on nested object
        Assert.Contains("Invalid JSON", invalidFeedback.TextContent);
    }

    [Fact]
    public async Task Input_TrimsWhitespace()
    {
        // Arrange
        var valueChangedInvoked = false;
        Dictionary<string, string>? newValue = null;

        var cut = RenderComponent<ModelOverridesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var textarea = cut.Find("textarea");

        // Act
        textarea.Input("   { \"gpt-4\": \"turbo\" }   ");

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal("turbo", newValue!["gpt-4"]);
    }
}
