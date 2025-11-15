# Part 4: Revision Agents - Implementation Plan

**Epic**: Deep Planning (Epic 03)
**Component**: FeedbackAnalysisAgent + PlanRevisionAgent
**Estimated Effort**: 2-3 days
**Dependencies**: Part 1 (Multi-Artifact Agents), Part 2 (Database Schema), Part 3 (Storage Agent)
**Status**: 🚧 Not Implemented

---

## Overview

This part implements the revision workflow agents that enable selective artifact regeneration based on user feedback:

1. **FeedbackAnalysisAgent** - Analyzes user feedback to determine which artifacts need updates
2. **PlanRevisionAgent** - Regenerates only the affected artifacts with updated context

This enables efficient revision workflows without regenerating entire plans.

---

## Task 1: Define Revision Messages

### Purpose

Message types for revision workflow communication between UI and graph.

### Files to Create

**Create:**
- `src/PRFactory.Infrastructure/Agents/Messages/PlanRevisionMessages.cs`

### Implementation

**File**: `src/PRFactory.Infrastructure/Agents/Messages/PlanRevisionMessages.cs`

```csharp
using PRFactory.Infrastructure.Agents.Base;
using System;

namespace PRFactory.Infrastructure.Agents.Messages;

/// <summary>
/// Message sent when user rejects a plan and requests revision.
/// Contains natural language feedback about what needs to change.
/// </summary>
public record PlanRejectedMessage(
    Guid TicketId,
    string Feedback,
    bool Regenerate,
    DateTime Timestamp
) : IAgentMessage
{
    /// <summary>
    /// Validates that feedback is not empty.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Feedback);
}

/// <summary>
/// Message sent when user approves a plan and workflow proceeds to implementation.
/// </summary>
public record PlanApprovedMessage(
    Guid TicketId,
    string? ApprovedBy,
    DateTime Timestamp
) : IAgentMessage;

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
```

---

## Task 2: Implement FeedbackAnalysisAgent

### Purpose

Analyzes user feedback to intelligently determine which plan artifacts are affected by the requested changes. Uses LLM to understand intent and map to specific artifacts, improving on simple keyword matching.

### Files to Create/Modify

**Create:**
- `src/PRFactory.Infrastructure/Agents/Planning/FeedbackAnalysisAgent.cs`
- `tests/PRFactory.Tests/Agents/Planning/FeedbackAnalysisAgentTests.cs`

### Implementation

**File**: `src/PRFactory.Infrastructure/Agents/Planning/FeedbackAnalysisAgent.cs`

```csharp
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
    private readonly ILogger<FeedbackAnalysisAgent> _logger;

    public override string Name => "Feedback Analysis Agent";
    public override string Description => "Analyzes revision feedback to determine which artifacts need updating";

    public FeedbackAnalysisAgent(
        ICliAgent cliAgent,
        ILogger<FeedbackAnalysisAgent> logger)
    {
        ArgumentNullException.ThrowIfNull(cliAgent);
        ArgumentNullException.ThrowIfNull(logger);

        _cliAgent = cliAgent;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Get the rejection message from context
        var rejectionMessage = context.State.GetValueOrDefault("RejectionMessage") as PlanRejectedMessage
            ?? throw new InvalidOperationException("RejectionMessage not found in context");

        if (!rejectionMessage.IsValid)
        {
            return AgentResult.Failed("Feedback is empty or whitespace");
        }

        _logger.LogInformation(
            "Analyzing feedback for ticket {TicketKey}: {FeedbackLength} chars",
            context.Ticket.TicketKey,
            rejectionMessage.Feedback.Length);

        try
        {
            // Build analysis prompt
            var prompt = BuildAnalysisPrompt(rejectionMessage.Feedback);

            // Call LLM to analyze feedback
            var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
                prompt,
                context.RepositoryPath!,
                cancellationToken);

            if (!cliResponse.Success)
            {
                _logger.LogWarning(
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
                _logger.LogWarning(
                    "No artifacts identified in feedback analysis. Defaulting to all artifacts.");

                analysis = new FeedbackAnalysisDto
                {
                    AffectedArtifacts = GetAllArtifactTypes(),
                    AnalysisSummary = "Could not determine specific artifacts - regenerating all"
                };
            }

            // Validate identified artifacts
            ValidateArtifactNames(analysis.AffectedArtifacts);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Unexpected error during feedback analysis");

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
            _logger.LogWarning(ex, "Failed to parse feedback analysis JSON response");
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
                _logger.LogWarning(
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
```

