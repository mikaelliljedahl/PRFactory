using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.FileSystem;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.FileSystem;

public class WriteFileToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;
    private readonly WriteFileTool _tool;

    public WriteFileToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        // Create temporary workspace
        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);

        _tool = new WriteFileTool(_mockLogger.Object, _mockTenantContext.Object);
    }

    public void Dispose()
    {
        // Cleanup temporary workspace
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task WriteFile_NewFile_CreatesFile()
    {
        // Arrange
        var fileName = "new.txt";
        var content = "Hello World";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully wrote", result.Output);
        Assert.Contains(content.Length.ToString(), result.Output);

        // Verify file was created with correct content
        var filePath = Path.Combine(_workspacePath, fileName);
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, fileContent);
    }

    [Fact]
    public async Task WriteFile_OverwriteExisting_ReplacesContent()
    {
        // Arrange
        var fileName = "existing.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, "Old content");

        var newContent = "New content";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = newContent;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(newContent, fileContent);
    }

    [Fact]
    public async Task WriteFile_EmptyContent_CreatesEmptyFile()
    {
        // Arrange
        var fileName = "empty.txt";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = string.Empty;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName);
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task WriteFile_FileInSubdirectory_CreatesDirectoryAndFile()
    {
        // Arrange
        var fileName = "subdir/nested/file.txt";
        var content = "Nested content";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, fileContent);
    }

    [Fact]
    public async Task WriteFile_LargeContent_WritesSuccessfully()
    {
        // Arrange
        var fileName = "large.txt";
        var content = new string('x', 500 * 1024); // 500KB (under 1MB limit)
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName);
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0);
    }

    #endregion

    #region Atomic Operation Tests

    [Fact]
    public async Task WriteFile_UsesAtomicWrite_NoTempFileRemains()
    {
        // Arrange
        var fileName = "atomic.txt";
        var content = "Atomic content";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);

        // Verify no temp files remain
        var tempFiles = Directory.GetFiles(_workspacePath, ".*.tmp");
        Assert.Empty(tempFiles);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task WriteFile_ContentTooLarge_ReturnsFailure()
    {
        // Arrange
        var fileName = "toolarge.txt";
        var content = new string('x', (int)ResourceLimits.MaxWriteSize + 1);
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("exceeds limit", result.ErrorMessage);
    }

    [Fact]
    public async Task WriteFile_MissingFilePathParameter_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["content"] = "Content";
        // Don't set filePath

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteFile_MissingContentParameter_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "test.txt";
        // Don't set content

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task WriteFile_PathTraversalWithDotDot_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "../../../etc/malicious.txt";
        context.Parameters["content"] = "Malicious";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public async Task WriteFile_AbsolutePath_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "/tmp/malicious.txt";
        context.Parameters["content"] = "Malicious";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Absolute paths", result.ErrorMessage);
    }

    [Fact]
    public async Task WriteFile_PathOutsideWorkspace_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "subdir/../../outside.txt";
        context.Parameters["content"] = "Outside";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task WriteFile_TenantMismatch_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.TenantId = Guid.NewGuid(); // Different tenant
        context.Parameters["filePath"] = "test.txt";
        context.Parameters["content"] = "Content";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("mismatch", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Special Content Tests

    [Fact]
    public async Task WriteFile_MultilineContent_PreservesFormatting()
    {
        // Arrange
        var fileName = "multiline.txt";
        var content = "Line 1\nLine 2\r\nLine 3";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName);
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, fileContent);
    }

    [Fact]
    public async Task WriteFile_SpecialCharacters_PreservesContent()
    {
        // Arrange
        var fileName = "special.txt";
        var content = "Special: !@#$%^&*(){}[]<>?/|\\~`";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName);
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, fileContent);
    }

    [Fact]
    public async Task WriteFile_Utf8Content_PreservesEncoding()
    {
        // Arrange
        var fileName = "utf8.txt";
        var content = "Unicode: 你好世界 🌍 Привет";
        var context = CreateContext();
        context.Parameters["filePath"] = fileName;
        context.Parameters["content"] = content;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var filePath = Path.Combine(_workspacePath, fileName);
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, fileContent);
    }

    #endregion

    #region Tool Properties Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("WriteFile", _tool.Name);
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
