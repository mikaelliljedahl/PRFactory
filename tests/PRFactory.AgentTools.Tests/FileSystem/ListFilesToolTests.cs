using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.FileSystem;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.FileSystem;

public class ListFilesToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;
    private readonly ListFilesTool _tool;

    public ListFilesToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        // Create temporary workspace
        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);

        _tool = new ListFilesTool(_mockLogger.Object, _mockTenantContext.Object);
    }

    public void Dispose()
    {
        // Cleanup temporary workspace
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task ListFiles_EmptyDirectory_ReturnsEmptyString()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    public async Task ListFiles_SingleFile_ReturnsFileName()
    {
        // Arrange
        var fileName = "test.txt";
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, fileName), "content");

        var context = CreateContext();

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(fileName, result.Output);
    }

    [Fact]
    public async Task ListFiles_MultipleFiles_ReturnsAllFiles()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
        foreach (var file in files)
        {
            await File.WriteAllTextAsync(Path.Combine(_workspacePath, file), "content");
        }

        var context = CreateContext();

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(files.Length, outputLines.Length);

        foreach (var file in files)
        {
            Assert.Contains(file, outputLines);
        }
    }

    [Fact]
    public async Task ListFiles_Subdirectory_ListsFilesInSubdir()
    {
        // Arrange
        var subDir = "subdir";
        var dirPath = Path.Combine(_workspacePath, subDir);
        Directory.CreateDirectory(dirPath);

        var files = new[] { "file1.txt", "file2.txt" };
        foreach (var file in files)
        {
            await File.WriteAllTextAsync(Path.Combine(dirPath, file), "content");
        }

        var context = CreateContext();
        context.Parameters["directory"] = subDir;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(files.Length, outputLines.Length);
    }

    [Fact]
    public async Task ListFiles_NonRecursive_ExcludesNestedFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "root.txt"), "content");

        var subDir = Path.Combine(_workspacePath, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "content");

        var context = CreateContext();
        context.Parameters["recursive"] = false;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(outputLines);
        Assert.Contains("root.txt", result.Output);
        Assert.DoesNotContain("nested.txt", result.Output);
    }

    [Fact]
    public async Task ListFiles_Recursive_IncludesNestedFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "root.txt"), "content");

        var subDir = Path.Combine(_workspacePath, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "content");

        var context = CreateContext();
        context.Parameters["recursive"] = true;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, outputLines.Length);
        Assert.Contains("root.txt", result.Output);
        Assert.Contains("nested.txt", result.Output);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public async Task ListFiles_PatternTxtFiles_ReturnsOnlyTxtFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file1.txt"), "content");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file2.txt"), "content");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file3.cs"), "content");

        var context = CreateContext();
        context.Parameters["pattern"] = "*.txt";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, outputLines.Length);
        Assert.DoesNotContain(".cs", result.Output);
    }

    [Fact]
    public async Task ListFiles_PatternCsFiles_ReturnsOnlyCsFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file1.cs"), "content");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file2.txt"), "content");

        var context = CreateContext();
        context.Parameters["pattern"] = "*.cs";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("file1.cs", result.Output);
        Assert.DoesNotContain(".txt", result.Output);
    }

    [Fact]
    public async Task ListFiles_DefaultPattern_ReturnsAllFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file1.txt"), "content");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file2.cs"), "content");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "file3.md"), "content");

        var context = CreateContext();
        // Don't set pattern parameter (should default to "*")

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, outputLines.Length);
    }

    #endregion

    #region Sorting and Ordering Tests

    [Fact]
    public async Task ListFiles_ReturnsSortedResults()
    {
        // Arrange
        var files = new[] { "zebra.txt", "alpha.txt", "beta.txt" };
        foreach (var file in files)
        {
            await File.WriteAllTextAsync(Path.Combine(_workspacePath, file), "content");
        }

        var context = CreateContext();

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var outputLines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("alpha.txt", outputLines[0]);
        Assert.Equal("beta.txt", outputLines[1]);
        Assert.Equal("zebra.txt", outputLines[2]);
    }

    #endregion

    #region Limit and Truncation Tests

    [Fact]
    public async Task ListFiles_ExceedsMaxResults_TruncatesWithMessage()
    {
        // Arrange - Create more files than ResourceLimits.MaxGlobResults
        var fileCount = ResourceLimits.MaxGlobResults + 10;
        for (int i = 0; i < fileCount; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(_workspacePath, $"file{i:D4}.txt"), "content");
        }

        var context = CreateContext();

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("truncated", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fileCount.ToString(), result.Output);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ListFiles_DirectoryNotFound_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["directory"] = "nonexistent";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ListFiles_PathTraversalWithDotDot_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["directory"] = "../../../etc";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public async Task ListFiles_AbsolutePath_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["directory"] = "/etc";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Absolute paths", result.ErrorMessage);
    }

    [Fact]
    public async Task ListFiles_TenantMismatch_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.TenantId = Guid.NewGuid(); // Different tenant

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("mismatch", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Relative Path Tests

    [Fact]
    public async Task ListFiles_ReturnsRelativePaths()
    {
        // Arrange
        var subDir = "subdir";
        var dirPath = Path.Combine(_workspacePath, subDir);
        Directory.CreateDirectory(dirPath);
        await File.WriteAllTextAsync(Path.Combine(dirPath, "file.txt"), "content");

        var context = CreateContext();
        context.Parameters["recursive"] = true;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains($"subdir{Path.DirectorySeparatorChar}file.txt", result.Output);
        Assert.DoesNotContain(_workspacePath, result.Output);
    }

    #endregion

    #region Tool Properties Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("ListFiles", _tool.Name);
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(_tool.Description));
    }

    #endregion

    private ToolExecutionContext CreateContext()
    {
        return new ToolExecutionContext
        {
            TenantId = _tenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>()
        };
    }
}
