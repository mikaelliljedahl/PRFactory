using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Search;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Search;

public class GrepToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly GrepTool _tool;
    private readonly string _tempWorkspace;
    private readonly Guid _tenantId;

    public GrepToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        _tool = new GrepTool(_mockLogger.Object, _mockTenantContext.Object);

        _tempWorkspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWorkspace);
    }

    [Fact]
    public async Task ExecuteAsync_BasicSearch_CaseInsensitive_ReturnsMatches()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllLinesAsync(testFile, new[]
        {
            "Hello World",
            "hello there",
            "HELLO AGAIN",
            "goodbye"
        });

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "hello"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.txt:1:Hello World", result.Output);
        Assert.Contains("test.txt:2:hello there", result.Output);
        Assert.Contains("test.txt:3:HELLO AGAIN", result.Output);
        Assert.DoesNotContain("goodbye", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_CaseSensitiveSearch_ReturnsOnlyExactMatches()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllLinesAsync(testFile, new[]
        {
            "Hello World",
            "hello there",
            "HELLO AGAIN"
        });

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "hello",
            ["caseSensitive"] = true
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.txt:2:hello there", result.Output);
        Assert.DoesNotContain("Hello World", result.Output);
        Assert.DoesNotContain("HELLO AGAIN", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithFilePattern_SearchesOnlyMatchingFiles()
    {
        // Arrange
        var csFile = Path.Combine(_tempWorkspace, "test.cs");
        var txtFile = Path.Combine(_tempWorkspace, "test.txt");

        await File.WriteAllTextAsync(csFile, "public class Test { }");
        await File.WriteAllTextAsync(txtFile, "public note");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "public",
            ["filePattern"] = "*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.cs", result.Output);
        Assert.DoesNotContain("test.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_RecursiveSearch_SearchesSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_tempWorkspace, "subdir");
        Directory.CreateDirectory(subDir);

        var rootFile = Path.Combine(_tempWorkspace, "root.txt");
        var subFile = Path.Combine(subDir, "sub.txt");

        await File.WriteAllTextAsync(rootFile, "match in root");
        await File.WriteAllTextAsync(subFile, "match in subdir");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "match"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("root.txt", result.Output);
        Assert.Contains("subdir", result.Output);
        Assert.Contains("sub.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithRegexPattern_MatchesRegex()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllLinesAsync(testFile, new[]
        {
            "abc123",
            "def456",
            "xyz"
        });

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = @"\d+"  // Match digits
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.txt:1:abc123", result.Output);
        Assert.Contains("test.txt:2:def456", result.Output);
        Assert.DoesNotContain("xyz", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_NoMatches_ReturnsEmptyOutput()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "no match here");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "needle"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsResultLimit_Truncates()
    {
        // Arrange
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        var lines = new List<string>();
        for (int i = 0; i < ResourceLimits.MaxResultLines + 100; i++)
        {
            lines.Add($"match line {i}");
        }
        await File.WriteAllLinesAsync(testFile, lines);

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "match"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("truncated", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsLargeFiles_DoesNotCrash()
    {
        // Arrange
        var largeFile = Path.Combine(_tempWorkspace, "large.txt");
        var smallFile = Path.Combine(_tempWorkspace, "small.txt");

        // Create file larger than limit
        await File.WriteAllTextAsync(largeFile, new string('x', (int)(ResourceLimits.MaxFileSize + 1)));
        await File.WriteAllTextAsync(smallFile, "match here");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "match"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("small.txt", result.Output);
        Assert.DoesNotContain("large.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithDirectory_SearchesSubdirectory()
    {
        // Arrange
        var subDir = Path.Combine(_tempWorkspace, "subdir");
        Directory.CreateDirectory(subDir);

        var subFile = Path.Combine(subDir, "test.txt");
        await File.WriteAllTextAsync(subFile, "match");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "match",
            ["directory"] = "subdir"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRegexPattern_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "["  // Invalid regex
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid regex pattern", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPattern_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>());

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("pattern", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_PathTraversal_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "test",
            ["directory"] = "../../etc"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public void Name_ReturnsGrep()
    {
        // Assert
        Assert.Equal("Grep", _tool.Name);
    }

    [Fact]
    public void Description_ReturnsValidDescription()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(_tool.Description));
        Assert.Contains("regex", _tool.Description, StringComparison.OrdinalIgnoreCase);
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
