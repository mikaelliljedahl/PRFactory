using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Git.Providers;
using Xunit;

namespace PRFactory.Tests.Git;

/// <summary>
/// Tests for GitPlatformService - the facade that coordinates platform providers
/// </summary>
public class GitPlatformServiceTests
{
    private readonly Mock<ILocalGitService> _mockLocalGitService;
    private readonly Mock<IGitPlatformProvider> _mockGitHubProvider;
    private readonly Mock<IGitPlatformProvider> _mockBitbucketProvider;
    private readonly Mock<IGitPlatformProvider> _mockAzureDevOpsProvider;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<GitPlatformService>> _mockLogger;

    public GitPlatformServiceTests()
    {
        _mockLocalGitService = new Mock<ILocalGitService>();
        _mockGitHubProvider = new Mock<IGitPlatformProvider>();
        _mockBitbucketProvider = new Mock<IGitPlatformProvider>();
        _mockAzureDevOpsProvider = new Mock<IGitPlatformProvider>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<GitPlatformService>>();

        // Setup provider names
        _mockGitHubProvider.Setup(p => p.PlatformName).Returns("GitHub");
        _mockBitbucketProvider.Setup(p => p.PlatformName).Returns("Bitbucket");
        _mockAzureDevOpsProvider.Setup(p => p.PlatformName).Returns("AzureDevOps");
    }

    #region CreatePullRequestAsync Tests

    [Fact]
    public async Task CreatePullRequestAsync_WithGitHubRepository_UsesGitHubProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest(
            "feature/test",
            "main",
            "Test PR",
            "Test Description"
        );
        var expectedPrInfo = new PullRequestInfo(123, "https://api.github.com/pr/123", "https://github.com/org/repo/pull/123");

        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreatePullRequestAsync(repositoryId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Number);
        Assert.Equal("https://github.com/org/repo/pull/123", result.HtmlUrl);

