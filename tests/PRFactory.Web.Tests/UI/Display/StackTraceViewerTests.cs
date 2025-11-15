using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for StackTraceViewer component
/// </summary>
public class StackTraceViewerTests : TestContext
{
    private const string SampleStackTrace = @"at MyApp.Controllers.HomeController.Index()
at System.Web.Mvc.ActionMethodDispatcher.Execute()
at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.Execute()
at MyApp.Services.DataService.GetData()";

    [Fact]
    public void Render_WithStackTrace_DisplaysStackTrace()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        Assert.Contains("MyApp.Controllers.HomeController", cut.Markup);
    }

    [Fact]
    public void Render_WithNullStackTrace_ShowsNoStackTraceMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, null));

        // Assert
        Assert.Contains("No stack trace available", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyStackTrace_ShowsNoStackTraceMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, ""));

        // Assert
        Assert.Contains("No stack trace available", cut.Markup);
    }

    [Fact]
    public void Render_WithWhitespaceStackTrace_ShowsNoStackTraceMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, "   "));

        // Assert
        Assert.Contains("No stack trace available", cut.Markup);
    }

    [Fact]
    public void Render_HasMonospaceFontStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        Assert.Contains("font-family: monospace", cut.Markup);
    }

    [Fact]
    public void Render_HasToggleInternalFramesButton()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Show Internal") || b.TextContent.Contains("Hide Internal"));
    }

    [Fact]
    public void Render_HasCopyButton()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Copy"));
    }

    [Fact]
    public void ToggleInternalFrames_Click_ChangesButtonText()
    {
        // Arrange
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace)
            .Add(p => p.ShowInternalFrames, false));

        // Act
        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show") || b.TextContent.Contains("Hide"));
        var initialText = toggleButton.TextContent;
        toggleButton.Click();

        // Assert
        var updatedButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show") || b.TextContent.Contains("Hide"));
        Assert.NotEqual(initialText, updatedButton.TextContent);
    }

    [Fact]
    public void Render_WhenShowInternalFramesFalse_FiltersInternalFrames()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace)
            .Add(p => p.ShowInternalFrames, false));

        // Assert
        Assert.Contains("MyApp.Controllers", cut.Markup);
        Assert.Contains("MyApp.Services", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowInternalFramesTrue_ShowsAllFrames()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert
        Assert.Contains("System.Web.Mvc", cut.Markup);
        Assert.Contains("Microsoft.AspNetCore", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_HidesInternalFrames()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show") || b.TextContent.Contains("Hide"));
        Assert.Contains("Show", toggleButton.TextContent);
    }

    [Fact]
    public void Render_AppliesDarkBackgroundAndLightText()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, SampleStackTrace));

        // Assert
        Assert.Contains("bg-dark", cut.Markup);
        Assert.Contains("text-light", cut.Markup);
    }
}
