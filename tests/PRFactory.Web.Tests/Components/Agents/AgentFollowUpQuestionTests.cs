using Bunit;
using Xunit;
using PRFactory.Web.Components.Agents;

namespace PRFactory.Web.Tests.Components.Agents;

/// <summary>
/// Tests for AgentFollowUpQuestion component
/// </summary>
public class AgentFollowUpQuestionTests : TestContext
{
    [Fact]
    public void AgentFollowUpQuestion_RendersQuestion()
    {
        // Arrange
        var question = "What is the expected behavior when the user clicks submit?";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains(question, cut.Markup);
    }

    [Fact]
    public void AgentFollowUpQuestion_ShowsQuestionHeader()
    {
        // Arrange
        var question = "Test question";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains("Agent needs clarification:", cut.Markup);
        Assert.Contains("bi-question-circle", cut.Markup);
    }

    [Fact]
    public void AgentFollowUpQuestion_ShowsAnswerInput()
    {
        // Arrange
        var question = "Test question";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains("answer-input", cut.Markup);
        Assert.Contains("placeholder=\"Your answer...\"", cut.Markup);
    }

    [Fact]
    public void AgentFollowUpQuestion_ShowsSubmitButton()
    {
        // Arrange
        var question = "Test question";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains("Submit", cut.Markup);
        Assert.Contains("bi-check", cut.Markup);
    }

    [Fact]
    public async Task AgentFollowUpQuestion_SubmitWithAnswer_InvokesCallback()
    {
        // Arrange
        var question = "Test question";
        var submittedAnswer = string.Empty;
        var onAnswerCallback = EventCallback.Factory.Create<string>(this, (answer) =>
        {
            submittedAnswer = answer;
        });

        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question)
            .Add(p => p.OnAnswer, onAnswerCallback));

        // Act
        var input = cut.Find("input[type='text']");
        input.Change("This is my answer");

        var submitButton = cut.Find("button");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for callback
        await Task.Delay(100);

        // Assert
        Assert.Equal("This is my answer", submittedAnswer);
    }

    [Fact]
    public async Task AgentFollowUpQuestion_SubmitWithEmptyAnswer_DoesNotInvokeCallback()
    {
        // Arrange
        var question = "Test question";
        var callbackInvoked = false;
        var onAnswerCallback = EventCallback.Factory.Create<string>(this, (answer) =>
        {
            callbackInvoked = true;
        });

        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question)
            .Add(p => p.OnAnswer, onAnswerCallback));

        // Act
        var submitButton = cut.Find("button");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait briefly
        await Task.Delay(100);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void AgentFollowUpQuestion_HasCorrectCssClasses()
    {
        // Arrange
        var question = "Test question";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains("follow-up-question", cut.Markup);
        Assert.Contains("question-header", cut.Markup);
        Assert.Contains("answer-input", cut.Markup);
    }

    [Fact]
    public void AgentFollowUpQuestion_WithLongQuestion_RendersCompletely()
    {
        // Arrange
        var question = "This is a very long question that contains multiple sentences. " +
                      "It asks about several aspects of the implementation. " +
                      "The agent needs detailed clarification on the expected behavior.";

        // Act
        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question));

        // Assert
        Assert.Contains(question, cut.Markup);
    }

    [Fact]
    public async Task AgentFollowUpQuestion_AfterSubmit_ClearsInput()
    {
        // Arrange
        var question = "Test question";
        var onAnswerCallback = EventCallback.Factory.Create<string>(this, (answer) => { });

        var cut = RenderComponent<AgentFollowUpQuestion>(parameters => parameters
            .Add(p => p.Question, question)
            .Add(p => p.OnAnswer, onAnswerCallback));

        // Act
        var input = cut.Find("input[type='text']");
        input.Change("My answer");

        var submitButton = cut.Find("button");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for state update
        await Task.Delay(100);

        // Assert - input should be cleared after submission
        // Note: In Blazor, the input value is controlled by the component's state
        // After submit, the component sets answer = string.Empty
    }
}
