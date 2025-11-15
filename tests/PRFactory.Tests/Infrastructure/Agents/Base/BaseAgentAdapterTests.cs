using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Base;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Agents.Base;

/// <summary>
/// Tests for BaseAgentAdapter abstract class functionality.
/// </summary>
public class BaseAgentAdapterTests
{
    private readonly Mock<IAgentFactory> _mockAgentFactory;
    private readonly ILogger<TestAgentAdapter> _logger;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public BaseAgentAdapterTests()
    {
        _mockAgentFactory = new Mock<IAgentFactory>();
        _logger = NullLogger<TestAgentAdapter>.Instance;
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithExistingConfig_ReturnsDbConfiguration()
    {
        // Arrange
        var expectedConfig = new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            AgentName = "TestAgent",
            Instructions = "Test instructions from DB",
            MaxTokens = 5000,
            Temperature = 0.7f,
            StreamingEnabled = false,
            EnabledTools = "[\"Tool1\", \"Tool2\"]",
            RequiresApproval = true
        };

        _mockAgentFactory
            .Setup(f => f.GetConfigurationAsync(_testTenantId, "TestAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        var result = await adapter.LoadConfigurationAsync(_testTenantId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig.Id, result.Id);
        Assert.Equal(expectedConfig.Instructions, result.Instructions);
        Assert.Equal(expectedConfig.MaxTokens, result.MaxTokens);
        Assert.Equal(expectedConfig.Temperature, result.Temperature);
        Assert.Equal(expectedConfig.StreamingEnabled, result.StreamingEnabled);
        Assert.Equal(expectedConfig.EnabledTools, result.EnabledTools);
        Assert.Equal(expectedConfig.RequiresApproval, result.RequiresApproval);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithMissingConfig_ReturnsDefaultConfiguration()
    {
        // Arrange
        _mockAgentFactory
            .Setup(f => f.GetConfigurationAsync(_testTenantId, "TestAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        var result = await adapter.LoadConfigurationAsync(_testTenantId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTenantId, result.TenantId);
        Assert.Equal("TestAgent", result.AgentName);
        Assert.Equal("Default test instructions", result.Instructions);
        Assert.Equal(8000, result.MaxTokens);
        Assert.Equal(0.3f, result.Temperature);
        Assert.True(result.StreamingEnabled);
        Assert.Equal("[]", result.EnabledTools);
        Assert.False(result.RequiresApproval);
    }

    [Fact]
    public async Task LoadConfigurationAsync_SetsInternalConfigurationField()
    {
        // Arrange
        var expectedConfig = new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            AgentName = "TestAgent",
            Instructions = "Test instructions"
        };

        _mockAgentFactory
            .Setup(f => f.GetConfigurationAsync(_testTenantId, "TestAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        await adapter.LoadConfigurationAsync(_testTenantId, CancellationToken.None);

        // Assert
        Assert.NotNull(adapter.GetConfiguration());
        Assert.Equal(expectedConfig.Instructions, adapter.GetConfiguration()!.Instructions);
    }

    [Fact]
    public void CreateDefaultConfiguration_ReturnsValidConfiguration()
    {
        // Arrange
        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        var result = adapter.CreateDefaultConfiguration(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTenantId, result.TenantId);
        Assert.Equal("TestAgent", result.AgentName);
        Assert.Equal("Default test instructions", result.Instructions);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void ApplyConfigurationToContext_SetsAllMetadataFields()
    {
        // Arrange
        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);
        var context = new AgentContext
        {
            TenantId = _testTenantId.ToString(),
            TicketId = Guid.NewGuid().ToString()
        };

        var config = new AgentConfiguration
        {
            Instructions = "Test instructions",
            MaxTokens = 5000,
            Temperature = 0.7f,
            EnabledTools = "[\"Tool1\"]",
            StreamingEnabled = false,
            RequiresApproval = true
        };

        // Act
        adapter.ApplyConfigurationToContext(context, config);

        // Assert
        Assert.Equal("Test instructions", context.Metadata["AgentInstructions"]);
        Assert.Equal(5000, context.Metadata["MaxTokens"]);
        Assert.Equal(0.7f, context.Metadata["Temperature"]);
        Assert.Equal("[\"Tool1\"]", context.Metadata["EnabledTools"]);
        Assert.Equal(false, context.Metadata["StreamingEnabled"]);
        Assert.Equal(true, context.Metadata["RequiresApproval"]);
    }

    [Fact]
    public void ApplyConfigurationToContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);
        var config = new AgentConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            adapter.ApplyConfigurationToContext(null!, config));
    }

    [Fact]
    public void ApplyConfigurationToContext_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);
        var context = new AgentContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            adapter.ApplyConfigurationToContext(context, null!));
    }

    [Fact]
    public void Constructor_WithNullAgentFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestAgentAdapter(null!, _logger));
    }

    [Fact]
    public async Task GetDefaultInstructions_ReturnsNonEmptyString_ThroughFallbackConfiguration()
    {
        // Arrange
        _mockAgentFactory
            .Setup(f => f.GetConfigurationAsync(_testTenantId, "TestAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration?)null);

        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        var config = await adapter.LoadConfigurationAsync(_testTenantId, CancellationToken.None);

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Instructions);
        Assert.NotEmpty(config.Instructions);
        Assert.Equal("Default test instructions", config.Instructions);
    }

    [Fact]
    public void ConfiguredAgentName_ReturnsCorrectValue()
    {
        // Arrange
        var adapter = new TestAgentAdapter(_mockAgentFactory.Object, _logger);

        // Act
        var result = adapter.GetConfiguredAgentName();

        // Assert
        Assert.Equal("TestAgent", result);
    }

    /// <summary>
    /// Concrete test implementation of BaseAgentAdapter for testing.
    /// </summary>
    public class TestAgentAdapter : BaseAgentAdapter
    {
        public override string Name => "TestAgentAdapter";
        public override string Description => "Test adapter for unit testing";
        protected override string ConfiguredAgentName => "TestAgent";

        public TestAgentAdapter(IAgentFactory agentFactory, ILogger<TestAgentAdapter> logger)
            : base(agentFactory, logger)
        {
        }

        protected override Task<AgentResult> ExecuteAsync(
            AgentContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new AgentResult
            {
                Status = AgentStatus.Completed
            });
        }

        protected override string GetDefaultInstructions()
        {
            return "Default test instructions";
        }

        // Expose protected members for testing
        public new Task<AgentConfiguration> LoadConfigurationAsync(Guid tenantId, CancellationToken cancellationToken)
            => base.LoadConfigurationAsync(tenantId, cancellationToken);

        public new AgentConfiguration CreateDefaultConfiguration(Guid tenantId)
            => base.CreateDefaultConfiguration(tenantId);

        public new void ApplyConfigurationToContext(AgentContext context, AgentConfiguration config)
            => base.ApplyConfigurationToContext(context, config);

        public AgentConfiguration? GetConfiguration() => _configuration;

        public string GetConfiguredAgentName() => ConfiguredAgentName;
    }
}
