using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Generates a detailed implementation plan using Claude AI.
/// Uses the full context (ticket, answers, analysis) to create a comprehensive plan.
/// </summary>
public class PlanningAgent : BaseAgent
{
    private readonly IClaudeClient _claudeClient;
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "PlanningAgent";
    public override string Description => "Generate detailed implementation plan with Claude AI";

    public PlanningAgent(
        ILogger<PlanningAgent> logger,
        IClaudeClient claudeClient,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _claudeClient = claudeClient ?? throw new ArgumentNullException(nameof(claudeClient));
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

        if (context.Analysis == null)
        {
            Logger.LogError("Codebase analysis is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Codebase analysis is required"
            };
        }

        Logger.LogInformation("Generating implementation plan for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Transition to Planning state
            var transitionResult = context.Ticket.TransitionTo(WorkflowState.Planning);
            if (!transitionResult.IsSuccess)
            {
                Logger.LogError("Failed to transition to Planning: {Error}", transitionResult.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = transitionResult.ErrorMessage
                };
            }

            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            // Prepare system prompt
            var systemPrompt = @"You are an expert software architect creating a detailed implementation plan.

Based on the ticket requirements, codebase analysis, and user answers, create a comprehensive implementation plan.

The plan should be in Markdown format and include:
1. Overview and objectives
2. Step-by-step implementation instructions
3. List of files to create/modify with specific changes
4. Testing strategy (unit tests, integration tests)
5. Potential risks and mitigation strategies
6. Dependencies and prerequisites
7. Rollback plan

Be specific and actionable. Reference actual file names and code patterns from the codebase.";

            // Build context message
            var contextMessage = BuildContextMessage(context);

            var messages = new List<Message>
            {
                new Message("user", contextMessage)
            };

            // Call Claude
            var planMarkdown = await _claudeClient.SendMessageAsync(
                systemPrompt,
                messages,
                maxTokens: 8000,
                ct: cancellationToken
            );

            // Store plan in context
            context.ImplementationPlan = planMarkdown;
            context.State["ImplementationPlan"] = planMarkdown;

            Logger.LogInformation("Implementation plan generated for ticket {JiraKey} ({Length} characters)",
                context.Ticket.TicketKey, planMarkdown.Length);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["PlanLength"] = planMarkdown.Length,
                    ["Plan"] = planMarkdown
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate plan for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate plan: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string BuildContextMessage(AgentContext context)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"# Implementation Plan Request");
        sb.AppendLine();
        sb.AppendLine($"## Ticket Information");
        sb.AppendLine($"**Key:** {context.Ticket.TicketKey}");
        sb.AppendLine($"**Title:** {context.Ticket.Title}");
        sb.AppendLine($"**Description:**");
        sb.AppendLine(context.Ticket.Description);
        sb.AppendLine();

        sb.AppendLine($"## Codebase Analysis");
        sb.AppendLine($"**Architecture:** {context.Analysis!.Architecture}");
        sb.AppendLine($"**Summary:** {context.Analysis.Summary}");
        sb.AppendLine();

        sb.AppendLine($"**Affected Files:**");
        foreach (var file in context.Analysis.AffectedFiles)
        {
            sb.AppendLine($"- {file}");
        }
        sb.AppendLine();

        sb.AppendLine($"**Technical Considerations:**");
        foreach (var consideration in context.Analysis.TechnicalConsiderations)
        {
            sb.AppendLine($"- {consideration}");
        }
        sb.AppendLine();

        // Add questions and answers
        if (context.Ticket.Questions.Any())
        {
            sb.AppendLine($"## Clarifying Questions & Answers");
            for (int i = 0; i < context.Ticket.Questions.Count; i++)
            {
                var question = context.Ticket.Questions[i];
                var answer = context.Ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

                sb.AppendLine($"**Q{i + 1} ({question.Category}):** {question.Text}");
                sb.AppendLine($"**A{i + 1}:** {answer?.Text ?? "No answer provided"}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine("Please create a detailed, actionable implementation plan in Markdown format.");

        return sb.ToString();
    }
}