        _mockGitHubProvider.Verify(
            p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBitbucketProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithBitbucketRepository_UsesBitbucketProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest(
            "feature/test",
            "main",
            "Test PR",
            "Test Description"
        );
        var expectedPrInfo = new PullRequestInfo(456, "https://api.bitbucket.org/pr/456", "https://bitbucket.org/org/repo/pull/456");

        var repository = CreateRepositoryEntity(repositoryId, "Bitbucket");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockBitbucketProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreatePullRequestAsync(repositoryId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(456, result.Number);
        Assert.Equal("https://bitbucket.org/org/repo/pull/456", result.HtmlUrl);

        _mockBitbucketProvider.Verify(
            p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithAzureDevOpsRepository_UsesAzureDevOpsProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest(
            "feature/test",
            "main",
            "Test PR",
            "Test Description"
        );
        var expectedPrInfo = new PullRequestInfo(789, "https://dev.azure.com/pr/789", "https://dev.azure.com/org/project/_git/repo/pullrequest/789");

        var repository = CreateRepositoryEntity(repositoryId, "AzureDevOps");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockAzureDevOpsProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreatePullRequestAsync(repositoryId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(789, result.Number);
        Assert.Equal("https://dev.azure.com/org/project/_git/repo/pullrequest/789", result.HtmlUrl);

        _mockAzureDevOpsProvider.Verify(
            p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockBitbucketProvider.Verify(
            p => p.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithUnknownPlatform_ThrowsNotSupportedException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest(
            "feature/test",
            "main",
            "Test PR",
            "Test Description"
        );

        var repository = CreateRepositoryEntity(repositoryId, "GitLab");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.CreatePullRequestAsync(repositoryId, request));

        Assert.Contains("GitLab", exception.Message);
        Assert.Contains("not supported", exception.Message);
        Assert.Contains("GitHub", exception.Message);
        Assert.Contains("Bitbucket", exception.Message);
        Assert.Contains("AzureDevOps", exception.Message);
    }

    [Fact]
    public async Task CreatePullRequestAsync_PassesParametersCorrectly()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var sourceBranch = "feature/my-feature";
        var targetBranch = "develop";
        var title = "Add new feature";
        var description = "This PR adds a new feature with tests";

        var request = new CreatePullRequestRequest(
            sourceBranch,
            targetBranch,
            title,
            description
        );

        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        CreatePullRequestRequest? capturedRequest = null;
        _mockGitHubProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CreatePullRequestRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(new PullRequestInfo(1, "url", "html_url"));

        var service = CreateService(repositoryGetter);

        // Act
        await service.CreatePullRequestAsync(repositoryId, request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(sourceBranch, capturedRequest!.SourceBranch);
        Assert.Equal(targetBranch, capturedRequest.TargetBranch);
        Assert.Equal(title, capturedRequest.Title);
        Assert.Equal(description, capturedRequest.Description);
    }

    #endregion

    #region AddPullRequestCommentAsync Tests

    [Fact]
    public async Task AddPullRequestCommentAsync_WithGitHubRepository_UsesGitHubProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var prNumber = 42;
        var comment = "Looks good to me!";

        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act
        await service.AddPullRequestCommentAsync(repositoryId, prNumber, comment);

        // Assert
        _mockGitHubProvider.Verify(
            p => p.AddPullRequestCommentAsync(repositoryId, prNumber, comment, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBitbucketProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPullRequestCommentAsync_WithBitbucketRepository_UsesBitbucketProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var prNumber = 42;
        var comment = "Please fix the tests";

        var repository = CreateRepositoryEntity(repositoryId, "Bitbucket");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act
        await service.AddPullRequestCommentAsync(repositoryId, prNumber, comment);

        // Assert
        _mockBitbucketProvider.Verify(
            p => p.AddPullRequestCommentAsync(repositoryId, prNumber, comment, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPullRequestCommentAsync_WithAzureDevOpsRepository_UsesAzureDevOpsProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var prNumber = 42;
        var comment = "Approved";

        var repository = CreateRepositoryEntity(repositoryId, "AzureDevOps");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act
        await service.AddPullRequestCommentAsync(repositoryId, prNumber, comment);

        // Assert
        _mockAzureDevOpsProvider.Verify(
            p => p.AddPullRequestCommentAsync(repositoryId, prNumber, comment, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockBitbucketProvider.Verify(
            p => p.AddPullRequestCommentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetRepositoryInfoAsync Tests

    [Fact]
    public async Task GetRepositoryInfoAsync_WithGitHubRepository_UsesGitHubProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var expectedInfo = new RepositoryInfo("test-repo", "main", "https://github.com/org/test-repo.git");

        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.GetRepositoryInfoAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-repo", result.Name);
        Assert.Equal("main", result.DefaultBranch);
        Assert.Equal("https://github.com/org/test-repo.git", result.CloneUrl);

        _mockGitHubProvider.Verify(
            p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBitbucketProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_WithBitbucketRepository_UsesBitbucketProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var expectedInfo = new RepositoryInfo("test-repo", "master", "https://bitbucket.org/org/test-repo.git");

        var repository = CreateRepositoryEntity(repositoryId, "Bitbucket");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockBitbucketProvider
            .Setup(p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.GetRepositoryInfoAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-repo", result.Name);
        Assert.Equal("master", result.DefaultBranch);
        Assert.Equal("https://bitbucket.org/org/test-repo.git", result.CloneUrl);

        _mockBitbucketProvider.Verify(
            p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAzureDevOpsProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_WithAzureDevOpsRepository_UsesAzureDevOpsProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var expectedInfo = new RepositoryInfo("test-repo", "main", "https://dev.azure.com/org/project/_git/test-repo");

        var repository = CreateRepositoryEntity(repositoryId, "AzureDevOps");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockAzureDevOpsProvider
            .Setup(p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInfo);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.GetRepositoryInfoAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-repo", result.Name);
        Assert.Equal("main", result.DefaultBranch);
        Assert.Equal("https://dev.azure.com/org/project/_git/test-repo", result.CloneUrl);

        _mockAzureDevOpsProvider.Verify(
            p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitHubProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockBitbucketProvider.Verify(
            p => p.GetRepositoryInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Provider Selection Tests

    [Fact]
    public async Task CreatePullRequestAsync_WithCaseInsensitivePlatformName_SelectsCorrectProvider()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest("feature", "main", "Test", "Description");

        var repository = CreateRepositoryEntity(repositoryId, "github"); // lowercase
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PullRequestInfo(1, "url", "html_url"));

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreatePullRequestAsync(repositoryId, request);

        // Assert
        Assert.NotNull(result);
        _mockGitHubProvider.Verify(
            p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPullRequestCommentAsync_WithUnknownPlatform_ThrowsNotSupportedException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        var repository = CreateRepositoryEntity(repositoryId, "GitLab");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.AddPullRequestCommentAsync(repositoryId, 1, "comment"));

        Assert.Contains("GitLab", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_WithUnknownPlatform_ThrowsNotSupportedException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        var repository = CreateRepositoryEntity(repositoryId, "Subversion");
        var repositoryGetter = CreateRepositoryGetter(repository);

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.GetRepositoryInfoAsync(repositoryId));

        Assert.Contains("Subversion", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    #endregion

    #region CloneRepositoryAsync Tests

    [Fact]
    public async Task CloneRepositoryAsync_FirstCall_ClonesAndCaches()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var expectedPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockLocalGitService
            .Setup(s => s.CloneAsync(repository.CloneUrl, repository.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPath);

        object? cachedValue = null;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _mockCache
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CloneRepositoryAsync(repositoryId);

        // Assert
        Assert.Equal(expectedPath, result);
        _mockLocalGitService.Verify(
            s => s.CloneAsync(repository.CloneUrl, repository.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCache.Verify(
            c => c.CreateEntry(It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CloneRepositoryAsync_CachedPath_ReturnsFromCacheWithoutCloning()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var cachedPath = "/tmp/repos/cached-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        // Create actual temp directory for test
        Directory.CreateDirectory(cachedPath);

        try
        {
            object? cacheValue = cachedPath;
            _mockCache
                .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(true);

            var service = CreateService(repositoryGetter);

            // Act
            var result = await service.CloneRepositoryAsync(repositoryId);

            // Assert
            Assert.Equal(cachedPath, result);
            _mockLocalGitService.Verify(
                s => s.CloneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(cachedPath))
            {
                Directory.Delete(cachedPath, true);
            }
        }
    }

    [Fact]
    public async Task CloneRepositoryAsync_CachedPathDoesNotExist_ClonesAgain()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var cachedPath = "/tmp/repos/nonexistent-repo";
        var newPath = "/tmp/repos/new-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        // Ensure the cached path does not exist
        if (Directory.Exists(cachedPath))
        {
            Directory.Delete(cachedPath, true);
        }

        object? cacheValue = cachedPath;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        _mockLocalGitService
            .Setup(s => s.CloneAsync(repository.CloneUrl, repository.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPath);

        _mockCache
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CloneRepositoryAsync(repositoryId);

        // Assert
        Assert.Equal(newPath, result);
        _mockLocalGitService.Verify(
            s => s.CloneAsync(repository.CloneUrl, repository.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CreateBranchAsync Tests

    [Fact]
    public async Task CreateBranchAsync_WithDefaultBranch_CreatesFromRepositoryDefaultBranch()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/new-feature";
        var repoPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        repository.DefaultBranch = "main";
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        _mockLocalGitService
            .Setup(s => s.CreateBranchAsync(repoPath, branchName, repository.DefaultBranch))
            .ReturnsAsync(branchName);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreateBranchAsync(repositoryId, branchName);

        // Assert
        Assert.Equal(branchName, result);
        _mockLocalGitService.Verify(
            s => s.CreateBranchAsync(repoPath, branchName, repository.DefaultBranch),
            Times.Once);
    }

    [Fact]
    public async Task CreateBranchAsync_WithSpecificSourceBranch_CreatesFromSpecifiedBranch()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/new-feature";
        var fromBranch = "develop";
        var repoPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        _mockLocalGitService
            .Setup(s => s.CreateBranchAsync(repoPath, branchName, fromBranch))
            .ReturnsAsync(branchName);

        var service = CreateService(repositoryGetter);

        // Act
        var result = await service.CreateBranchAsync(repositoryId, branchName, fromBranch);

        // Assert
        Assert.Equal(branchName, result);
        _mockLocalGitService.Verify(
            s => s.CreateBranchAsync(repoPath, branchName, fromBranch),
            Times.Once);
    }

    #endregion

    #region CommitFilesAsync Tests

    [Fact]
    public async Task CommitFilesAsync_WithValidFiles_CommitsSuccessfully()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/test";
        var repoPath = "/tmp/repos/test-repo";
        var files = new Dictionary<string, string>
        {
            { "file1.txt", "content1" },
            { "file2.cs", "content2" }
        };
        var commitMessage = "Add new files";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        var service = CreateService(repositoryGetter);

        // Act
        await service.CommitFilesAsync(repositoryId, branchName, files, commitMessage);

        // Assert
        _mockLocalGitService.Verify(
            s => s.CommitAsync(
                repoPath,
                files,
                commitMessage,
                "Claude AI <claude@prfactory.ai>"),
            Times.Once);
    }

    [Fact]
    public async Task CommitFilesAsync_WithEmptyFiles_StillCallsCommit()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/test";
        var repoPath = "/tmp/repos/test-repo";
        var files = new Dictionary<string, string>();
        var commitMessage = "Empty commit";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        var service = CreateService(repositoryGetter);

        // Act
        await service.CommitFilesAsync(repositoryId, branchName, files, commitMessage);

        // Assert
        _mockLocalGitService.Verify(
            s => s.CommitAsync(
                repoPath,
                files,
                commitMessage,
                "Claude AI <claude@prfactory.ai>"),
            Times.Once);
    }

    #endregion

    #region PushBranchAsync Tests

    [Fact]
    public async Task PushBranchAsync_WithValidBranch_PushesSuccessfully()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/test";
        var repoPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        var service = CreateService(repositoryGetter);

        // Act
        await service.PushBranchAsync(repositoryId, branchName);

        // Assert
        _mockLocalGitService.Verify(
            s => s.PushAsync(repoPath, branchName, repository.AccessToken),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreatePullRequestAsync_WhenProviderThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var request = new CreatePullRequestRequest("feature", "main", "Test", "Description");
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.CreatePullRequestAsync(repositoryId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider error"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreatePullRequestAsync(repositoryId, request));
        Assert.Equal("Provider error", exception.Message);
    }

    [Fact]
    public async Task AddPullRequestCommentAsync_WhenProviderThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.AddPullRequestCommentAsync(repositoryId, 1, "comment", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Comment error"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddPullRequestCommentAsync(repositoryId, 1, "comment"));
        Assert.Equal("Comment error", exception.Message);
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_WhenProviderThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        _mockGitHubProvider
            .Setup(p => p.GetRepositoryInfoAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Info error"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetRepositoryInfoAsync(repositoryId));
        Assert.Equal("Info error", exception.Message);
    }

    [Fact]
    public async Task CloneRepositoryAsync_WhenLocalGitServiceThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        object? cachedValue = null;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _mockLocalGitService
            .Setup(s => s.CloneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Clone failed"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CloneRepositoryAsync(repositoryId));
        Assert.Equal("Clone failed", exception.Message);
    }

    [Fact]
    public async Task CreateBranchAsync_WhenLocalGitServiceThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/test";
        var repoPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        _mockLocalGitService
            .Setup(s => s.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Branch creation failed"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBranchAsync(repositoryId, branchName));
        Assert.Equal("Branch creation failed", exception.Message);
    }

    [Fact]
    public async Task PushBranchAsync_WhenLocalGitServiceThrowsException_PropagatesException()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/test";
        var repoPath = "/tmp/repos/test-repo";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        _mockLocalGitService
            .Setup(s => s.PushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Push failed"));

        var service = CreateService(repositoryGetter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PushBranchAsync(repositoryId, branchName));
        Assert.Equal("Push failed", exception.Message);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWorkflow_CreateBranchCommitAndPush_ExecutesInCorrectOrder()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branchName = "feature/full-workflow";
        var repoPath = "/tmp/repos/test-repo";
        var files = new Dictionary<string, string> { { "test.txt", "content" } };
        var commitMessage = "Test commit";
        var repository = CreateRepositoryEntity(repositoryId, "GitHub");
        var repositoryGetter = CreateRepositoryGetter(repository);

        SetupCachedRepository(repositoryId, repoPath);

        var callOrder = new List<string>();

        _mockLocalGitService
            .Setup(s => s.CreateBranchAsync(repoPath, branchName, repository.DefaultBranch))
            .Callback(() => callOrder.Add("CreateBranch"))
            .ReturnsAsync(branchName);

        _mockLocalGitService
            .Setup(s => s.CommitAsync(repoPath, files, commitMessage, It.IsAny<string>()))
            .Callback(() => callOrder.Add("Commit"))
            .Returns(Task.CompletedTask);

        _mockLocalGitService
            .Setup(s => s.PushAsync(repoPath, branchName, repository.AccessToken))
            .Callback(() => callOrder.Add("Push"))
            .Returns(Task.CompletedTask);

        var service = CreateService(repositoryGetter);

        // Act
        await service.CreateBranchAsync(repositoryId, branchName);
        await service.CommitFilesAsync(repositoryId, branchName, files, commitMessage);
        await service.PushBranchAsync(repositoryId, branchName);

        // Assert
        Assert.Equal(3, callOrder.Count);
        Assert.Equal("CreateBranch", callOrder[0]);
        Assert.Equal("Commit", callOrder[1]);
        Assert.Equal("Push", callOrder[2]);
    }

    [Fact]
    public async Task MultipleRepositories_DifferentPlatforms_SelectsCorrectProviders()
    {
        // Arrange
        var githubRepoId = Guid.NewGuid();
        var bitbucketRepoId = Guid.NewGuid();
        var azureRepoId = Guid.NewGuid();

        var githubRepo = CreateRepositoryEntity(githubRepoId, "GitHub");
        var bitbucketRepo = CreateRepositoryEntity(bitbucketRepoId, "Bitbucket");
        var azureRepo = CreateRepositoryEntity(azureRepoId, "AzureDevOps");

        var repositories = new Dictionary<Guid, RepositoryEntity>
        {
            { githubRepoId, githubRepo },
            { bitbucketRepoId, bitbucketRepo },
            { azureRepoId, azureRepo }
        };

        Func<Guid, CancellationToken, Task<RepositoryEntity>> multiRepoGetter = (id, ct) =>
        {
            if (repositories.TryGetValue(id, out var repo))
            {
                return Task.FromResult(repo);
            }
            throw new InvalidOperationException($"Repository {id} not found");
        };

        _mockGitHubProvider
            .Setup(p => p.GetRepositoryInfoAsync(githubRepoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryInfo("github-repo", "main", "https://github.com/org/repo.git"));

        _mockBitbucketProvider
            .Setup(p => p.GetRepositoryInfoAsync(bitbucketRepoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryInfo("bitbucket-repo", "main", "https://bitbucket.org/org/repo.git"));

        _mockAzureDevOpsProvider
            .Setup(p => p.GetRepositoryInfoAsync(azureRepoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryInfo("azure-repo", "main", "https://dev.azure.com/org/repo"));

        var service = CreateService(multiRepoGetter);

        // Act
        var githubInfo = await service.GetRepositoryInfoAsync(githubRepoId);
        var bitbucketInfo = await service.GetRepositoryInfoAsync(bitbucketRepoId);
        var azureInfo = await service.GetRepositoryInfoAsync(azureRepoId);

        // Assert
        Assert.Equal("github-repo", githubInfo.Name);
        Assert.Equal("bitbucket-repo", bitbucketInfo.Name);
        Assert.Equal("azure-repo", azureInfo.Name);

        _mockGitHubProvider.Verify(
            p => p.GetRepositoryInfoAsync(githubRepoId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBitbucketProvider.Verify(
            p => p.GetRepositoryInfoAsync(bitbucketRepoId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAzureDevOpsProvider.Verify(
            p => p.GetRepositoryInfoAsync(azureRepoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private GitPlatformService CreateService(Func<Guid, CancellationToken, Task<RepositoryEntity>> repositoryGetter)
    {
        var providers = new List<IGitPlatformProvider>
        {
            _mockGitHubProvider.Object,
            _mockBitbucketProvider.Object,
            _mockAzureDevOpsProvider.Object
        };

        return new GitPlatformService(
            _mockLocalGitService.Object,
            providers,
            _mockCache.Object,
            _mockLogger.Object,
            repositoryGetter
        );
    }

    private static RepositoryEntity CreateRepositoryEntity(Guid id, string platform)
    {
        return new RepositoryEntity
        {
            Id = id,
            Name = "test-repo",
            GitPlatform = platform,
            CloneUrl = $"https://{platform.ToLower()}.com/org/test-repo.git",
            DefaultBranch = "main",
            AccessToken = "test-token"
        };
    }

    private static Func<Guid, CancellationToken, Task<RepositoryEntity>> CreateRepositoryGetter(RepositoryEntity repository)
    {
        return (id, ct) =>
        {
            if (id == repository.Id)
            {
                return Task.FromResult(repository);
            }
            throw new InvalidOperationException($"Repository {id} not found");
        };
    }

    private void SetupCachedRepository(Guid repositoryId, string repoPath)
    {
        object? cacheValue = repoPath;
        _mockCache
            .Setup(c => c.TryGetValue($"repo:path:{repositoryId}", out cacheValue))
            .Returns(true);
    }

    #endregion
}
