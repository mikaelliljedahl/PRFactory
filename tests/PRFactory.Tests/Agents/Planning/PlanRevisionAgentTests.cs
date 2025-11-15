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
        var ticket = Ticket.Create("PROJ-200", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old user stories",
            apiDesign: "Old API design",
            databaseSchema: "Old schema",
            testCases: "Old test cases",
            implementationSteps: "Old steps");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("ApiDesign"));
        Assert.Equal(updatedApiDesign, result.Output["ApiDesign"]);
    }

    [Fact]
    public async Task Execute_WithMultipleArtifactRevisions_UpdatesAll()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-201", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories",
            apiDesign: "Old API",
            databaseSchema: "Old schema",
            testCases: "Old tests",
            implementationSteps: "Old steps");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

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
        var ticket = Ticket.Create("PROJ-202", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var context = new AgentContext
        {
            Ticket = ticket,
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

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("No existing plan found", result.Error);
    }

    [Fact]
    public async Task Execute_WithNoAffectedArtifacts_ThrowsInvalidOperation()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-203", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(ticketId);

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>()
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("AffectedArtifacts not found", result.Error);
    }

    [Fact]
    public async Task Execute_WithCliFailureOnOneArtifact_ContinuesWithOthers()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-204", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories",
            apiDesign: "Old API",
            databaseSchema: "Old schema",
            testCases: "Old tests",
            implementationSteps: "Old steps");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Single(result.Output); // One success, one failure
        Assert.True(result.Output.ContainsKey("TestCases"));
    }

    [Fact]
    public async Task Execute_WithAllCliFailures_ReturnsFailed()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-205", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Execute_WithUnknownArtifactType_SkipsAndContinues()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-206", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            apiDesign: "Old API");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        // Should have only ApiDesign (UnknownArtifact threw exception but was caught)
        Assert.True(result.Output.ContainsKey("ApiDesign"));
    }

    [Fact]
    public async Task Execute_WithYamlInMarkdown_ExtractsCorrectly()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-207", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            apiDesign: "Old API");

        var context = new AgentContext
        {
            Ticket = ticket,
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
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var apiDesign = result.Output["ApiDesign"] as string;
        Assert.NotNull(apiDesign);
        Assert.StartsWith("openapi: 3.0.0", apiDesign);
        Assert.DoesNotContain("Here's the updated", apiDesign);
    }

    [Fact]
    public async Task Execute_WithUserStoriesRevision_UpdatesContextState()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-208", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories");

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "UserStories" },
                ["RevisionFeedback"] = "Add new story"
            }
        };

        var updatedStories = "Old stories\nNew story";

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = updatedStories });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(context.State.ContainsKey("UserStories"));
        Assert.Equal(updatedStories, context.State["UserStories"]);
    }

    [Fact]
    public async Task Execute_WithDatabaseSchemaRevision_IncludesExistingSchema()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-209", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingSchema = "CREATE TABLE users (id INT);";
        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            databaseSchema: existingSchema);

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "DatabaseSchema" },
                ["RevisionFeedback"] = "Add email column"
            }
        };

        string? capturedPrompt = null;
        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((prompt, path, ct) => capturedPrompt = prompt)
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated schema" });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.NotNull(capturedPrompt);
        Assert.Contains(existingSchema, capturedPrompt);
        Assert.Contains("Add email column", capturedPrompt);
    }

    [Fact]
    public async Task Execute_WithTestCasesRevision_PreservesExistingTestCases()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-210", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingTests = "Test: User login";
        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            testCases: existingTests);

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "TestCases" },
                ["RevisionFeedback"] = "Add logout test"
            }
        };

        string? capturedPrompt = null;
        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((prompt, path, ct) => capturedPrompt = prompt)
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated tests" });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.NotNull(capturedPrompt);
        Assert.Contains(existingTests, capturedPrompt);
    }

    [Fact]
    public async Task Execute_WithImplementationStepsRevision_UpdatesSuccessfully()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-211", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingSteps = "Step 1: Setup\nStep 2: Implement";
        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            implementationSteps: existingSteps);

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ImplementationSteps" },
                ["RevisionFeedback"] = "Add testing step"
            }
        };

        var updatedSteps = "Step 1: Setup\nStep 2: Implement\nStep 3: Test";

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = updatedSteps });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("ImplementationSteps"));
        Assert.Equal(updatedSteps, result.Output["ImplementationSteps"]);
    }

    [Fact]
    public async Task Execute_WithAllFiveArtifacts_UpdatesAllSuccessfully()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-212", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories",
            apiDesign: "Old API",
            databaseSchema: "Old schema",
            testCases: "Old tests",
            implementationSteps: "Old steps");

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string>
                {
                    "UserStories", "ApiDesign", "DatabaseSchema", "TestCases", "ImplementationSteps"
                },
                ["RevisionFeedback"] = "Complete redesign"
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated" });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Equal(5, result.Output.Count);
        Assert.True(result.Output.ContainsKey("UserStories"));
        Assert.True(result.Output.ContainsKey("ApiDesign"));
        Assert.True(result.Output.ContainsKey("DatabaseSchema"));
        Assert.True(result.Output.ContainsKey("TestCases"));
        Assert.True(result.Output.ContainsKey("ImplementationSteps"));
    }

    [Fact]
    public async Task Execute_WithYmlCodeBlock_ExtractsYaml()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-213", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            apiDesign: "Old API");

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "ApiDesign" },
                ["RevisionFeedback"] = "Update"
            }
        };

        var yamlResponse = @"```yml
openapi: 3.0.0
info:
  title: My API
```";

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = yamlResponse });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var apiDesign = result.Output["ApiDesign"] as string;
        Assert.NotNull(apiDesign);
        Assert.StartsWith("openapi:", apiDesign);
    }

    [Fact]
    public async Task Execute_WithEmptyRevisionFeedback_UsesEmptyString()
    {
        // Arrange
        var ticket = Ticket.Create("PROJ-214", Guid.NewGuid(), Guid.NewGuid());
        var ticketId = ticket.Id;

        var existingPlan = Plan.CreateWithArtifacts(
            ticketId,
            userStories: "Old stories");

        var context = new AgentContext
        {
            Ticket = ticket,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>
            {
                ["AffectedArtifacts"] = new List<string> { "UserStories" }
                // No RevisionFeedback in state
            }
        };

        _mockPlanRepository
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "Updated" });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
    }
}