### Unit Tests

**File**: `tests/PRFactory.Tests/Agents/Planning/FeedbackAnalysisAgentTests.cs`

```csharp
using Xunit;
using Moq;
using PRFactory.Infrastructure.Agents.Planning;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Tests.Agents.Planning;

public class FeedbackAnalysisAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<ILogger<FeedbackAnalysisAgent>> _mockLogger;
    private readonly FeedbackAnalysisAgent _agent;

    public FeedbackAnalysisAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockLogger = new Mock<ILogger<FeedbackAnalysisAgent>>();
        _agent = new FeedbackAnalysisAgent(_mockCliAgent.Object, _mockLogger.Object);
    }

    [Fact]
    public void AgentName_ReturnsCorrectName()
    {
        // Arrange & Act
        var name = _agent.Name;

        // Assert
        Assert.Equal("Feedback Analysis Agent", name);
    }

    [Fact]
    public async Task Execute_WithValidFeedback_IdentifiesAffectedArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add rate limiting to the API endpoints";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-123" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"```json
{
  ""affected_artifacts"": [""ApiDesign""],
  ""analysis_summary"": ""Rate limiting is an API concern"",
  ""confidence_score"": 0.95
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.NotNull(result.Output);
        Assert.True(result.Output.ContainsKey("AffectedArtifacts"));

        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Contains("ApiDesign", artifacts);
    }

    [Fact]
    public async Task Execute_WithMultipleArtifactsFeedback_IdentifiesAll()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Redesign database schema and update API to match new data model";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-124" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"```json
{
  ""affected_artifacts"": [""ApiDesign"", ""DatabaseSchema"", ""TestCases""],
  ""analysis_summary"": ""Schema changes affect API design and require new test coverage"",
  ""confidence_score"": 0.92
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(3, artifacts.Count);
        Assert.Contains("ApiDesign", artifacts);
        Assert.Contains("DatabaseSchema", artifacts);
        Assert.Contains("TestCases", artifacts);
    }

    [Fact]
    public async Task Execute_WithCliFailure_FallsBackToAllArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Something is wrong with the plan";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-125" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "LLM service unavailable"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(5, artifacts.Count); // All artifacts
        Assert.True((bool)result.Output["FallbackMode"]);
    }

    [Fact]
    public async Task Execute_WithEmptyFeedback_ReturnsFailed()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var rejection = new PlanRejectedMessage(ticketId, "   ", true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-126" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Execute_WithNoRejectionMessage_ThrowsInvalidOperation()
    {
        // Arrange
        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = Guid.NewGuid(), TicketKey = "PROJ-127" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task Execute_WithJsonParsingError_FallsBackToAllArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Fix the implementation";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-128" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Invalid JSON response"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(5, artifacts.Count); // Fallback to all
    }

    [Fact]
    public async Task Execute_WithValidJsonInMarkdownBlock_ParsesCorrectly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Update test cases for new endpoints";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-129" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"Based on the feedback, here's my analysis:

```json
{
  ""affected_artifacts"": [""TestCases"", ""ApiDesign""],
  ""analysis_summary"": ""New endpoints require updated test cases"",
  ""confidence_score"": 0.88
}
```

This makes sense because..."
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(2, artifacts.Count);
        Assert.Contains("TestCases", artifacts);
        Assert.Contains("ApiDesign", artifacts);
    }

    [Fact]
    public async Task Execute_WithInvalidArtifactName_LogsWarningButContinues()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Update the documentation";
        var rejection = new PlanRejectedMessage(ticketId, feedback, true, DateTime.UtcNow);

        var context = new AgentContext
        {
            Ticket = new Domain.Entities.Ticket { Id = ticketId, TicketKey = "PROJ-130" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"```json
{
  ""affected_artifacts"": [""Documentation"", ""ApiDesign""],
  ""analysis_summary"": ""Documentation and API design need updates"",
  ""confidence_score"": 0.75
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        // Invalid artifact names are returned as-is, but logged as warning
        Assert.Contains("Documentation", artifacts);
    }

    [Fact]
    public async Task FeedbackAnalysisResult_GetArtifactSummary_FormatsCorrectly()
    {
        // Arrange
        var result1 = new FeedbackAnalysisResult(
            Guid.NewGuid(),
            "feedback",
            new List<string>(),
            "summary",
            DateTime.UtcNow);

        var result2 = new FeedbackAnalysisResult(
            Guid.NewGuid(),
            "feedback",
            new List<string> { "ApiDesign" },
            "summary",
            DateTime.UtcNow);

        var result3 = new FeedbackAnalysisResult(
            Guid.NewGuid(),
            "feedback",
            new List<string> { "ApiDesign", "DatabaseSchema" },
            "summary",
            DateTime.UtcNow);

        // Act & Assert
        Assert.Equal("None (defaulting to full regeneration)", result1.GetArtifactSummary());
        Assert.Equal("Only ApiDesign", result2.GetArtifactSummary());
        Assert.Equal("ApiDesign, DatabaseSchema", result3.GetArtifactSummary());
    }
}
```

---

## Task 3: Implement PlanRevisionAgent

### Purpose

Regenerates only the artifacts identified by FeedbackAnalysisAgent, using existing plan data as context for unchanged artifacts.

### Files to Create/Modify

**Create:**
- `src/PRFactory.Infrastructure/Agents/Planning/PlanRevisionAgent.cs`
- `tests/PRFactory.Tests/Agents/Planning/PlanRevisionAgentTests.cs`

### Implementation

**File**: `src/PRFactory.Infrastructure/Agents/Planning/PlanRevisionAgent.cs`

```csharp
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Regenerates only the plan artifacts identified as needing updates.
/// Uses existing plan as context for unchanged artifacts.
/// </summary>
public class PlanRevisionAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PlanRevisionAgent> _logger;

    public override string Name => "Plan Revision Agent";
    public override string Description => "Regenerates specific plan artifacts based on feedback";

    public PlanRevisionAgent(
        ICliAgent cliAgent,
        IPlanRepository planRepository,
        ILogger<PlanRevisionAgent> logger)
    {
        ArgumentNullException.ThrowIfNull(cliAgent);
        ArgumentNullException.ThrowIfNull(planRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _cliAgent = cliAgent;
        _planRepository = planRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = context.Ticket.Id;

        // Get existing plan
        var existingPlan = await _planRepository.GetByTicketIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException($"No existing plan found for ticket {ticketId}");

        // Get affected artifacts from context
        var affectedArtifacts = context.State.GetValueOrDefault("AffectedArtifacts") as List<string>
            ?? throw new InvalidOperationException("AffectedArtifacts not found in context");

        var feedback = context.State.GetValueOrDefault("RevisionFeedback") as string ?? string.Empty;

        _logger.LogInformation(
            "Regenerating artifacts for ticket {TicketKey}. Affected: {Artifacts}",
            context.Ticket.TicketKey,
            string.Join(", ", affectedArtifacts));

        var updatedArtifacts = new Dictionary<string, object>();

        // Regenerate each affected artifact
        foreach (var artifactType in affectedArtifacts)
        {
            try
            {
                var artifact = artifactType switch
                {
                    "UserStories" => await RegenerateUserStoriesAsync(context, existingPlan, feedback, cancellationToken),
                    "ApiDesign" => await RegenerateApiDesignAsync(context, existingPlan, feedback, cancellationToken),
                    "DatabaseSchema" => await RegenerateDatabaseSchemaAsync(context, existingPlan, feedback, cancellationToken),
                    "TestCases" => await RegenerateTestCasesAsync(context, existingPlan, feedback, cancellationToken),
                    "ImplementationSteps" => await RegenerateImplementationStepsAsync(context, existingPlan, feedback, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown artifact type: {artifactType}")
                };

                if (artifact == null)
                {
                    _logger.LogWarning("Failed to regenerate {ArtifactType}", artifactType);
                    continue;
                }

                updatedArtifacts[artifactType] = artifact;
                context.State[artifactType] = artifact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating {ArtifactType}", artifactType);
                // Continue with other artifacts on error
            }
        }

        if (updatedArtifacts.Count == 0)
        {
            return AgentResult.Failed("No artifacts were successfully regenerated");
        }

        _logger.LogInformation(
            "Successfully regenerated {Count} artifacts",
            updatedArtifacts.Count);

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = updatedArtifacts
        };
    }

    private async Task<string?> RegenerateUserStoriesAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Product Manager revising user stories based on feedback.

<existing_user_stories>
{existingPlan.UserStories ?? "None"}
</existing_user_stories>

<feedback>
{feedback}
</feedback>

Revise the user stories to address the feedback. Keep stories that don't need changes, and update/add new ones as needed.

Output only the revised markdown (no preamble).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateApiDesignAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Software Architect revising API design based on feedback.

<existing_api_design>
{existingPlan.ApiDesign ?? "None"}
</existing_api_design>

<feedback>
{feedback}
</feedback>

Revise the OpenAPI specification to address the feedback. Keep endpoints that don't need changes, and update/add new ones as needed.

Output only the revised OpenAPI YAML (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        if (response.Success)
        {
            return ExtractYamlContent(response.Content);
        }

        return null;
    }

    private async Task<string?> RegenerateDatabaseSchemaAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Database Architect revising database schema based on feedback.

<existing_schema>
{existingPlan.DatabaseSchema ?? "None"}
</existing_schema>

<feedback>
{feedback}
</feedback>

Revise the database schema to address the feedback. Keep tables/columns that don't need changes, and update/add new ones as needed.

Output only the revised SQL DDL statements (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateTestCasesAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a QA Engineer revising test cases based on feedback.

<existing_test_cases>
{existingPlan.TestCases ?? "None"}
</existing_test_cases>

<feedback>
{feedback}
</feedback>

Revise the test cases to address the feedback. Keep test cases that are still valid, and update/add new ones as needed.

Output only the revised test cases in markdown format (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateImplementationStepsAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Tech Lead revising implementation steps based on feedback.

<existing_implementation_steps>
{existingPlan.ImplementationSteps ?? "None"}
</existing_implementation_steps>

<feedback>
{feedback}
</feedback>

Revise the implementation steps to address the feedback. Keep steps that are still valid, and update/add new ones as needed.

Output only the revised implementation steps in markdown format (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private string ExtractYamlContent(string response)
    {
        if (response.Contains("```yaml") || response.Contains("```yml"))
        {
            var start = response.IndexOf("```", StringComparison.Ordinal);
            if (start >= 0)
            {
                start += 3;
                var newlineIndex = response.IndexOf('\n', start);
                if (newlineIndex >= 0)
                {
                    start = newlineIndex + 1;
                }

                var end = response.IndexOf("```", start, StringComparison.Ordinal);
                if (end > start)
                {
                    return response.Substring(start, end - start).Trim();
                }
            }
        }

        return response.Trim();
    }
}
```

### Unit Tests

**File**: `tests/PRFactory.Tests/Agents/Planning/PlanRevisionAgentTests.cs`

```csharp
using Xunit;
using Moq;
using PRFactory.Infrastructure.Agents.Planning;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Tests.Agents.Planning;

public class PlanRevisionAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IPlanRepository> _mockPlanRepository;
    private readonly Mock<ILogger<PlanRevisionAgent>> _mockLogger;
    private readonly PlanRevisionAgent _agent;

    public PlanRevisionAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockPlanRepository = new Mock<IPlanRepository>();
        _mockLogger = new Mock<ILogger<PlanRevisionAgent>>();
        _agent = new PlanRevisionAgent(
            _mockCliAgent.Object,
            _mockPlanRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void AgentName_ReturnsCorrectName()
    {
        // Arrange & Act
        var name = _agent.Name;

        // Assert
        Assert.Equal("Plan Revision Agent", name);
    }

    [Fact]
    public async Task Execute_WithSingleArtifactRevision_UpdatesSuccessfully()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserStories = "Old user stories",
            ApiDesign = "Old API design",
            DatabaseSchema = "Old schema",
            TestCases = "Old test cases",
            ImplementationSteps = "Old steps"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-200" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign" },
                ["RevisionFeedback"] = "Add rate limiting"
            }
        };

        var updatedApiDesign = "Updated API design with rate limiting";

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = updatedApiDesign });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("ApiDesign"));
        Assert.Equal(updatedApiDesign, result.Output["ApiDesign"]);
    }

    [Fact]
    public async Task Execute_WithMultipleArtifactRevisions_UpdatesAll()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserStories = "Old stories",
            ApiDesign = "Old API",
            DatabaseSchema = "Old schema",
            TestCases = "Old tests",
            ImplementationSteps = "Old steps"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-201" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign", "DatabaseSchema", "TestCases" },
                ["RevisionFeedback"] = "Update schema and related artifacts"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated content" });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Equal(3, result.Output.Count);
        Assert.True(result.Output.ContainsKey("ApiDesign"));
        Assert.True(result.Output.ContainsKey("DatabaseSchema"));
        Assert.True(result.Output.ContainsKey("TestCases"));
    }

    [Fact]
    public async Task Execute_WithNonexistentPlan_ThrowsInvalidOperation()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-202" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign" },
                ["RevisionFeedback"] = "Update API"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task Execute_WithNoAffectedArtifacts_ThrowsInvalidOperation()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-203" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>()
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plan { Id = Guid.NewGuid(), TicketId = ticketId });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task Execute_WithCliFailureOnOneArtifact_ContinuesWithOthers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserStories = "Old stories",
            ApiDesign = "Old API",
            DatabaseSchema = "Old schema",
            TestCases = "Old tests",
            ImplementationSteps = "Old steps"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-204" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign", "TestCases" },
                ["RevisionFeedback"] = "Update both"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        var callCount = 0;
        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string prompt, string path, CancellationToken ct) =>
            {
                callCount++;
                // First call fails, second succeeds
                if (callCount == 1)
                {
                    return Task.FromResult(new CliAgentResponse { Success = false, ErrorMessage = "Error" });
                }
                return Task.FromResult(new CliAgentResponse { Success = true, Content = "Updated" });
            });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Single(result.Output); // One success, one failure
        Assert.True(result.Output.ContainsKey("TestCases"));
    }

    [Fact]
    public async Task Execute_WithAllCliFailures_ReturnsFailed()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserStories = "Old stories"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-205" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "UserStories" },
                ["RevisionFeedback"] = "Rewrite"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = false, ErrorMessage = "Service error" });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Execute_WithUnknownArtifactType_SkipsAndContinues()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ApiDesign = "Old API"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-206" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "UnknownArtifact", "ApiDesign" },
                ["RevisionFeedback"] = "Update"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated" });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        // Should have only ApiDesign (UnknownArtifact threw exception but was caught)
        Assert.True(result.Output.ContainsKey("ApiDesign"));
    }

    [Fact]
    public async Task Execute_WithYamlInMarkdown_ExtractsCorrectly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingPlan = new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ApiDesign = "Old API"
        };

        var context = new AgentContext
        {
            Ticket = new Ticket { Id = ticketId, TicketKey = "PROJ-207" },
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign" },
                ["RevisionFeedback"] = "Update API"
            }
        };

        var yamlResponse = @"Here's the updated API:

```yaml
openapi: 3.0.0
info:
  title: Updated API
```

This addresses your feedback.";

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = yamlResponse });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var apiDesign = result.Output["ApiDesign"] as string;
        Assert.NotNull(apiDesign);
        Assert.StartsWith("openapi: 3.0.0", apiDesign);
        Assert.DoesNotContain("Here's the updated", apiDesign);
    }
}
```

---

## Acceptance Criteria

### Message Types
- [ ] `PlanRejectedMessage` with feedback text and validation
- [ ] `PlanApprovedMessage` with approval metadata
- [ ] `FeedbackAnalysisResult` with artifact mapping
- [ ] `FeedbackAnalysisDto` for JSON deserialization

### FeedbackAnalysisAgent
- [ ] Analyzes user feedback using LLM
- [ ] Returns list of affected artifacts
- [ ] Fallbacks to all artifacts on LLM failure
- [ ] Validates artifact names
- [ ] Handles JSON parsing errors gracefully
- [ ] 15+ unit tests with >80% coverage
- [ ] Logs analysis results with confidence scores

### PlanRevisionAgent
- [ ] Fetches existing plan from repository
- [ ] Regenerates each affected artifact independently
- [ ] Preserves unchanged artifacts in context
- [ ] Continues on individual artifact failures
- [ ] Extracts YAML from markdown blocks
- [ ] 15+ unit tests with >80% coverage
- [ ] Supports all 5 artifact types

### Error Handling
- [ ] Graceful fallback when LLM unavailable
- [ ] Partial success handling (some artifacts updated, some failed)
- [ ] Validation of artifact types before regeneration
- [ ] Comprehensive logging for debugging

---

## Implementation Checklist

- [ ] Create `PlanRevisionMessages.cs` with all message types
- [ ] Implement `FeedbackAnalysisAgent` with LLM integration
- [ ] Create comprehensive unit tests for FeedbackAnalysisAgent
- [ ] Implement `PlanRevisionAgent` for selective regeneration
- [ ] Create comprehensive unit tests for PlanRevisionAgent
- [ ] Verify all unit tests pass (16+ tests minimum)
- [ ] Ensure xUnit assertions only (no FluentAssertions)
- [ ] Validate error handling for edge cases
- [ ] Verify coverage >80% for both agents
- [ ] Run `dotnet test` successfully

---

## Files to Create/Modify

**Create:**
- `src/PRFactory.Infrastructure/Agents/Messages/PlanRevisionMessages.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/FeedbackAnalysisAgent.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/PlanRevisionAgent.cs`
- `tests/PRFactory.Tests/Agents/Planning/FeedbackAnalysisAgentTests.cs`
- `tests/PRFactory.Tests/Agents/Planning/PlanRevisionAgentTests.cs`

**Modify:**
- None (no existing files require changes)

---

## Dependencies

**Internal:**
- `BaseAgent` (infrastructure)
- `ICliAgent` (LLM abstraction)
- `IPlanRepository` (data access)
- `AgentContext` (execution context)

**External:**
- System.Text.Json (JSON parsing)
- Microsoft.Extensions.Logging (logging)

---

## Estimated Effort

- **Implementation**: 1 day (agents + tests)
- **Code review**: 2-4 hours
- **Integration testing**: 2-4 hours
- **Total**: 1.5-2 days

---

## Related Parts

- **Part 1**: Multi-artifact agents (dependency)
- **Part 2**: Database schema (dependency)
- **Part 3**: Storage agent (dependency)
- **Part 5**: Graph orchestration (consumes these agents)
- **Part 7**: Web UI (triggers revision workflow)

---

## Success Metrics

- 16+ unit tests, all passing
- >80% code coverage for both agents
- Feedback analysis completes in <5 seconds
- Artifact regeneration parallelizable in future versions
- Zero hardcoded artifact type strings (uses constants)
