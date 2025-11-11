using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.LLM;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Specialized;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Agents.Specialized;

/// <summary>
/// Comprehensive tests for CodeReviewAgent covering:
/// - Agent execution with valid PR
/// - Critical issues detection
/// - Template variable building
/// - Review response parsing
/// - Language detection
/// - Test file detection
/// - Error handling
/// </summary>
public class CodeReviewAgentTests
{
    private readonly Mock<ILogger<CodeReviewAgent>> _mockLogger;
    private readonly Mock<ILlmProviderFactory> _mockProviderFactory;
    private readonly Mock<IPromptLoaderService> _mockPromptService;
    private readonly Mock<ICodeReviewResultRepository> _mockReviewResultRepo;
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<ILlmProvider> _mockProvider;

    public CodeReviewAgentTests()
    {
        _mockLogger = new Mock<ILogger<CodeReviewAgent>>();
        _mockProviderFactory = new Mock<ILlmProviderFactory>();
        _mockPromptService = new Mock<IPromptLoaderService>();
        _mockReviewResultRepo = new Mock<ICodeReviewResultRepository>();
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockProvider = new Mock<ILlmProvider>();

        // Default provider setup
        _mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");
        _mockProvider.Setup(p => p.SupportedModels).Returns(new List<string> { "claude-sonnet-4-5" });

        _mockProviderFactory
            .Setup(f => f.CreateProvider(It.IsAny<string>()))
            .Returns(_mockProvider.Object);
    }

    private CodeReviewAgent CreateAgent(Guid? llmProviderId = null)
    {
        return new CodeReviewAgent(
            _mockLogger.Object,
            _mockProviderFactory.Object,
            _mockPromptService.Object,
            _mockReviewResultRepo.Object,
            _mockTicketRepo.Object,
            llmProviderId);
    }

