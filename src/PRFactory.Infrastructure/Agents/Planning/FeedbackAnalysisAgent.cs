using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Core.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Analyzes user feedback on rejected plans to determine which artifacts need regeneration.
/// Uses LLM to understand intent rather than simple keyword matching.
/// </summary>
public class FeedbackAnalysisAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public override string Name => "Feedback Analysis Agent";
    public override string Description => "Analyzes revision feedback to determine which artifacts need updating";

    public FeedbackAnalysisAgent(
        ICliAgent cliAgent,
        ILogger<FeedbackAnalysisAgent> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(cliAgent);
        _cliAgent = cliAgent;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Get the rejection message from context
        var rejectionMessage = context.State.GetValueOrDefault("RejectionMessage") as PlanRejectedMessage
            ?? throw new InvalidOperationException("RejectionMessage not found in context");

        if (string.IsNullOrWhiteSpace(rejectionMessage.Reason))
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Feedback is empty or whitespace"
            };
        }

        Logger.LogInformation(
            "Analyzing feedback for ticket {TicketKey}: {FeedbackLength} chars",
            context.Ticket.TicketKey,
            rejectionMessage.Reason.Length);

        try
        {
            // Build analysis prompt
            var prompt = BuildAnalysisPrompt(rejectionMessage.Reason);

            // Call LLM to analyze feedback
            var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
                prompt,
                context.RepositoryPath!,
                cancellationToken);

            if (!cliResponse.Success)
            {
                Logger.LogWarning(
                    "Feedback analysis failed: {Error}",
                    cliResponse.ErrorMessage);

                // Fallback: regenerate all artifacts on analysis failure
                return new AgentResult
                {
                    Status = AgentStatus.Completed,
                    Output = new Dictionary<string, object>
                    {
                        ["AffectedArtifacts"] = GetAllArtifactTypes(),
                        ["AnalysisSummary"] = "Analysis failed - regenerating all artifacts as precaution",
                        ["FallbackMode"] = true
                    }
                };
            }

            // Parse JSON response from LLM
            var analysis = ParseAnalysisResponse(cliResponse.Content);

            if (analysis == null || !analysis.AffectedArtifacts.Any())
            {
                Logger.LogWarning(
                    "No artifacts identified in feedback analysis. Defaulting to all artifacts.");

                analysis = new FeedbackAnalysisDto
                {
                    AffectedArtifacts = GetAllArtifactTypes(),
                    AnalysisSummary = "Could not determine specific artifacts - regenerating all"
                };
            }

            // Validate identified artifacts
            ValidateArtifactNames(analysis.AffectedArtifacts);

            Logger.LogInformation(
                "Feedback analysis complete. Affected artifacts: {Artifacts}",
                string.Join(", ", analysis.AffectedArtifacts));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["AffectedArtifacts"] = analysis.AffectedArtifacts,
                    ["AnalysisSummary"] = analysis.AnalysisSummary,
                    ["ConfidenceScore"] = analysis.ConfidenceScore
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during feedback analysis");

            // Fallback: regenerate all artifacts on error
            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["AffectedArtifacts"] = GetAllArtifactTypes(),
                    ["AnalysisSummary"] = $"Analysis error: {ex.Message} - regenerating all artifacts",
                    ["FallbackMode"] = true
                }
            };
        }
    }

    private string BuildAnalysisPrompt(string feedback)
    {
        return $@"You are an AI assistant analyzing user feedback on a software implementation plan.

The user has provided feedback on a generated plan that includes these artifact types:
1. UserStories - User stories with acceptance criteria
2. ApiDesign - OpenAPI specification for REST endpoints
3. DatabaseSchema - SQL database schema and migrations
4. TestCases - Comprehensive test scenarios
5. ImplementationSteps - Detailed step-by-step implementation guide

<user_feedback>
{feedback}
</user_feedback>

Analyze the feedback and determine which artifact(s) the feedback applies to.

Output ONLY valid JSON (no preamble) in this exact format:
{{
  ""affected_artifacts"": [""ArtifactType1"", ""ArtifactType2""],
  ""analysis_summary"": ""Brief explanation of which artifacts are affected and why"",
  ""confidence_score"": 0.85
}}

Rules:
- If feedback is about requirements, user needs, or features → include ""UserStories""
- If feedback is about API endpoints, request/response format, HTTP methods → include ""ApiDesign""
- If feedback is about tables, columns, database structure, data model → include ""DatabaseSchema""
- If feedback is about test coverage, test scenarios, edge cases → include ""TestCases""
- If feedback is about implementation approach, code structure, deployment → include ""ImplementationSteps""
- If uncertain about specific artifacts, it's better to include them than exclude them
- confidence_score: 0.0-1.0, where 1.0 means very confident about artifact selection

If feedback mentions multiple concerns, include all relevant artifacts.
If feedback is vague or unclear, err on the side of including more artifacts.";
    }

    private FeedbackAnalysisDto? ParseAnalysisResponse(string response)
    {
        try
        {
            // Try to extract JSON from response (may include markdown blocks)
            var jsonContent = ExtractJsonFromResponse(response);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var analysis = JsonSerializer.Deserialize<FeedbackAnalysisDto>(jsonContent, options);
            return analysis;
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Failed to parse feedback analysis JSON response");
            return null;
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Check if response is wrapped in markdown code block
        if (response.Contains("```json"))
        {
            var start = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase) + 7;
            var end = response.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
            {
                return response.Substring(start, end - start).Trim();
            }
        }

        // Check for plain ``` blocks
        if (response.Contains("```"))
        {
            var start = response.IndexOf("```", StringComparison.Ordinal) + 3;
            var end = response.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
            {
                return response.Substring(start, end - start).Trim();
            }
        }

        // Try to extract JSON object directly (find first { and last })
        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        return response;
    }

    private void ValidateArtifactNames(List<string> artifacts)
    {
        var validArtifacts = GetAllArtifactTypes();

        foreach (var artifact in artifacts)
        {
            if (!validArtifacts.Contains(artifact))
            {
                Logger.LogWarning(
                    "Invalid artifact name identified: {InvalidArtifact}. Valid artifacts: {ValidArtifacts}",
                    artifact,
                    string.Join(", ", validArtifacts));
            }
        }
    }

    private List<string> GetAllArtifactTypes()
    {
        return new List<string>
        {
            "UserStories",
            "ApiDesign",
            "DatabaseSchema",
            "TestCases",
            "ImplementationSteps"
        };
    }
}
