using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

public class TechLeadImplementationAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IArchitectureContextService> _mockArchContextService;
    private readonly Mock<ILogger<TechLeadImplementationAgent>> _mockLogger;
    private readonly TechLeadImplementationAgent _agent;

    public TechLeadImplementationAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockArchContextService = new Mock<IArchitectureContextService>();
        _mockLogger = new Mock<ILogger<TechLeadImplementationAgent>>();

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

        _agent = new TechLeadImplementationAgent(_mockLogger.Object, _mockCliAgent.Object, _mockArchContextService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsImplementationSteps()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        var mockResponse = CreateMockImplementationStepsResponse();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(context.State.ContainsKey("ImplementationSteps"));
        Assert.NotNull(context.State["ImplementationSteps"]);

        var implementationSteps = context.State["ImplementationSteps"] as string;
        Assert.Contains("# Implementation", implementationSteps);
        Assert.Contains("## Step", implementationSteps);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUserStories_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingApiDesign_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingDatabaseSchema_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["TestCases"] = "# Test Cases";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingTestCases_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CliAgentFails_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "Implementation steps generation failed"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("Implementation steps generation failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidImplementationStepsFormat_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Invalid content without implementation steps format"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsImplementationStepsFromMarkdownCodeBlock()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        var responseWithCodeBlock = new CliAgentResponse
        {
            Success = true,
            Content = @"Here is the implementation plan:

```markdown
# Implementation Plan

## Step 1: Database Migration
Create migration for Users table
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseWithCodeBlock);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var implementationSteps = context.State["ImplementationSteps"] as string;
        Assert.Contains("# Implementation Plan", implementationSteps);
        Assert.DoesNotContain("Here is the implementation plan:", implementationSteps);
        Assert.DoesNotContain("```markdown", implementationSteps);
    }

    [Fact]
    public async Task ExecuteAsync_CountsStepsAndFilesCorrectly()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";
        context.State["TestCases"] = "# Test Cases";

        var mockResponse = CreateMockImplementationStepsResponse();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("StepCount"));
        Assert.True(result.Output.ContainsKey("FileCount"));
        Assert.True((int)result.Output["StepCount"] > 0);
    }

    private AgentContext CreateValidContext()
    {
        var tenant = Tenant.Create("Test Tenant", "Personal", "test-id", "https://test.atlassian.net", "test-token", "test-key");
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
            State = new Dictionary<string, object>()
        };
    }

    private CliAgentResponse CreateMockImplementationStepsResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"# Implementation Plan

## Overview
Implement OAuth 2.0 authentication using Google as the provider

## Step 1: Database Migration
**Files to modify/create**:
- `src/Infrastructure/Migrations/20251115_AddAuthTables.cs`

**Implementation details**:
1. Create Users table with GoogleId column
2. Create AuthTokens table for JWT management
3. Add indexes on email and googleId columns

## Step 2: Domain Entities
**Files to modify/create**:
- `src/Domain/Entities/User.cs` (create)
- `src/Domain/Entities/AuthToken.cs` (create)

**Implementation details**:
1. Create User entity with OAuth properties
2. Create AuthToken entity for JWT tokens
3. Add validation rules

## Step 3: Repository Layer
**Files to modify/create**:
- `src/Domain/Interfaces/IUserRepository.cs` (create)
- `src/Infrastructure/Repositories/UserRepository.cs` (create)

**Implementation details**:
1. Define repository interface
2. Implement repository with EF Core

## Step 4: Application Service Layer
**Files to modify/create**:
- `src/Infrastructure/Application/AuthService.cs` (create)

**Implementation details**:
1. Implement OAuth flow logic
2. JWT token generation and validation

## Step 5: API Controllers
**Files to modify/create**:
- `src/Web/Controllers/AuthController.cs` (create)

**Implementation details**:
1. Implement /auth/google endpoint
2. Implement /auth/callback endpoint
3. Add proper error handling",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 2500
            }
        };
    }
}
