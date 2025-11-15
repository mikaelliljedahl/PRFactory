using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the UserRoleEditor component.
/// Verifies role selection, active state toggling, owner count validation, and callbacks.
/// </summary>
public class UserRoleEditorTests : TestContext
{
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<UserRoleEditor>> _mockLogger;

    public UserRoleEditorTests()
    {
        _mockUserManagementService = new Mock<IUserManagementService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<UserRoleEditor>>();

        Services.AddSingleton(_mockUserManagementService.Object);
        Services.AddSingleton(_mockCurrentUserService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    private UserManagementDto CreateTestUser(
        string displayName = "John Doe",
        string email = "john@example.com",
        UserRole role = UserRole.Member,
        bool isActive = true)
    {
        return new UserManagementDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            Role = role,
            IsActive = isActive,
            AvatarUrl = null,
            IdentityProvider = "AzureAD",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    [Fact]
    public async Task OnInitializedAsync_LoadsUserCountForOwnerCheck()
    {
        // Arrange
        var currentUser = CreateTestUser(role: UserRole.Member);
        var users = new List<UserManagementDto>
        {
            CreateTestUser(role: UserRole.Owner, isActive: true),
            CreateTestUser(role: UserRole.Admin, isActive: true)
        };

        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        await Task.Delay(100);

        // Assert
        _mockUserManagementService.Verify(
            s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Render_DisplaysRoleLabel()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var label = cut.FindAll("label").First(l => l.TextContent.Contains("Role"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysRoleDropdown()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var select = cut.Find("select.form-select");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_DisplaysAllRoleOptions()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var select = cut.Find("select.form-select");
        var markup = select.OuterHtml;
        Assert.Contains("Owner", markup);
        Assert.Contains("Admin", markup);
        Assert.Contains("Member", markup);
        Assert.Contains("Viewer", markup);
    }

    [Fact]
    public void Render_DisplaysRoleDescription()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Owner)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Full access to all settings and users", markup);
    }

    [Fact]
    public void Render_WithOwnerRole_ShowsOwnerDescription()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Owner)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Full access", markup);
    }

    [Fact]
    public void Render_WithAdminRole_ShowsAdminDescription()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Admin)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("manage repositories", markup);
    }

    [Fact]
    public void Render_DisplaysActiveCheckbox()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.NotNull(checkbox);
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_DisplaysActiveLabel()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var label = cut.FindAll("label").First(l => l.TextContent.Contains("Active"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_WithoutOwnerWarning_DoesNotShowWarning()
    {
        // Arrange
        var currentUser = CreateTestUser(role: UserRole.Admin);
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Admin)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("last Owner", markup);
    }

    [Fact]
    public async Task OnRoleChanged_InvokesCallback()
    {
        // Arrange
        var currentUser = CreateTestUser(role: UserRole.Member);
        var users = new List<UserManagementDto>
        {
            CreateTestUser(role: UserRole.Owner, isActive: true)
        };
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(users);

        UserRole? selectedRole = null;
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser)
            .Add(p => p.OnRoleChanged, EventCallback.Factory.Create<UserRole>(this, role =>
            {
                selectedRole = role;
            })));

        await Task.Delay(100);

        // Act
        var select = cut.Find("select.form-select");
        var options = cut.FindAll("select.form-select option");
        var adminOption = options.First(o => o.GetAttribute("value") == "Admin");
        select.Change(adminOption.GetAttribute("value"));

        await Task.Delay(100);

        // Assert
        Assert.Equal(UserRole.Admin, selectedRole);
    }

    [Fact]
    public async Task OnActiveChanged_InvokesCallback()
    {
        // Arrange
        var currentUser = CreateTestUser();
        var users = new List<UserManagementDto> { currentUser };
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(users);

        bool? isActive = null;
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser)
            .Add(p => p.OnActiveChanged, EventCallback.Factory.Create<bool>(this, active =>
            {
                isActive = active;
            })));

        await Task.Delay(100);

        // Act
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Change(false);

        await Task.Delay(100);

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void Render_WithInactiveUser_UnchecksActiveCheckbox()
    {
        // Arrange
        var currentUser = CreateTestUser(isActive: false);
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, false)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.False(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_DisplaysHelpTextForActiveCheckbox()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var helpText = cut.Find(".form-text.text-muted");
        Assert.NotNull(helpText);
        Assert.Contains("Inactive users cannot access", helpText.TextContent);
    }

    [Fact]
    public void Render_DisplaysContextualHelp()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Owner:", markup);
        Assert.Contains("Admin:", markup);
    }

    [Fact]
    public async Task WhenMultipleOwners_DoesNotShowOwnerWarning()
    {
        // Arrange
        var currentUser = CreateTestUser(role: UserRole.Owner);
        var users = new List<UserManagementDto>
        {
            CreateTestUser(role: UserRole.Owner, isActive: true),
            CreateTestUser(role: UserRole.Owner, isActive: true),
            CreateTestUser(role: UserRole.Admin, isActive: true)
        };
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Owner)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        await Task.Delay(100);

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("last Owner", markup);
    }

    [Fact]
    public async Task WhenOnlyOneActiveOwner_IfChangingOwnerToOtherRole_ShowsWarning()
    {
        // Arrange
        var currentUser = CreateTestUser(role: UserRole.Owner);
        var users = new List<UserManagementDto>
        {
            CreateTestUser(role: UserRole.Owner, isActive: true),
            CreateTestUser(role: UserRole.Admin, isActive: true)
        };
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Owner)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        await Task.Delay(100);

        // Assert - warning should be present when trying to change the only owner
        var markup = cut.Markup;
        Assert.Contains("At least one Owner must exist", markup);
    }

    [Fact]
    public async Task WhenOwnerCountError_LogsWarning()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        await Task.Delay(100);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Render_DisplaysTwoColumns()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Member)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var cols = cut.FindAll(".col-md-6");
        Assert.Equal(2, cols.Count);
    }

    [Fact]
    public void Render_PreSelectsCurrentRole()
    {
        // Arrange
        var currentUser = CreateTestUser();
        _mockUserManagementService
            .Setup(s => s.GetUsersForTenantAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<UserManagementDto> { currentUser });

        // Act
        var cut = RenderComponent<UserRoleEditor>(parameters => parameters
            .Add(p => p.SelectedRole, UserRole.Admin)
            .Add(p => p.IsActive, true)
            .Add(p => p.CurrentUser, currentUser));

        // Assert
        var select = cut.Find("select.form-select");
        Assert.Equal("Admin", select.GetAttribute("value"));
    }
}
