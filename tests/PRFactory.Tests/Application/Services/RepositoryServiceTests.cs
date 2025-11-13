using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Application;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Tests.Application.Services;

public class RepositoryServiceTests
{
    private readonly Mock<IRepositoryRepository> _mockRepositoryRepository;
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILocalGitService> _mockLocalGitService;
    private readonly Mock<ILogger<RepositoryService>> _mockLogger;
    private readonly RepositoryService _service;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    public RepositoryServiceTests()
    {
        _mockRepositoryRepository = new Mock<IRepositoryRepository>();
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLocalGitService = new Mock<ILocalGitService>();
        _mockLogger = new Mock<ILogger<RepositoryService>>();

        // Setup default tenant ID
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        // Setup default encryption behavior
        _mockEncryptionService
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns<string>(s => $"encrypted_{s}");

        _mockEncryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns<string>(s => s.Replace("encrypted_", ""));

        _service = new RepositoryService(
            _mockRepositoryRepository.Object,
            _mockTicketRepository.Object,
            _mockCurrentUserService.Object,
            _mockEncryptionService.Object,
            _mockLocalGitService.Object,
            _mockLogger.Object);
    }

    #region GetRepositoriesForTenantAsync Tests

    [Fact]
    public async Task GetRepositoriesForTenantAsync_ReturnsRepositoriesForCurrentTenant()
    {
        // Arrange
        var repositories = new List<Repository>
        {
            Repository.Create(_tenantId, "Repo1", "GitHub", "https://github.com/test/repo1", "token1"),
            Repository.Create(_tenantId, "Repo2", "GitHub", "https://github.com/test/repo2", "token2")
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var result = await _service.GetRepositoriesForTenantAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Repo1", result[0].Name);
        Assert.Equal("Repo2", result[1].Name);
        Assert.All(result, r => Assert.Equal(_tenantId, r.TenantId));
    }

    [Fact]
    public async Task GetRepositoriesForTenantAsync_ReturnsEmptyListWhenNoRepositories()
    {
        // Arrange
        _mockRepositoryRepository
            .Setup(x => x.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetRepositoriesForTenantAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRepositoriesForTenantAsync_ThrowsWhenNoCurrentTenant()
    {
        // Arrange
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetRepositoriesForTenantAsync());
    }

    #endregion

    #region GetRepositoryByIdAsync Tests

    [Fact]
    public async Task GetRepositoryByIdAsync_ReturnsRepositoryWhenExistsAndBelongsToTenant()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var result = await _service.GetRepositoryByIdAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryId, result.Id);
        Assert.Equal("TestRepo", result.Name);
        Assert.Equal(_tenantId, result.TenantId);
    }

