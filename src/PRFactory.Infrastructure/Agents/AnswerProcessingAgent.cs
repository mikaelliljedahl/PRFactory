using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using System.Text.RegularExpressions;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Processes user answers from Jira comments.
/// Parses the comment text, validates completeness, and adds answers to the Ticket entity.
/// </summary>
public class AnswerProcessingAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "AnswerProcessingAgent";
    public override string Description => "Process and validate user answers from Jira comments";

    public AnswerProcessingAgent(
        ILogger<AnswerProcessingAgent> logger,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Ticket == null)
        {
            Logger.LogError("Ticket entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Ticket entity is required"
            };
        }

        // Get the answer text from metadata (populated by webhook handler)
        if (!context.Metadata.ContainsKey("AnswerText"))
        {
            Logger.LogError("AnswerText not found in context metadata");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "AnswerText is required in metadata"
            };
        }

        var answerText = context.Metadata["AnswerText"].ToString()!;

        Logger.LogInformation("Processing answers for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Parse answers from the comment text
            var parsedAnswers = ParseAnswers(answerText);

            if (!parsedAnswers.Any())
            {
                Logger.LogWarning("No answers found in the comment text");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "No answers found in comment. Please use format: Q1: [answer], Q2: [answer], etc."
                };
            }

            // Validate that all questions have been answered
            var questions = context.Ticket.Questions.ToList();
            var unansweredQuestions = new List<string>();

            foreach (var question in questions)
            {
                var questionNumber = questions.IndexOf(question) + 1;
                if (!parsedAnswers.ContainsKey($"Q{questionNumber}") && !parsedAnswers.ContainsKey(question.Id))
                {
                    unansweredQuestions.Add($"Q{questionNumber}");
                }
            }

            if (unansweredQuestions.Any())
            {
                Logger.LogWarning("Not all questions were answered. Missing: {Missing}",
                    string.Join(", ", unansweredQuestions));
                // Continue anyway but log the warning
            }

            // Add answers to ticket
            var answersAdded = 0;
            foreach (var question in questions)
            {
                var questionNumber = questions.IndexOf(question) + 1;
                var questionKey = $"Q{questionNumber}";

                if (parsedAnswers.TryGetValue(questionKey, out var answer))
                {
                    context.Ticket.AddAnswer(question.Id, answer);
                    answersAdded++;
                }
            }

            // Transition to AnswersReceived state
            var transitionResult = context.Ticket.TransitionTo(WorkflowState.AnswersReceived);
            if (!transitionResult.IsSuccess)
            {
                Logger.LogError("Failed to transition to AnswersReceived: {Error}", transitionResult.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = transitionResult.ErrorMessage
                };
            }

            // Update ticket
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            Logger.LogInformation("Processed {Count} answers for ticket {JiraKey}",
                answersAdded, context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["AnswersProcessed"] = answersAdded,
                    ["TotalQuestions"] = questions.Count,
                    ["Completeness"] = (double)answersAdded / questions.Count
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process answers for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to process answers: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private Dictionary<string, string> ParseAnswers(string answerText)
    {
        var answers = new Dictionary<string, string>();

        // Pattern: Q1: answer text
        // or Q1. answer text
        // or 1: answer text
        // or 1. answer text
        var pattern = @"Q?(\d+)[\.\:]\s*(.+?)(?=Q?\d+[\.\:]|$)";
        var matches = Regex.Matches(answerText, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var questionNumber = match.Groups[1].Value;
                var answer = match.Groups[2].Value.Trim();

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    answers[$"Q{questionNumber}"] = answer;
                }
            }
        }

        // If the pattern above didn't work, try a simpler line-by-line approach
        if (!answers.Any())
        {
            var lines = answerText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var currentQuestion = "";
            var currentAnswer = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check if line starts with Q1:, Q2:, etc.
                var questionMatch = Regex.Match(trimmedLine, @"^Q?(\d+)[\.\:](.*)", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (questionMatch.Success)
                {
                    // Save previous answer if exists
                    if (!string.IsNullOrEmpty(currentQuestion) && currentAnswer.Length > 0)
                    {
                        answers[currentQuestion] = currentAnswer.ToString().Trim();
                    }

                    // Start new question
                    currentQuestion = $"Q{questionMatch.Groups[1].Value}";
                    currentAnswer = new System.Text.StringBuilder();
                    currentAnswer.AppendLine(questionMatch.Groups[2].Value.Trim());
                }
                else if (!string.IsNullOrEmpty(currentQuestion))
                {
                    // Continue current answer
                    currentAnswer.AppendLine(trimmedLine);
                }
            }

            // Save last answer
            if (!string.IsNullOrEmpty(currentQuestion) && currentAnswer.Length > 0)
            {
                answers[currentQuestion] = currentAnswer.ToString().Trim();
            }
        }

        return answers;
    }
}
