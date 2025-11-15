using PRFactory.Infrastructure.Agents.Base;
using System;
using System.Collections.Generic;

namespace PRFactory.Infrastructure.Agents.Messages;

/// <summary>
/// Internal message with feedback analysis results.
/// Produced by FeedbackAnalysisAgent, consumed by PlanRevisionAgent.
/// </summary>
public record FeedbackAnalysisResult(
    Guid TicketId,
    string Feedback,
    List<string> AffectedArtifacts,
    string AnalysisSummary,
    DateTime Timestamp
) : IAgentMessage
{
    /// <summary>
    /// Checks if any artifacts are marked for regeneration.
    /// </summary>
    public bool HasAffectedArtifacts => AffectedArtifacts.Count > 0;

    /// <summary>
    /// Gets a human-readable summary of affected artifacts.
    /// </summary>
    public string GetArtifactSummary()
    {
        return AffectedArtifacts.Count switch
        {
            0 => "None (defaulting to full regeneration)",
            1 => $"Only {AffectedArtifacts[0]}",
            _ => string.Join(", ", AffectedArtifacts)
        };
    }
}

/// <summary>
/// DTO for feedback analysis response from LLM.
/// </summary>
public record FeedbackAnalysisDto
{
    [System.Text.Json.Serialization.JsonPropertyName("affected_artifacts")]
    public List<string> AffectedArtifacts { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("analysis_summary")]
    public string AnalysisSummary { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("confidence_score")]
    public float ConfidenceScore { get; set; }
}
