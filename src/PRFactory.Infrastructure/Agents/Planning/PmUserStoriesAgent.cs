using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Product Manager agent that generates user stories with acceptance criteria
/// from ticket requirements and Q&A context.
/// </summary>
public class PmUserStoriesAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public override string Name => "PM User Stories Agent";
    public override string Description => "Generates user stories with acceptance criteria using Product Manager persona";

    public PmUserStoriesAgent(
        ILogger<PmUserStoriesAgent> logger,
        ICliAgent cliAgent)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Validate context
        ValidateContext(context);

        Logger.LogInformation("Generating user stories for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Build comprehensive prompt
            var prompt = BuildUserStoriesPrompt(context);

            // Call LLM via ICliAgent
            var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
                prompt,
                context.RepositoryPath!,
                cancellationToken);

            if (!cliResponse.Success)
            {
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"User stories generation failed: {cliResponse.ErrorMessage}"
                };
            }

            // Extract and validate user stories
            var userStories = ExtractUserStories(cliResponse.Content);
            ValidateUserStoriesFormat(userStories);

            // Store in context
            context.State["UserStories"] = userStories;

            Logger.LogInformation(
                "User stories generated successfully for ticket {TicketKey}. Story count: {StoryCount}",
                context.Ticket.TicketKey,
                CountStories(userStories));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["UserStories"] = userStories,
                    ["StoryCount"] = CountStories(userStories),
                    ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate user stories for ticket {TicketKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate user stories: {ex.Message}"
            };
        }
    }

    private void ValidateContext(AgentContext context)
    {
        if (context.Ticket == null)
        {
            throw new InvalidOperationException("Ticket is required in context");
        }

        if (string.IsNullOrWhiteSpace(context.RepositoryPath))
        {
            throw new InvalidOperationException("RepositoryPath is required in context");
        }

        if (context.Analysis == null)
        {
            Logger.LogWarning("CodebaseAnalysis is missing from context. Proceeding without architecture insights.");
        }
    }

    private string BuildUserStoriesPrompt(AgentContext context)
    {
        var ticket = context.Ticket;
        var analysis = context.Analysis;

        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a Product Manager analyzing a ticket and writing user stories.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<role>");
        promptBuilder.AppendLine("Your role is to:");
        promptBuilder.AppendLine("1. Understand user needs from the ticket description");
        promptBuilder.AppendLine("2. Break down requirements into clear user stories");
        promptBuilder.AppendLine("3. Define acceptance criteria for each story");
        promptBuilder.AppendLine("4. Identify edge cases and non-functional requirements");
        promptBuilder.AppendLine("</role>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<ticket>");
        promptBuilder.AppendLine($"Key: {ticket.TicketKey}");
        promptBuilder.AppendLine($"Title: {ticket.Title}");
        promptBuilder.AppendLine("Description:");
        promptBuilder.AppendLine(ticket.Description);
        promptBuilder.AppendLine();

        // Include refined description if available
        var refinedDescription = ticket.GetMetadata<string>("RefinedDescription");
        if (!string.IsNullOrWhiteSpace(refinedDescription))
        {
            promptBuilder.AppendLine("Refined Requirements:");
            promptBuilder.AppendLine(refinedDescription);
            promptBuilder.AppendLine();
        }

        // Include Q&A context
        promptBuilder.AppendLine("Q&A Context:");
        promptBuilder.AppendLine(FormatQuestionsAndAnswers(ticket));
        promptBuilder.AppendLine("</ticket>");
        promptBuilder.AppendLine();

        if (analysis != null)
        {
            promptBuilder.AppendLine("<codebase_analysis>");
            promptBuilder.AppendLine($"Architecture: {analysis.Architecture}");
            if (analysis.AffectedFiles != null && analysis.AffectedFiles.Count > 0)
            {
                promptBuilder.AppendLine($"Affected Files: {string.Join(", ", analysis.AffectedFiles)}");
            }
            if (analysis.TechnicalConsiderations != null && analysis.TechnicalConsiderations.Count > 0)
            {
                promptBuilder.AppendLine("Technical Considerations:");
                foreach (var consideration in analysis.TechnicalConsiderations)
                {
                    promptBuilder.AppendLine($"- {consideration}");
                }
            }
            promptBuilder.AppendLine("</codebase_analysis>");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("Generate user stories in the following format:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("# User Stories");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Story 1: [Story Title]");
        promptBuilder.AppendLine("**As a** [persona (user, admin, developer, API consumer, etc.)]");
        promptBuilder.AppendLine("**I want** [feature/capability]");
        promptBuilder.AppendLine("**So that** [benefit/value]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Acceptance Criteria");
        promptBuilder.AppendLine("- [ ] Criterion 1 (specific, measurable, testable)");
        promptBuilder.AppendLine("- [ ] Criterion 2");
        promptBuilder.AppendLine("- [ ] Criterion 3");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Edge Cases");
        promptBuilder.AppendLine("- Edge case 1");
        promptBuilder.AppendLine("- Edge case 2");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Story 2: [Story Title]");
        promptBuilder.AppendLine("...");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Non-Functional Requirements");
        promptBuilder.AppendLine("- Performance requirements (e.g., response time < 200ms)");
        promptBuilder.AppendLine("- Security requirements (e.g., authentication, authorization, data encryption)");
        promptBuilder.AppendLine("- Scalability considerations (e.g., handle 10,000 concurrent users)");
        promptBuilder.AppendLine("- Accessibility requirements (e.g., WCAG 2.1 AA compliance)");
        promptBuilder.AppendLine("- Observability requirements (e.g., logging, metrics, tracing)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Output ONLY the markdown content (no preamble or explanation).");

        return promptBuilder.ToString();
    }

    private string ExtractUserStories(string cliResponse)
    {
        // Strategy 1: Look for markdown heading
        var lines = cliResponse.Split('\n');
        var inContent = false;
        var contentLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("# User Stories", StringComparison.OrdinalIgnoreCase))
            {
                inContent = true;
            }

            if (inContent)
            {
                contentLines.Add(line);
            }
        }

        if (contentLines.Count > 0)
        {
            return string.Join("\n", contentLines).Trim();
        }

        // Strategy 2: Look for markdown code block
        var codeBlockPattern = @"```markdown\s*(.*?)\s*```";
        var match = Regex.Match(cliResponse, codeBlockPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Fallback: Use full response
        return cliResponse.Trim();
    }

    private void ValidateUserStoriesFormat(string userStories)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(userStories))
        {
            throw new InvalidOperationException("Generated user stories are empty");
        }

        // Check for required sections
        if (!userStories.Contains("# User Stories", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("User stories missing main heading");
        }

        // Check for at least one story
        if (!userStories.Contains("**As a**", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No user stories found in expected format");
        }
    }

    private int CountStories(string userStories)
    {
        return Regex.Matches(userStories, @"\*\*As a\*\*", RegexOptions.IgnoreCase).Count;
    }

    private string FormatQuestionsAndAnswers(Ticket ticket)
    {
        if (ticket.Questions == null || ticket.Questions.Count == 0)
            return "No Q&A available";

        var formatted = new StringBuilder();
        foreach (var question in ticket.Questions)
        {
            formatted.AppendLine($"Q: {question.Text}");

            var answer = ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            formatted.AppendLine($"A: {answer?.Text ?? "Not answered"}");
            formatted.AppendLine();
        }
        return formatted.ToString();
    }
}
