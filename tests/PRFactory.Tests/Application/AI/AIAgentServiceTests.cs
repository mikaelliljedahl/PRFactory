using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.AI;
using PRFactory.Core.Application.LLM;
using PRFactory.Infrastructure.AI;
using Xunit;

namespace PRFactory.Tests.Application.AI;

public class AIAgentServiceTests
{
    private readonly Mock<ILlmProviderFactory> _mockLlmProviderFactory;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<ILogger<AIAgentService>> _mockLogger;

    public AIAgentServiceTests()
    {
        _mockLlmProviderFactory = new Mock<ILlmProviderFactory>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockLogger = new Mock<ILogger<AIAgentService>>();

        _mockLlmProviderFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(_mockLlmProvider.Object);
    }

    private AIAgentService CreateService()
    {
        return new AIAgentService(
            _mockLlmProviderFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAgentAsync_WithValidConfiguration_ReturnsAgent()
    {
        var service = CreateService();
        var config = new AIAgentConfiguration
        {
            AgentName = "TestAgent",
            Instructions = "Test instructions",
            EnabledTools = new[] { "tool1", "tool2" },
            MaxTokens = 4000,
            Temperature = 0.5f,
            StreamingEnabled = true
        };

        var tools = new List<ITool>
        {
            CreateMockTool("tool1", "Tool 1 description"),
            CreateMockTool("tool2", "Tool 2 description")
        };

        var agent = await service.CreateAgentAsync(config, tools);

        Assert.NotNull(agent);
    }

    [Fact]
    public async Task CreateAgentAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.CreateAgentAsync(null!, new List<ITool>()));
    }

    [Fact]
    public async Task ExecuteAgentAsync_WithValidInput_YieldsChunks()
    {
        var service = CreateService();
        var config = new AIAgentConfiguration
        {
            AgentName = "TestAgent",
            Instructions = "Test instructions",
            EnabledTools = new[] { "TestTool" },
            MaxTokens = 4000,
            Temperature = 0.3f,
            StreamingEnabled = false
        };

        var tools = new List<ITool> { CreateMockTool("TestTool", "Test tool") };
        var agent = await service.CreateAgentAsync(config, tools);

        _mockLlmProvider
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Success = true, Content = "AI response", Usage = new LlmUsageMetrics() });

        var chunks = new List<AgentStreamChunk>();
        await foreach (var chunk in service.ExecuteAgentAsync(agent, "Test message", new List<AgentChatMessage>()))
        {
            chunks.Add(chunk);
        }

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Type == ChunkType.Reasoning);
        Assert.Contains(chunks, c => c.Type == ChunkType.Response);
        Assert.Contains(chunks, c => c.Type == ChunkType.Complete && c.IsFinal);
    }

    private ITool CreateMockTool(string name, string description)
    {
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(name);
        mockTool.Setup(t => t.Description).Returns(description);
        return mockTool.Object;
    }
}
