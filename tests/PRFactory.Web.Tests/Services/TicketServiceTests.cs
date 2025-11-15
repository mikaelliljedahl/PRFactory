using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.Results;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Web.Tests.Services;

/// <summary>
/// Unit tests for TicketService - Phase 2 methods (GetDiffContentAsync, CreatePullRequestAsync)
/// </summary>
public class TicketServiceTests
{
    private readonly Mock<ILogger<TicketService>> _loggerMock;
    private readonly Mock<ITicketApplicationService> _ticketApplicationServiceMock;
    private readonly Mock<ITicketUpdateService> _ticketUpdateServiceMock;
    private readonly Mock<IQuestionApplicationService> _questionApplicationServiceMock;
    private readonly Mock<IWorkflowEventApplicationService> _workflowEventApplicationServiceMock;
    private readonly Mock<IPlanService> _planServiceMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly Mock<IPlanReviewService> _planReviewServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _loggerMock = new Mock<ILogger<TicketService>>();
        _ticketApplicationServiceMock = new Mock<ITicketApplicationService>();
        _ticketUpdateServiceMock = new Mock<ITicketUpdateService>();
        _questionApplicationServiceMock = new Mock<IQuestionApplicationService>();
        _workflowEventApplicationServiceMock = new Mock<IWorkflowEventApplicationService>();
        _planServiceMock = new Mock<IPlanService>();
        _tenantContextMock = new Mock<ITenantContext>();
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _planReviewServiceMock = new Mock<IPlanReviewService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _mapperMock = new Mock<IMapper>();

        _service = new TicketService(
            _loggerMock.Object,
            _ticketApplicationServiceMock.Object,
            _ticketUpdateServiceMock.Object,
            _questionApplicationServiceMock.Object,
            _workflowEventApplicationServiceMock.Object,
            _planServiceMock.Object,
            _tenantContextMock.Object,
            _ticketRepositoryMock.Object,
            _planReviewServiceMock.Object,
            _currentUserServiceMock.Object,
            _mapperMock.Object
        );
    }

    #region GetDiffContentAsync Tests

    [Fact]
    public async Task GetDiffContentAsync_ReturnsNull_WhenApplicationServiceReturnsNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        _ticketApplicationServiceMock.Setup(x => x.GetDiffContentAsync(ticketId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetDiffContentAsync(ticketId);

        // Assert
        Assert.Null(result);
        _ticketApplicationServiceMock.Verify(x => x.GetDiffContentAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task GetDiffContentAsync_ReturnsDto_WhenDiffExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var diffContent = "diff --git a/file1.txt b/file1.txt\nindex 1234567..89abcdef 100644\n--- a/file1.txt\n+++ b/file1.txt\n@@ -1,1 +1,1 @@\n-old content\n+new content\ndiff --git a/file2.txt b/file2.txt\nindex 1234567..89abcdef 100644\n--- a/file2.txt\n+++ b/file2.txt\n@@ -1,1 +1,1 @@\n-old content\n+new content";
        _ticketApplicationServiceMock.Setup(x => x.GetDiffContentAsync(ticketId))
            .ReturnsAsync(diffContent);

        // Act
        var result = await _service.GetDiffContentAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal(diffContent, result.DiffContent);
        Assert.Equal(diffContent.Length, result.SizeBytes);
        Assert.True(result.Available);
        _ticketApplicationServiceMock.Verify(x => x.GetDiffContentAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task GetDiffContentAsync_ParsesFileCount()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        // Create a diff with 3 files
        var diffContent = @"diff --git a/file1.txt b/file1.txt
index 1234567..89abcdef 100644
--- a/file1.txt
+++ b/file1.txt
@@ -1,1 +1,1 @@
-old content
+new content
diff --git a/file2.txt b/file2.txt
index 1234567..89abcdef 100644
--- a/file2.txt
+++ b/file2.txt
@@ -1,1 +1,1 @@
-old content
+new content
diff --git a/file3.txt b/file3.txt
index 1234567..89abcdef 100644
--- a/file3.txt
+++ b/file3.txt
@@ -1,1 +1,1 @@
-old content
+new content";

        _ticketApplicationServiceMock.Setup(x => x.GetDiffContentAsync(ticketId))
            .ReturnsAsync(diffContent);

        // Act
        var result = await _service.GetDiffContentAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FilesChanged);
    }

    #endregion

    #region CreatePullRequestAsync Tests

    [Fact]
    public async Task CreatePullRequestAsync_MapsSuccessResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var successResult = PullRequestCreationResult.Successful(
            "https://github.com/test/repo/pull/123",
            123);

        _ticketApplicationServiceMock.Setup(x => x.CreatePullRequestAsync(ticketId, "testUser"))
            .ReturnsAsync(successResult);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId, "testUser");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("https://github.com/test/repo/pull/123", result.PullRequestUrl);
        Assert.Equal(123, result.PullRequestNumber);
        Assert.Null(result.ErrorMessage);
        _ticketApplicationServiceMock.Verify(x => x.CreatePullRequestAsync(ticketId, "testUser"), Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_MapsFailureResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var failureResult = PullRequestCreationResult.Failed("Ticket not in correct state");

        _ticketApplicationServiceMock.Setup(x => x.CreatePullRequestAsync(ticketId, null))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _service.CreatePullRequestAsync(ticketId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.PullRequestUrl);
        Assert.Equal(0, result.PullRequestNumber);
        Assert.Equal("Ticket not in correct state", result.ErrorMessage);
        _ticketApplicationServiceMock.Verify(x => x.CreatePullRequestAsync(ticketId, null), Times.Once);
    }

    #endregion
}
