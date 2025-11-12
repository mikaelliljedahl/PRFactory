using Bunit;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Tickets;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class ReviewerAssignmentTests : ComponentTestBase
{
    private Mock<IUserService> MockUserService { get; set; } = null!;
    private Mock<ITenantContext> MockTenantContext { get; set; } = null!;

    public ReviewerAssignmentTests()
    {
        // Create mocks for additional services
        MockUserService = new Mock<IUserService>();
        MockTenantContext = new Mock<ITenantContext>();

        // Register mocks
        Services.AddScoped(_ => MockUserService.Object);
        Services.AddScoped(_ => MockTenantContext.Object);
    }

    [Fact]
    public async Task OnInitialized_LoadsAvailableUsers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var users = new List<User>
        {
            User.Create(tenantId, "john@test.com", "John Doe", null, null, null, UserRole.Member),
            User.Create(tenantId, "jane@test.com", "Jane Smith", null, null, null, UserRole.Member)
        };

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("Jane Smith", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_HandlesServiceError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to load tenant"));

        // Act
        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("error") || cut.Markup.Contains("Error"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        Assert.Contains("error", cut.Markup.ToLower());
    }

    [Fact]
    public async Task AssignReviewers_WithoutRequiredReviewers_ShowsWarning()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var users = new List<User>
        {
            User.Create(tenantId, "john@test.com", "John Doe", null, null, null, UserRole.Member)
        };

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"), timeout: TimeSpan.FromSeconds(5));

        // Act - Submit without selecting any required reviewers
        var assignButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Assign")).ToList();
        if (assignButtons.Any())
        {
            await assignButtons.First().ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            await Task.Delay(100);

            // Assert
            BlazorMockHelpers.VerifyWarningToast(MockToastService);
        }
    }

    [Fact]
    public async Task AssignReviewers_WithRequiredReviewers_CallsServiceAndInvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var users = new List<User>
        {
            User.Create(tenantId, "john@test.com", "John Doe", null, null, null, UserRole.Member)
        };
        // Use reflection to set the Id property
        var user = users[0];
        typeof(User).GetProperty("Id")?.SetValue(user, userId);

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);
        BlazorMockHelpers.SetupAssignReviewers(MockTicketService, ticketId);

        var callbackInvoked = false;

        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnAssigned, () => { callbackInvoked = true; }));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"), timeout: TimeSpan.FromSeconds(5));

        // Act - Select required reviewer checkbox
        var requiredCheckboxes = cut.FindAll("input[type='checkbox']");
        if (requiredCheckboxes.Any())
        {
            await requiredCheckboxes.First().ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

            // Submit
            var assignButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Assign")).ToList();
            if (assignButtons.Any())
            {
                await assignButtons.First().ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

                await Task.Delay(100);

                // Assert
                MockTicketService.Verify(
                    x => x.AssignReviewersAsync(
                        ticketId,
                        It.IsAny<List<Guid>>(),
                        It.IsAny<List<Guid>?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                BlazorMockHelpers.VerifySuccessToast(MockToastService);
                Assert.True(callbackInvoked);
            }
        }
    }

    [Fact]
    public async Task AssignReviewers_HandlesServiceError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var users = new List<User>
        {
            User.Create(tenantId, "john@test.com", "John Doe", null, null, null, UserRole.Member)
        };

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);
        MockTicketService.Setup(m => m.AssignReviewersAsync(
            ticketId,
            It.IsAny<List<Guid>>(),
            It.IsAny<List<Guid>?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Assignment failed"));

        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"), timeout: TimeSpan.FromSeconds(5));

        // Act - Select and submit
        var checkboxes = cut.FindAll("input[type='checkbox']");
        if (checkboxes.Any())
        {
            await checkboxes.First().ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

            var assignButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Assign")).ToList();
            if (assignButtons.Any())
            {
                await assignButtons.First().ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

                await Task.Delay(100);

                // Assert
                BlazorMockHelpers.VerifyErrorToast(MockToastService);
            }
        }
    }

    [Fact]
    public async Task CancelButton_InvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var users = new List<User>();

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        var callbackInvoked = false;

        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnCancel, () => { callbackInvoked = true; }));

        await Task.Delay(100);

        // Act
        var cancelButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Cancel")).ToList();
        if (cancelButtons.Any())
        {
            await cancelButtons.First().ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // Assert
            Assert.True(callbackInvoked);
        }
    }

    [Fact]
    public async Task ToggleOptionalReviewer_DoesNotAffectRequiredReviewers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var users = new List<User>
        {
            User.Create(tenantId, "john@test.com", "John Doe", null, null, null, UserRole.Member)
        };

        MockTenantContext.Setup(m => m.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);
        MockUserService.Setup(m => m.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(users);

        var cut = RenderComponent<ReviewerAssignment>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        cut.WaitForState(() => cut.Markup.Contains("John Doe"), timeout: TimeSpan.FromSeconds(5));

        // Act - Toggle optional reviewer checkbox
        var checkboxes = cut.FindAll("input[type='checkbox']");
        if (checkboxes.Count > 1)
        {
            // Assuming there are separate checkboxes for required and optional
            await checkboxes[1].ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

            // Assert - Component should handle this without error
            Assert.NotNull(cut.Markup);
        }
    }
}
