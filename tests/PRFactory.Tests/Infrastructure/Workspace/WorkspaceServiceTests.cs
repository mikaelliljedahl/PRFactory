using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Workspace;

namespace PRFactory.Tests.Infrastructure.Workspace;

public class WorkspaceServiceTests : IDisposable
{
    private readonly List<string> _tempDirectories = new();

    [Fact]
    public void GetWorkspaceDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/base");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var path = service.GetWorkspaceDirectory(ticketId);

        // Assert
        var expected = Path.Combine("/base", ticketId.ToString());
        Assert.Equal(expected, path);
    }

    [Fact]
    public void GetWorkspaceDirectory_ThrowsException_WhenTicketIdEmpty()
    {
        // Arrange
        var config = CreateConfiguration("/base");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => service.GetWorkspaceDirectory(Guid.Empty));
        Assert.Equal("ticketId", ex.ParamName);
    }

    [Fact]
    public void GetRepositoryPath_ReturnsCorrectPath()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/base");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var path = service.GetRepositoryPath(ticketId);

        // Assert
        var expected = Path.Combine("/base", ticketId.ToString(), "repo");
        Assert.Equal(expected, path);
    }

    [Fact]
    public void GetDiffPath_ReturnsCorrectPath()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/base");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var path = service.GetDiffPath(ticketId);

        // Assert
        var expected = Path.Combine("/base", ticketId.ToString(), "diff.patch");
        Assert.Equal(expected, path);
    }

    [Fact]
    public async Task ReadDiffAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/nonexistent");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var content = await service.ReadDiffAsync(ticketId);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task ReadDiffAsync_ReturnsContent_WhenFileExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        var diffContent = "diff --git a/file.txt b/file.txt\nindex abc..def\n--- a/file.txt\n+++ b/file.txt";
        await service.WriteDiffAsync(ticketId, diffContent);

        // Act
        var content = await service.ReadDiffAsync(ticketId);

        // Assert
        Assert.NotNull(content);
        Assert.Equal(diffContent, content);
    }

    [Fact]
    public async Task WriteDiffAsync_CreatesDiffFile()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);
        var diffContent = "diff --git a/file.txt b/file.txt\n...";

        // Act
        await service.WriteDiffAsync(ticketId, diffContent);

        // Assert
        var diffPath = service.GetDiffPath(ticketId);
        Assert.True(File.Exists(diffPath));

        var readContent = await File.ReadAllTextAsync(diffPath);
        Assert.Equal(diffContent, readContent);
    }

    [Fact]
    public async Task WriteDiffAsync_ThrowsException_WhenContentEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.WriteDiffAsync(ticketId, ""));
        Assert.Equal("diffContent", ex.ParamName);
    }

    [Fact]
    public async Task DiffExistsAsync_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var exists = await service.DiffExistsAsync(ticketId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task DiffExistsAsync_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        var diffContent = "diff --git a/file.txt b/file.txt\n...";
        await service.WriteDiffAsync(ticketId, diffContent);

        // Act
        var exists = await service.DiffExistsAsync(ticketId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task DeleteDiffAsync_RemovesDiffFile()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        var diffContent = "diff --git a/file.txt b/file.txt\n...";
        await service.WriteDiffAsync(ticketId, diffContent);

        // Verify file exists before deletion
        var existsBefore = await service.DiffExistsAsync(ticketId);
        Assert.True(existsBefore);

        // Act
        await service.DeleteDiffAsync(ticketId);

        // Assert
        var existsAfter = await service.DiffExistsAsync(ticketId);
        Assert.False(existsAfter);
    }

    [Fact]
    public async Task DeleteDiffAsync_DoesNotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act & Assert - should not throw
        await service.DeleteDiffAsync(ticketId);
    }

    [Fact]
    public async Task EnsureWorkspaceExistsAsync_CreatesDirectory()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        var expectedPath = service.GetWorkspaceDirectory(ticketId);

        // Verify directory doesn't exist before
        Assert.False(Directory.Exists(expectedPath));

        // Act
        var path = await service.EnsureWorkspaceExistsAsync(ticketId);

        // Assert
        Assert.Equal(expectedPath, path);
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public async Task EnsureWorkspaceExistsAsync_DoesNotThrow_WhenDirectoryAlreadyExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = CreateTempDirectory();
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Create directory first
        await service.EnsureWorkspaceExistsAsync(ticketId);

        // Act & Assert - should not throw
        var path = await service.EnsureWorkspaceExistsAsync(ticketId);
        Assert.True(Directory.Exists(path));
    }

    private IConfiguration CreateConfiguration(string basePath)
    {
        var configData = new Dictionary<string, string>
        {
            { "Workspace:BasePath", basePath }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "prfactory-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    public void Dispose()
    {
        // Cleanup temp directories created during tests
        foreach (var tempDir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
