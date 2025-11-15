using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

public class PmUserStoriesAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<ILogger<PmUserStoriesAgent>> _mockLogger;
    private readonly PmUserStoriesAgent _agent;

    public PmUserStoriesAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockLogger = new Mock<ILogger<PmUserStoriesAgent>>();
        _agent = new PmUserStoriesAgent(_mockLogger.Object, _mockCliAgent.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsUserStories()
    {
        // Arrange
        var context = CreateValidContext();
        var mockResponse = CreateMockUserStoriesResponse();

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
        Assert.True(context.State.ContainsKey("UserStories"));
        Assert.NotNull(context.State["UserStories"]);

        var userStories = context.State["UserStories"] as string;
        Assert.Contains("# User Stories", userStories);
        Assert.Contains("**As a**", userStories);
    }

    [Fact]
    public async Task ExecuteAsync_CliAgentFails_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "LLM API timeout"
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
        Assert.Contains("LLM API timeout", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidUserStoriesFormat_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Invalid content without user stories format"
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
    public async Task ExecuteAsync_MissingTicket_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.Ticket = null!;

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingRepositoryPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.RepositoryPath = null;

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithQuestionsAndAnswers_IncludesInPrompt()
    {
        // Arrange
        var context = CreateValidContext();
        context.Ticket.AddQuestion(Question.Create("Which OAuth provider?", "technical"));
        var answer = new Answer(context.Ticket.Questions[0].Id, "Google OAuth 2.0", DateTime.UtcNow);
        context.Ticket.Answers.Add(answer);

        var mockResponse = CreateMockUserStoriesResponse();

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
        Assert.Contains("Q&A Context", capturedPrompt);
        Assert.Contains("Which OAuth provider?", capturedPrompt);
        Assert.Contains("Google OAuth 2.0", capturedPrompt);
    }

    [Fact]
    public async Task ExecuteAsync_WithCodebaseAnalysis_IncludesInPrompt()
    {
        // Arrange
        var context = CreateValidContext();
        context.Analysis = new CodebaseAnalysis
        {
            Architecture = ".NET 10 Web API with Clean Architecture",
            AffectedFiles = new List<string> { "Controllers/AuthController.cs" },
            TechnicalConsiderations = new List<string> { "Existing JWT middleware" }
        };

        var mockResponse = CreateMockUserStoriesResponse();

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
        Assert.Contains("codebase_analysis", capturedPrompt);
        Assert.Contains(".NET 10 Web API with Clean Architecture", capturedPrompt);
        Assert.Contains("Existing JWT middleware", capturedPrompt);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsStoriesFromMarkdownCodeBlock()
    {
        // Arrange
        var context = CreateValidContext();
        var responseWithCodeBlock = new CliAgentResponse
        {
            Success = true,
            Content = @"Here are the user stories:

```markdown
# User Stories

## Story 1: User Login
**As a** user
**I want** to log in
**So that** I can access the system
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
        var userStories = context.State["UserStories"] as string;
        Assert.Contains("# User Stories", userStories);
        Assert.DoesNotContain("Here are the user stories:", userStories);
        Assert.DoesNotContain("```markdown", userStories);
    }

    [Fact]
    public async Task ExecuteAsync_CountsStoriesCorrectly()
    {
        // Arrange
        var context = CreateValidContext();
        var mockResponse = CreateMockUserStoriesResponse();

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
        Assert.True(result.Output.ContainsKey("StoryCount"));
        Assert.True((int)result.Output["StoryCount"] > 0);
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

    private CliAgentResponse CreateMockUserStoriesResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"# User Stories

## Story 1: User Login with Google OAuth
**As a** user
**I want** to log in using my Google account
**So that** I don't need to create a new password

### Acceptance Criteria
- [ ] User can click 'Sign in with Google' button
- [ ] User is redirected to Google OAuth consent screen
- [ ] After successful authentication, user is redirected back with JWT token
- [ ] JWT token includes user email and profile information

### Edge Cases
- User denies OAuth consent
- OAuth token expires during flow

## Non-Functional Requirements
- Authentication must complete within 5 seconds
- JWT tokens must be securely stored (httpOnly cookies)
- Must comply with OWASP authentication best practices",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 1500
            }
        };
    }
}
