using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// (Optional) Generates code implementation using a CLI-based AI agent based on the approved plan.
/// This agent is only executed if the tenant configuration enables auto-implementation.
/// </summary>
public class ImplementationAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IContextBuilder _contextBuilder;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILocalGitService _localGitService;
    private readonly IWorkspaceService _workspaceService;

    public override string Name => "ImplementationAgent";
    public override string Description => "Generate code implementation using AI based on approved plan";

    public ImplementationAgent(
        ILogger<ImplementationAgent> logger,
        ICliAgent cliAgent,
        IContextBuilder contextBuilder,
        ITicketRepository ticketRepository,
        ILocalGitService localGitService,
        IWorkspaceService workspaceService)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _localGitService = localGitService ?? throw new ArgumentNullException(nameof(localGitService));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
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
            // Build full context
            var codebaseContext = await _contextBuilder.BuildImplementationContextAsync(
                context.Ticket,
                context.RepositoryPath!
            );

            // Build combined prompt for CLI agent
            var prompt = $@"You are an expert software developer implementing code based on an approved plan.

Generate the actual code implementation following the plan exactly. For each file:
1. Provide the complete file path
2. Provide the complete file contents
3. Ensure the code follows best practices and coding standards
4. Include appropriate error handling
5. Add inline comments for complex logic

Respond with JSON in this format:
{{
  ""files"": [
    {{
      ""path"": ""src/Example.cs"",
      ""content"": ""// file contents here"",
      ""action"": ""create|modify""
    }}
  ]
}}

Please implement the following plan:

{context.ImplementationPlan}

Codebase Context:
{codebaseContext}

Generate complete, production-ready code for all files mentioned in the plan.";

            Logger.LogInformation("Executing {AgentName} to generate code implementation", _cliAgent.AgentName);

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

            var response = cliResponse.Content;

            // Parse response
            var jsonResponse = ExtractJsonFromResponse(response);

            // Store implementation in context
            context.State["Implementation"] = response;
            context.State["ImplementationJson"] = jsonResponse;

            Logger.LogInformation("Code implementation generated for ticket {JiraKey}", context.Ticket.TicketKey);

            // Generate diff after code implementation
            await GenerateDiffAsync(context.Ticket.Id, context.RepositoryPath!);

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

    private async Task GenerateDiffAsync(Guid ticketId, string repoPath)
    {
        Logger.LogInformation("Generating diff for ticket {TicketId} from repository {RepoPath}",
            ticketId, repoPath);

        try
        {
            // Get diff from HEAD (includes uncommitted changes + last commit)
            var diffContent = await _localGitService.GetDiffAsync(
                repoPath: repoPath,
                filePath: null,  // All files
                baseBranch: "main",  // Compare against main branch
                compareBranch: null  // HEAD (current branch)
            );

            if (string.IsNullOrEmpty(diffContent))
            {
                Logger.LogWarning("Generated diff is empty for ticket {TicketId}", ticketId);
                // Continue anyway - might be no changes, or issue with diff generation
            }
            else
            {
                Logger.LogInformation("Generated diff for ticket {TicketId}: {Size} bytes",
                    ticketId, diffContent.Length);

                // Save diff to workspace
                await _workspaceService.WriteDiffAsync(ticketId, diffContent);
                Logger.LogInformation("Saved diff to workspace for ticket {TicketId}", ticketId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating diff for ticket {TicketId}", ticketId);
            // Don't fail the entire agent - diff generation is secondary to code generation
            // Continue with workflow, user can still create PR without diff preview
        }
    }
}
