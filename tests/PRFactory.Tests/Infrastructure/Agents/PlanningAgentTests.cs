using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Tests.Infrastructure.Agents;

/// <summary>
/// Comprehensive tests for PlanningAgent covering:
/// - Enhanced prompt building with domain templates
/// - Architectural context injection
/// - Code snippet integration
/// - Domain template selection logic
/// - Error handling scenarios
/// - Agent execution workflow
/// </summary>
public class PlanningAgentTests : IDisposable
{
    private readonly Mock<ILogger<PlanningAgent>> _mockLogger;
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IArchitectureContextService> _mockArchitectureContext;
    private readonly PlanningAgent _agent;
    private readonly string _testRepositoryPath;
    private bool _disposed;

    public PlanningAgentTests()
    {
        _mockLogger = new Mock<ILogger<PlanningAgent>>();
        _mockCliAgent = new Mock<ICliAgent>();
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockArchitectureContext = new Mock<IArchitectureContextService>();

        _agent = new PlanningAgent(
            _mockLogger.Object,
            _mockCliAgent.Object,
            _mockTicketRepository.Object,
            _mockArchitectureContext.Object);

        // Create a temporary test directory
        _testRepositoryPath = Path.Combine(Path.GetTempPath(), "PRFactory_PlanningAgent_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRepositoryPath);

        // Setup default responses
        _mockArchitectureContext
            .Setup(a => a.GetTechnologyStack())
            .Returns(".NET 10 with C# 13");

        _mockArchitectureContext
            .Setup(a => a.GetCodeStyleGuidelines())
            .Returns("UTF-8 without BOM");

        _mockArchitectureContext
            .Setup(a => a.GetArchitecturePatternsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Clean Architecture patterns");

        _mockArchitectureContext
            .Setup(a => a.GetRelevantCodeSnippetsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CodeSnippet>());

        _mockCliAgent
            .Setup(c => c.AgentName)
            .Returns("TestCliAgent");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && Directory.Exists(_testRepositoryPath))
        {
            // Clean up test directory
            Directory.Delete(_testRepositoryPath, recursive: true);
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlanningAgent(
            null!,
            _mockCliAgent.Object,
            _mockTicketRepository.Object,
            _mockArchitectureContext.Object));
    }

    [Fact]
    public void Constructor_WithNullCliAgent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlanningAgent(
            _mockLogger.Object,
            null!,
            _mockTicketRepository.Object,
            _mockArchitectureContext.Object));
    }

    [Fact]
    public void Constructor_WithNullTicketRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlanningAgent(
            _mockLogger.Object,
            _mockCliAgent.Object,
            null!,
            _mockArchitectureContext.Object));
    }

    [Fact]
    public void Constructor_WithNullArchitectureContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlanningAgent(
            _mockLogger.Object,
            _mockCliAgent.Object,
            _mockTicketRepository.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var agent = new PlanningAgent(
            _mockLogger.Object,
            _mockCliAgent.Object,
            _mockTicketRepository.Object,
            _mockArchitectureContext.Object);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("PlanningAgent", agent.Name);
        Assert.Equal("Generate detailed implementation plan with AI", agent.Description);
    }

    #endregion

    #region Agent Properties Tests

    [Fact]
    public void AgentName_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("PlanningAgent", _agent.Name);
    }

    [Fact]
    public void AgentDescription_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("Generate detailed implementation plan with AI", _agent.Description);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNullTicket_ReturnsFailedResult()
    {
        // Arrange
        var context = new AgentContext
        {
            Ticket = null!
        };

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("Ticket entity is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullAnalysis_ReturnsFailedResult()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = null
        };

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("Codebase analysis is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStateTransition_ReturnsFailedResult()
    {
        // Arrange
        var ticket = CreateTestTicket();
        ticket.GetType()
            .GetProperty("State")!
            .SetValue(ticket, WorkflowState.Completed);

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis()
        };

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidContext_CallsCliAgent()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var analysis = CreateTestAnalysis();

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = analysis,
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse
            {
                Success = true,
                Content = "# Implementation Plan\n\nDetailed plan content"
            });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                _testRepositoryPath,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulCliExecution_ReturnsCompletedResult()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var analysis = CreateTestAnalysis();

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = analysis,
            RepositoryPath = _testRepositoryPath
        };

        var planContent = "# Implementation Plan\n\nThis is a detailed plan.";

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse
            {
                Success = true,
                Content = planContent
            });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Equal(planContent, context.ImplementationPlan);
        Assert.True(result.Output.ContainsKey("Plan"));
        Assert.Equal(planContent, result.Output["Plan"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedCliExecution_ReturnsFailedResult()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var analysis = CreateTestAnalysis();

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = analysis,
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse
            {
                Success = false,
                ErrorMessage = "CLI execution failed"
            });

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("CLI agent execution failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesTicketRepository()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var analysis = CreateTestAnalysis();

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = analysis,
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse
            {
                Success = true,
                Content = "# Plan"
            });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockTicketRepository.Verify(
            r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Domain Template Selection Tests

    [Fact]
    public async Task InferDomain_WithBlazorKeyword_SelectsWebUiTemplate()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);
        ticket.GetType().GetProperty("Description")!.SetValue(ticket, "Create a new Blazor component");

        await CreateDomainTemplate("web_ui.txt");

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("Blazor Server developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("Blazor Server developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InferDomain_WithApiKeyword_SelectsRestApiTemplate()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);
        ticket.GetType().GetProperty("Description")!.SetValue(ticket, "Add REST API endpoint");

        await CreateDomainTemplate("rest_api.txt");

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("REST API developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("REST API developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InferDomain_WithDatabaseKeyword_SelectsDatabaseTemplate()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);
        ticket.GetType().GetProperty("Description")!.SetValue(ticket, "Create database migration");

        await CreateDomainTemplate("database.txt");

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("database developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("database developer")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Architectural Context Integration Tests

    [Fact]
    public async Task ExecuteAsync_IncludesArchitecturalPatterns()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockArchitectureContext
            .Setup(a => a.GetArchitecturePatternsAsync(_testRepositoryPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Clean Architecture with DDD");

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("Clean Architecture with DDD")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockArchitectureContext.Verify(
            a => a.GetArchitecturePatternsAsync(_testRepositoryPath, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("Clean Architecture with DDD")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesTechnologyStack()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockArchitectureContext
            .Setup(a => a.GetTechnologyStack())
            .Returns(".NET 10 with Blazor Server");

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains(".NET 10 with Blazor Server")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockArchitectureContext.Verify(a => a.GetTechnologyStack(), Times.Once);

        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains(".NET 10 with Blazor Server")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesCodeStyleGuidelines()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        _mockArchitectureContext
            .Setup(a => a.GetCodeStyleGuidelines())
            .Returns("UTF-8 without BOM mandatory");

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("UTF-8 without BOM mandatory")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockArchitectureContext.Verify(a => a.GetCodeStyleGuidelines(), Times.Once);

        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("UTF-8 without BOM mandatory")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesCodeSnippets()
    {
        // Arrange
        var ticket = CreateTestTicket();
        // Set ticket to AnswersReceived state (valid state to transition to Planning) using reflection for test setup
        ticket.GetType().GetProperty("State")!.SetValue(ticket, WorkflowState.AnswersReceived);

        var context = new AgentContext
        {
            Ticket = ticket,
            Analysis = CreateTestAnalysis(),
            RepositoryPath = _testRepositoryPath
        };

        var snippets = new List<CodeSnippet>
        {
            new CodeSnippet
            {
                FilePath = "src/Domain/Ticket.cs",
                Code = "public class Ticket { }",
                Language = "csharp"
            }
        };

        _mockArchitectureContext
            .Setup(a => a.GetRelevantCodeSnippetsAsync(
                _testRepositoryPath,
                It.IsAny<string>(),
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snippets);

        _mockCliAgent
            .Setup(c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("src/Domain/Ticket.cs") && s.Contains("public class Ticket")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliAgentResponse { Success = true, Content = "# Plan" });

        // Act
        await _agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        _mockArchitectureContext.Verify(
            a => a.GetRelevantCodeSnippetsAsync(
                _testRepositoryPath,
                It.IsAny<string>(),
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCliAgent.Verify(
            c => c.ExecuteWithProjectContextAsync(
                It.Is<string>(s => s.Contains("src/Domain/Ticket.cs")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Ticket CreateTestTicket()
    {
        var ticket = Ticket.Create(
            "TEST-123",
            Guid.NewGuid(), // tenantId
            Guid.NewGuid(), // repositoryId
            "Jira",
            TicketSource.WebUI);

        return ticket;
    }

    private CodebaseAnalysis CreateTestAnalysis()
    {
        return new CodebaseAnalysis
        {
            Architecture = "Clean Architecture",
            Summary = "Test summary",
            AffectedFiles = new List<string> { "Test.cs" },
            TechnicalConsiderations = new List<string> { "Testing" }
        };
    }

    private async Task CreateDomainTemplate(string templateName)
    {
        var promptsPath = Path.Combine(_testRepositoryPath, "..", "..", "..", "..", "..", "prompts", "plan", "anthropic", "domains");
        Directory.CreateDirectory(promptsPath);

        var templatePath = Path.Combine(promptsPath, templateName);
        var content = templateName switch
        {
            "web_ui.txt" => "You are an expert Blazor Server developer",
            "rest_api.txt" => "You are an expert REST API developer",
            "database.txt" => "You are an expert database developer",
            _ => "You are an expert developer"
        };

        await File.WriteAllTextAsync(templatePath, content);
    }

    #endregion
}
