using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Base.Middleware;

namespace PRFactory.Tests.Infrastructure.Agents.Middleware;

public class TokenBudgetMiddlewareTests
{
    private readonly Mock<ITokenBudgetService> _mockTokenBudgetService;
    private readonly Mock<ILogger<TokenBudgetMiddleware>> _mockLogger;
    private readonly TokenBudgetMiddleware _middleware;
    private readonly Guid _tenantId;

    public TokenBudgetMiddlewareTests()
    {
        _mockTokenBudgetService = new Mock<ITokenBudgetService>();
        _mockLogger = new Mock<ILogger<TokenBudgetMiddleware>>();
        _middleware = new TokenBudgetMiddleware(_mockTokenBudgetService.Object, _mockLogger.Object);
        _tenantId = Guid.NewGuid();
    }

    [Fact]
    public async Task ExecuteAsync_WithSufficientBudget_ExecutesNextMiddleware()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

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
    public async Task ExecuteAsync_WithExhaustedBudget_ReturnsFailureWithoutExecuting()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 9900);
        context.Metadata["EstimatedTokens"] = 1000;

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var nextCalled = false;
        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("Token budget exhausted", result.Error);
        Assert.False(result.ShouldRetry);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsTokenUsageAfterExecution()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        const int tokensUsed = 500;
        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            var result = new AgentResult { Status = AgentStatus.Completed };
            result.Output["TokensUsed"] = tokensUsed;
            return Task.FromResult(result);
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait a bit for fire-and-forget task
        await Task.Delay(100);

        // Assert
        _mockTokenBudgetService.Verify(
            x => x.RecordUsageAsync(
                _tenantId,
                tokensUsed,
                It.IsAny<string>(),
                context.TicketId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesEstimatedTokensWhenActualNotReported()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        context.Metadata["EstimatedTokens"] = 750;
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            // Don't report TokensUsed in output
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait a bit for fire-and-forget task
        await Task.Delay(100);

        // Assert
        _mockTokenBudgetService.Verify(
            x => x.RecordUsageAsync(
                _tenantId,
                750, // Should use estimated tokens
                It.IsAny<string>(),
                context.TicketId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsTokenBudgetCheck()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Checking token budget")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsTokenBudgetExhausted()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 9900);
        context.Metadata["EstimatedTokens"] = 1000;

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token budget exhausted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBudgetServiceThrows_AllowsExecution()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Budget service unavailable"));

        var nextCalled = false;
        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new AgentResult { Status = AgentStatus.Completed });
        };

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert - Should fail open for availability
        Assert.True(nextCalled);
        Assert.Equal(AgentStatus.Completed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTenantId_AllowsExecution()
    {
        // Arrange
        var context = CreateContext("invalid-guid");

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
            Task.FromResult(new AgentResult { Status = AgentStatus.Completed });

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Assert - Should allow execution but log warning
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid TenantId format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRecordUsageWhenTokensUsedIsZero()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        context.Metadata["EstimatedTokens"] = 0;
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            var result = new AgentResult { Status = AgentStatus.Completed };
            result.Output["TokensUsed"] = 0;
            return Task.FromResult(result);
        };

        // Act
        await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait a bit to ensure fire-and-forget doesn't run
        await Task.Delay(100);

        // Assert
        _mockTokenBudgetService.Verify(
            x => x.RecordUsageAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesRecordUsageFailureGracefully()
    {
        // Arrange
        var context = CreateContext(_tenantId.ToString());
        var budget = CreateBudget(_tenantId, totalTokens: 10000, usedTokens: 1000);

        _mockTokenBudgetService.Setup(x => x.GetBudgetAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        _mockTokenBudgetService.Setup(x => x.RecordUsageAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Recording failed"));

        Func<AgentContext, CancellationToken, Task<AgentResult>> next = (ctx, ct) =>
        {
            var result = new AgentResult { Status = AgentStatus.Completed };
            result.Output["TokensUsed"] = 500;
            return Task.FromResult(result);
        };

        // Act
        var result = await _middleware.ExecuteAsync(context, next, CancellationToken.None);

        // Wait for fire-and-forget to complete
        await Task.Delay(200);

        // Assert - Should not throw, just log error
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to record token usage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
                ["CurrentPhase"] = "TestAgent",
                ["EstimatedTokens"] = 1000
            },
            Ticket = null!,
            Tenant = null!,
            Repository = null!
        };
    }

    private TokenBudget CreateBudget(Guid tenantId, int totalTokens, int usedTokens)
    {
        return new TokenBudget
        {
            TenantId = tenantId,
            TotalTokens = totalTokens,
            UsedTokens = usedTokens,
            PeriodStart = DateTime.UtcNow.AddDays(-15),
            PeriodEnd = DateTime.UtcNow.AddDays(15)
        };
    }
}
