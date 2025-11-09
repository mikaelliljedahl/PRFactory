using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using System.Text.Json;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Generates clarifying questions using a CLI-based AI agent.
/// Analyzes the ticket and codebase to generate 3-7 questions that will help
/// ensure the implementation meets requirements.
/// </summary>
public class QuestionGenerationAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public override string Name => "QuestionGenerationAgent";
    public override string Description => "Generate clarifying questions to ensure implementation requirements are clear";

    public QuestionGenerationAgent(
        ILogger<QuestionGenerationAgent> logger,
        ICliAgent cliAgent)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
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

        if (context.Analysis == null)
        {
            Logger.LogError("Codebase analysis is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Codebase analysis must be completed before generating questions"
            };
        }

        Logger.LogInformation("Generating questions for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Build combined prompt for CLI agent
            var prompt = $@"You are an expert software architect helping to clarify requirements for a software implementation.

Based on the ticket description and codebase analysis, generate 3-7 clarifying questions that will help ensure the implementation meets the requirements.

Focus on:
- Functional requirements that aren't clear
- Technical implementation choices
- Edge cases and error handling
- Testing requirements
- Non-functional requirements (performance, security, etc.)

Respond with JSON in this format:
{{
  ""questions"": [
    {{
      ""text"": ""Question text here?"",
      ""category"": ""requirements|technical|testing""
    }}
  ]
}}

Ticket: {context.Ticket.TicketKey}
Title: {context.Ticket.Title}
Description: {context.Ticket.Description}

Codebase Analysis:
{context.Analysis.Summary}

Affected Files:
{string.Join("\n", context.Analysis.AffectedFiles)}

Technical Considerations:
{string.Join("\n", context.Analysis.TechnicalConsiderations)}

Please generate 3-7 clarifying questions to ensure the implementation is well-defined.";

            Logger.LogInformation("Executing {AgentName} to generate questions", _cliAgent.AgentName);

            // Call CLI agent (doesn't need project context, just the analysis)
            var cliResponse = await _cliAgent.ExecutePromptAsync(
                prompt,
                cancellationToken
            );

            if (!cliResponse.Success)
            {
                Logger.LogError("CLI agent execution failed: {Error}", cliResponse.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"CLI agent execution failed: {cliResponse.ErrorMessage}"
                };
            }

            var response = cliResponse.Content;

            // Parse response
            var jsonResponse = ExtractJsonFromResponse(response);
            var questionsDto = JsonSerializer.Deserialize<QuestionsResponseDto>(jsonResponse);

            if (questionsDto?.Questions == null || !questionsDto.Questions.Any())
            {
                Logger.LogError("Failed to parse questions from Claude response");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Failed to parse questions from response"
                };
            }

            // Validate we have 3-7 questions
            if (questionsDto.Questions.Count < 3)
            {
                Logger.LogWarning("Generated only {Count} questions, expected at least 3", questionsDto.Questions.Count);
            }
            else if (questionsDto.Questions.Count > 7)
            {
                Logger.LogInformation("Generated {Count} questions, truncating to 7", questionsDto.Questions.Count);
                questionsDto.Questions = questionsDto.Questions.Take(7).ToList();
            }

            // Add questions to ticket
            foreach (var questionDto in questionsDto.Questions)
            {
                var question = new Question
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = questionDto.Text,
                    Category = questionDto.Category,
                    CreatedAt = DateTime.UtcNow
                };

                context.Ticket.AddQuestion(question);
            }

            // Store in context
            context.State["Questions"] = questionsDto.Questions;

            Logger.LogInformation("Generated {Count} questions for ticket {JiraKey}",
                questionsDto.Questions.Count, context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["QuestionCount"] = questionsDto.Questions.Count,
                    ["Questions"] = questionsDto.Questions
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate questions for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate questions: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        var jsonStart = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (jsonStart >= 0)
        {
            jsonStart = response.IndexOf('{', jsonStart);
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
        }

        jsonStart = response.IndexOf('{');
        var jsonEnd2 = response.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd2 > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd2 - jsonStart + 1);
        }

        return response;
    }

    private class QuestionsResponseDto
    {
        public List<QuestionDto> Questions { get; set; } = new();
    }

    private class QuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
