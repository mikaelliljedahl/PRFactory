using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Analyzes user feedback to determine which plan artifacts need regeneration.
/// Uses LLM to understand intent and map to specific artifacts.
/// </summary>
public class FeedbackAnalysisAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public override string Name => "Feedback Analysis Agent";
    public override string Description => "Analyzes revision feedback to determine affected artifacts";

    public FeedbackAnalysisAgent(
        ILogger<FeedbackAnalysisAgent> logger,
        ICliAgent cliAgent)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var feedback = GetRequiredStateValue<string>(context, "RevisionFeedback");

        Logger.LogInformation(
            "Analyzing feedback for ticket {TicketKey}: {Feedback}",
            context.Ticket.TicketKey,
            feedback);

        // Build prompt for LLM to analyze feedback
        var prompt = BuildFeedbackAnalysisPrompt(feedback);

        var cliResponse = await _cliAgent.ExecutePromptAsync(prompt, cancellationToken);

        if (!cliResponse.Success)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Feedback analysis failed: {cliResponse.ErrorMessage}"
            };
        }

        // Parse JSON response
        var analysisJson = ExtractJsonFromResponse(cliResponse.Content);
        FeedbackAnalysisDto? analysis;

        try
        {
            analysis = JsonSerializer.Deserialize<FeedbackAnalysisDto>(analysisJson);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Failed to parse JSON response. Defaulting to regenerate all artifacts.");
            analysis = null;
        }

        if (analysis == null || !analysis.AffectedArtifacts.Any())
        {
            Logger.LogWarning("No affected artifacts identified. Defaulting to regenerate all.");
            analysis = new FeedbackAnalysisDto
            {
                AffectedArtifacts = new List<string>
                {
                    "UserStories", "ApiDesign", "DatabaseSchema", "TestCases", "ImplementationSteps"
                },
                Summary = "Unable to determine specific artifacts, regenerating all."
            };
        }

        // Store analysis result in context
        context.State["AffectedArtifacts"] = analysis.AffectedArtifacts;
        context.State["AnalysisSummary"] = analysis.Summary;

        Logger.LogInformation(
            "Analysis complete. Affected artifacts: {Artifacts}",
            string.Join(", ", analysis.AffectedArtifacts));

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = analysis.AffectedArtifacts,
                ["Summary"] = analysis.Summary
            }
        };
    }

    private string BuildFeedbackAnalysisPrompt(string feedback)
    {
        return $@"You are analyzing user feedback on an implementation plan to determine which artifacts need regeneration.

<feedback>
{feedback}
</feedback>

<artifacts>
The plan consists of 5 artifacts:
1. UserStories - User stories with acceptance criteria
2. ApiDesign - OpenAPI specification (YAML)
3. DatabaseSchema - SQL DDL statements
4. TestCases - Test scenarios and cases
5. ImplementationSteps - Step-by-step implementation guide
</artifacts>

<task>
Analyze the feedback and determine which artifacts are affected and need regeneration.

Keywords to look for:
- ""user stor"", ""acceptance criteria"" → UserStories
- ""api"", ""endpoint"", ""request"", ""response"" → ApiDesign
- ""database"", ""schema"", ""table"", ""column"", ""index"" → DatabaseSchema
- ""test"", ""qa"", ""scenario"" → TestCases
- ""implementation"", ""step"", ""code"" → ImplementationSteps

Output a JSON object with:
{{
  ""affectedArtifacts"": [""ArtifactName1"", ""ArtifactName2""],
  ""summary"": ""Brief explanation of what needs to change""
}}

Important:
- If feedback is unclear or mentions multiple areas, include all related artifacts
- If feedback mentions ""plan"" or ""everything"", include all artifacts
- Output ONLY valid JSON (no preamble or explanation)
</task>";
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Try to extract JSON from markdown code blocks
        var jsonPattern = @"```json\s*(.*?)\s*```";
        var match = Regex.Match(response, jsonPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try to find JSON object directly
        var jsonObjectPattern = @"\{[\s\S]*\}";
        match = Regex.Match(response, jsonObjectPattern);

        if (match.Success)
        {
            return match.Value.Trim();
        }

        // Fallback: assume entire response is JSON
        return response.Trim();
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }

    private class FeedbackAnalysisDto
    {
        [JsonPropertyName("affectedArtifacts")]
        public List<string> AffectedArtifacts { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }
}
