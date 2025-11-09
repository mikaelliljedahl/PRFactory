using PRFactory.Domain.ValueObjects;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing questions and answers.
/// Retrieves questions from Ticket value objects and combines them with answers.
/// </summary>
public interface IQuestionApplicationService
{
    /// <summary>
    /// Gets all questions for a ticket, with answer status
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of questions with their answers (if any)</returns>
    Task<List<QuestionWithAnswer>> GetQuestionsWithAnswersAsync(Guid ticketId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a question with its optional answer
/// </summary>
public class QuestionWithAnswer
{
    /// <summary>
    /// The question
    /// </summary>
    public required Question Question { get; init; }

    /// <summary>
    /// The answer (if provided)
    /// </summary>
    public Answer? Answer { get; init; }

    /// <summary>
    /// Whether the question has been answered
    /// </summary>
    public bool IsAnswered => Answer != null;
}
