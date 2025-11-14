using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Generates a detailed implementation plan using a CLI-based AI agent.
/// Uses the full context (ticket, answers, analysis) to create a comprehensive plan.
/// Enhanced with domain-specific prompts and architectural context injection.
/// </summary>
public class PlanningAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly ITicketRepository _ticketRepository;
    private readonly IArchitectureContextService _architectureContext;

    public override string Name => "PlanningAgent";
    public override string Description => "Generate detailed implementation plan with AI";

    public PlanningAgent(
        ILogger<PlanningAgent> logger,
        ICliAgent cliAgent,
        ITicketRepository ticketRepository,
        IArchitectureContextService architectureContext)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _architectureContext = architectureContext ?? throw new ArgumentNullException(nameof(architectureContext));
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

            // Build enhanced prompt with domain template and architectural context
            var prompt = await BuildEnhancedPromptAsync(context, cancellationToken);

            Logger.LogInformation("Executing {AgentName} to generate implementation plan", _cliAgent.AgentName);

            // Call CLI agent with project context for full codebase access
            var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
                prompt,
                context.RepositoryPath!,
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

            var planMarkdown = cliResponse.Content;

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

    /// <summary>
    /// Builds an enhanced prompt with domain-specific template and architectural context.
    /// </summary>
    private async Task<string> BuildEnhancedPromptAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var sb = new System.Text.StringBuilder();

        // 1. Add domain-specific system prompt
        var domainTemplate = await LoadDomainTemplateAsync(context.Ticket.Description);
        sb.AppendLine(domainTemplate);
        sb.AppendLine();

        // 2. Add architectural context
        if (!string.IsNullOrWhiteSpace(context.RepositoryPath))
        {
            sb.AppendLine("# Architectural Context");
            sb.AppendLine();

            var archPatterns = await _architectureContext.GetArchitecturePatternsAsync(
                context.RepositoryPath, cancellationToken);
            sb.AppendLine("## Architecture Patterns");
            sb.AppendLine(archPatterns);
            sb.AppendLine();

            var techStack = _architectureContext.GetTechnologyStack();
            sb.AppendLine("## Technology Stack");
            sb.AppendLine(techStack);
            sb.AppendLine();

            var codeStyle = _architectureContext.GetCodeStyleGuidelines();
            sb.AppendLine("## Code Style Guidelines");
            sb.AppendLine(codeStyle);
            sb.AppendLine();
        }

        // 3. Add ticket information
        sb.AppendLine("# Ticket Information");
        sb.AppendLine();
        sb.AppendLine($"**Key:** {context.Ticket.TicketKey}");
        sb.AppendLine($"**Title:** {context.Ticket.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Description:**");
        sb.AppendLine(context.Ticket.Description);
        sb.AppendLine();

        // 4. Add codebase analysis
        if (context.Analysis != null)
        {
            sb.AppendLine("# Codebase Analysis");
            sb.AppendLine();
            sb.AppendLine($"**Architecture:** {context.Analysis.Architecture}");
            sb.AppendLine($"**Summary:** {context.Analysis.Summary}");
            sb.AppendLine();

            if (context.Analysis.AffectedFiles.Any())
            {
                sb.AppendLine("**Affected Files:**");
                foreach (var file in context.Analysis.AffectedFiles)
                {
                    sb.AppendLine($"- {file}");
                }
                sb.AppendLine();
            }

            if (context.Analysis.TechnicalConsiderations.Any())
            {
                sb.AppendLine("**Technical Considerations:**");
                foreach (var consideration in context.Analysis.TechnicalConsiderations)
                {
                    sb.AppendLine($"- {consideration}");
                }
                sb.AppendLine();
            }
        }

        // 5. Add code snippets
        if (!string.IsNullOrWhiteSpace(context.RepositoryPath))
        {
            var snippets = await _architectureContext.GetRelevantCodeSnippetsAsync(
                context.RepositoryPath,
                context.Ticket.Description,
                maxSnippets: 3,
                cancellationToken: cancellationToken);

            if (snippets.Any())
            {
                sb.AppendLine("# Relevant Code Examples");
                sb.AppendLine();

                foreach (var snippet in snippets)
                {
                    sb.AppendLine($"## {snippet.FilePath}");
                    if (!string.IsNullOrEmpty(snippet.Description))
                    {
                        sb.AppendLine($"**Purpose:** {snippet.Description}");
                        sb.AppendLine();
                    }
                    sb.AppendLine($"```{snippet.Language}");
                    sb.AppendLine(snippet.Code);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }
        }

        // 6. Add questions and answers
        if (context.Ticket.Questions.Any())
        {
            sb.AppendLine("# Clarifying Questions & Answers");
            sb.AppendLine();

            for (int i = 0; i < context.Ticket.Questions.Count; i++)
            {
                var question = context.Ticket.Questions[i];
                var answer = context.Ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

                sb.AppendLine($"**Q{i + 1} ({question.Category}):** {question.Text}");
                sb.AppendLine($"**A{i + 1}:** {answer?.Text ?? "No answer provided"}");
                sb.AppendLine();
            }
        }

        // 7. Final instructions
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("Please create a detailed, actionable implementation plan in Markdown format that follows the architectural patterns and code style guidelines above.");

        return sb.ToString();
    }

    /// <summary>
    /// Loads the domain-specific prompt template based on ticket description keywords.
    /// </summary>
    private async Task<string> LoadDomainTemplateAsync(string ticketDescription)
    {
        var domainTemplate = InferDomainFromDescription(ticketDescription);

        try
        {
            var templatePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "prompts", "plan", "anthropic", "domains", $"{domainTemplate}.txt");

            var normalizedPath = Path.GetFullPath(templatePath);

            if (File.Exists(normalizedPath))
            {
                Logger.LogInformation("Loading domain template: {Template}", domainTemplate);
                return await File.ReadAllTextAsync(normalizedPath);
            }
            else
            {
                Logger.LogWarning("Domain template not found at {Path}, using default", normalizedPath);
                return await LoadDefaultTemplateAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load domain template, using default");
            return await LoadDefaultTemplateAsync();
        }
    }

    /// <summary>
    /// Infers the domain type from ticket description keywords.
    /// </summary>
    private string InferDomainFromDescription(string description)
    {
        var lowerDesc = description.ToLower();

        // Check for domain-specific keywords
        if (lowerDesc.Contains("blazor") || lowerDesc.Contains("component") ||
            lowerDesc.Contains("page") || lowerDesc.Contains("ui") ||
            lowerDesc.Contains("web") || lowerDesc.Contains("frontend"))
        {
            return "web_ui";
        }

        if (lowerDesc.Contains("api") || lowerDesc.Contains("controller") ||
            lowerDesc.Contains("endpoint") || lowerDesc.Contains("rest") ||
            lowerDesc.Contains("dto"))
        {
            return "rest_api";
        }

        if (lowerDesc.Contains("database") || lowerDesc.Contains("migration") ||
            lowerDesc.Contains("entity") || lowerDesc.Contains("table") ||
            lowerDesc.Contains("schema") || lowerDesc.Contains("ef core"))
        {
            return "database";
        }

        if (lowerDesc.Contains("job") || lowerDesc.Contains("background") ||
            lowerDesc.Contains("hangfire") || lowerDesc.Contains("scheduled") ||
            lowerDesc.Contains("cron"))
        {
            return "background_jobs";
        }

        if (lowerDesc.Contains("refactor") || lowerDesc.Contains("clean up") ||
            lowerDesc.Contains("improve") || lowerDesc.Contains("restructure"))
        {
            return "refactoring";
        }

        // Default to web_ui as it's the most common
        return "web_ui";
    }

    /// <summary>
    /// Loads the default system template as fallback.
    /// </summary>
    private async Task<string> LoadDefaultTemplateAsync()
    {
        try
        {
            var templatePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "prompts", "plan", "anthropic", "system.txt");

            var normalizedPath = Path.GetFullPath(templatePath);

            if (File.Exists(normalizedPath))
            {
                return await File.ReadAllTextAsync(normalizedPath);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load default template");
        }

        // Hardcoded fallback if files are missing
        return @"You are an expert software architect creating a detailed implementation plan.

Create a comprehensive implementation plan in Markdown format that includes:
1. Overview and objectives
2. Step-by-step implementation instructions
3. Files to create/modify
4. Testing strategy
5. Potential risks and mitigation
6. Dependencies and prerequisites
7. Rollback plan

Be specific and actionable.";
    }
}
