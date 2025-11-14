using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for UserManagementService covering permissions, tenant isolation, and statistics
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<ICurrentUserService> _currentUserService;
    private readonly Mock<IPlanReviewRepository> _planReviewRepository;
    private readonly Mock<IReviewCommentRepository> _reviewCommentRepository;
    private readonly Mock<ILogger<UserManagementService>> _logger;
    private readonly UserManagementService _service;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UserManagementServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _currentUserService = new Mock<ICurrentUserService>();
        _planReviewRepository = new Mock<IPlanReviewRepository>();
        _reviewCommentRepository = new Mock<IReviewCommentRepository>();
        _logger = new Mock<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _userRepository.Object,
            _currentUserService.Object,
            _planReviewRepository.Object,
            _reviewCommentRepository.Object,
            _logger.Object);

        // Default setup for current user (Owner role)
        SetupCurrentUser(UserRole.Owner);
    }

    #region Helper Methods

    private void SetupCurrentUser(UserRole role)
    {
        var currentUser = CreateUser(_currentUserId, _tenantId, "current@example.com", "Current User", role, true);
        _currentUserService.Setup(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _currentUserService.Setup(s => s.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_currentUserId);
        _currentUserService.Setup(s => s.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);
    }

    private static User CreateUser(Guid id, Guid tenantId, string email, string displayName, UserRole role, bool isActive)
    {
        var user = User.Create(tenantId, email, displayName, null, $"ext-{id}", "TestProvider", role);

        // Use reflection to set the ID and IsActive since they're private setters
        typeof(User).GetProperty("Id")!.SetValue(user, id);
        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }

    #endregion

    #region GetUsersForTenantAsync Tests

    [Fact]
    public async Task GetUsersForTenantAsync_ReturnsAllUsersForCurrentTenant()
    {
        // Arrange
        var user1 = CreateUser(Guid.NewGuid(), _tenantId, "user1@example.com", "User One", UserRole.Owner, true);
        var user2 = CreateUser(Guid.NewGuid(), _tenantId, "user2@example.com", "User Two", UserRole.Admin, true);
        var user3 = CreateUser(Guid.NewGuid(), _tenantId, "user3@example.com", "User Three", UserRole.Member, false);

        _userRepository.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([user1, user2, user3]);

        // Act
        var result = await _service.GetUsersForTenantAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, u => u.Email == "user1@example.com" && u.Role == UserRole.Owner);
        Assert.Contains(result, u => u.Email == "user2@example.com" && u.Role == UserRole.Admin);
        Assert.Contains(result, u => u.Email == "user3@example.com" && u.Role == UserRole.Member && !u.IsActive);
    }

    [Fact]
    public async Task GetUsersForTenantAsync_WhenNoCurrentTenant_ThrowsException()
    {
        // Arrange
        _currentUserService.Setup(s => s.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetUsersForTenantAsync());
        Assert.Equal("Current tenant ID is not available", exception.Message);
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal(UserRole.Member, result.Role);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserBelongsToDifferentTenant_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _otherTenantId, "user@other.com", "Other User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert (tenant isolation - user from other tenant not returned)
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateUserRoleAsync Tests

    [Fact]
    public async Task UpdateUserRoleAsync_OwnerCanChangeRole_UpdatesSuccessfully()
    {
        // Arrange
        SetupCurrentUser(UserRole.Owner);
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.Admin);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_AdminCanChangeRole_UpdatesSuccessfully()
    {
        // Arrange
        SetupCurrentUser(UserRole.Admin);
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.Viewer);

        // Assert
        Assert.Equal(UserRole.Viewer, user.Role);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_MemberCannotChangeRole_ThrowsException()
    {
        // Arrange
        SetupCurrentUser(UserRole.Member);
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.UpdateUserRoleAsync(userId, UserRole.Admin));
        Assert.Equal("Only Owners and Admins can change user roles", exception.Message);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ViewerCannotChangeRole_ThrowsException()
    {
        // Arrange
        SetupCurrentUser(UserRole.Viewer);
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.UpdateUserRoleAsync(userId, UserRole.Admin));
        Assert.Equal("Only Owners and Admins can change user roles", exception.Message);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_RemovingLastOwner_ThrowsException()
    {
        // Arrange
        SetupCurrentUser(UserRole.Owner);
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "owner@example.com", "Only Owner", UserRole.Owner, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Only one owner in the tenant
        _userRepository.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.UpdateUserRoleAsync(userId, UserRole.Admin));
        Assert.Equal("Cannot remove the last Owner from the tenant", exception.Message);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_RemovingOwnerWhenOthersExist_Succeeds()
    {
        // Arrange
        SetupCurrentUser(UserRole.Owner);
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "owner1@example.com", "Owner One", UserRole.Owner, true);
        var otherOwner = CreateUser(Guid.NewGuid(), _tenantId, "owner2@example.com", "Owner Two", UserRole.Owner, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Two owners in the tenant
        _userRepository.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([user, otherOwner]);

        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.Admin);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserFromDifferentTenant_ThrowsException()
    {
        // Arrange
        SetupCurrentUser(UserRole.Owner);
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _otherTenantId, "user@other.com", "Other User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.UpdateUserRoleAsync(userId, UserRole.Admin));
        Assert.Equal("Cannot modify users from other tenants", exception.Message);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        SetupCurrentUser(UserRole.Owner);
        var userId = Guid.NewGuid();

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.UpdateUserRoleAsync(userId, UserRole.Admin));
        Assert.Equal($"User with ID {userId} not found", exception.Message);
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_WhenUserExists_ActivatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, false);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ActivateUserAsync(userId);

        // Assert
        Assert.True(user.IsActive);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateUserAsync_UserFromDifferentTenant_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _otherTenantId, "user@other.com", "Other User", UserRole.Member, false);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.ActivateUserAsync(userId));
        Assert.Equal("Cannot modify users from other tenants", exception.Message);
    }

    [Fact]
    public async Task ActivateUserAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ActivateUserAsync(userId));
        Assert.Equal($"User with ID {userId} not found", exception.Message);
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WhenUserExists_DeactivatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeactivateUserAsync(userId);

        // Assert
        Assert.False(user.IsActive);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_UserFromDifferentTenant_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _otherTenantId, "user@other.com", "Other User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.DeactivateUserAsync(userId));
        Assert.Equal("Cannot modify users from other tenants", exception.Message);
    }

    [Fact]
    public async Task DeactivateUserAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DeactivateUserAsync(userId));
        Assert.Equal($"User with ID {userId} not found", exception.Message);
    }

    #endregion

    #region GetUserStatisticsAsync Tests

    [Fact]
    public async Task GetUserStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);
        user.UpdateLastSeen(); // Set LastSeenAt

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Mock plan reviews (3 pending reviews)
        var planReviews = new List<PlanReview>
        {
            CreateMockPlanReview(Guid.NewGuid(), userId),
            CreateMockPlanReview(Guid.NewGuid(), userId),
            CreateMockPlanReview(Guid.NewGuid(), userId)
        };
        _planReviewRepository.Setup(r => r.GetPendingByReviewerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planReviews);

        // Mock comments (5 comments)
        var comments = new List<ReviewComment>
        {
            CreateMockComment(Guid.NewGuid(), userId),
            CreateMockComment(Guid.NewGuid(), userId),
            CreateMockComment(Guid.NewGuid(), userId),
            CreateMockComment(Guid.NewGuid(), userId),
            CreateMockComment(Guid.NewGuid(), userId)
        };
        _reviewCommentRepository.Setup(r => r.GetByAuthorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        // Act
        var result = await _service.GetUserStatisticsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(3, result.TotalPlanReviews);
        Assert.Equal(5, result.TotalComments);
        Assert.NotNull(result.LastSeenAt);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithNoActivity_ReturnsZeroCounts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _tenantId, "user@example.com", "Test User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _planReviewRepository.Setup(r => r.GetPendingByReviewerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _reviewCommentRepository.Setup(r => r.GetByAuthorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetUserStatisticsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(0, result.TotalPlanReviews);
        Assert.Equal(0, result.TotalComments);
        Assert.Null(result.LastSeenAt);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserFromDifferentTenant_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, _otherTenantId, "user@other.com", "Other User", UserRole.Member, true);

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.GetUserStatisticsAsync(userId));
        Assert.Equal("Cannot access statistics for users from other tenants", exception.Message);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetUserStatisticsAsync(userId));
        Assert.Equal($"User with ID {userId} not found", exception.Message);
    }

    #endregion

    #region Mock Helper Methods for Navigation Properties

    private static PlanReview CreateMockPlanReview(Guid reviewId, Guid reviewerId)
    {
        // Create a mock PlanReview using reflection (since constructor is private)
        var review = (PlanReview)Activator.CreateInstance(typeof(PlanReview), true)!;
        typeof(PlanReview).GetProperty("Id")!.SetValue(review, reviewId);
        typeof(PlanReview).GetProperty("ReviewerId")!.SetValue(review, reviewerId);
        return review;
    }

    private static ReviewComment CreateMockComment(Guid commentId, Guid authorId)
    {
        // Create a mock ReviewComment using reflection (since constructor is private)
        var comment = (ReviewComment)Activator.CreateInstance(typeof(ReviewComment), true)!;
        typeof(ReviewComment).GetProperty("Id")!.SetValue(comment, commentId);
        typeof(ReviewComment).GetProperty("AuthorId")!.SetValue(comment, authorId);
        return comment;
    }

    #endregion
}
