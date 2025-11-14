using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Core;

public class ToolBaseTests
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;

    public ToolBaseTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidContext_ReturnsSuccess()
    {
        // Arrange
        var tool = new TestTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test output", result.Output);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ReturnsFailure()
    {
        // Arrange
        var tool = new TestTool(_mockLogger.Object, _mockTenantContext.Object);

        // Act
        var result = await tool.ExecuteAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("context", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTenantId_ReturnsFailure()
    {
        // Arrange
        var tool = new TestTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();
        context.TenantId = Guid.Empty;

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("TenantId", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyWorkspacePath_ReturnsFailure()
    {
        // Arrange
        var tool = new TestTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();
        context.WorkspacePath = string.Empty;

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("WorkspacePath", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithTenantMismatch_ReturnsFailure()
    {
        // Arrange
        var tool = new TestTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();
        context.TenantId = Guid.NewGuid(); // Different from mock tenant context

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("mismatch", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithToolException_ReturnsFailure()
    {
        // Arrange
        var tool = new ThrowingTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("Test exception", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidation_CallsValidateInputAsync()
    {
        // Arrange
        var tool = new ValidatingTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.True(tool.ValidationCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedValidation_ReturnsFailure()
    {
        // Arrange
        var tool = new FailingValidationTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("Validation failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_MeasuresDuration()
    {
        // Arrange
        var tool = new SlowTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Duration.TotalMilliseconds >= 100);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithinTimeout_ReturnsResult()
    {
        // Arrange
        var tool = new TimeoutTestTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Completed", result.Output);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ExceedsTimeout_ThrowsToolTimeoutException()
    {
        // Arrange
        var tool = new TimeoutExceedingTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = CreateValidContext();

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private ToolExecutionContext CreateValidContext()
    {
        return new ToolExecutionContext
        {
            TenantId = _tenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = "/test/workspace",
            Parameters = new Dictionary<string, object>()
        };
    }

    // Test tool implementations
    private class TestTool : ToolBase
    {
        public override string Name => "TestTool";
        public override string Description => "A test tool";

        public TestTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            return Task.FromResult("Test output");
        }
    }

    private class ThrowingTool : ToolBase
    {
        public override string Name => "ThrowingTool";
        public override string Description => "A tool that throws";

        public ThrowingTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private class ValidatingTool : ToolBase
    {
        public bool ValidationCalled { get; private set; }

        public override string Name => "ValidatingTool";
        public override string Description => "A tool with validation";

        public ValidatingTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task ValidateInputAsync(ToolExecutionContext context)
        {
            ValidationCalled = true;
            return Task.CompletedTask;
        }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            return Task.FromResult("Validated output");
        }
    }

    private class FailingValidationTool : ToolBase
    {
        public override string Name => "FailingValidationTool";
        public override string Description => "A tool with failing validation";

        public FailingValidationTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override Task ValidateInputAsync(ToolExecutionContext context)
        {
            throw new ArgumentException("Validation failed");
        }

        protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            return Task.FromResult("Should not reach here");
        }
    }

    private class SlowTool : ToolBase
    {
        public override string Name => "SlowTool";
        public override string Description => "A slow tool";

        public SlowTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            await Task.Delay(100);
            return "Slow output";
        }
    }

    private class TimeoutTestTool : ToolBase
    {
        public override string Name => "TimeoutTestTool";
        public override string Description => "Tests timeout functionality";

        public TimeoutTestTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            var result = await ExecuteWithTimeoutAsync(
                async () =>
                {
                    await Task.Delay(50);
                    return "Completed";
                },
                TimeSpan.FromSeconds(1));

            return result;
        }
    }

    private class TimeoutExceedingTool : ToolBase
    {
        public override string Name => "TimeoutExceedingTool";
        public override string Description => "Exceeds timeout";

        public TimeoutExceedingTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
            : base(logger, tenantContext) { }

        protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
        {
            var result = await ExecuteWithTimeoutAsync(
                async () =>
                {
                    await Task.Delay(2000);
                    return "Should not complete";
                },
                TimeSpan.FromMilliseconds(100));

            return result;
        }
    }
}
