using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Core;

public class ToolRegistryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public ToolRegistryTests()
    {
        var services = new ServiceCollection();

        // Register test tools
        services.AddTransient<TestToolA>();
        services.AddTransient<TestToolB>();
        services.AddTransient<ITool, TestToolA>();
        services.AddTransient<ITool, TestToolB>();

        // Register dependencies
        services.AddLogging();
        _mockTenantContext = new Mock<ITenantContext>();
        services.AddSingleton(_mockTenantContext.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_DiscoversTools()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();

        // Act
        var registry = new ToolRegistry(_serviceProvider, logger);
        var allTools = registry.GetAllToolsTyped().ToList();

        // Assert
        Assert.NotEmpty(allTools);
    }

    [Fact]
    public void GetAllTools_ReturnsAllRegisteredTools()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);

        // Act
        var tools = registry.GetAllToolsTyped().ToList();

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, t => t.Name == "TestToolA");
        Assert.Contains(tools, t => t.Name == "TestToolB");
    }

    [Fact]
    public void GetTool_WithValidName_ReturnsTool()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);

        // Act
        var tool = registry.GetToolTyped("TestToolA");

        // Assert
        Assert.NotNull(tool);
        Assert.Equal("TestToolA", tool.Name);
    }

    [Fact]
    public void GetTool_WithInvalidName_ReturnsNull()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);

        // Act
        var tool = registry.GetToolTyped("NonExistentTool");

        // Assert
        Assert.Null(tool);
    }

    [Fact]
    public void GetTool_IsCaseInsensitive()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);

        // Act
        var tool1 = registry.GetToolTyped("TestToolA");
        var tool2 = registry.GetToolTyped("testtoola");
        var tool3 = registry.GetToolTyped("TESTTOOLA");

        // Assert
        Assert.NotNull(tool1);
        Assert.NotNull(tool2);
        Assert.NotNull(tool3);
        Assert.Equal(tool1.Name, tool2.Name);
        Assert.Equal(tool1.Name, tool3.Name);
    }

    [Fact]
    public void GetTools_WithEnabledToolNames_ReturnsFilteredTools()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);
        var tenantId = Guid.NewGuid();
        var enabledTools = new[] { "TestToolA" };

        // Act
        var tools = registry.GetToolsTyped(tenantId, enabledTools).ToList();

        // Assert
        Assert.Single(tools);
        Assert.Equal("TestToolA", tools[0].Name);
    }

    [Fact]
    public void GetTools_WithMultipleEnabledTools_ReturnsAllMatching()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);
        var tenantId = Guid.NewGuid();
        var enabledTools = new[] { "TestToolA", "TestToolB" };

        // Act
        var tools = registry.GetToolsTyped(tenantId, enabledTools).ToList();

        // Assert
        Assert.Equal(2, tools.Count);
    }

    [Fact]
    public void GetTools_WithNoEnabledTools_ReturnsEmpty()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);
        var tenantId = Guid.NewGuid();
        var enabledTools = Array.Empty<string>();

        // Act
        var tools = registry.GetToolsTyped(tenantId, enabledTools).ToList();

        // Assert
        Assert.Empty(tools);
    }

    [Fact]
    public void GetTools_WithNonExistentToolNames_ReturnsEmpty()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);
        var tenantId = Guid.NewGuid();
        var enabledTools = new[] { "NonExistent1", "NonExistent2" };

        // Act
        var tools = registry.GetToolsTyped(tenantId, enabledTools).ToList();

        // Assert
        Assert.Empty(tools);
    }

    [Fact]
    public void GetTools_IsCaseInsensitive()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ToolRegistry>>();
        var registry = new ToolRegistry(_serviceProvider, logger);
        var tenantId = Guid.NewGuid();
        var enabledTools = new[] { "testtoola", "TESTTOOLB" };

        // Act
        var tools = registry.GetToolsTyped(tenantId, enabledTools).ToList();

        // Assert
        Assert.Equal(2, tools.Count);
    }

    // Test tool implementations
    public class TestToolA : ToolBase
    {
        public override string Name => "TestToolA";
        public override string Description => "Test tool A";

        public TestToolA(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            return Task.FromResult("Tool A output");
        }
    }

    public class TestToolB : ToolBase
    {
        public override string Name => "TestToolB";
        public override string Description => "Test tool B";

        public TestToolB(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            return Task.FromResult("Tool B output");
        }
    }
}
