using Bunit;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Services;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for ReviewerAssignment component
/// </summary>
public class ReviewerAssignmentTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IToastService> _mockToastService;
    private readonly Mock<ILogger<ReviewerAssignment>> _mockLogger;

    public ReviewerAssignmentTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockUserService = new Mock<IUserService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockToastService = new Mock<IToastService>();
        _mockLogger = new Mock<ILogger<ReviewerAssignment>>();

        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockUserService.Object);
        Services.AddSingleton(_mockTenantContext.Object);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public void ReviewerAssignment_OnInitialized_LoadsAvailableUsers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        _mockUserService.Verify(
            x => x.GetByTenantIdAsync(tenantId),
            Times.Once);
    }

    [Fact]
    public void ReviewerAssignment_WhileLoading_ShowsLoadingState()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        var tcs = new TaskCompletionSource<List<User>>();
        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("Loading", cut.Markup);

        // Clean up
        tcs.SetResult(new List<User>());
    }

    [Fact]
    public void ReviewerAssignment_WithAvailableUsers_DisplaysUserList()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var users = new List<User>
        {
            new User(tenantId, "john@example.com", "John Doe"),
            new User(tenantId, "jane@example.com", "Jane Smith")
        };

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("John Doe"));
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("jane@example.com", cut.Markup);
    }

    [Fact]
    public void ReviewerAssignment_WithNoUsers_ShowsNoUsersMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("No users available"));
        Assert.Contains("No users available for assignment", cut.Markup);
    }

    [Fact]
    public void ReviewerAssignment_ShowsRequiredReviewersSection()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Required Reviewers"));
        Assert.Contains("Required Reviewers", cut.Markup);
        Assert.Contains("All required reviewers must approve the plan", cut.Markup);
    }

    [Fact]
    public void ReviewerAssignment_ShowsOptionalReviewersSection()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Optional Reviewers"));
        Assert.Contains("Optional Reviewers", cut.Markup);
    }

    [Fact]
    public void ReviewerAssignment_ShowsAssignReviewersButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Assign Reviewers"));
        Assert.Contains("Assign Reviewers", cut.Markup);
    }

    [Fact]
    public async Task ReviewerAssignment_AssignWithoutRequiredReviewers_ShowsWarning()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var users = new List<User>
        {
            new User(tenantId, "john@example.com", "John Doe")
        };

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("Assign Reviewers"));

        var assignButton = cut.Find("button:contains('Assign Reviewers')");
        await assignButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockToastService.Verify(
            x => x.ShowWarning("Please select at least one required reviewer"),
            Times.Once);
    }

    [Fact]
    public async Task ReviewerAssignment_AssignWithRequiredReviewers_CallsService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var users = new List<User>
        {
            new User(tenantId, "john@example.com", "John Doe")
        };

        // Store userId from the created user
        var userId = users[0].Id;

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        _mockTicketService
            .Setup(x => x.AssignReviewersAsync(ticketId, It.IsAny<List<Guid>>(), It.IsAny<List<Guid>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var callbackInvoked = false;

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnAssigned, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"));

        // Select the required reviewer checkbox
        var checkbox = cut.Find($"input#required-{userId}");
        await checkbox.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

        // Click assign button
        var assignButton = cut.Find("button:contains('Assign Reviewers')");
        await assignButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(
            x => x.AssignReviewersAsync(
                ticketId,
                It.Is<List<Guid>>(list => list.Contains(userId)),
                It.IsAny<List<Guid>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void ReviewerAssignment_LoadError_ShowsErrorMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ThrowsAsync(new Exception("Load error"));

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Error loading users"));
        Assert.Contains("Error loading users", cut.Markup);
    }

    [Fact]
    public void ReviewerAssignment_WithCancelCallback_ShowsCancelButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext
            .Setup(x => x.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        _mockUserService
            .Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(new List<User>());

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnCancel, () => Task.CompletedTask));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Cancel"));
        Assert.Contains("Cancel", cut.Markup);
    }
}
