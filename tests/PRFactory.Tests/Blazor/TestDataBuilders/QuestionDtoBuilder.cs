using PRFactory.Web.Models;

namespace PRFactory.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating QuestionDto instances for testing
/// </summary>
public class QuestionDtoBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _text = "Sample question?";
    private string _category = "requirements";
    private DateTime _createdAt = DateTime.UtcNow;
    private bool _isAnswered = false;
    private string? _answerText = null;
    private DateTime? _answeredAt = null;

    public QuestionDtoBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public QuestionDtoBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    public QuestionDtoBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public QuestionDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public QuestionDtoBuilder AsAnswered(string answerText, DateTime? answeredAt = null)
    {
        _isAnswered = true;
        _answerText = answerText;
        _answeredAt = answeredAt ?? DateTime.UtcNow;
        return this;
    }

    public QuestionDtoBuilder AsUnanswered()
    {
        _isAnswered = false;
        _answerText = null;
        _answeredAt = null;
        return this;
    }

    public QuestionDto Build()
    {
        return new QuestionDto
        {
            Id = _id,
            Text = _text,
            Category = _category,
            CreatedAt = _createdAt,
            IsAnswered = _isAnswered,
            AnswerText = _answerText,
            AnsweredAt = _answeredAt
        };
    }

    /// <summary>
    /// Creates a requirements question
    /// </summary>
    public static QuestionDtoBuilder RequirementsQuestion()
    {
        return new QuestionDtoBuilder()
            .WithCategory("requirements")
            .WithText("What are the specific requirements for this feature?");
    }

    /// <summary>
    /// Creates a technical question
    /// </summary>
    public static QuestionDtoBuilder TechnicalQuestion()
    {
        return new QuestionDtoBuilder()
            .WithCategory("technical")
            .WithText("What technology stack should be used?");
    }

    /// <summary>
    /// Creates a testing question
    /// </summary>
    public static QuestionDtoBuilder TestingQuestion()
    {
        return new QuestionDtoBuilder()
            .WithCategory("testing")
            .WithText("What test scenarios should be covered?");
    }

    /// <summary>
    /// Creates an answered question
    /// </summary>
    public static QuestionDtoBuilder Answered()
    {
        return new QuestionDtoBuilder()
            .AsAnswered("This is the answer to the question");
    }

    /// <summary>
    /// Creates an unanswered question
    /// </summary>
    public static QuestionDtoBuilder Unanswered()
    {
        return new QuestionDtoBuilder()
            .AsUnanswered();
    }
}
