using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.AI;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.AI;
using PRFactory.Infrastructure.Agents.Base;
using System.Text.Json;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Agents.AI;

/// <summary>
/// Unit tests for AFAnalyzerAgent with tool integration.
/// Tests cover configuration loading, tool usage, and analysis output.
/// </summary>
public class AFAnalyzerAgentTests
{
    private readonly Mock<IAIAgentService> _mockAIService;
    private readonly Mock<IToolRegistry> _mockToolRegistry;
    private readonly Mock<IAgentConfigurationRepository> _mockConfigRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly AFAnalyzerAgent _agent;

    public AFAnalyzerAgentTests()
    {
        _mockAIService = new Mock<IAIAgentService>();
        _mockToolRegistry = new Mock<IToolRegistry>();
        _mockConfigRepo = new Mock<IAgentConfigurationRepository>();
        _mockTenantContext = new Mock<ITenantContext>();

        _agent = new AFAnalyzerAgent(
            _mockAIService.Object,
            _mockToolRegistry.Object,
            _mockConfigRepo.Object,
            _mockTenantContext.Object,
            NullLogger<AFAnalyzerAgent>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidContext_ReturnsAnalysis()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";

        var context = CreateTestContext(tenantId, ticketId);

        SetupDefaultMocks(tenantId);

        var chunks = CreateSuccessfulResponseChunks();
        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("Analysis"));
        Assert.True(result.Output.ContainsKey("ToolsUsed"));
        Assert.True(result.Output.ContainsKey("ReasoningSteps"));
    }