    private AgentContext CreateContext(Guid ticketId, int? prNumber = 123, string? prUrl = "https://github.com/test/repo/pull/123")
    {
        return new AgentContext
        {
            TicketId = ticketId.ToString(),
            TenantId = Guid.NewGuid().ToString(),
            RepositoryId = Guid.NewGuid().ToString(),
            PullRequestNumber = prNumber,
            PullRequestUrl = prUrl,
            ImplementationPlan = "# Implementation Plan\n\nImplement feature X.",
            Repository = new RepositoryBuilder()
                .WithName("test-repo")
                .WithDefaultBranch("main")
                .Build()
        };
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithValidPR_ReturnsSuccessfulReview()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTicketKey("TEST-123")
            .WithTitle("Test ticket")
            .Build();

        _mockTicketRepo
            .Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPromptService
            .Setup(p => p.LoadPrompt("code-review", "anthropic", "system"))
            .Returns("You are a code review expert.");

        _mockPromptService
            .Setup(p => p.RenderTemplate("code-review", "anthropic", "user_template", It.IsAny<object>()))
            .Returns("Review this PR...");

        _mockProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = @"## Critical Issues
- None found

## Suggestions
- Consider adding more tests

## Praise
- Good code structure"
            });

        _mockReviewResultRepo
            .Setup(r => r.GetRetryCountAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockReviewResultRepo
            .Setup(r => r.AddAsync(It.IsAny<CodeReviewResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodeReviewResult r, CancellationToken ct) => r);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Null(result.Error);
        Assert.NotNull(result.Output);
        Assert.True(result.Output.ContainsKey("ReviewId"));
        Assert.True(result.Output.ContainsKey("HasCriticalIssues"));

        _mockTicketRepo.Verify(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()), Times.Once);
        _mockProvider.Verify(p => p.SendMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<LlmOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCriticalIssues_IdentifiesIssues()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTicketKey("TEST-123")
            .Build();

        _mockTicketRepo
            .Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPromptService
            .Setup(p => p.LoadPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("System prompt");

        _mockPromptService
            .Setup(p => p.RenderTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns("User prompt");

        _mockProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = @"## Critical Issues
- SQL injection vulnerability in UserController
- Missing input validation

## Suggestions
- Add logging

## Praise
- Good structure"
            });

        _mockReviewResultRepo
            .Setup(r => r.GetRetryCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockReviewResultRepo
            .Setup(r => r.AddAsync(It.IsAny<CodeReviewResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodeReviewResult r, CancellationToken ct) => r);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.NotNull(result.Output);
        Assert.True(result.Output.ContainsKey("CriticalIssues"));

        var criticalIssues = result.Output["CriticalIssues"] as List<string>;
        Assert.NotNull(criticalIssues);
        Assert.Equal(2, criticalIssues.Count);
        Assert.Contains("SQL injection vulnerability in UserController", criticalIssues);
        Assert.Contains("Missing input validation", criticalIssues);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingPullRequestNumber_ReturnsFailed()
    {
        // Arrange
        var agent = CreateAgent();
        var context = CreateContext(Guid.NewGuid(), prNumber: null);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("PullRequestNumber is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingPullRequestUrl_ReturnsFailed()
    {
        // Arrange
        var agent = CreateAgent();
        var context = CreateContext(Guid.NewGuid(), prUrl: null);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("PullRequestUrl is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLlmFails_ReturnsFailedResult()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTicketKey("TEST-123")
            .Build();

        _mockTicketRepo
            .Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPromptService
            .Setup(p => p.LoadPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("System prompt");

        _mockPromptService
            .Setup(p => p.RenderTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns("User prompt");

        _mockProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = false,
                ErrorMessage = "Rate limit exceeded"
            });

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("Rate limit exceeded", result.Error);
    }

    #endregion

    #region BuildTemplateVariables Tests

    [Fact]
    public async Task BuildTemplateVariables_CreatesAllRequiredVariables()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder()
            .WithId(ticketId)
            .WithTicketKey("TEST-123")
            .WithTitle("Test ticket")
            .WithDescription("Test description")
            .Build();

        _mockTicketRepo
            .Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPromptService
            .Setup(p => p.LoadPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("System prompt");

        object? capturedVariables = null;
        _mockPromptService
            .Setup(p => p.RenderTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, _, _, vars) => capturedVariables = vars)
            .Returns("Rendered prompt");

        _mockProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Success = true, Content = "## Critical Issues\n- None" });

        _mockReviewResultRepo
            .Setup(r => r.GetRetryCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockReviewResultRepo
            .Setup(r => r.AddAsync(It.IsAny<CodeReviewResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodeReviewResult r, CancellationToken ct) => r);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedVariables);

        // Verify the variables have expected properties using reflection
        var varsType = capturedVariables.GetType();
        var ticketNumberProp = varsType.GetProperty("ticket_number");
        var ticketTitleProp = varsType.GetProperty("ticket_title");
        var prUrlProp = varsType.GetProperty("pull_request_url");
        var branchNameProp = varsType.GetProperty("branch_name");

        Assert.NotNull(ticketNumberProp);
        Assert.NotNull(ticketTitleProp);
        Assert.NotNull(prUrlProp);
        Assert.NotNull(branchNameProp);

        Assert.Equal("TEST-123", ticketNumberProp.GetValue(capturedVariables));
        Assert.Equal("Test ticket", ticketTitleProp.GetValue(capturedVariables));
        Assert.Equal("https://github.com/test/repo/pull/123", prUrlProp.GetValue(capturedVariables));
    }

    #endregion

    #region ParseReviewResponse Tests

    [Fact]
    public async Task ParseReviewResponse_WithCriticalIssuesSection_ParsesCorrectly()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder().WithId(ticketId).Build();
        _mockTicketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var reviewContent = @"## Critical Issues
- Memory leak in cache implementation
- Race condition in concurrent access

## Suggestions
- Add unit tests

## Praise
- Clean code";

        SetupSuccessfulReview(reviewContent);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var criticalIssues = result.Output["CriticalIssues"] as List<string>;
        Assert.NotNull(criticalIssues);
        Assert.Equal(2, criticalIssues.Count);
        Assert.Contains("Memory leak in cache implementation", criticalIssues);
        Assert.Contains("Race condition in concurrent access", criticalIssues);
    }

    [Fact]
    public async Task ParseReviewResponse_WithSuggestionsSection_ParsesCorrectly()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder().WithId(ticketId).Build();
        _mockTicketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var reviewContent = @"## Critical Issues
- None

## Suggestions
- Consider using async/await
- Extract helper method
- Add XML documentation

## Praise
- Good structure";

        SetupSuccessfulReview(reviewContent);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var suggestions = result.Output["Suggestions"] as List<string>;
        Assert.NotNull(suggestions);
        Assert.Equal(3, suggestions.Count);
        Assert.Contains("Consider using async/await", suggestions);
        Assert.Contains("Extract helper method", suggestions);
        Assert.Contains("Add XML documentation", suggestions);
    }

    [Fact]
    public async Task ParseReviewResponse_WithPraiseSection_ParsesCorrectly()
    {
        // Arrange
        var agent = CreateAgent();
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var ticket = new TicketBuilder().WithId(ticketId).Build();
        _mockTicketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var reviewContent = @"## Critical Issues
- None

## Suggestions
- None

## Praise
- Excellent test coverage
- Clear variable names
- Good separation of concerns";

        SetupSuccessfulReview(reviewContent);

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var praise = result.Output["Praise"] as List<string>;
        Assert.NotNull(praise);
        Assert.Equal(3, praise.Count);
        Assert.Contains("Excellent test coverage", praise);
        Assert.Contains("Clear variable names", praise);
        Assert.Contains("Good separation of concerns", praise);
    }

    #endregion

    #region DetectLanguage Tests

    [Fact]
    public void DetectLanguage_WithCSharpFile_ReturnsCSharp()
    {
        // This tests the private DetectLanguage method indirectly through reflection
        var agent = CreateAgent();
        var detectLanguageMethod = typeof(CodeReviewAgent).GetMethod(
            "DetectLanguage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(detectLanguageMethod);

        var result = detectLanguageMethod.Invoke(agent, new object[] { "Service.cs" }) as string;
        Assert.Equal("csharp", result);
    }

    [Fact]
    public void DetectLanguage_WithTypeScriptFile_ReturnsTypeScript()
    {
        var agent = CreateAgent();
        var detectLanguageMethod = typeof(CodeReviewAgent).GetMethod(
            "DetectLanguage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(detectLanguageMethod);

        var result = detectLanguageMethod.Invoke(agent, new object[] { "component.ts" }) as string;
        Assert.Equal("typescript", result);
    }

    #endregion

    #region IsTestFile Tests

    [Fact]
    public void IsTestFile_WithTestFile_ReturnsTrue()
    {
        var agent = CreateAgent();
        var isTestFileMethod = typeof(CodeReviewAgent).GetMethod(
            "IsTestFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(isTestFileMethod);

        var testFilePaths = new[]
        {
            "ServiceTests.cs",
            "Service.test.ts",
            "Service.spec.js",
            "tests/Service.cs",
            "__tests__/component.jsx"
        };

        foreach (var path in testFilePaths)
        {
            var result = (bool?)isTestFileMethod.Invoke(agent, new object[] { path });
            Assert.True(result, $"Expected {path} to be detected as test file");
        }
    }

    [Fact]
    public void IsTestFile_WithNonTestFile_ReturnsFalse()
    {
        var agent = CreateAgent();
        var isTestFileMethod = typeof(CodeReviewAgent).GetMethod(
            "IsTestFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(isTestFileMethod);

        var nonTestFilePaths = new[]
        {
            "Service.cs",
            "Controller.ts",
            "Component.jsx",
            "src/Service.cs"
        };

        foreach (var path in nonTestFilePaths)
        {
            var result = (bool?)isTestFileMethod.Invoke(agent, new object[] { path });
            Assert.False(result, $"Expected {path} NOT to be detected as test file");
        }
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulReview(string reviewContent)
    {
        _mockPromptService
            .Setup(p => p.LoadPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("System prompt");

        _mockPromptService
            .Setup(p => p.RenderTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns("User prompt");

        _mockProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = reviewContent
            });

        _mockReviewResultRepo
            .Setup(r => r.GetRetryCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockReviewResultRepo
            .Setup(r => r.AddAsync(It.IsAny<CodeReviewResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodeReviewResult r, CancellationToken ct) => r);
    }

    #endregion
}
