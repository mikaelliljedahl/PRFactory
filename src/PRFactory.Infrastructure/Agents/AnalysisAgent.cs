using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using System.Text.Json;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Analyzes the codebase using a CLI-based AI agent.
/// Uses ICliAgent and ContextBuilder to understand the repository structure
/// and generate an analysis relevant to the ticket requirements.
/// </summary>
public class AnalysisAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IContextBuilder _contextBuilder;

    public override string Name => "AnalysisAgent";
    public override string Description => "Analyze codebase with AI to understand architecture and requirements";

    public AnalysisAgent(
        ILogger<AnalysisAgent> logger,
        ICliAgent cliAgent,
        IContextBuilder contextBuilder)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.RepositoryPath))
        {
            Logger.LogError("Repository path is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Repository must be cloned before analysis"
            };
        }

        if (context.Ticket == null)
        {
            Logger.LogError("Ticket entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Ticket entity is required"
            };
        }

        Logger.LogInformation("Starting codebase analysis for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Build context from repository
            var codebaseContext = await _contextBuilder.BuildAnalysisContextAsync(
                context.Ticket,
                context.RepositoryPath!
            );

            // Build combined prompt for CLI agent
            var prompt = $@"You are an expert software architect analyzing a codebase to understand how to implement a new feature.

Your task is to analyze the provided codebase and ticket requirements, then provide:
1. A summary of the codebase architecture
2. List of files that will likely be affected by this change
3. Technical considerations for implementation
4. Any potential risks or challenges

Respond with JSON in this format:
{{
  ""summary"": ""Brief architecture summary"",
  ""affectedFiles"": [""file1.cs"", ""file2.cs""],
  ""technicalConsiderations"": [""consideration 1"", ""consideration 2""],
  ""architecture"": ""Detailed architecture description""
}}

Ticket: {context.Ticket.TicketKey}
Title: {context.Ticket.Title}
Description: {context.Ticket.Description}

Codebase Context:
{codebaseContext}";

            Logger.LogInformation("Executing {AgentName} analysis with project context", _cliAgent.AgentName);

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

            // Parse JSON response
            var analysisJson = ExtractJsonFromResponse(response);
            var analysis = JsonSerializer.Deserialize<CodebaseAnalysisDto>(analysisJson);

            if (analysis == null)
            {
                Logger.LogError("Failed to parse analysis response from Claude");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Failed to parse analysis response"
                };
            }

            // Create CodebaseAnalysis object
            var codebaseAnalysis = new CodebaseAnalysis
            {
                Summary = analysis.Summary,
                AffectedFiles = analysis.AffectedFiles,
                TechnicalConsiderations = analysis.TechnicalConsiderations,
                Architecture = analysis.Architecture,
                AnalyzedAt = DateTime.UtcNow
            };

            // Store in context
            context.Analysis = codebaseAnalysis;
            context.State["Analysis"] = codebaseAnalysis;

            // Store analysis in context (Ticket entity doesn't have SetCodebaseAnalysis method yet)
            // TODO: Add SetCodebaseAnalysis method to Ticket entity or store in dedicated table
            // context.Ticket.SetCodebaseAnalysis(codebaseAnalysis.Summary, codebaseAnalysis.AffectedFiles);

            Logger.LogInformation("Codebase analysis completed for ticket {JiraKey}", context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["Analysis"] = codebaseAnalysis,
                    ["Summary"] = codebaseAnalysis.Summary
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to analyze codebase for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to analyze codebase: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Try to find JSON block in markdown code fence
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

        // Try to find raw JSON
        jsonStart = response.IndexOf('{');
        var jsonEnd2 = response.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd2 > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd2 - jsonStart + 1);
        }

        return response;
    }

    private class CodebaseAnalysisDto
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> AffectedFiles { get; set; } = new();
        public List<string> TechnicalConsiderations { get; set; } = new();
        public string Architecture { get; set; } = string.Empty;
    }
}
