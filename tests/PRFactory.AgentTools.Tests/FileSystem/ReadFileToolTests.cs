using System.Security;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.FileSystem;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.FileSystem;

public class ReadFileToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;
    private readonly ReadFileTool _tool;

    public ReadFileToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        // Create temporary workspace
        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);

        _tool = new ReadFileTool(_mockLogger.Object, _mockTenantContext.Object);
    }

    public void Dispose()
    {
        // Cleanup temporary workspace
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task ReadFile_ValidFile_ReturnsContent()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello World";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
    }

    [Fact]
    public async Task ReadFile_EmptyFile_ReturnsEmptyString()
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
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    public async Task ReadFile_LargeFile_ReturnsContent()
    {
        // Arrange
        var fileName = "large.txt";
        var content = new string('x', 1024 * 1024); // 1MB
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content.Length, result.Output.Length);
    }

    [Fact]
    public async Task ReadFile_FileInSubdirectory_ReturnsContent()
    {
        // Arrange
        var subDir = "subdir";
        var fileName = "nested.txt";
        var content = "Nested content";
        var dirPath = Path.Combine(_workspacePath, subDir);
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = $"{subDir}/{fileName}";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ReadFile_FileNotFound_ReturnsFailure()
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
    public async Task ReadFile_FileTooLarge_ReturnsFailure()
    {
        // Arrange - Create file larger than ResourceLimits.MaxFileSize
        var fileName = "toolarge.txt";
        var filePath = Path.Combine(_workspacePath, fileName);

        // Create a file larger than MaxFileSize (10MB)
        using (var stream = File.Create(filePath))
        {
            stream.SetLength(ResourceLimits.MaxFileSize + 1);
        }

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("exceeds limit", result.ErrorMessage);
    }

    [Fact]
    public async Task ReadFile_MissingFilePathParameter_ReturnsFailure()
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
    public async Task ReadFile_PathTraversalWithDotDot_ReturnsFailure()
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
    public async Task ReadFile_AbsolutePath_ReturnsFailure()
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
    public async Task ReadFile_PathOutsideWorkspace_ReturnsFailure()
    {
        // Arrange
        var context = CreateContext();
        // Try to escape workspace using multiple path segments
        context.Parameters["filePath"] = "subdir/../../outside.txt";

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ReadFile_TenantMismatch_ReturnsFailure()
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
    }

    #endregion

    #region Special Content Tests

    [Fact]
    public async Task ReadFile_MultilineContent_PreservesFormatting()
    {
        // Arrange
        var fileName = "multiline.txt";
        var content = "Line 1\nLine 2\r\nLine 3";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
    }

    [Fact]
    public async Task ReadFile_SpecialCharacters_PreservesContent()
    {
        // Arrange
        var fileName = "special.txt";
        var content = "Special: !@#$%^&*(){}[]<>?/|\\~`";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
    }

    [Fact]
    public async Task ReadFile_Utf8Content_PreservesEncoding()
    {
        // Arrange
        var fileName = "utf8.txt";
        var content = "Unicode: 你好世界 🌍 Привет";
        var filePath = Path.Combine(_workspacePath, fileName);
        await File.WriteAllTextAsync(filePath, content);

        var context = CreateContext();
        context.Parameters["filePath"] = fileName;

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
    }

    #endregion

    #region Tool Properties Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("ReadFile", _tool.Name);
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
