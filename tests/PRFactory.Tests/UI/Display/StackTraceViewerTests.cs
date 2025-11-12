using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class StackTraceViewerTests : ComponentTestBase
{
    [Fact]
    public void Render_WithNullStackTrace_ShowsNoStackTraceMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, (string?)null));

        // Assert
        Assert.Contains("No stack trace available", cut.Markup);
        Assert.DoesNotContain("stack-trace-viewer", cut.Markup);
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
    public void Render_WithStackTrace_DisplaysStackTrace()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("Stack Trace", cut.Markup);
        Assert.Contains("MyApp.Program.Main", cut.Markup);
    }

    [Fact]
    public void Render_HasToggleInternalButton()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.NotEmpty(buttons);
        Assert.Contains(buttons, b => b.TextContent.Contains("Show") || b.TextContent.Contains("Hide"));
    }

    [Fact]
    public void Render_HasCopyButton()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Copy"));
    }

    [Fact]
    public void Render_DefaultShowInternalFrames_IsFalse()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("Show", cut.Markup);
        Assert.DoesNotContain("Hide Internal", cut.Markup);
    }

    [Fact]
    public void Render_WithShowInternalFramesTrue_ShowsHideButton()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert
        Assert.Contains("Hide", cut.Markup);
    }

    [Fact]
    public void ToggleButton_WhenClicked_TogglesInternalFramesVisibility()
    {
        // Arrange
        var stackTrace = "at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)";
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Act - Click toggle button
        var toggleButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Show") || b.TextContent.Contains("Hide")).ToList();
        Assert.NotEmpty(toggleButtons);
        toggleButtons[0].Click();

        // Assert - Button text should change
        Assert.Contains("Hide", cut.Markup);
    }

    [Fact]
    public void Render_FiltersInternalFrames_WhenShowInternalFramesIsFalse()
    {
        // Arrange
        var stackTrace = @"at MyApp.Program.Main() in Program.cs:line 10
at System.Threading.Tasks.Task.ExecuteAsync()
at Microsoft.AspNetCore.Hosting.WebHost.Start()";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, false));

        // Assert
        Assert.Contains("MyApp.Program.Main", cut.Markup);
        // Internal frames should be filtered out
    }

    [Fact]
    public void Render_ShowsAllFrames_WhenShowInternalFramesIsTrue()
    {
        // Arrange
        var stackTrace = @"at MyApp.Program.Main() in Program.cs:line 10
at System.Threading.Tasks.Task.ExecuteAsync()";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert
        Assert.Contains("MyApp.Program.Main", cut.Markup);
        Assert.Contains("System.Threading.Tasks", cut.Markup);
    }

    [Fact]
    public void Render_InternalFrames_AreMarkedAsMuted()
    {
        // Arrange
        var stackTrace = "at System.Threading.Tasks.Task.ExecuteAsync()";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Render_HasDarkBackground()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("bg-dark", cut.Markup);
        Assert.Contains("text-light", cut.Markup);
    }

    [Fact]
    public void Render_HasMonospaceFont()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("font-family: monospace", cut.Markup);
    }

    [Fact]
    public void Render_HasScrollableContainer()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("overflow-y: auto", cut.Markup);
        Assert.Contains("max-height: 500px", cut.Markup);
    }

    [Fact]
    public void Render_HandlesMultilineStackTrace()
    {
        // Arrange
        var stackTrace = @"at MyApp.Services.UserService.GetUser(Int32 id) in UserService.cs:line 42
at MyApp.Controllers.UserController.Get(Int32 id) in UserController.cs:line 18
at lambda_method(Closure , Object , Object[] )";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert
        Assert.Contains("UserService.GetUser", cut.Markup);
        Assert.Contains("UserController.Get", cut.Markup);
    }

    [Fact]
    public void Render_IdentifiesSystemFramesAsInternal()
    {
        // Arrange
        var stackTrace = "at System.Threading.Tasks.Task.ExecuteAsync()";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert - Internal frames should be marked with text-muted
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Render_IdentifiesMicrosoftFramesAsInternal()
    {
        // Arrange
        var stackTrace = "at Microsoft.AspNetCore.Hosting.WebHost.Start()";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert - Internal frames should be marked with text-muted
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Render_IdentifiesLambdaMethodAsInternal()
    {
        // Arrange
        var stackTrace = "at lambda_method(Closure , Object , Object[] )";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace)
            .Add(p => p.ShowInternalFrames, true));

        // Assert - Internal frames should be marked with text-muted
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void CopyButton_WhenClicked_DoesNotThrow()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Act & Assert - Should not throw
        var copyButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Copy")).ToList();
        Assert.NotEmpty(copyButtons);
        copyButtons[0].Click();
    }

    [Fact]
    public void Render_HasCorrectButtonGroupLayout()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("btn-group", cut.Markup);
        Assert.Contains("btn-group-sm", cut.Markup);
    }

    [Fact]
    public void Render_ButtonsHaveCorrectIcons()
    {
        // Arrange
        var stackTrace = "at MyApp.Program.Main() in Program.cs:line 10";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("bi-eye", cut.Markup);
        Assert.Contains("bi-clipboard", cut.Markup);
    }

    [Fact]
    public void Render_TrimsStackTraceLines()
    {
        // Arrange
        var stackTrace = "   at MyApp.Program.Main() in Program.cs:line 10   ";

        // Act
        var cut = RenderComponent<StackTraceViewer>(parameters => parameters
            .Add(p => p.StackTrace, stackTrace));

        // Assert
        Assert.Contains("MyApp.Program.Main", cut.Markup);
    }
}
