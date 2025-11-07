using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// (Optional) Generates code implementation using Claude AI based on the approved plan.
/// This agent is only executed if the tenant configuration enables auto-implementation.
/// </summary>
public class ImplementationAgent : BaseAgent
{
    private readonly IClaudeClient _claudeClient;
    private readonly IContextBuilder _contextBuilder;
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "ImplementationAgent";
    public override string Description => "Generate code implementation using Claude AI based on approved plan";

    public ImplementationAgent(
        ILogger<ImplementationAgent> logger,
        IClaudeClient claudeClient,
        IContextBuilder contextBuilder,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _claudeClient = claudeClient ?? throw new ArgumentNullException(nameof(claudeClient));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
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

        if (string.IsNullOrEmpty(context.ImplementationPlan))
        {
            Logger.LogError("Implementation plan is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Implementation plan is required"
            };
        }

        // Check if auto-implementation is enabled
        if (!context.Tenant.Configuration.AutoImplementAfterPlanApproval)
        {
            Logger.LogInformation("Auto-implementation is disabled for tenant {TenantId}, skipping", context.TenantId);
            return new AgentResult
            {
                Status = AgentStatus.Skipped,
                Output = new Dictionary<string, object>
                {
                    ["Reason"] = "Auto-implementation disabled"
                }
            };
        }

        Logger.LogInformation("Generating code implementation for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Prepare system prompt
            var systemPrompt = @"You are an expert software developer implementing code based on an approved plan.

Generate the actual code implementation following the plan exactly. For each file:
1. Provide the complete file path
2. Provide the complete file contents
3. Ensure the code follows best practices and coding standards
4. Include appropriate error handling
5. Add inline comments for complex logic

Respond with JSON in this format:
{
  ""files"": [
    {
      ""path"": ""src/Example.cs"",
      ""content"": ""// file contents here"",
      ""action"": ""create|modify""
    }
  ]
}";

            // Build full context
            var codebaseContext = await _contextBuilder.BuildImplementationContextAsync(
                context.Ticket,
                context.RepositoryPath!
            );

            var messages = new List<Message>
            {
                new Message(
                    "user",
                    $@"Please implement the following plan:

{context.ImplementationPlan}

Codebase Context:
{codebaseContext}

Generate complete, production-ready code for all files mentioned in the plan.")
            };

            // Call Claude
            var response = await _claudeClient.SendMessageAsync(
                systemPrompt,
                messages,
                maxTokens: 16000,
                ct: cancellationToken
            );

            // Parse response
            var jsonResponse = ExtractJsonFromResponse(response);

            // Store implementation in context
            context.State["Implementation"] = response;
            context.State["ImplementationJson"] = jsonResponse;

            Logger.LogInformation("Code implementation generated for ticket {JiraKey}", context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["Implementation"] = jsonResponse,
                    ["ResponseLength"] = response.Length
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate implementation for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate implementation: {ex.Message}",
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
}
