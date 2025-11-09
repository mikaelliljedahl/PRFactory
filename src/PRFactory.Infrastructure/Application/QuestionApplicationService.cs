using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing questions and answers.
/// Retrieves questions from Ticket entities and combines them with answers.
/// </summary>
public class QuestionApplicationService : IQuestionApplicationService
{
    private readonly ILogger<QuestionApplicationService> _logger;
    private readonly ITicketRepository _ticketRepository;

    public QuestionApplicationService(
        ILogger<QuestionApplicationService> logger,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
    }

    /// <inheritdoc/>
    public async Task<List<QuestionWithAnswer>> GetQuestionsWithAnswersAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting questions with answers for ticket {TicketId}", ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return new List<QuestionWithAnswer>();
        }

        var result = new List<QuestionWithAnswer>();

        foreach (var question in ticket.Questions)
        {
            var answer = ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

            result.Add(new QuestionWithAnswer
            {
                Question = question,
                Answer = answer
            });
        }

        _logger.LogDebug("Found {QuestionCount} questions for ticket {TicketId}, {AnsweredCount} answered",
            result.Count, ticketId, result.Count(q => q.IsAnswered));

        return result;
    }
}
