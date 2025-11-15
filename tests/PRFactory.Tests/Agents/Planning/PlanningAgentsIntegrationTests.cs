using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

/// <summary>
/// Integration tests for the full planning agent chain:
/// PmUserStoriesAgent -> ArchitectApiDesignAgent -> ArchitectDbSchemaAgent -> QaTestCasesAgent -> TechLeadImplementationAgent
/// </summary>
public class PlanningAgentsIntegrationTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IArchitectureContextService> _mockArchContextService;
    private readonly PmUserStoriesAgent _pmAgent;
    private readonly ArchitectApiDesignAgent _apiAgent;
    private readonly ArchitectDbSchemaAgent _dbAgent;
    private readonly QaTestCasesAgent _qaAgent;
    private readonly TechLeadImplementationAgent _techLeadAgent;

    public PlanningAgentsIntegrationTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockArchContextService = new Mock<IArchitectureContextService>();

        // Setup Epic 07 service mocks
        _mockArchContextService.Setup(x => x.GetArchitecturePatternsAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Clean Architecture patterns...");

        _mockArchContextService.Setup(x => x.GetTechnologyStack())
            .Returns(".NET 10, Blazor Server...");

        _mockArchContextService.Setup(x => x.GetCodeStyleGuidelines())
            .Returns("UTF-8 without BOM, file-scoped namespaces...");

        _mockArchContextService.Setup(x => x.GetRelevantCodeSnippetsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CodeSnippet>
            {
                new() { FilePath = "src/Example.cs", Language = "csharp", Code = "public class Example {}" }
            });

        _pmAgent = new PmUserStoriesAgent(
            Mock.Of<ILogger<PmUserStoriesAgent>>(),
            _mockCliAgent.Object);

        _apiAgent = new ArchitectApiDesignAgent(
            Mock.Of<ILogger<ArchitectApiDesignAgent>>(),
            _mockCliAgent.Object,
            _mockArchContextService.Object);

        _dbAgent = new ArchitectDbSchemaAgent(
            Mock.Of<ILogger<ArchitectDbSchemaAgent>>(),
            _mockCliAgent.Object,
            _mockArchContextService.Object);

        _qaAgent = new QaTestCasesAgent(
            Mock.Of<ILogger<QaTestCasesAgent>>(),
            _mockCliAgent.Object);

        _techLeadAgent = new TechLeadImplementationAgent(
            Mock.Of<ILogger<TechLeadImplementationAgent>>(),
            _mockCliAgent.Object,
            _mockArchContextService.Object);
    }

    [Fact]
    public async Task ExecuteAllAgents_ValidContext_GeneratesAllArtifacts()
    {
        // Arrange
        var context = CreateIntegrationTestContext();

        SetupMockResponses();

        // Act - Execute all agents sequentially
        var pmResult = await _pmAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, pmResult.Status);

        var apiResult = await _apiAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, apiResult.Status);

        var dbResult = await _dbAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, dbResult.Status);

        var qaResult = await _qaAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, qaResult.Status);

        var techLeadResult = await _techLeadAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, techLeadResult.Status);

        // Assert - All artifacts should be present in context
        Assert.True(context.State.ContainsKey("UserStories"));
        Assert.True(context.State.ContainsKey("ApiDesign"));
        Assert.True(context.State.ContainsKey("DatabaseSchema"));
        Assert.True(context.State.ContainsKey("TestCases"));
        Assert.True(context.State.ContainsKey("ImplementationSteps"));

        // Verify artifact content
        var userStories = context.State["UserStories"] as string;
        Assert.Contains("# User Stories", userStories);
        Assert.Contains("**As a**", userStories);

        var apiDesign = context.State["ApiDesign"] as string;
        Assert.Contains("openapi:", apiDesign);
        Assert.Contains("paths:", apiDesign);

        var dbSchema = context.State["DatabaseSchema"] as string;
        Assert.Contains("CREATE TABLE", dbSchema);

        var testCases = context.State["TestCases"] as string;
        Assert.Contains("# Test Cases", testCases);
        Assert.Contains("### Test Case", testCases);

        var implementationSteps = context.State["ImplementationSteps"] as string;
        Assert.Contains("# Implementation", implementationSteps);
        Assert.Contains("## Step", implementationSteps);
    }

    [Fact]
    public async Task ExecuteAllAgents_FirstAgentFails_SubsequentAgentsFail()
    {
        // Arrange
        var context = CreateIntegrationTestContext();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse
            {
                Success = false,
                ErrorMessage = "First agent failed"
            });

        // Act - Execute PM agent
        var pmResult = await _pmAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, pmResult.Status);

        // Try to execute API agent without UserStories
        var apiResult = await _apiAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert - Subsequent agents should fail due to missing dependencies
        Assert.Equal(AgentStatus.Failed, apiResult.Status);
    }

    [Fact]
    public async Task ExecuteAllAgents_ArtifactsArePassedBetweenAgents()
    {
        // Arrange
        var context = CreateIntegrationTestContext();

        string? pmPrompt = null;
        string? apiPrompt = null;
        string? dbPrompt = null;
        string? qaPrompt = null;
        string? techLeadPrompt = null;

        var callCount = 0;
        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string prompt, string path, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    pmPrompt = prompt;
                    return CreateMockUserStoriesResponse();
                }
                else if (callCount == 2)
                {
                    apiPrompt = prompt;
                    return CreateMockApiDesignResponse();
                }
                else if (callCount == 3)
                {
                    dbPrompt = prompt;
                    return CreateMockDatabaseSchemaResponse();
                }
                else if (callCount == 4)
                {
                    qaPrompt = prompt;
                    return CreateMockTestCasesResponse();
                }
                else if (callCount == 5)
                {
                    techLeadPrompt = prompt;
                    return CreateMockImplementationStepsResponse();
                }
                return CreateMockUserStoriesResponse();
            });

        // Act
        await _pmAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        await _apiAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        await _dbAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        await _qaAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        await _techLeadAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert - Verify that artifacts are included in subsequent prompts
        Assert.NotNull(apiPrompt);
        Assert.Contains("User Stories", apiPrompt);

        Assert.NotNull(dbPrompt);
        Assert.Contains("User Stories", dbPrompt);

        Assert.NotNull(qaPrompt);
        Assert.Contains("user_stories", qaPrompt);
        Assert.Contains("api_design", qaPrompt);
        Assert.Contains("database_schema", qaPrompt);

        Assert.NotNull(techLeadPrompt);
        Assert.Contains("user_stories", techLeadPrompt);
        Assert.Contains("api_design", techLeadPrompt);
        Assert.Contains("database_schema", techLeadPrompt);
        Assert.Contains("test_cases", techLeadPrompt);
    }

    [Fact]
    public async Task ExecuteAllAgents_VerifyTokenUsageReported()
    {
        // Arrange
        var context = CreateIntegrationTestContext();

        SetupMockResponses();

        // Act
        var pmResult = await _pmAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        var apiResult = await _apiAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        var dbResult = await _dbAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        var qaResult = await _qaAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        var techLeadResult = await _techLeadAgent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert - All agents should report token usage
        Assert.True(pmResult.Output.ContainsKey("TokensUsed"));
        Assert.True(apiResult.Output.ContainsKey("TokensUsed"));
        Assert.True(dbResult.Output.ContainsKey("TokensUsed"));
        Assert.True(qaResult.Output.ContainsKey("TokensUsed"));
        Assert.True(techLeadResult.Output.ContainsKey("TokensUsed"));

        // Calculate total tokens used
        var totalTokens = (int)pmResult.Output["TokensUsed"] +
                          (int)apiResult.Output["TokensUsed"] +
                          (int)dbResult.Output["TokensUsed"] +
                          (int)qaResult.Output["TokensUsed"] +
                          (int)techLeadResult.Output["TokensUsed"];

        Assert.True(totalTokens > 0);
    }

    private void SetupMockResponses()
    {
        _mockCliAgent
            .SetupSequence(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockUserStoriesResponse())
            .ReturnsAsync(CreateMockApiDesignResponse())
            .ReturnsAsync(CreateMockDatabaseSchemaResponse())
            .ReturnsAsync(CreateMockTestCasesResponse())
            .ReturnsAsync(CreateMockImplementationStepsResponse());
    }

    private AgentContext CreateIntegrationTestContext()
    {
        var tenant = Tenant.Create(
            "Test Tenant",
            "Personal",
            "test-external-id",
            "https://test.atlassian.net",
            "test-api-token",
            "test-claude-key");
        var repository = Repository.Create(
            tenant.Id,
            "Test Repo",
            "GitHub",
            "https://github.com/test/repo.git",
            "access-token");

        var ticket = Ticket.Create("PROJ-123", tenant.Id, repository.Id);
        ticket.UpdateTicketInfo("Add user authentication", "Implement OAuth 2.0 authentication");

        return new AgentContext
        {
            TicketId = ticket.Id.ToString(),
            TenantId = tenant.Id.ToString(),
            RepositoryId = repository.Id.ToString(),
            Ticket = ticket,
            Tenant = tenant,
            Repository = repository,
            RepositoryPath = "/tmp/repo",
            State = new Dictionary<string, object>(),
            Analysis = new CodebaseAnalysis
            {
                Architecture = ".NET 10 Web API",
                AffectedFiles = new List<string> { "Controllers/AuthController.cs" },
                TechnicalConsiderations = new List<string> { "Existing JWT middleware" }
            }
        };
    }

    private CliAgentResponse CreateMockUserStoriesResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = "# User Stories\n\n## Story 1\n**As a** user\n**I want** to authenticate\n**So that** I can access the system",
            Metadata = new Dictionary<string, object> { ["tokens_used"] = 1500 }
        };
    }

    private CliAgentResponse CreateMockApiDesignResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = "openapi: 3.0.0\ninfo:\n  title: Auth API\npaths:\n  /auth/google:\n    post: {}",
            Metadata = new Dictionary<string, object> { ["tokens_used"] = 2000 }
        };
    }

    private CliAgentResponse CreateMockDatabaseSchemaResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = "CREATE TABLE Users (Id INT PRIMARY KEY);",
            Metadata = new Dictionary<string, object> { ["tokens_used"] = 1800 }
        };
    }

    private CliAgentResponse CreateMockTestCasesResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = "# Test Cases\n\n### Test Case 1: User Login\n**Description**: Test login",
            Metadata = new Dictionary<string, object> { ["tokens_used"] = 2200 }
        };
    }

    private CliAgentResponse CreateMockImplementationStepsResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = "# Implementation Plan\n\n## Step 1: Database Migration\nCreate Users table",
            Metadata = new Dictionary<string, object> { ["tokens_used"] = 2500 }
        };
    }
}
