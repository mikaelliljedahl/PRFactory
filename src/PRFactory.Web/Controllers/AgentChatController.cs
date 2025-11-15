using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace PRFactory.Web.Controllers;

/// <summary>
/// Controller for AG-UI protocol agent chat interactions.
/// Provides Server-Sent Events (SSE) endpoint for real-time agent streaming.
///
/// AG-UI PROTOCOL COMPLIANCE:
/// This controller implements the Microsoft AG-UI protocol specification for
/// agent-to-UI communication using Server-Sent Events (SSE).
///
/// Why Custom Implementation Instead of MapAGUI():
/// - Multi-Agent Routing: Dynamically selects agents based on tenant configuration
/// - Chat History: Persists conversations to database with entity tracking
/// - Tenant Isolation: Applies tenant context before agent creation
/// - Advanced Features: Follow-up questions, workflow state sync, SignalR integration
///
/// Protocol Specification:
/// - Endpoint: GET /api/agent/chat/stream?ticketId={guid}&amp;message={string}
/// - Response: text/event-stream with chunks in format "data: {json}\n\n"
/// - Chunk Types: Reasoning, ToolUse, Response, Complete, Error
/// - Conversation: Multi-turn support via chat history
///
/// Package Reference: Microsoft.Agents.AI.Hosting.AGUI.AspNetCore v1.0.0-preview
/// is included for future migration when multi-agent scenarios are supported.
///
/// See Also: AGUIConfiguration.cs for detailed design rationale.
/// </summary>
[ApiController]
[Route("api/agent/chat")]
[Produces("application/json")]
public class AgentChatController : ControllerBase
{
    private readonly IAgentChatService _chatService;
    private readonly ILogger<AgentChatController> _logger;

    public AgentChatController(
        IAgentChatService chatService,
        ILogger<AgentChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Streams agent response using Server-Sent Events (SSE) following AG-UI protocol.
    /// </summary>
    /// <remarks>
    /// AG-UI Protocol Compliance:
    /// - Content-Type: text/event-stream
    /// - Format: data: {json}\n\n
    /// - Chunk Types: Reasoning, ToolUse, Response, Complete, Error
    ///
    /// Example SSE Stream:
    /// data: {"type":"Reasoning","content":"Analyzing ticket...","chunkId":"1","isFinal":false}
    ///
    /// data: {"type":"ToolUse","content":"Searching codebase...","chunkId":"2","isFinal":false}
    ///
    /// data: {"type":"Response","content":"I found 3 files...","chunkId":"3","isFinal":false}
    ///
    /// data: {"type":"Complete","content":"","chunkId":"4","isFinal":true}
    ///
    /// </remarks>
    /// <param name="ticketId">The ticket ID to associate the conversation with</param>
    /// <param name="message">The user's message to the agent</param>
    /// <param name="cancellationToken">Cancellation token for client disconnect</param>
    /// <returns>SSE stream of AG-UI protocol chunks</returns>
    /// <response code="200">SSE stream started successfully (text/event-stream)</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task StreamAgentResponse(
        [FromQuery] Guid ticketId,
        [FromQuery] string message,
        CancellationToken cancellationToken)
    {
        if (ticketId == Guid.Empty)
        {
            _logger.LogWarning("Stream request with empty ticket ID");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Ticket ID is required", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Stream request with empty message for ticket {TicketId}", ticketId);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Message is required", cancellationToken);
            return;
        }

        _logger.LogInformation(
            "Starting SSE stream for ticket {TicketId} with message: {Message}",
            ticketId,
            message);

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (var chunk in _chatService.StreamResponseAsync(
                ticketId,
                message,
                cancellationToken))
            {
                var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                _logger.LogDebug(
                    "Sent SSE chunk {ChunkId} of type {ChunkType} for ticket {TicketId}",
                    chunk.ChunkId,
                    chunk.Type,
                    ticketId);
            }

            _logger.LogInformation("SSE stream completed for ticket {TicketId}", ticketId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SSE stream cancelled by client for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE stream for ticket {TicketId}", ticketId);

            var errorChunk = new AgentStreamChunk
            {
                Type = ChunkType.Error,
                Content = "Stream error occurred",
                IsFinal = true,
                Metadata = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                }
            };

            var errorJson = JsonSerializer.Serialize(errorChunk, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets chat history for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chat messages</returns>
    /// <response code="200">Chat history retrieved successfully</response>
    /// <response code="400">Invalid ticket ID</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<AgentChatMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AgentChatMessage>>> GetHistory(
        [FromQuery] Guid ticketId,
        CancellationToken cancellationToken)
    {
        if (ticketId == Guid.Empty)
        {
            _logger.LogWarning("History request with empty ticket ID");
            return BadRequest(new { error = "Ticket ID is required" });
        }

        _logger.LogInformation("Getting chat history for ticket {TicketId}", ticketId);

        try
        {
            var history = await _chatService.GetChatHistoryAsync(ticketId, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat history for ticket {TicketId}", ticketId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve chat history" });
        }
    }

    /// <summary>
    /// Answers a follow-up question from the agent.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="questionId">The question ID</param>
    /// <param name="answer">The answer to the question</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation message</returns>
    /// <response code="200">Answer recorded successfully</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost("answer")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> AnswerFollowUpQuestion(
        [FromQuery] Guid ticketId,
        [FromQuery] string questionId,
        [FromBody] string answer,
        CancellationToken cancellationToken)
    {
        if (ticketId == Guid.Empty)
        {
            _logger.LogWarning("Answer request with empty ticket ID");
            return BadRequest(new { error = "Ticket ID is required" });
        }

        if (string.IsNullOrWhiteSpace(questionId))
        {
            _logger.LogWarning("Answer request with empty question ID for ticket {TicketId}", ticketId);
            return BadRequest(new { error = "Question ID is required" });
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            _logger.LogWarning(
                "Answer request with empty answer for ticket {TicketId}, question {QuestionId}",
                ticketId,
                questionId);
            return BadRequest(new { error = "Answer is required" });
        }

        _logger.LogInformation(
            "Answering question {QuestionId} for ticket {TicketId}",
            questionId,
            ticketId);

        try
        {
            var result = await _chatService.AnswerFollowUpQuestionAsync(
                ticketId,
                questionId,
                answer,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to answer question {QuestionId} for ticket {TicketId}",
                questionId,
                ticketId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Failed to record answer" });
        }
    }
}
