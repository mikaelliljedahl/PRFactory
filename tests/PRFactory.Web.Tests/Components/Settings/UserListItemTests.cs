using Bunit;
using Microsoft.AspNetCore.Components;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class UserListItemTests : TestContext
{
    private UserManagementDto CreateTestUser(
        string displayName = "Test User",
        string email = "test@example.com",
        UserRole role = UserRole.Member,
        bool isActive = true,
        string identityProvider = "AzureAD",
        bool hasLastSeenAt = true,
        DateTime? lastSeenAt = null)
    {
        return new UserManagementDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            Role = role,
            IsActive = isActive,
            IdentityProvider = identityProvider,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = hasLastSeenAt ? (lastSeenAt ?? DateTime.UtcNow.AddHours(-2)) : null
        };
    }

    [Fact]
    public void Render_WithUser_DisplaysUserInformation()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains(user.DisplayName, cut.Markup);
        Assert.Contains(user.Email, cut.Markup);
        Assert.Contains(user.IdentityProvider, cut.Markup);
    }

    [Theory]
    [InlineData(UserRole.Owner, "bg-danger")]
    [InlineData(UserRole.Admin, "bg-primary")]
    [InlineData(UserRole.Member, "bg-success")]
    [InlineData(UserRole.Viewer, "bg-secondary")]
    public void Render_WithRole_ShowsCorrectBadgeColor(UserRole role, string expectedClass)
    {
        // Arrange
        var user = CreateTestUser(role: role);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithActiveUser_ShowsActiveStatus()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithInactiveUser_ShowsInactiveStatus()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("Inactive", cut.Markup);
        Assert.Contains("bg-secondary", cut.Markup);
    }

    [Fact]
    public void Render_WithLastSeenAt_DisplaysRelativeTime()
    {
        // Arrange
        var lastSeen = DateTime.UtcNow.AddHours(-2);
        var user = CreateTestUser(lastSeenAt: lastSeen);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert - RelativeTime component should be rendered
        Assert.DoesNotContain("Never", cut.Markup);
    }

    [Fact]
    public void Render_WithoutLastSeenAt_DisplaysNever()
    {
        // Arrange
        var user = CreateTestUser(hasLastSeenAt: false);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("Never", cut.Markup);
    }

    [Fact]
    public void Render_WithAvatarUrl_DisplaysAvatar()
    {
        // Arrange
        var user = CreateTestUser();
        user.AvatarUrl = "https://example.com/avatar.png";

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains(user.AvatarUrl, cut.Markup);
    }

    [Fact]
    public void Render_WithoutAvatarUrl_DisplaysPlaceholder()
    {
        // Arrange
        var user = CreateTestUser();
        user.AvatarUrl = null;

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("avatar-placeholder", cut.Markup);
        Assert.Contains("bi-person", cut.Markup);
    }

    [Fact]
    public void Render_WithCanEditTrue_ShowsEditButtons()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count > 1); // View + Edit + Activate/Deactivate
    }

    [Fact]
    public void Render_WithCanEditFalse_ShowsOnlyViewButton()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Single(buttons); // Only View button
    }

    [Fact]
    public void Render_WithActiveUserAndCanEdit_ShowsDeactivateButton()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("bi-x-circle", cut.Markup);
        Assert.Contains("Deactivate", cut.Markup);
    }

    [Fact]
    public void Render_WithInactiveUserAndCanEdit_ShowsActivateButton()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
        Assert.Contains("Activate", cut.Markup);
    }

    [Fact]
    public void ViewDetailsButton_WhenClicked_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser();
        var callbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var viewButton = cut.Find("button[title='View Details']");
        viewButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void EditButton_WhenClicked_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser();
        var callbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnEdit, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var editButton = cut.Find("button[title='Edit']");
        editButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void DeactivateButton_WhenClicked_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);
        var callbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnDeactivate, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var deactivateButton = cut.Find("button[title='Deactivate']");
        deactivateButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void ActivateButton_WhenClicked_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var callbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnActivate, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var activateButton = cut.Find("button[title='Activate']");
        activateButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }
}
