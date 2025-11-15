using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Agents;

/// <summary>
/// Tests for AgentFactory implementation.
/// Validates agent creation, configuration loading, and validation logic.
/// </summary>
public class AgentFactoryTests
{
    private readonly Mock<IAgentConfigurationRepository> _mockConfigRepo;
    private readonly Mock<ILogger<AgentFactory>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentFactory _agentFactory;

    public AgentFactoryTests()
    {
        _mockConfigRepo = new Mock<IAgentConfigurationRepository>();
        _mockLogger = new Mock<ILogger<AgentFactory>>();

        // Setup service provider with necessary dependencies for agent creation
        var services = new ServiceCollection();

        // Register all agent types
        services.AddTransient<AnalysisAgent>();
        services.AddTransient<PlanningAgent>();
        services.AddTransient<ImplementationAgent>();
        services.AddTransient<RepositoryCloneAgent>();
        services.AddTransient<QuestionGenerationAgent>();
        services.AddTransient<AnswerProcessingAgent>();
        services.AddTransient<GitPlanAgent>();
        services.AddTransient<GitCommitAgent>();
        services.AddTransient<PullRequestAgent>();
        services.AddTransient<JiraPostAgent>();
        services.AddTransient<TicketUpdateGenerationAgent>();
        services.AddTransient<TicketUpdatePostAgent>();
        services.AddTransient<PRFactory.Infrastructure.Agents.Specialized.CodeReviewAgent>();
        services.AddTransient<PRFactory.Infrastructure.Agents.Specialized.PostReviewCommentsAgent>();
        services.AddTransient<PRFactory.Infrastructure.Agents.Specialized.PostApprovalCommentAgent>();

        // Mock dependencies required by agents
        services.AddSingleton(Mock.Of<ILogger<AnalysisAgent>>());
        services.AddSingleton(Mock.Of<ILogger<PlanningAgent>>());
        services.AddSingleton(Mock.Of<ILogger<ImplementationAgent>>());
        services.AddSingleton(Mock.Of<ILogger<RepositoryCloneAgent>>());
        services.AddSingleton(Mock.Of<ILogger<QuestionGenerationAgent>>());
        services.AddSingleton(Mock.Of<ILogger<AnswerProcessingAgent>>());
        services.AddSingleton(Mock.Of<ILogger<GitPlanAgent>>());
        services.AddSingleton(Mock.Of<ILogger<GitCommitAgent>>());
        services.AddSingleton(Mock.Of<ILogger<PullRequestAgent>>());
        services.AddSingleton(Mock.Of<ILogger<JiraPostAgent>>());
        services.AddSingleton(Mock.Of<ILogger<TicketUpdateGenerationAgent>>());
        services.AddSingleton(Mock.Of<ILogger<TicketUpdatePostAgent>>());
        services.AddSingleton(Mock.Of<ILogger<PRFactory.Infrastructure.Agents.Specialized.CodeReviewAgent>>());
        services.AddSingleton(Mock.Of<ILogger<PRFactory.Infrastructure.Agents.Specialized.PostReviewCommentsAgent>>());
        services.AddSingleton(Mock.Of<ILogger<PRFactory.Infrastructure.Agents.Specialized.PostApprovalCommentAgent>>());

        // Mock other agent dependencies
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.Services.ICliAgent>());
        services.AddSingleton(Mock.Of<PRFactory.Infrastructure.Claude.IContextBuilder>());
        services.AddSingleton(Mock.Of<ITicketRepository>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.Services.IArchitectureContextService>());
        services.AddSingleton(Mock.Of<PRFactory.Infrastructure.Git.ILocalGitService>());
        services.AddSingleton(Mock.Of<PRFactory.Infrastructure.Git.IGitPlatformService>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.Services.ITicketUpdateService>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.Services.IQuestionApplicationService>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.LLM.ILlmProviderFactory>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.LLM.IPromptLoaderService>());
        services.AddSingleton(Mock.Of<PRFactory.Domain.Interfaces.ICodeReviewResultRepository>());
        services.AddSingleton(Mock.Of<PRFactory.Core.Application.Services.IWorkspaceService>());

        _serviceProvider = services.BuildServiceProvider();
        _agentFactory = new AgentFactory(_mockConfigRepo.Object, _serviceProvider, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAgentAsync_WithValidConfiguration_ReturnsAgentInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var agent = await _agentFactory.CreateAgentAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<AnalysisAgent>(agent);
        var typedAgent = (BaseAgent)agent;
        Assert.Equal("AnalysisAgent", typedAgent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithPlanningAgent_ReturnsPlanningAgentInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "PlanningAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var agent = await _agentFactory.CreateAgentAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<PlanningAgent>(agent);
        var typedAgent = (BaseAgent)agent;
        Assert.Equal("PlanningAgent", typedAgent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithImplementationAgent_ReturnsImplementationAgentInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "ImplementationAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var agent = await _agentFactory.CreateAgentAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<ImplementationAgent>(agent);
        var typedAgent = (BaseAgent)agent;
        Assert.Equal("ImplementationAgent", typedAgent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithCodeReviewAgent_ReturnsCodeReviewAgentInstance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "CodeReviewAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var agent = await _agentFactory.CreateAgentAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<PRFactory.Infrastructure.Agents.Specialized.CodeReviewAgent>(agent);
        var typedAgent = (BaseAgent)agent;
        Assert.Equal("code-review-agent", typedAgent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithNonExistentConfiguration_ThrowsAgentConfigurationNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AgentConfigurationNotFoundException>(
            () => _agentFactory.CreateAgentAsync(tenantId, agentName));

        Assert.Equal(tenantId, exception.TenantId);
        Assert.Equal(agentName, exception.AgentName);
    }

    [Fact]
    public async Task CreateAgentAsync_WithUnregisteredAgentType_ThrowsAgentTypeNotRegisteredException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "NonExistentAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AgentTypeNotRegisteredException>(
            () => _agentFactory.CreateAgentAsync(tenantId, agentName));

        Assert.Equal(agentName, exception.AgentName);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithExistingConfiguration_ReturnsConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.GetConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agentName, result.AgentName);
        Assert.Equal(tenantId, result.TenantId);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNonExistentConfiguration_ReturnsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        // Act
        var result = await _agentFactory.GetConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithMissingConfiguration_ReturnsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithUnregisteredAgentType_ReturnsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "NonExistentAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not registered"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithEmptyInstructions_ReturnsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.Instructions = string.Empty;

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Instructions are empty", result.Warnings[0]);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidMaxTokens_ReturnsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.MaxTokens = 0;

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MaxTokens must be greater than 0"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithVeryHighMaxTokens_ReturnsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.MaxTokens = 250000;

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("very high"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidTemperature_ReturnsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.Temperature = 1.5f;

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Temperature must be between"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidEnabledToolsJson_ReturnsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.EnabledTools = "invalid json {";

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("EnabledTools is not valid JSON"));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidEnabledToolsJson_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var agentName = "AnalysisAgent";
        var configuration = CreateValidConfiguration(tenantId, agentName);
        configuration.EnabledTools = "[\"ReadFile\", \"Grep\"]";

        _mockConfigRepo.Setup(r => r.GetByTenantAndNameAsync(tenantId, agentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        // Act
        var result = await _agentFactory.ValidateConfigurationAsync(tenantId, agentName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private AgentConfiguration CreateValidConfiguration(Guid tenantId, string agentName)
    {
        return new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = agentName,
            Instructions = "Test instructions for agent",
            EnabledTools = "[\"ReadFile\", \"Grep\"]",
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
