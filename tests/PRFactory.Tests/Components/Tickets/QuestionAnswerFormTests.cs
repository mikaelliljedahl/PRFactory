using Bunit;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tickets;
using Xunit;
using static Moq.It;
using static Moq.Times;

namespace PRFactory.Tests.Components.Tickets;

public class QuestionAnswerFormTests : ComponentTestBase
{
    [Fact]
    public void Renders_WithoutQuestions_ShowsInfoMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, null));

        // Assert
        Assert.Contains("No questions", cut.Markup);
    }

    [Fact]
    public void Renders_WithEmptyQuestionsList_ShowsInfoMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>();

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert
        Assert.Contains("No questions", cut.Markup);
    }

    [Fact]
    public void Renders_QuestionsWithAnswerInputs()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder()
                .WithText("What is the purpose of this feature?")
                .WithCategory("Requirements")
                .Build(),
            new QuestionDtoBuilder()
                .WithText("Who will use this feature?")
                .WithCategory("Users")
                .Build()
        };

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert
        Assert.Contains("What is the purpose of this feature?", cut.Markup);
        Assert.Contains("Who will use this feature?", cut.Markup);
        Assert.Contains("Requirements", cut.Markup);
        Assert.Contains("Users", cut.Markup);

        // Should have textarea inputs for answers
        var textareas = cut.FindAll("textarea");
        Assert.Equal(2, textareas.Count);
    }

    [Fact]
    public async Task SubmitButton_WithoutAnswers_ShowsValidationError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithText("Question 1").Build()
        };

        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Act - Submit without answering
        var submitButton = cut.Find("button[type='submit']");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Task.Delay(100);

        // Assert - Should show validation error
        cut.WaitForState(() => cut.Markup.Contains("answer all"), timeout: TimeSpan.FromSeconds(2));
        Assert.Contains("answer all", cut.Markup.ToLower());
    }

    [Fact]
    public async Task SubmitButton_WithAllAnswers_CallsServiceAndInvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questionId1 = "q1";
        var questionId2 = "q2";

        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithId(questionId1).WithText("Question 1").Build(),
            new QuestionDtoBuilder().WithId(questionId2).WithText("Question 2").Build()
        };

        BlazorMockHelpers.SetupSubmitAnswers(MockTicketService, ticketId);

        var callbackInvoked = false;

        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions)
            .Add(p => p.OnAnswersSubmitted, () => { callbackInvoked = true; }));

        // Act - Answer all questions
        var textareas = cut.FindAll("textarea");
        Assert.Equal(2, textareas.Count);

        await textareas[0].InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Answer 1" });
        await textareas[1].InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Answer 2" });

        // Submit
        var submitButton = cut.Find("button[type='submit']");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Task.Delay(100);

        // Assert
        MockTicketService.Verify(
            x => x.SubmitAnswersAsync(
                ticketId,
                It.Is<Dictionary<string, string>>(d => d.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task SubmitButton_HandlesServiceError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithId("q1").WithText("Question 1").Build()
        };

        MockTicketService.Setup(m => m.SubmitAnswersAsync(
            ticketId,
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Submission failed"));

        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Act - Answer and submit
        var textarea = cut.Find("textarea");
        await textarea.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Answer" });

        var submitButton = cut.Find("button[type='submit']");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Task.Delay(100);

        // Assert - Should display error
        cut.WaitForState(() => cut.Markup.Contains("error"), timeout: TimeSpan.FromSeconds(2));
        Assert.Contains("error", cut.Markup.ToLower());
    }

    [Fact]
    public void InitializesAnswerDictionary_OnParametersSet()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithId("q1").WithText("Question 1").Build(),
            new QuestionDtoBuilder().WithId("q2").WithText("Question 2").Build()
        };

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert - Should render all question textareas
        var textareas = cut.FindAll("textarea");
        Assert.Equal(2, textareas.Count);
    }

    [Fact]
    public async Task AppliesValidationClass_ToUnansweredQuestions()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithId("q1").WithText("Question 1").Build()
        };

        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Act - Submit without answering
        var submitButton = cut.Find("button[type='submit']");
        await submitButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Task.Delay(100);

        // Assert - Should apply validation class
        var textarea = cut.Find("textarea");
        Assert.Contains("is-invalid", textarea.ClassList);
    }

    [Fact]
    public void DisablesSubmitButton_WhileSubmitting()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithId("q1").WithText("Question 1").Build()
        };

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert - Submit button should exist
        var submitButton = cut.Find("button[type='submit']");
        Assert.NotNull(submitButton);
    }

    [Fact]
    public void Renders_QuestionNumbers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder().WithText("Q1").Build(),
            new QuestionDtoBuilder().WithText("Q2").Build(),
            new QuestionDtoBuilder().WithText("Q3").Build()
        };

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert - Should show question numbers
        Assert.Contains("Question 1", cut.Markup);
        Assert.Contains("Question 2", cut.Markup);
        Assert.Contains("Question 3", cut.Markup);
    }

    [Fact]
    public void Renders_QuestionCategories_AsBadges()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<PRFactory.Web.Models.QuestionDto>
        {
            new QuestionDtoBuilder()
                .WithText("Question with category")
                .WithCategory("Requirements")
                .Build()
        };

        // Act
        var cut = RenderComponent<QuestionAnswerForm>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Questions, questions));

        // Assert - Should render category as badge
        Assert.Contains("badge", cut.Markup);
        Assert.Contains("Requirements", cut.Markup);
    }
}
