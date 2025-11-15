using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Base.Middleware;

namespace PRFactory.Tests.Infrastructure.Agents.Middleware;

public class TenantIsolationMiddlewareTests
{
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<TenantIsolationMiddleware>> _mockLogger;
    private readonly TenantIsolationMiddleware _middleware;
    private readonly Guid _tenantId;

    public TenantIsolationMiddlewareTests()
    {
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<TenantIsolationMiddleware>>();
        _middleware = new TenantIsolationMiddleware(_mockTenantContext.Object, _mockLogger.Object);
        _tenantId = Guid.NewGuid();
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTenantId_ThrowsTenantIsolationException()
    {
        // Arrange
        var context = CreateContext(string.Empty);
        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Equal("TenantId cannot be empty", exception.Message);
        Assert.Equal(context.TicketId, exception.TicketId);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTenantIdFormat_ThrowsTenantIsolationException()
    {
        // Arrange
        var context = CreateContext("invalid-guid-format");
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Contains("Invalid TenantId format", exception.Message);
        Assert.Equal(context.TicketId, exception.TicketId);
    }

    [Fact]
    public async Task ExecuteAsync_WithTenantMismatch_ThrowsTenantIsolationException()
    {
        // Arrange
        var contextTenantId = Guid.NewGuid();
        var currentTenantId = Guid.NewGuid(); // Different tenant
        var context = CreateContext(contextTenantId.ToString());

        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentTenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Contains("Tenant context mismatch", exception.Message);
        Assert.Equal(contextTenantId.ToString(), exception.ExpectedTenantId);
        Assert.Equal(currentTenantId.ToString(), exception.ActualTenantId);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTenant_ExecutesNextMiddleware()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        var nextCalled = false;
        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(AgentStatus.Completed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SetsTenantIdInContextState()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            // Verify state contains validated tenant ID
            Assert.True(ctx.State.ContainsKey("ValidatedTenantId"));
            Assert.Equal(_tenantId.ToString(), ctx.State["ValidatedTenantId"]);
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert - verified in next delegate
    }

    [Fact]
    public async Task ExecuteAsync_WhenTenantChangedDuringExecution_ThrowsTenantIsolationException()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            // Simulate malicious tenant change
            ctx.TenantId = Guid.NewGuid().ToString();
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Contains("Tenant ID was changed during execution", exception.Message);
        Assert.Equal(_tenantId.ToString(), exception.ExpectedTenantId);
    }

    [Fact]
    public async Task ExecuteAsync_LogsValidationStarted()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tenant validation started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsValidationCompleted()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tenant validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTenantContextThrows_ThrowsTenantIsolationException()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tenant context unavailable"));

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Contains("Failed to retrieve current tenant context", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteAsync_ReThrowsTenantIsolationException()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            throw new TenantIsolationException("Test isolation violation");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Equal("Test isolation violation", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesOtherExceptions()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        _mockTenantContext.Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            throw new InvalidOperationException("Test error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _middleware.ExecuteAsync(context, next, CancellationToken.None));

        Assert.Equal("Test error", exception.Message);
    }

    private AgentContext CreateContext(string tenantId)
    {
        return new AgentContext
        {
            TenantId = tenantId,
            TicketId = Guid.NewGuid().ToString(),
            State = new Dictionary<string, object>(),
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
