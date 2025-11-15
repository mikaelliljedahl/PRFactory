using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Base.Middleware;

namespace PRFactory.Tests.Infrastructure.Agents.Middleware;

public class AuditLoggingMiddlewareTests
{
    private readonly Mock<IAgentExecutionAuditService> _mockAuditService;
    private readonly Mock<ILogger<AuditLoggingMiddleware>> _mockLogger;
    private readonly AuditLoggingMiddleware _middleware;
    private readonly Guid _tenantId;

    public AuditLoggingMiddlewareTests()
    {
        _mockAuditService = new Mock<IAgentExecutionAuditService>();
        _mockLogger = new Mock<ILogger<AuditLoggingMiddleware>>();
        _middleware = new AuditLoggingMiddleware(_mockAuditService.Object, _mockLogger.Object);
        _tenantId = Guid.NewGuid();
    }

    [Fact]
    public async Task ExecuteAsync_CreatesAuditLogBeforeExecution()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Equal(context.TenantId, capturedLog.TenantId);
        Assert.Equal(context.TicketId, capturedLog.TicketId);
        Assert.Equal("TestAgent", capturedLog.AgentName);
        Assert.NotEqual(Guid.Empty, capturedLog.AuditId);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsSuccessfulExecution()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            var result = new AgentResult { Status = AgentStatus.Completed };
            result.Output["TestKey"] = "TestValue";
            return Task.FromResult(result);
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.True(capturedLog.Success);
        Assert.Equal("Completed", capturedLog.Status);
        Assert.NotNull(capturedLog.CompletedAt);
        Assert.True(capturedLog.DurationMs >= 0);
        Assert.Null(capturedLog.ErrorMessage);
        Assert.Null(capturedLog.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsFailedExecution()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            return Task.FromResult(new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Test error message"
            });
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.False(capturedLog.Success);
        Assert.Equal("Failed", capturedLog.Status);
        Assert.Equal("Test error message", capturedLog.ErrorMessage);
        Assert.Equal("AgentError", capturedLog.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsExceptionInformation()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Verify audit log captured exception
        Assert.NotNull(capturedLog);
        Assert.False(capturedLog.Success);
        Assert.Equal("Failed", capturedLog.Status);
        Assert.Equal("Test exception", capturedLog.ErrorMessage);
        Assert.Equal("InvalidOperationException", capturedLog.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsDuration()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = async (ctx, ct) =>
        {
            await Task.Delay(50, ct); // Simulate some work
            return new AgentResult { Status = AgentStatus.Completed };
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.True(capturedLog.DurationMs >= 50); // Should be at least 50ms
        Assert.NotNull(capturedLog.StartedAt);
        Assert.NotNull(capturedLog.CompletedAt);
        Assert.True(capturedLog.CompletedAt > capturedLog.StartedAt);
    }

    [Fact]
    public async Task ExecuteAsync_SerializesInputState()
    {
        // Arrange
        var context = CreateContext();
        context.State["TestKey"] = "TestValue";
        context.State["NumericKey"] = 42;

        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.NotNull(capturedLog.InputState);
        Assert.Contains("TestKey", capturedLog.InputState);
        Assert.Contains("TestValue", capturedLog.InputState);
    }

    [Fact]
    public async Task ExecuteAsync_SerializesOutputData()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            var result = new AgentResult { Status = AgentStatus.Completed };
            result.Output["OutputKey"] = "OutputValue";
            result.Output["Count"] = 123;
            return Task.FromResult(result);
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.NotNull(capturedLog.OutputData);
        Assert.Contains("OutputKey", capturedLog.OutputData);
        Assert.Contains("OutputValue", capturedLog.OutputData);
    }

    [Fact]
    public async Task ExecuteAsync_PersistsAuditLogAsynchronously()
    {
        // Arrange
        var context = CreateContext();
        var persistCalled = false;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback(() => persistCalled = true)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert - Should return immediately without waiting for persistence
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Verify persistence was called
        Assert.True(persistCalled);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotBlockOnPersistenceFailure()
    {
        // Arrange
        var context = CreateContext();

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persistence failed"));

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget to complete
        await Task.Delay(200);

        // Assert - Should not throw, execution should succeed
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist audit log")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsAuditLogCreated()
    {
        // Arrange
        var context = CreateContext();

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audit log created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsAuditLogPersisted()
    {
        // Arrange
        var context = CreateContext();

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audit log persisted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyState_HandlesGracefully()
    {
        // Arrange
        var context = CreateContext();
        context.State.Clear(); // Empty state

        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Null(capturedLog.InputState); // Should be null for empty state
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyOutput_HandlesGracefully()
    {
        // Arrange
        var context = CreateContext();
        AgentExecutionAuditLog? capturedLog = null;

        _mockAuditService.Setup(x => x.SaveAuditLogAsync(It.IsAny<AgentExecutionAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionAuditLog, CancellationToken>((log, ct) => capturedLog = log)
            .Returns(Task.CompletedTask);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed }); // Empty output

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget task
        await Task.Delay(100);

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Null(capturedLog.OutputData); // Should be null for empty output
    }

    private AgentContext CreateContext()
    {
        return new AgentContext
        {
            TenantId = _tenantId.ToString(),
            TicketId = Guid.NewGuid().ToString(),
            State = new Dictionary<string, object>
            {
                ["InitialKey"] = "InitialValue"
            },
            Metadata = new Dictionary<string, object>
            {
                ["CurrentPhase"] = "TestAgent"
            },
            Ticket = null!,
            Tenant = null!,
            Repository = null!
        };
    }
}
