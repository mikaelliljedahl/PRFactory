using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

public class ArchitectApiDesignAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IArchitectureContextService> _mockArchContextService;
    private readonly Mock<ILogger<ArchitectApiDesignAgent>> _mockLogger;
    private readonly ArchitectApiDesignAgent _agent;

    public ArchitectApiDesignAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockArchContextService = new Mock<IArchitectureContextService>();
        _mockLogger = new Mock<ILogger<ArchitectApiDesignAgent>>();

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

        _agent = new ArchitectApiDesignAgent(_mockLogger.Object, _mockCliAgent.Object, _mockArchContextService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsApiDesign()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories\n## Story 1\n**As a** user...";

        var mockResponse = CreateMockApiDesignResponse();

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
        Assert.True(context.State.ContainsKey("ApiDesign"));
        Assert.NotNull(context.State["ApiDesign"]);

        var apiDesign = context.State["ApiDesign"] as string;
        Assert.Contains("openapi:", apiDesign);
        Assert.Contains("paths:", apiDesign);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUserStories_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        // UserStories not added to context

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CliAgentFails_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories\n## Story 1";

        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "API generation failed"
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
        Assert.Contains("API generation failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidYamlSyntax_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "This is not valid YAML: {[{]}"
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
    public async Task ExecuteAsync_MissingOpenApiVersion_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"info:
  title: Test API
paths:
  /test:
    get: {}"
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
    public async Task ExecuteAsync_ExtractsYamlFromCodeBlock()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var responseWithCodeBlock = new CliAgentResponse
        {
            Success = true,
            Content = @"Here is the API design:

```yaml
openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /users:
    get:
      summary: Get users
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
        var apiDesign = context.State["ApiDesign"] as string;
        Assert.Contains("openapi: 3.0.0", apiDesign);
        Assert.DoesNotContain("Here is the API design:", apiDesign);
        Assert.DoesNotContain("```yaml", apiDesign);
    }

    [Fact]
    public async Task ExecuteAsync_CountsEndpointsCorrectly()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var mockResponse = CreateMockApiDesignResponse();

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
        Assert.True(result.Output.ContainsKey("EndpointCount"));
    }

    [Fact]
    public async Task ExecuteAsync_IncludesUserStoriesInPrompt()
    {
        // Arrange
        var context = CreateValidContext();
        var userStories = "# User Stories\n## Story 1: User Login";
        context.State["UserStories"] = userStories;

        var mockResponse = CreateMockApiDesignResponse();

        string? capturedPrompt = null;
        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((prompt, path, ct) => capturedPrompt = prompt)
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.NotNull(capturedPrompt);
        Assert.Contains("User Stories", capturedPrompt);
        Assert.Contains("Story 1: User Login", capturedPrompt);
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

    private CliAgentResponse CreateMockApiDesignResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"openapi: 3.0.0
info:
  title: Authentication API
  version: 1.0.0
  description: API for user authentication
paths:
  /auth/google:
    post:
      summary: Initiate Google OAuth flow
      responses:
        '200':
          description: OAuth URL returned
          content:
            application/json:
              schema:
                type: object
                properties:
                  authUrl:
                    type: string
  /auth/callback:
    get:
      summary: OAuth callback endpoint
      parameters:
        - name: code
          in: query
          required: true
          schema:
            type: string
      responses:
        '200':
          description: JWT token returned
          content:
            application/json:
              schema:
                type: object
                properties:
                  token:
                    type: string",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 2000
            }
        };
    }
}
