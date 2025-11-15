using Xunit;
using Moq;
using PRFactory.Infrastructure.Agents.Planning;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
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
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var ticket = Ticket.Create("PROJ-123", Guid.NewGuid(), Guid.NewGuid());

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-124", Guid.NewGuid(), Guid.NewGuid()),
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-125", Guid.NewGuid(), Guid.NewGuid()),
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var rejection = new PlanRejectedMessage(ticketId, "   ");

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-126", Guid.NewGuid(), Guid.NewGuid()),
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
            Ticket = Ticket.Create("PROJ-127", Guid.NewGuid(), Guid.NewGuid()),
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>()
        };

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("RejectionMessage not found", result.Error);
    }

    [Fact]
    public async Task Execute_WithJsonParsingError_FallsBackToAllArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Fix the implementation";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-128", Guid.NewGuid(), Guid.NewGuid()),
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-129", Guid.NewGuid(), Guid.NewGuid()),
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-130", Guid.NewGuid(), Guid.NewGuid()),
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        // Invalid artifact names are returned as-is, but logged as warning
        Assert.Contains("Documentation", artifacts);
    }

    [Fact]
    public void FeedbackAnalysisResult_GetArtifactSummary_FormatsCorrectly()
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

    [Fact]
    public async Task Execute_WithPlainJsonInCodeBlock_ExtractsCorrectly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add new user story";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-131", Guid.NewGuid(), Guid.NewGuid()),
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"```
{
  ""affected_artifacts"": [""UserStories""],
  ""analysis_summary"": ""New user story needed"",
  ""confidence_score"": 0.90
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Single(artifacts);
        Assert.Contains("UserStories", artifacts);
    }

    [Fact]
    public async Task Execute_WithDirectJsonResponse_ParsesCorrectly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Revise implementation steps";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-132", Guid.NewGuid(), Guid.NewGuid()),
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"{
  ""affected_artifacts"": [""ImplementationSteps""],
  ""analysis_summary"": ""Implementation steps revision requested"",
  ""confidence_score"": 0.85
}"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Single(artifacts);
        Assert.Contains("ImplementationSteps", artifacts);
    }

    [Fact]
    public async Task Execute_WithAllArtifactsAffected_ReturnsAllFive()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Complete redesign of the entire plan";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-133", Guid.NewGuid(), Guid.NewGuid()),
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
  ""affected_artifacts"": [""UserStories"", ""ApiDesign"", ""DatabaseSchema"", ""TestCases"", ""ImplementationSteps""],
  ""analysis_summary"": ""Complete redesign affects all artifacts"",
  ""confidence_score"": 1.0
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(5, artifacts.Count);
        Assert.Contains("UserStories", artifacts);
        Assert.Contains("ApiDesign", artifacts);
        Assert.Contains("DatabaseSchema", artifacts);
        Assert.Contains("TestCases", artifacts);
        Assert.Contains("ImplementationSteps", artifacts);
    }

    [Fact]
    public async Task Execute_WithEmptyArtifactsList_FallsBackToAll()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Some vague feedback";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-134", Guid.NewGuid(), Guid.NewGuid()),
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
  ""affected_artifacts"": [],
  ""analysis_summary"": ""Could not determine specific artifacts"",
  ""confidence_score"": 0.1
}
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(5, artifacts.Count); // Fallback to all
    }

    [Fact]
    public async Task Execute_WithException_FallsBackToAllArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Test exception handling";
        var rejection = new PlanRejectedMessage(ticketId, feedback);

        var context = new AgentContext
        {
            Ticket = Ticket.Create("PROJ-135", Guid.NewGuid(), Guid.NewGuid()),
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["RejectionMessage"] = rejection
            }
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var artifacts = result.Output["AffectedArtifacts"] as List<string>;
        Assert.NotNull(artifacts);
        Assert.Equal(5, artifacts.Count); // Fallback to all
        Assert.True((bool)result.Output["FallbackMode"]);
        Assert.Contains("Network error", (string)result.Output["AnalysisSummary"]);
    }

    [Fact]
    public void FeedbackAnalysisResult_HasAffectedArtifacts_ReturnsCorrectValue()
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

        // Act & Assert
        Assert.False(result1.HasAffectedArtifacts);
        Assert.True(result2.HasAffectedArtifacts);
    }
}
