using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Controllers;
using System.Text;
using Xunit;

namespace PRFactory.Web.Tests.Controllers;

public class AgentChatControllerTests
{
    private readonly Mock<IAgentChatService> _mockChatService;
    private readonly Mock<ILogger<AgentChatController>> _mockLogger;
    private readonly Guid _ticketId = Guid.NewGuid();

    public AgentChatControllerTests()
    {
        _mockChatService = new Mock<IAgentChatService>();
        _mockLogger = new Mock<ILogger<AgentChatController>>();
    }

    private AgentChatController CreateController()
    {
        var controller = new AgentChatController(_mockChatService.Object, _mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task StreamAgentResponse_WithValidRequest_SetsCorrectHeaders()
    {
        // Arrange
        var controller = CreateController();
        var message = "Test message";

        var chunks = new List<AgentStreamChunk>
        {
            new AgentStreamChunk
            {
                Type = ChunkType.Reasoning,
                Content = "Analyzing...",
                IsFinal = false
            },
            new AgentStreamChunk
            {
                Type = ChunkType.Complete,
                Content = "Done",
                IsFinal = true
            }
        };

        _mockChatService
            .Setup(x => x.StreamResponseAsync(_ticketId, message, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        await controller.StreamAgentResponse(_ticketId, message, CancellationToken.None);

        // Assert
        var headers = controller.Response.Headers;
        Assert.Equal("text/event-stream", headers["Content-Type"]);
        Assert.Equal("no-cache", headers["Cache-Control"]);
        Assert.Equal("keep-alive", headers["Connection"]);
        Assert.Equal("no", headers["X-Accel-Buffering"]);
    }

    [Fact]
    public async Task StreamAgentResponse_WithEmptyTicketId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var message = "Test message";

        // Act
        await controller.StreamAgentResponse(Guid.Empty, message, CancellationToken.None);

        // Assert
        Assert.Equal(400, controller.Response.StatusCode);

        controller.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(controller.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("Ticket ID is required", body);
    }

    [Fact]
    public async Task StreamAgentResponse_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        await controller.StreamAgentResponse(_ticketId, "", CancellationToken.None);

        // Assert
        Assert.Equal(400, controller.Response.StatusCode);

        controller.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(controller.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("Message is required", body);
    }

    [Fact]
    public async Task StreamAgentResponse_WithValidRequest_WritesSSEFormattedData()
    {
        // Arrange
        var controller = CreateController();
        var message = "Test message";

        var chunks = new List<AgentStreamChunk>
        {
            new AgentStreamChunk
            {
                Type = ChunkType.Response,
                Content = "Test response",
                IsFinal = false
            }
        };

        _mockChatService
            .Setup(x => x.StreamResponseAsync(_ticketId, message, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        await controller.StreamAgentResponse(_ticketId, message, CancellationToken.None);

        // Assert
        controller.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(controller.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("data: ", body);
        Assert.Contains("Test response", body);
        Assert.Contains("\n\n", body);
    }

    [Fact]
    public async Task GetHistory_WithValidTicketId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var messages = new List<AgentChatMessage>
        {
            new AgentChatMessage
            {
                Type = MessageType.UserMessage,
                Content = "User message"
            }
        };

        _mockChatService
            .Setup(x => x.GetChatHistoryAsync(_ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await controller.GetHistory(_ticketId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMessages = Assert.IsType<List<AgentChatMessage>>(okResult.Value);
        Assert.Single(returnedMessages);
        Assert.Equal("User message", returnedMessages[0].Content);
    }

    [Fact]
    public async Task GetHistory_WithEmptyTicketId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetHistory(Guid.Empty, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task GetHistory_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController();

        _mockChatService
            .Setup(x => x.GetChatHistoryAsync(_ticketId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.GetHistory(_ticketId, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task AnswerFollowUpQuestion_WithValidData_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var questionId = "q-123";
        var answer = "This is my answer";

        _mockChatService
            .Setup(x => x.AnswerFollowUpQuestionAsync(
                _ticketId,
                questionId,
                answer,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Answer recorded");

        // Act
        var result = await controller.AnswerFollowUpQuestion(
            _ticketId,
            questionId,
            answer,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Answer recorded", okResult.Value);
    }

    [Fact]
    public async Task AnswerFollowUpQuestion_WithEmptyTicketId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.AnswerFollowUpQuestion(
            Guid.Empty,
            "q-123",
            "answer",
            CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task AnswerFollowUpQuestion_WithEmptyQuestionId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.AnswerFollowUpQuestion(
            _ticketId,
            "",
            "answer",
            CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task AnswerFollowUpQuestion_WithEmptyAnswer_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.AnswerFollowUpQuestion(
            _ticketId,
            "q-123",
            "",
            CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task AnswerFollowUpQuestion_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController();

        _mockChatService
            .Setup(x => x.AnswerFollowUpQuestionAsync(
                _ticketId,
                "q-123",
                "answer",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await controller.AnswerFollowUpQuestion(
            _ticketId,
            "q-123",
            "answer",
            CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
