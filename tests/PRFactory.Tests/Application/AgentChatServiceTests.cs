using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.AgentUI;
using PRFactory.Infrastructure.Agents.Base;
using System.Text.Json;
using Xunit;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;
using DomainCheckpoint = PRFactory.Domain.Entities.Checkpoint;

namespace PRFactory.Tests.Application;

public class AgentChatServiceTests
{
    private readonly Mock<IAgentFactory> _mockAgentFactory;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<DomainCheckpointRepository> _mockCheckpointRepository;
    private readonly Mock<ILogger<AgentChatService>> _mockLogger;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _ticketId = Guid.NewGuid();

    public AgentChatServiceTests()
    {
        _mockAgentFactory = new Mock<IAgentFactory>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockCheckpointRepository = new Mock<DomainCheckpointRepository>();
        _mockLogger = new Mock<ILogger<AgentChatService>>();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);
    }

    private AgentChatService CreateService()
    {
        return new AgentChatService(
            _mockAgentFactory.Object,
            _mockTenantContext.Object,
            _mockCheckpointRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StreamResponseAsync_WithValidMessage_YieldsReasoningAndResponseChunks()
    {
        // Arrange
        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["Response"] = "This is the agent response"
            }
        };

        var testAgent = new TestAgent(agentResult);

        _mockAgentFactory
            .Setup(x => x.CreateAgentAsync(_tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAgent);

        var service = CreateService();

        // Act
        var chunks = new List<AgentStreamChunk>();
        await foreach (var chunk in service.StreamResponseAsync(_ticketId, "Test message"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Type == ChunkType.Reasoning);
        Assert.Contains(chunks, c => c.Type == ChunkType.Response);
        Assert.Contains(chunks, c => c.Type == ChunkType.Complete);

        var responseChunk = chunks.First(c => c.Type == ChunkType.Response);
        Assert.Equal("This is the agent response", responseChunk.Content);

        var completeChunk = chunks.First(c => c.Type == ChunkType.Complete);
        Assert.True(completeChunk.IsFinal);
    }

    private class TestAgent : BaseAgent
    {
        private readonly AgentResult _result;
        private readonly Exception? _exception;

        public TestAgent(AgentResult result, Exception? exception = null)
            : base(Mock.Of<ILogger<BaseAgent>>(), "test-agent-id")
        {
            _result = result;
            _exception = exception;
        }

        public override string Name => "AnalyzerAgent";
        public override string Description => "Test analyzer agent";

        protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            await Task.CompletedTask;
            return _result;
        }
    }

    [Fact]
    public async Task StreamResponseAsync_WhenAgentFactoryThrows_YieldsErrorChunk()
    {
        // Arrange
        _mockAgentFactory
            .Setup(x => x.CreateAgentAsync(_tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Agent creation failed"));

        var service = CreateService();

        // Act
        var chunks = new List<AgentStreamChunk>();
        await foreach (var chunk in service.StreamResponseAsync(_ticketId, "Test message"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Type == ChunkType.Error);

        var errorChunk = chunks.First(c => c.Type == ChunkType.Error);
        Assert.Contains("Failed to initialize agent", errorChunk.Content);
        Assert.True(errorChunk.IsFinal);
    }

    [Fact]
    public async Task StreamResponseAsync_WhenAgentExecutionFails_YieldsResponseWithFailedStatus()
    {
        // Arrange
        var testAgent = new TestAgent(
            new AgentResult
            {
                Status = AgentStatus.Failed,
                Output = new Dictionary<string, object>
                {
                    ["Response"] = "Agent encountered an error"
                }
            });

        _mockAgentFactory
            .Setup(x => x.CreateAgentAsync(_tenantId, "AnalyzerAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAgent);

        var service = CreateService();

        // Act
        var chunks = new List<AgentStreamChunk>();
        await foreach (var chunk in service.StreamResponseAsync(_ticketId, "Test message"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Type == ChunkType.Response);
        Assert.Contains(chunks, c => c.Type == ChunkType.Complete);

        var responseChunk = chunks.First(c => c.Type == ChunkType.Response);
        Assert.Equal("Agent encountered an error", responseChunk.Content);
        Assert.Equal("Failed", responseChunk.Metadata["status"]);
    }

    [Fact]
    public async Task GetChatHistoryAsync_WithNoCheckpoints_ReturnsEmptyList()
    {
        // Arrange
        _mockCheckpointRepository
            .Setup(x => x.GetCheckpointsByTicketIdAsync(_ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainCheckpoint>());

        var service = CreateService();

        // Act
        var result = await service.GetChatHistoryAsync(_ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChatHistoryAsync_WithCheckpointsContainingHistory_ReturnsMessages()
    {
        // Arrange
        var messages = new List<AgentChatMessage>
        {
            new AgentChatMessage
            {
                Id = "msg-1",
                Type = MessageType.UserMessage,
                Content = "User question",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new AgentChatMessage
            {
                Id = "msg-2",
                Type = MessageType.AssistantMessage,
                Content = "Agent response",
                Timestamp = DateTime.UtcNow.AddMinutes(-4)
            }
        };

        var conversationHistory = JsonSerializer.Serialize(messages);

        var checkpoint = DomainCheckpoint.Create(
            _tenantId,
            _ticketId,
            "RefinementGraph",
            "after_analysis",
            "{}");

        checkpoint.UpdateAgentState(conversationHistory: conversationHistory);

        _mockCheckpointRepository
            .Setup(x => x.GetCheckpointsByTicketIdAsync(_ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainCheckpoint> { checkpoint });

        var service = CreateService();

        // Act
        var result = await service.GetChatHistoryAsync(_ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("User question", result[0].Content);
        Assert.Equal("Agent response", result[1].Content);
    }

    [Fact]
    public async Task GetChatHistoryAsync_WithInvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var checkpoint = DomainCheckpoint.Create(
            _tenantId,
            _ticketId,
            "RefinementGraph",
            "after_analysis",
            "{}");

        checkpoint.UpdateAgentState(conversationHistory: "invalid-json");

        _mockCheckpointRepository
            .Setup(x => x.GetCheckpointsByTicketIdAsync(_ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainCheckpoint> { checkpoint });

        var service = CreateService();

        // Act
        var result = await service.GetChatHistoryAsync(_ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnswerFollowUpQuestionAsync_ReturnsConfirmationMessage()
    {
        // Arrange
        var service = CreateService();
        var questionId = "q-123";
        var answer = "This is my answer";

        // Act
        var result = await service.AnswerFollowUpQuestionAsync(_ticketId, questionId, answer);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(questionId, result);
        Assert.Contains(answer, result);
    }
}
