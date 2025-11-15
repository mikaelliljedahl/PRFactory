using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.FileSystem;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.FileSystem;

public class DeleteFileToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;
    private readonly DeleteFileTool _tool;

    public DeleteFileToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        // Create temporary workspace
        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);

        _tool = new DeleteFileTool(_mockLogger.Object, _mockTenantContext.Object);
    }

    public void Dispose()
    {
        // Cleanup temporary workspace
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task DeleteFile_ExistingFile_DeletesFile()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully deleted", result.Output);
        Assert.Contains(fileName, result.Output);

        // Verify file was deleted
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFile_FileInSubdirectory_DeletesFile()
    {
        // Arrange
        var subDir = "subdir";
        var fileName = "nested.txt";
        var dirPath = Path.Combine(_workspacePath, subDir);
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        var context = CreateContext();
        context.Parameters["filePath"] = $"{subDir}/{fileName}";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFile_LargeFile_DeletesSuccessfully()
    {
        // Arrange
        var fileName = "large.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        var content = new string('x', 5 * 1024 * 1024); // 5MB
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFile_EmptyFile_DeletesSuccessfully()
    {
        // Arrange
        var fileName = "empty.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, string.Empty);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task DeleteFile_FileNotFound_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "nonexistent.txt";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteFile_MissingFilePathParameter_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        // Don't set filePath parameter

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
    public async Task DeleteFile_PathTraversalWithDotDot_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "../../../etc/passwd";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("..", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteFile_AbsolutePath_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "/etc/passwd";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Absolute paths", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteFile_PathOutsideWorkspace_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        context.Parameters["filePath"] = "subdir/../../outside.txt";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteFile_TenantMismatch_ReturnsFailure()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        var context = CreateContext();
        context.TenantId = Guid.NewGuid(); // Different tenant
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("mismatch", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify file was NOT deleted
        Assert.True(File.Exists(filePath));
    }

    #endregion

    #region File Protection Tests

    [Fact]
    public async Task DeleteFile_ReadOnlyFile_DeletesSuccessfully()
    {
        // Arrange
        var fileName = "readonly.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        // Make file read-only
        var fileInfo = new FileInfo(filePath);
        fileInfo.IsReadOnly = true;

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        // Reset read-only flag first (to allow deletion)
        fileInfo.IsReadOnly = false;
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    #endregion

    #region Directory Tests

    [Fact]
    public async Task DeleteFile_DoesNotDeleteDirectory()
    {
        // Arrange
        var dirName = "testdir";
        var dirPath = Path.Combine(_workspacePath, dirName);
        Directory.CreateDirectory(dirPath);

        var context = CreateContext();
        context.Parameters["filePath"] = dirName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify directory still exists
        Assert.True(Directory.Exists(dirPath));
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task DeleteFile_MultipleSequentialDeletes_AllSucceed()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
        foreach (var file in files)
        {
            await File.WriteAllTextAsync(Path.Combine(_workspacePath, file), "content");
        }

        // Act
        foreach (var file in files)
        {
            var context = CreateContext();
            context.Parameters["filePath"] = file;
            var result = await _tool.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
        }

        // Verify all files deleted
        foreach (var file in files)
        {
            Assert.False(File.Exists(Path.Combine(_workspacePath, file)));
        }
    }

    [Fact]
    public async Task DeleteFile_AlreadyDeleted_ReturnsFailure()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        var context1 = CreateContext();
        context1.Parameters["filePath"] = fileName;

        // First delete
        var result1 = await _tool.ExecuteAsync(context1);
        Assert.True(result1.Success);

        // Try to delete again
        var context2 = CreateContext();
        context2.Parameters["filePath"] = fileName;

        // Act
        var result2 = await _tool.ExecuteAsync(context2);

        // Assert
        Assert.False(result2.Success);
        Assert.Contains("not found", result2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Tool Properties Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("DeleteFile", _tool.Name);
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
