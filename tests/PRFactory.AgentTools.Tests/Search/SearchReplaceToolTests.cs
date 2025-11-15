using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Search;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Search;

public class SearchReplaceToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly SearchReplaceTool _tool;
    private readonly string _tempWorkspace;
    private readonly Guid _tenantId;

    public SearchReplaceToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        _tool = new SearchReplaceTool(_mockLogger.Object, _mockTenantContext.Object);

        _tempWorkspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWorkspace);
    }

    [Fact]
    public async Task ExecuteAsync_BasicReplace_CaseInsensitive_ReplacesAllMatches()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello world\nhello again\nHELLO there");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "hello",
            ["replacement"] = "hi",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Replaced 3 occurrences", result.Output);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("hi world\nhi again\nhi there", newContent);
    }

    [Fact]
    public async Task ExecuteAsync_CaseSensitiveReplace_ReplacesOnlyExactMatches()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello world\nhello again\nHELLO there");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "hello",
            ["replacement"] = "hi",
            ["filePath"] = "test.txt",
            ["caseSensitive"] = true
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Replaced 1 occurrences", result.Output);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("Hello world\nhi again\nHELLO there", newContent);
    }

    [Fact]
    public async Task ExecuteAsync_RegexPattern_MatchesAndReplaces()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "Version 1.0\nVersion 2.5\nVersion 3.14");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = @"Version (\d+\.\d+)",
            ["replacement"] = "v$1",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Replaced 3 occurrences", result.Output);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("v1.0\nv2.5\nv3.14", newContent);
    }

    [Fact]
    public async Task ExecuteAsync_NoMatches_FileUnchanged()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        var originalContent = "No matches here";
        await File.WriteAllTextAsync(testFile, originalContent);

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "needle",
            ["replacement"] = "pin",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("No matches found", result.Output);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(originalContent, newContent);
    }

    [Fact]
    public async Task ExecuteAsync_AtomicWrite_UsesTemporaryFile()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "original content");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "original",
            ["replacement"] = "new",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);

        // Verify no temp files left behind
        var tempFiles = Directory.GetFiles(_tempWorkspace, ".*.tmp");
        Assert.Empty(tempFiles);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("new content", newContent);
    }

    [Fact]
    public async Task ExecuteAsync_FileNotFound_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "test",
            ["replacement"] = "new",
            ["filePath"] = "nonexistent.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRegex_ReturnsFailure()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "[",  // Invalid regex
            ["replacement"] = "new",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid regex pattern", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ContentSizeExceedsLimit_ReturnsFailure()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "x");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "x",
            ["replacement"] = new string('y', (int)(ResourceLimits.MaxWriteSize + 1)),
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("exceeds limit", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_PathTraversal_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "test",
            ["replacement"] = "new",
            ["filePath"] = "../../etc/passwd"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPattern_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["replacement"] = "new",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("pattern", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_MissingReplacement_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "test",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("replacement", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_MissingFilePath_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "test",
            ["replacement"] = "new"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("filePath", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleReplacements_CountsCorrectly()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "foo bar foo baz foo");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "foo",
            ["replacement"] = "qux",
            ["filePath"] = "test.txt"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Replaced 3 occurrences", result.Output);

        var newContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("qux bar qux baz qux", newContent);
    }

    [Fact]
    public void Name_ReturnsSearchReplace()
    {
        // Assert
        Assert.Equal("SearchReplace", _tool.Name);
    }

    [Fact]
    public void Description_ReturnsValidDescription()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(_tool.Description));
        Assert.Contains("replace", _tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    private ToolExecutionContext CreateContext(Dictionary<string, object> parameters)
    {
        return new ToolExecutionContext
        {
            TenantId = _tenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = _tempWorkspace,
            Parameters = parameters
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWorkspace))
            Directory.Delete(_tempWorkspace, recursive: true);
    }
}