    [Fact]
    public async Task ExecuteAsync_WithDatabaseConfig_UsesConfigFromDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        var dbConfig = new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = "AnalyzerAgent",
            Instructions = "Custom instructions from database",
            EnabledTools = JsonSerializer.Serialize(new[] { "ReadFileTool", "GrepTool" }),
            MaxTokens = 5000,
            Temperature = 0.5f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockConfigRepo
            .Setup(r => r.GetByTenantAndNameAsync(tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbConfig);

        SetupToolRegistryMock();

        var chunks = CreateSuccessfulResponseChunks();
        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        AIAgentConfiguration? capturedConfig = null;
        _mockAIService
            .Setup(s => s.CreateAgentAsync(
                It.IsAny<AIAgentConfiguration>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgentConfiguration, IEnumerable<object>, CancellationToken>((config, tools, ct) =>
            {
                capturedConfig = config;
            })
            .ReturnsAsync(new object());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedConfig);
        Assert.Equal("Custom instructions from database", capturedConfig.Instructions);
        Assert.Equal(5000, capturedConfig.MaxTokens);
        Assert.Equal(0.5f, capturedConfig.Temperature);
        Assert.Equal(2, capturedConfig.EnabledTools.Length);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConfigInDatabase_UsesDefaultConfig()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        _mockConfigRepo
            .Setup(r => r.GetByTenantAndNameAsync(tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        SetupToolRegistryMock();

        var chunks = CreateSuccessfulResponseChunks();
        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        AIAgentConfiguration? capturedConfig = null;
        _mockAIService
            .Setup(s => s.CreateAgentAsync(
                It.IsAny<AIAgentConfiguration>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgentConfiguration, IEnumerable<object>, CancellationToken>((config, tools, ct) =>
            {
                capturedConfig = config;
            })
            .ReturnsAsync(new object());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedConfig);
        Assert.Contains("expert software architect", capturedConfig.Instructions);
        Assert.Equal(8000, capturedConfig.MaxTokens);
        Assert.Equal(0.3f, capturedConfig.Temperature);
        Assert.Equal(5, capturedConfig.EnabledTools.Length); // Default: 5 tools
    }

    [Fact]
    public async Task ExecuteAsync_WithToolUsage_LogsToolInvocations()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        SetupDefaultMocks(tenantId);

        var chunks = new List<AgentStreamChunk>
        {
            new AgentStreamChunk { Type = ChunkType.Reasoning, Content = "Analyzing codebase..." },
            new AgentStreamChunk { Type = ChunkType.ToolUse, Content = "ReadFileTool" },
            new AgentStreamChunk { Type = ChunkType.ToolResult, Content = "File contents..." },
            new AgentStreamChunk { Type = ChunkType.ToolUse, Content = "GrepTool" },
            new AgentStreamChunk { Type = ChunkType.ToolResult, Content = "Search results..." },
            new AgentStreamChunk { Type = ChunkType.Response, Content = "Analysis complete" },
            new AgentStreamChunk { Type = ChunkType.Complete, IsFinal = true }
        };

        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var toolsUsed = result.Output["ToolsUsed"] as List<string>;
        Assert.NotNull(toolsUsed);
        Assert.Equal(2, toolsUsed.Count);
        Assert.Contains("ReadFileTool", toolsUsed);
        Assert.Contains("GrepTool", toolsUsed);
    }

    [Fact]
    public async Task ExecuteAsync_WithReasoningSteps_CapturesReasoningInOutput()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        SetupDefaultMocks(tenantId);

        var chunks = new List<AgentStreamChunk>
        {
            new AgentStreamChunk { Type = ChunkType.Reasoning, Content = "Step 1: Analyzing..." },
            new AgentStreamChunk { Type = ChunkType.Reasoning, Content = "Step 2: Searching..." },
            new AgentStreamChunk { Type = ChunkType.Response, Content = "Analysis complete" },
            new AgentStreamChunk { Type = ChunkType.Complete, IsFinal = true }
        };

        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        var reasoningSteps = result.Output["ReasoningSteps"] as List<string>;
        Assert.NotNull(reasoningSteps);
        Assert.Equal(2, reasoningSteps.Count);
        Assert.Contains("Step 1: Analyzing...", reasoningSteps);
        Assert.Contains("Step 2: Searching...", reasoningSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsFailedStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        _mockConfigRepo
            .Setup(r => r.GetByTenantAndNameAsync(tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("Database error", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoResponseChunks_ReturnsDefaultAnalysis()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        SetupDefaultMocks(tenantId);

        var chunks = new List<AgentStreamChunk>
        {
            new AgentStreamChunk { Type = ChunkType.Reasoning, Content = "Thinking..." },
            new AgentStreamChunk { Type = ChunkType.Complete, IsFinal = true }
        };

        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var analysis = result.Output["Analysis"] as string;
        Assert.NotNull(analysis);
        Assert.Equal("No analysis generated", analysis);
    }

    [Fact]
    public async Task ExecuteAsync_StoresAnalysisInContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        SetupDefaultMocks(tenantId);

        var chunks = CreateSuccessfulResponseChunks();
        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(context.Analysis);
        Assert.True(context.State.ContainsKey("Analysis"));
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        Assert.Equal("AFAnalyzerAgent", _agent.Name);
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        Assert.Contains("AI-powered analyzer agent", _agent.Description);
        Assert.Contains("autonomous tool use", _agent.Description);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidToolsJson_UsesAllTools()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = "PROJ-123";
        var context = CreateTestContext(tenantId, ticketId);

        var dbConfig = new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = "AnalyzerAgent",
            Instructions = "Test instructions",
            EnabledTools = "invalid-json", // Invalid JSON
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockConfigRepo
            .Setup(r => r.GetByTenantAndNameAsync(tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbConfig);

        var allTools = new List<object>
        {
            Mock.Of<ITool>(t => t.Name == "ReadFileTool"),
            Mock.Of<ITool>(t => t.Name == "GrepTool")
        };

        _mockToolRegistry
            .Setup(r => r.GetAllTools())
            .Returns(allTools);

        var chunks = CreateSuccessfulResponseChunks();
        _mockAIService
            .Setup(s => s.ExecuteAgentAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<List<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        _mockAIService
            .Setup(s => s.CreateAgentAsync(
                It.IsAny<AIAgentConfiguration>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockToolRegistry.Verify(r => r.GetAllTools(), Times.Once);
    }

    // Helper methods

    private AgentContext CreateTestContext(Guid tenantId, string ticketId)
    {
        var repositoryId = Guid.NewGuid();
        var ticket = Ticket.Create(
            ticketId,
            tenantId,
            repositoryId,
            "Jira",
            PRFactory.Domain.ValueObjects.TicketSource.WebUI);

        ticket.UpdateTicketInfo("Test Ticket", "Fix authentication bug");

        return new AgentContext
        {
            TenantId = tenantId.ToString(),
            TicketId = ticketId,
            RepositoryPath = "/path/to/repo",
            Ticket = ticket,
            State = new Dictionary<string, object>
            {
                ["TicketDescription"] = "Fix authentication bug",
                ["RepositoryPath"] = "/path/to/repo"
            }
        };
    }

    private void SetupDefaultMocks(Guid tenantId)
    {
        var dbConfig = new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = "AnalyzerAgent",
            Instructions = "Default instructions",
            EnabledTools = JsonSerializer.Serialize(new[] { "ReadFileTool", "GrepTool" }),
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockConfigRepo
            .Setup(r => r.GetByTenantAndNameAsync(tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbConfig);

        SetupToolRegistryMock();

        _mockAIService
            .Setup(s => s.CreateAgentAsync(
                It.IsAny<AIAgentConfiguration>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    private void SetupToolRegistryMock()
    {
        var tools = new List<object>
        {
            Mock.Of<ITool>(t => t.Name == "ReadFileTool"),
            Mock.Of<ITool>(t => t.Name == "GrepTool")
        };

        _mockToolRegistry
            .Setup(r => r.GetTools(It.IsAny<Guid>(), It.IsAny<string[]>()))
            .Returns(tools);
    }

    private List<AgentStreamChunk> CreateSuccessfulResponseChunks()
    {
        return new List<AgentStreamChunk>
        {
            new AgentStreamChunk { Type = ChunkType.Reasoning, Content = "Analyzing codebase..." },
            new AgentStreamChunk { Type = ChunkType.ToolUse, Content = "ReadFileTool" },
            new AgentStreamChunk { Type = ChunkType.ToolResult, Content = "File contents" },
            new AgentStreamChunk { Type = ChunkType.Response, Content = "Analysis complete. The codebase uses a layered architecture." },
            new AgentStreamChunk { Type = ChunkType.Complete, IsFinal = true }
        };
    }
}

/// <summary>
/// Extension methods for creating async enumerables from lists.
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