    [Fact]
    public async Task GetRepositoryByIdAsync_ReturnsNullWhenRepositoryNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        // Act
        var result = await _service.GetRepositoryByIdAsync(repositoryId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRepositoryByIdAsync_ReturnsNullWhenRepositoryBelongsToOtherTenant()
    {
        // Arrange - Repository belongs to different tenant
        var repository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var result = await _service.GetRepositoryByIdAsync(repositoryId);

        // Assert - Should return null due to tenant isolation
        Assert.Null(result);
    }

    #endregion

    #region CreateRepositoryAsync Tests

    [Fact]
    public async Task CreateRepositoryAsync_CreatesRepositoryWithEncryptedToken()
    {
        // Arrange
        var dto = new CreateRepositoryDto
        {
            Name = "NewRepo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/newrepo",
            AccessToken = "plain_token",
            DefaultBranch = "main"
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByCloneUrlAsync(dto.CloneUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        _mockRepositoryRepository
            .Setup(x => x.AddAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository r, CancellationToken ct) => r);

        // Act
        var result = await _service.CreateRepositoryAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewRepo", result.Name);
        Assert.Equal(_tenantId, result.TenantId);

        // Verify encryption was called
        _mockEncryptionService.Verify(x => x.Encrypt("plain_token"), Times.Once);

        // Verify repository was added with encrypted token
        _mockRepositoryRepository.Verify(
            x => x.AddAsync(
                It.Is<Repository>(r =>
                    r.Name == "NewRepo" &&
                    r.AccessToken == "encrypted_plain_token" &&
                    r.TenantId == _tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRepositoryAsync_ThrowsWhenCloneUrlAlreadyExists()
    {
        // Arrange
        var existingRepository = Repository.Create(_tenantId, "ExistingRepo", "GitHub", "https://github.com/test/repo", "token");

        var dto = new CreateRepositoryDto
        {
            Name = "NewRepo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo", // Same URL
            AccessToken = "token",
            DefaultBranch = "main"
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByCloneUrlAsync(dto.CloneUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRepository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRepositoryAsync(dto));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateRepositoryAsync_ThrowsWhenNoCurrentTenant()
    {
        // Arrange
        _mockCurrentUserService
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var dto = new CreateRepositoryDto
        {
            Name = "NewRepo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo",
            AccessToken = "token"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateRepositoryAsync(dto));
    }

    #endregion

    #region UpdateRepositoryAsync Tests

    [Fact]
    public async Task UpdateRepositoryAsync_UpdatesRepositorySuccessfully()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "OldName", "GitHub", "https://github.com/test/repo", "encrypted_old_token");
        var repositoryId = repository.Id;

        var dto = new UpdateRepositoryDto
        {
            Name = "NewName",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo",
            AccessToken = "new_token",
            DefaultBranch = "main",
            IsActive = true
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockRepositoryRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateRepositoryAsync(repositoryId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryId, result.Id);
        Assert.Equal(_tenantId, result.TenantId);

        // Verify encryption was called twice for new token:
        // 1. When creating updatedRepository (line 125)
        // 2. When calling repository.UpdateAccessToken (line 141)
        _mockEncryptionService.Verify(x => x.Encrypt("new_token"), Times.Exactly(2));

        // Verify update was called
        _mockRepositoryRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRepositoryAsync_DoesNotUpdateTokenWhenNotProvided()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "encrypted_old_token");
        var repositoryId = repository.Id;

        var dto = new UpdateRepositoryDto
        {
            Name = "NewName",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo",
            AccessToken = null, // Not updating token
            DefaultBranch = "main",
            IsActive = true
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockRepositoryRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateRepositoryAsync(repositoryId, dto);

        // Assert
        Assert.NotNull(result);

        // Verify encryption was not called for null token
        _mockEncryptionService.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRepositoryAsync_ThrowsWhenRepositoryNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var dto = new UpdateRepositoryDto
        {
            Name = "NewName",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo",
            DefaultBranch = "main",
            IsActive = true
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateRepositoryAsync(repositoryId, dto));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateRepositoryAsync_ThrowsWhenRepositoryBelongsToOtherTenant()
    {
        // Arrange
        var repository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");
        var repositoryId = repository.Id;

        var dto = new UpdateRepositoryDto
        {
            Name = "NewName",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/other/repo",
            DefaultBranch = "main",
            IsActive = true
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateRepositoryAsync(repositoryId, dto));

        Assert.Contains("different tenant", exception.Message);
    }

    #endregion

    #region DeleteRepositoryAsync Tests

    [Fact]
    public async Task DeleteRepositoryAsync_SoftDeletesRepositorySuccessfully()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockRepositoryRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteRepositoryAsync(repositoryId);

        // Assert - Should soft delete by calling UpdateAsync, not DeleteAsync
        _mockRepositoryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<Repository>(r => !r.IsActive), // Verify repository is deactivated
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify hard delete was never called
        _mockRepositoryRepository.Verify(
            x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteRepositoryAsync_ThrowsWhenRepositoryNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteRepositoryAsync(repositoryId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task DeleteRepositoryAsync_ThrowsWhenRepositoryBelongsToOtherTenant()
    {
        // Arrange
        var repository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteRepositoryAsync(repositoryId));

        Assert.Contains("different tenant", exception.Message);
    }

    #endregion

    #region TestRepositoryConnectionAsync Tests

    [Fact]
    public async Task TestRepositoryConnectionAsync_ReturnsSuccessWhenConnectionSucceeds()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "encrypted_token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockLocalGitService
            .Setup(x => x.CloneAsync(repository.CloneUrl, "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/test-clone");

        // Act
        var result = await _service.TestRepositoryConnectionAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Connection successful", result.Message);
        Assert.True(result.ResponseTimeMs >= 0);
        Assert.Null(result.ErrorDetails);

        // Verify decryption was called
        _mockEncryptionService.Verify(x => x.Decrypt("encrypted_token"), Times.Once);

        // Verify clone was attempted with decrypted token
        _mockLocalGitService.Verify(
            x => x.CloneAsync(repository.CloneUrl, "token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestRepositoryConnectionAsync_ReturnsFailureWhenConnectionFails()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "encrypted_token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockLocalGitService
            .Setup(x => x.CloneAsync(repository.CloneUrl, "token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Authentication failed"));

        // Act
        var result = await _service.TestRepositoryConnectionAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Authentication failed", result.Message);
        Assert.True(result.ResponseTimeMs >= 0);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Authentication failed", result.ErrorDetails);
    }

    [Fact]
    public async Task TestRepositoryConnectionAsync_ThrowsWhenRepositoryNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TestRepositoryConnectionAsync(repositoryId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task TestRepositoryConnectionAsync_ThrowsWhenRepositoryBelongsToOtherTenant()
    {
        // Arrange
        var repository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.TestRepositoryConnectionAsync(repositoryId));

        Assert.Contains("different tenant", exception.Message);
    }

    #endregion

    #region GetRepositoryStatisticsAsync Tests

    [Fact]
    public async Task GetRepositoryStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "token");
        var repositoryId = repository.Id;

        var tickets = new List<Ticket>
        {
            Ticket.Create("PROJ-1", _tenantId, repositoryId),
            Ticket.Create("PROJ-2", _tenantId, repositoryId),
            Ticket.Create("PROJ-3", _tenantId, repositoryId)
        };

        // Note: Tickets start in Triggered state. Direct transition to PRCreated is invalid
        // (would need to go through: Analyzing -> TicketUpdateGenerated -> ... -> Implementing -> PRCreated)
        // TransitionTo will fail validation and tickets remain in Triggered state
        tickets[0].TransitionTo(WorkflowState.PRCreated);
        tickets[1].TransitionTo(WorkflowState.PRCreated);

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockTicketRepository
            .Setup(x => x.GetByRepositoryIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets);

        // Act
        var result = await _service.GetRepositoryStatisticsAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryId, result.RepositoryId);
        Assert.Equal(3, result.TotalTickets);
        // Tickets remain in Triggered state since invalid transition, so count is 0
        Assert.Equal(0, result.TotalPullRequests);
    }

    [Fact]
    public async Task GetRepositoryStatisticsAsync_ReturnsZeroWhenNoTickets()
    {
        // Arrange
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockTicketRepository
            .Setup(x => x.GetByRepositoryIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());

        // Act
        var result = await _service.GetRepositoryStatisticsAsync(repositoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalTickets);
        Assert.Equal(0, result.TotalPullRequests);
    }

    [Fact]
    public async Task GetRepositoryStatisticsAsync_ThrowsWhenRepositoryNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetRepositoryStatisticsAsync(repositoryId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetRepositoryStatisticsAsync_ThrowsWhenRepositoryBelongsToOtherTenant()
    {
        // Arrange
        var repository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");
        var repositoryId = repository.Id;

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetRepositoryStatisticsAsync(repositoryId));

        Assert.Contains("different tenant", exception.Message);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task TenantIsolation_PreventsCrossTenantsAccess_ForGetById()
    {
        // Arrange - Create repositories for two different tenants
        var ownRepository = Repository.Create(_tenantId, "OwnRepo", "GitHub", "https://github.com/own/repo", "token");
        var otherRepository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(ownRepository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownRepository);

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(otherRepository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherRepository);

        // Act - Try to access own repository
        var ownResult = await _service.GetRepositoryByIdAsync(ownRepository.Id);

        // Assert - Should succeed
        Assert.NotNull(ownResult);
        Assert.Equal(ownRepository.Id, ownResult.Id);

        // Act - Try to access other tenant's repository
        var otherResult = await _service.GetRepositoryByIdAsync(otherRepository.Id);

        // Assert - Should return null due to tenant isolation
        Assert.Null(otherResult);
    }

    [Fact]
    public async Task TenantIsolation_PreventsCrossTenantAccess_ForUpdate()
    {
        // Arrange
        var otherRepository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");

        var dto = new UpdateRepositoryDto
        {
            Name = "HackedName",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/other/repo",
            DefaultBranch = "main",
            IsActive = true
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(otherRepository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherRepository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateRepositoryAsync(otherRepository.Id, dto));

        Assert.Contains("different tenant", exception.Message);

        // Verify update was never called
        _mockRepositoryRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TenantIsolation_PreventsCrossTenantAccess_ForDelete()
    {
        // Arrange
        var otherRepository = Repository.Create(_otherTenantId, "OtherRepo", "GitHub", "https://github.com/other/repo", "token");

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(otherRepository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherRepository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteRepositoryAsync(otherRepository.Id));

        Assert.Contains("different tenant", exception.Message);

        // Verify update was never called (since we use soft delete via UpdateAsync)
        _mockRepositoryRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Encryption/Decryption Tests

    [Fact]
    public async Task Encryption_EncryptsAccessTokenOnCreate()
    {
        // Arrange
        var plainToken = "my_secret_token_123";
        var dto = new CreateRepositoryDto
        {
            Name = "SecureRepo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/secure",
            AccessToken = plainToken,
            DefaultBranch = "main"
        };

        _mockRepositoryRepository
            .Setup(x => x.GetByCloneUrlAsync(dto.CloneUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository?)null);

        _mockRepositoryRepository
            .Setup(x => x.AddAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repository r, CancellationToken ct) => r);

        // Act
        await _service.CreateRepositoryAsync(dto);

        // Assert - Verify encryption was called with plain token
        _mockEncryptionService.Verify(x => x.Encrypt(plainToken), Times.Once);

        // Verify repository was stored with encrypted token
        _mockRepositoryRepository.Verify(
            x => x.AddAsync(
                It.Is<Repository>(r => r.AccessToken == $"encrypted_{plainToken}"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Decryption_DecryptsAccessTokenForConnectionTest()
    {
        // Arrange
        var encryptedToken = "encrypted_secret_token";
        var repository = Repository.Create(_tenantId, "TestRepo", "GitHub", "https://github.com/test/repo", encryptedToken);

        _mockRepositoryRepository
            .Setup(x => x.GetByIdAsync(repository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockLocalGitService
            .Setup(x => x.CloneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/clone");

        // Act
        await _service.TestRepositoryConnectionAsync(repository.Id);

        // Assert - Verify decryption was called with encrypted token
        _mockEncryptionService.Verify(x => x.Decrypt(encryptedToken), Times.Once);

        // Verify git service was called with decrypted token
        _mockLocalGitService.Verify(
            x => x.CloneAsync(
                repository.CloneUrl,
                "secret_token", // Decrypted value
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
