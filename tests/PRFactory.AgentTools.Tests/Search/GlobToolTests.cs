using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Search;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Search;

public class GlobToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly GlobTool _tool;
    private readonly string _tempWorkspace;
    private readonly Guid _tenantId;

    public GlobToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        _tool = new GlobTool(_mockLogger.Object, _mockTenantContext.Object);

        _tempWorkspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWorkspace);
    }

    [Fact]
    public async Task ExecuteAsync_SimplePattern_ReturnsMatchingFiles()
    {
        // Arrange
        await CreateTestFile("test.cs");
        await CreateTestFile("test.txt");
        await CreateTestFile("app.cs");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test.cs", result.Output);
        Assert.Contains("app.cs", result.Output);
        Assert.DoesNotContain("test.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_RecursivePattern_SearchesAllDirectories()
    {
        // Arrange
        await CreateTestFile("root.cs");
        await CreateTestFile("src/file1.cs");
        await CreateTestFile("src/sub/file2.cs");
        await CreateTestFile("docs/readme.txt");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "**/*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("root.cs", result.Output);
        Assert.Contains("file1.cs", result.Output);
        Assert.Contains("file2.cs", result.Output);
        Assert.DoesNotContain("readme.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_MatchesCSharpFiles()
    {
        // Arrange
        await CreateTestFile("file.cs");
        await CreateTestFile("file.ts");
        await CreateTestFile("file.js");
        await CreateTestFile("file.txt");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("file.cs", result.Output);
        Assert.DoesNotContain("file.ts", result.Output);
        Assert.DoesNotContain("file.js", result.Output);
        Assert.DoesNotContain("file.txt", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_SpecificDirectory_SearchesOnlyThatDirectory()
    {
        // Arrange
        await CreateTestFile("src/file.cs");
        await CreateTestFile("tests/test.cs");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs",
            ["directory"] = "src"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("file.cs", result.Output);
        Assert.DoesNotContain("test.cs", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_NoMatches_ReturnsEmptyOutput()
    {
        // Arrange
        await CreateTestFile("file.txt");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
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
        for (int i = 0; i < ResourceLimits.MaxGlobResults + 100; i++)
        {
            await CreateTestFile($"file{i}.cs");
        }

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("truncated", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_ResultsSortedAlphabetically()
    {
        // Arrange
        await CreateTestFile("zebra.cs");
        await CreateTestFile("apple.cs");
        await CreateTestFile("banana.cs");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var lines = result.Output.Split(Environment.NewLine);
        Assert.Equal("apple.cs", lines[0]);
        Assert.Equal("banana.cs", lines[1]);
        Assert.Equal("zebra.cs", lines[2]);
    }

    [Fact]
    public async Task ExecuteAsync_RelativePathsReturned()
    {
        // Arrange
        await CreateTestFile("src/sub/file.cs");

        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "**/*.cs"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("src", result.Output);
        Assert.Contains("sub", result.Output);
        Assert.Contains("file.cs", result.Output);
        // Should not contain absolute path
        Assert.DoesNotContain(_tempWorkspace, result.Output);
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
            ["pattern"] = "*.cs",
            ["directory"] = "../../../etc"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentDirectory_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs",
            ["directory"] = "nonexistent"
        });

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Name_ReturnsGlob()
    {
        // Assert
        Assert.Equal("Glob", _tool.Name);
    }

    [Fact]
    public void Description_ReturnsValidDescription()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(_tool.Description));
        Assert.Contains("glob", _tool.Description, StringComparison.OrdinalIgnoreCase);
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

    private async Task CreateTestFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempWorkspace, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(fullPath, "test content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWorkspace))
            Directory.Delete(_tempWorkspace, recursive: true);
    }
}
