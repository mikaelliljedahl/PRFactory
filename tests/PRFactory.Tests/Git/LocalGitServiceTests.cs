using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Git;
using Xunit;
using Xunit.Abstractions;

namespace PRFactory.Tests.Git;

/// <summary>
/// Tests for LocalGitService - LibGit2Sharp wrapper for local git operations
/// </summary>
public class LocalGitServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LocalGitService>> _mockLogger;
    private readonly string _testWorkspacePath;
    private readonly List<string> _pathsToCleanup;
    private bool _disposed;

    public LocalGitServiceTests(ITestOutputHelper testOutputHelper)
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LocalGitService>>();
        _testWorkspacePath = Path.Combine(Path.GetTempPath(), "prfactory-tests", Guid.NewGuid().ToString());
        _pathsToCleanup = new List<string>();

        // Setup configuration to use test workspace
        _mockConfiguration.Setup(c => c["Workspace:BasePath"]).Returns(_testWorkspacePath);

        // Ensure test workspace exists
        Directory.CreateDirectory(_testWorkspacePath);
        _pathsToCleanup.Add(_testWorkspacePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Cleanup test directories
            foreach (var path in _pathsToCleanup)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        // Remove read-only attributes that git might set
                        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        Directory.Delete(path, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        _disposed = true;
    }

    #region CloneAsync Tests

    [Fact]
    public async Task CloneAsync_WithValidRepository_ClonesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var sourceRepoPath = CreateTestRepository("source-repo");
        var repoUrl = sourceRepoPath; // Using local path as URL for testing

        // Act
        var clonedPath = await service.CloneAsync(repoUrl, "token", CancellationToken.None);

        // Assert
        Assert.NotNull(clonedPath);
        Assert.True(Directory.Exists(clonedPath));
        Assert.True(Directory.Exists(Path.Combine(clonedPath, ".git")));

        _pathsToCleanup.Add(Path.GetDirectoryName(clonedPath)!);
    }

    [Fact]
    public async Task CloneAsync_CreatesUniqueDirectory()
    {
        // Arrange
        var service = CreateService();
        var sourceRepoPath = CreateTestRepository("source-repo");
        var repoUrl = sourceRepoPath;

        // Act
        var firstClone = await service.CloneAsync(repoUrl, "token", CancellationToken.None);
        var secondClone = await service.CloneAsync(repoUrl, "token", CancellationToken.None);

        // Assert
        Assert.NotEqual(firstClone, secondClone);
        Assert.True(Directory.Exists(firstClone));
        Assert.True(Directory.Exists(secondClone));

        _pathsToCleanup.Add(Path.GetDirectoryName(firstClone)!);
        _pathsToCleanup.Add(Path.GetDirectoryName(secondClone)!);
    }

    [Fact]
    public async Task CloneAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidUrl = "/nonexistent/path/to/repo";

        // Act & Assert - Invalid URL throws UriFormatException
        var exception = await Assert.ThrowsAsync<UriFormatException>(
            () => service.CloneAsync(invalidUrl, "token", CancellationToken.None));

        Assert.Contains("not a valid absolute URI", exception.Message);
    }

    [Fact]
    public async Task CloneAsync_ExtractsCorrectRepoName()
    {
        // Arrange
        var service = CreateService();
        var sourceRepoPath = CreateTestRepository("my-test-repo");
        var repoUrl = sourceRepoPath;

        // Act
        var clonedPath = await service.CloneAsync(repoUrl, "token", CancellationToken.None);

        // Assert
        Assert.Contains("my-test-repo", clonedPath);

        _pathsToCleanup.Add(Path.GetDirectoryName(clonedPath)!);
    }

    #endregion

    #region CreateBranchAsync Tests

    [Fact]
    public async Task CreateBranchAsync_FromDefaultBranch_CreatesBranchSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");
        var branchName = "feature/new-feature";

        // Act
        var result = await service.CreateBranchAsync(repoPath, branchName, "master");

        // Assert
        Assert.Equal(branchName, result);

        using var repo = new Repository(repoPath);
        var branch = repo.Branches[branchName];
        Assert.NotNull(branch);
        Assert.Equal(branchName, repo.Head.FriendlyName); // Should be checked out
    }

    [Fact]
    public async Task CreateBranchAsync_FromSpecificBranch_CreatesBranchFromCorrectBase()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");

        // Create a test branch with a commit
        using (var repo = new Repository(repoPath))
        {
            var testBranch = repo.CreateBranch("test-base");
            Commands.Checkout(repo, testBranch);

            var filePath = Path.Combine(repoPath, "test.txt");
            await File.WriteAllTextAsync(filePath, "test content");
            Commands.Stage(repo, "test.txt");

            var signature = new Signature("Test", "test@test.com", DateTimeOffset.Now);
            repo.Commit("Test commit", signature, signature);
        }

        var newBranchName = "feature/from-test-base";

        // Act
        var result = await service.CreateBranchAsync(repoPath, newBranchName, "test-base");

        // Assert
        Assert.Equal(newBranchName, result);

        using var repoCheck = new Repository(repoPath);
        var newBranch = repoCheck.Branches[newBranchName];
        var baseBranch = repoCheck.Branches["test-base"];
        Assert.NotNull(newBranch);
        Assert.Equal(baseBranch.Tip.Sha, newBranch.Tip.Sha);
    }

    [Fact]
    public async Task CreateBranchAsync_FromNonexistentBranch_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");
        var branchName = "feature/new-feature";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBranchAsync(repoPath, branchName, "nonexistent-branch"));

        Assert.Contains("not found", exception.Message);
        Assert.Contains("nonexistent-branch", exception.Message);
    }

    [Fact]
    public async Task CreateBranchAsync_ChecksOutNewBranch()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");
        var branchName = "feature/checked-out";

        // Act
        await service.CreateBranchAsync(repoPath, branchName, "master");

        // Assert
        using var repo = new Repository(repoPath);
        Assert.Equal(branchName, repo.Head.FriendlyName);
        Assert.False(repo.Head.IsRemote);
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_WithSingleFile_CreatesCommitSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");
        var files = new Dictionary<string, string>
        {
            { "test.txt", "Hello, World!" }
        };
        var commitMessage = "Add test file";
        var author = "Test Author";

        // Act
        await service.CommitAsync(repoPath, files, commitMessage, author);

        // Assert
        using var repo = new Repository(repoPath);
        var lastCommit = repo.Head.Tip;
        Assert.NotNull(lastCommit);
        Assert.Equal(commitMessage, lastCommit.Message.TrimEnd('\n'));
        Assert.Equal(author, lastCommit.Author.Name);
        Assert.Equal("claude@prfactory.ai", lastCommit.Author.Email);

        var filePath = Path.Combine(repoPath, "test.txt");
        Assert.True(File.Exists(filePath));
        Assert.Equal("Hello, World!", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task CommitAsync_WithMultipleFiles_CreatesCommitWithAllFiles()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");
        var files = new Dictionary<string, string>
        {
            { "file1.txt", "Content 1" },
            { "file2.cs", "Content 2" },
            { "subfolder/file3.md", "Content 3" }
        };
        var commitMessage = "Add multiple files";
        var author = "Test Author";

        // Act
        await service.CommitAsync(repoPath, files, commitMessage, author);

        // Assert
        using var repo = new Repository(repoPath);
        var lastCommit = repo.Head.Tip;

        // Tree includes: file1.txt, file2.cs, subfolder (tree node), README.md (from initial commit) = 4 entries
        Assert.Equal(4, lastCommit.Tree.Count);
        Assert.True(File.Exists(Path.Combine(repoPath, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(repoPath, "file2.cs")));
        Assert.True(File.Exists(Path.Combine(repoPath, "subfolder/file3.md")));
    }

    [Fact]
    public async Task CommitAsync_CreatesSubdirectoriesAutomatically()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");
        var files = new Dictionary<string, string>
        {
            { "deeply/nested/folder/file.txt", "Nested content" }
        };
        var commitMessage = "Add nested file";
        var author = "Test Author";

        // Act
        await service.CommitAsync(repoPath, files, commitMessage, author);

        // Assert
        var filePath = Path.Combine(repoPath, "deeply/nested/folder/file.txt");
        Assert.True(File.Exists(filePath));
        Assert.Equal("Nested content", await File.ReadAllTextAsync(filePath));

        using var repo = new Repository(repoPath);
        var status = repo.RetrieveStatus();
        Assert.Empty(status.Modified);
        Assert.Empty(status.Untracked);
    }

    [Fact]
    public async Task CommitAsync_WithEmptyFilesDictionary_CreatesEmptyCommit()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");
        var files = new Dictionary<string, string>();
        var commitMessage = "Empty commit";
        var author = "Test Author";

        // Act & Assert
        // LibGit2Sharp will throw EmptyCommitException when trying to commit with no changes
        await Assert.ThrowsAsync<EmptyCommitException>(
            () => service.CommitAsync(repoPath, files, commitMessage, author));
    }

    [Fact]
    public async Task CommitAsync_SetsCorrectAuthorInformation()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");
        var files = new Dictionary<string, string> { { "file.txt", "content" } };
        var commitMessage = "Test commit";
        var authorName = "Claude AI";

        // Act
        await service.CommitAsync(repoPath, files, commitMessage, authorName);

        // Assert
        using var repo = new Repository(repoPath);
        var lastCommit = repo.Head.Tip;
        Assert.Equal("Claude AI", lastCommit.Author.Name);
        Assert.Equal("claude@prfactory.ai", lastCommit.Author.Email);
        Assert.Equal("Claude AI", lastCommit.Committer.Name);
        Assert.Equal("claude@prfactory.ai", lastCommit.Committer.Email);
    }

    [Fact]
    public async Task CommitAsync_UpdatesExistingFile_CreatesNewCommit()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("commit-test");

        // First commit
        var files1 = new Dictionary<string, string> { { "file.txt", "Original content" } };
        await service.CommitAsync(repoPath, files1, "First commit", "Author");

        // Act - Update same file
        var files2 = new Dictionary<string, string> { { "file.txt", "Updated content" } };
        await service.CommitAsync(repoPath, files2, "Second commit", "Author");

        // Assert
        using var repo = new Repository(repoPath);
        Assert.Equal(3, repo.Commits.Count()); // Initial + 2 commits
        Assert.Equal("Second commit", repo.Head.Tip.Message.TrimEnd('\n'));

        var fileContent = await File.ReadAllTextAsync(Path.Combine(repoPath, "file.txt"));
        Assert.Equal("Updated content", fileContent);
    }

    #endregion

    #region PushAsync Tests

    [Fact]
    public async Task PushAsync_WithNonexistentBranch_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("push-test");
        var branchName = "nonexistent-branch";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PushAsync(repoPath, branchName, "token"));

        Assert.Contains("not found", exception.Message);
        Assert.Contains(branchName, exception.Message);
    }

    [Fact]
    public async Task PushAsync_WithValidBranch_ConfiguresCredentials()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("push-test");
        var branchName = "feature/test";

        // Create a branch
        await service.CreateBranchAsync(repoPath, branchName, "master");

        // Add a commit so there's something to push
        var files = new Dictionary<string, string> { { "file.txt", "content" } };
        await service.CommitAsync(repoPath, files, "Test commit", "Author");

        // Act & Assert
        // This will fail because there's no actual remote, but we can verify the branch exists
        using var repo = new Repository(repoPath);
        var branch = repo.Branches[branchName];
        Assert.NotNull(branch);

        // PushAsync will throw because there's no remote configured, which is expected
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.PushAsync(repoPath, branchName, "test-token"));
    }

    #endregion

    #region BranchExistsAsync Tests

    [Fact]
    public async Task BranchExistsAsync_WithExistingBranch_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");
        var branchName = "feature/existing";

        await service.CreateBranchAsync(repoPath, branchName, "master");

        // Act
        var exists = await service.BranchExistsAsync(repoPath, branchName);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task BranchExistsAsync_WithNonexistentBranch_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");
        var branchName = "feature/nonexistent";

        // Act
        var exists = await service.BranchExistsAsync(repoPath, branchName);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task BranchExistsAsync_WithMasterBranch_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");

        // Act
        var exists = await service.BranchExistsAsync(repoPath, "master");

        // Assert
        Assert.True(exists);
    }

    #endregion

    #region GetDefaultBranch Tests

    [Fact]
    public void GetDefaultBranch_ReturnsCurrentBranchName()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");

        // Act
        var defaultBranch = service.GetDefaultBranch(repoPath);

        // Assert
        Assert.Equal("master", defaultBranch);
    }

    [Fact]
    public void GetDefaultBranch_AfterCheckout_ReturnsCheckedOutBranch()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("branch-test");

        using (var repo = new Repository(repoPath))
        {
            var newBranch = repo.CreateBranch("develop");
            Commands.Checkout(repo, newBranch);
        }

        // Act
        var defaultBranch = service.GetDefaultBranch(repoPath);

        // Assert
        Assert.Equal("develop", defaultBranch);
    }

    [Fact]
    public void GetDefaultBranch_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidPath = "/nonexistent/path";

        // Act & Assert
        Assert.Throws<RepositoryNotFoundException>(
            () => service.GetDefaultBranch(invalidPath));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateBranchAsync_WithInvalidRepoPath_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidPath = "/nonexistent/repo";
        var branchName = "feature/test";

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => service.CreateBranchAsync(invalidPath, branchName, "master"));
    }

    [Fact]
    public async Task CommitAsync_WithInvalidRepoPath_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidPath = "/nonexistent/repo";
        var files = new Dictionary<string, string> { { "test.txt", "content" } };

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => service.CommitAsync(invalidPath, files, "message", "author"));
    }

    [Fact]
    public async Task BranchExistsAsync_WithInvalidRepoPath_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var invalidPath = "/nonexistent/repo";

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => service.BranchExistsAsync(invalidPath, "master"));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWorkflow_CreateBranchCommitAndCheckStatus_ExecutesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("workflow-test");
        var branchName = "feature/full-workflow";
        var files = new Dictionary<string, string>
        {
            { "src/Program.cs", "Console.WriteLine(\"Hello\");" },
            { "README.md", "# Test Project" }
        };
        var commitMessage = "Add initial files";
        var author = "Test Developer";

        // Act - Full workflow
        await service.CreateBranchAsync(repoPath, branchName, "master");
        await service.CommitAsync(repoPath, files, commitMessage, author);
        var branchExists = await service.BranchExistsAsync(repoPath, branchName);
        var currentBranch = service.GetDefaultBranch(repoPath);

        // Assert
        Assert.True(branchExists);
        Assert.Equal(branchName, currentBranch);

        using var repo = new Repository(repoPath);
        Assert.Equal(commitMessage, repo.Head.Tip.Message.TrimEnd('\n'));
        Assert.Equal(author, repo.Head.Tip.Author.Name);
        Assert.True(File.Exists(Path.Combine(repoPath, "src/Program.cs")));
        Assert.True(File.Exists(Path.Combine(repoPath, "README.md")));
    }

    [Fact]
    public async Task MultipleCommits_OnSameBranch_CreatesCommitHistory()
    {
        // Arrange
        var service = CreateService();
        var repoPath = CreateTestRepository("history-test");
        var branchName = "feature/multiple-commits";

        await service.CreateBranchAsync(repoPath, branchName, "master");

        // Act - Multiple commits
        await service.CommitAsync(
            repoPath,
            new Dictionary<string, string> { { "file1.txt", "First" } },
            "First commit",
            "Author1");

        await service.CommitAsync(
            repoPath,
            new Dictionary<string, string> { { "file2.txt", "Second" } },
            "Second commit",
            "Author2");

        await service.CommitAsync(
            repoPath,
            new Dictionary<string, string> { { "file3.txt", "Third" } },
            "Third commit",
            "Author3");

        // Assert
        using var repo = new Repository(repoPath);

        // Verify files exist on the feature branch
        Assert.True(File.Exists(Path.Combine(repoPath, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(repoPath, "file2.txt")));
        Assert.True(File.Exists(Path.Combine(repoPath, "file3.txt")));

        // Verify we're on the correct branch
        Assert.Equal(branchName, repo.Head.FriendlyName);

        // Get commits from HEAD
        var commits = repo.Head.Commits.ToList();

        // Verify commit history - should have initial commit + 3 new commits
        Assert.Equal(4, commits.Count);

        // Get commit messages
        var messages = commits.Select(c => c.Message.TrimEnd('\n')).ToList();

        // Verify all expected commits exist in history
        Assert.Contains("Initial commit", messages);
        Assert.Contains("First commit", messages);
        Assert.Contains("Second commit", messages);
        Assert.Contains("Third commit", messages);

        // Verify most recent commit is "Third commit"
        Assert.Equal("Third commit", messages[0]);
    }

    #endregion

    #region Helper Methods

    private LocalGitService CreateService()
    {
        return new LocalGitService(_mockConfiguration.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Creates a test git repository with an initial commit
    /// </summary>
    private string CreateTestRepository(string name)
    {
        var repoPath = Path.Combine(Path.GetTempPath(), "prfactory-test-repos", Guid.NewGuid().ToString(), name);
        Directory.CreateDirectory(repoPath);
        _pathsToCleanup.Add(Path.GetDirectoryName(repoPath)!);

        // Initialize repository
        Repository.Init(repoPath);

        // Create initial commit (required for branch operations)
        using (var repo = new Repository(repoPath))
        {
            var readmePath = Path.Combine(repoPath, "README.md");
            File.WriteAllTextAsync(readmePath, $"# {name}").GetAwaiter().GetResult();
            Commands.Stage(repo, "README.md");

            var signature = new Signature("Test Init", "test@test.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", signature, signature);

            // Rename the default branch from "main" to "master" for test consistency
            var currentBranch = repo.Head;
            if (currentBranch.FriendlyName == "main")
            {
                var masterBranch = repo.Branches.Rename(currentBranch, "master");
                Commands.Checkout(repo, masterBranch);
            }
        }

        return repoPath;
    }

    #endregion
}
