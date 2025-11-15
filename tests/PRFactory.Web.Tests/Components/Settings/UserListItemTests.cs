using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the UserListItem component.
/// Verifies user information display, role badges, avatars, and action buttons.
/// </summary>
public class UserListItemTests : TestContext
{
    private UserManagementDto CreateTestUser(
        string displayName = "John Doe",
        string email = "john@example.com",
        UserRole role = UserRole.Member,
        bool isActive = true,
        string? avatarUrl = null,
        string identityProvider = "AzureAD",
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
            AvatarUrl = avatarUrl,
            IdentityProvider = identityProvider,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = lastSeenAt
        };
    }

    [Fact]
    public void Render_DisplaysUserDisplayName()
    {
        // Arrange
        var user = CreateTestUser(displayName: "Alice Smith");

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var link = cut.Find("a");
        Assert.NotNull(link);
        Assert.Contains("Alice Smith", link.TextContent);
    }

    [Fact]
    public void Render_DisplaysUserEmail()
    {
        // Arrange
        var user = CreateTestUser(email: "alice@example.com");

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var code = cut.Find("code");
        Assert.NotNull(code);
        Assert.Contains("alice@example.com", code.TextContent);
    }

    [Fact]
    public void Render_DisplaysUserAvatar()
    {
        // Arrange
        var user = CreateTestUser(avatarUrl: "https://example.com/avatar.jpg");

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var img = cut.Find("img.rounded-circle");
        Assert.NotNull(img);
        Assert.True(img.HasAttribute("src"));
        Assert.Contains("https://example.com/avatar.jpg", img.GetAttribute("src"));
    }

    [Fact]
    public void Render_WithoutAvatar_DisplaysPlaceholder()
    {
        // Arrange
        var user = CreateTestUser(avatarUrl: null);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var placeholder = cut.Find(".avatar-placeholder");
        Assert.NotNull(placeholder);
        var icon = cut.Find("i.bi-person");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Render_WithOwnerRole_DisplaysDangerBadge()
    {
        // Arrange
        var user = CreateTestUser(role: UserRole.Owner);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.Find(".badge.bg-danger");
        Assert.NotNull(badge);
        Assert.Contains("Owner", badge.TextContent);
    }

    [Fact]
    public void Render_WithAdminRole_DisplaysPrimaryBadge()
    {
        // Arrange
        var user = CreateTestUser(role: UserRole.Admin);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.NotNull(badge);
        Assert.Contains("Admin", badge.TextContent);
    }

    [Fact]
    public void Render_WithMemberRole_DisplaysSuccessBadge()
    {
        // Arrange
        var user = CreateTestUser(role: UserRole.Member);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.Find(".badge.bg-success");
        Assert.NotNull(badge);
        Assert.Contains("Member", badge.TextContent);
    }

    [Fact]
    public void Render_WithViewerRole_DisplaysSecondaryBadge()
    {
        // Arrange
        var user = CreateTestUser(role: UserRole.Viewer);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        Assert.NotNull(badge);
        Assert.Contains("Viewer", badge.TextContent);
    }

    [Fact]
    public void Render_DisplaysIdentityProvider()
    {
        // Arrange
        var user = CreateTestUser(identityProvider: "GoogleWorkspace");

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("GoogleWorkspace", markup);
    }

    [Fact]
    public void Render_WithActiveUser_DisplaysActiveBadge()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.FindAll(".badge").First(b => b.TextContent.Contains("Active") || b.TextContent.Contains("Inactive"));
        Assert.Contains("Active", badge.TextContent);
        Assert.Contains("bg-success", badge.GetAttribute("class"));
    }

    [Fact]
    public void Render_WithInactiveUser_DisplaysInactiveBadge()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var badge = cut.FindAll(".badge").First(b => b.TextContent.Contains("Inactive"));
        Assert.Contains("Inactive", badge.TextContent);
        Assert.Contains("bg-secondary", badge.GetAttribute("class"));
    }

    [Fact]
    public void Render_WithLastSeenAt_DisplaysRelativeTime()
    {
        // Arrange
        var lastSeen = DateTime.UtcNow.AddHours(-2);
        var user = CreateTestUser(lastSeenAt: lastSeen);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("Never", markup);
    }

    [Fact]
    public void Render_WithoutLastSeenAt_DisplaysNever()
    {
        // Arrange
        var user = CreateTestUser(lastSeenAt: null);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var never = cut.Find(".text-muted");
        Assert.NotNull(never);
        Assert.Contains("Never", never.TextContent);
    }

    [Fact]
    public void Render_DisplaysViewDetailsButton()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var eyeButton = cut.FindAll("button").First(b => b.FindAll("i").Any(i => i.HasAttribute("class") && i.GetAttribute("class").Contains("bi-eye")));
        Assert.NotNull(eyeButton);
    }

    [Fact]
    public void Render_WithCanEdit_DisplaysEditButton()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        var pencilButton = cut.FindAll("button").FirstOrDefault(b => b.FindAll("i").Any(i => i.HasAttribute("class") && i.GetAttribute("class").Contains("bi-pencil")));
        Assert.NotNull(pencilButton);
    }

    [Fact]
    public void Render_WithoutCanEdit_HidesEditButton()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, false));

        // Assert
        var pencilButton = cut.FindAll("button").FirstOrDefault(b => b.FindAll("i").Any(i => i.HasAttribute("class") && i.GetAttribute("class").Contains("bi-pencil")));
        Assert.Null(pencilButton);
    }

    [Fact]
    public void Render_WithActiveUserAndCanEdit_DisplaysDeactivateButton()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        var deactivateButton = cut.FindAll("button").FirstOrDefault(b => b.FindAll("i").Any(i => i.HasAttribute("class") && i.GetAttribute("class").Contains("bi-x-circle")));
        Assert.NotNull(deactivateButton);
    }

    [Fact]
    public void Render_WithInactiveUserAndCanEdit_DisplaysActivateButton()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true));

        // Assert
        var activateButton = cut.FindAll("button").FirstOrDefault(b => b.FindAll("i").Any(i => i.HasAttribute("class") && i.GetAttribute("class").Contains("bi-check-circle")));
        Assert.NotNull(activateButton);
    }

    [Fact]
    public void Click_ViewDetailsButton_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser();
        var viewDetailsCallbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create(this, () =>
            {
                viewDetailsCallbackInvoked = true;
            })));

        // Act
        var viewDetailsLink = cut.Find("a");
        viewDetailsLink.Click();

        // Assert
        Assert.True(viewDetailsCallbackInvoked);
    }

    [Fact]
    public void Click_EditButton_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser();
        var editCallbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnEdit, EventCallback.Factory.Create(this, () =>
            {
                editCallbackInvoked = true;
            })));

        // Act
        var editButton = cut.FindAll("button").First(b => b.FindAll("i").Any(i => i.GetAttribute("class").Contains("bi-pencil")));
        editButton.Click();

        // Assert
        Assert.True(editCallbackInvoked);
    }

    [Fact]
    public void Click_DeactivateButton_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser(isActive: true);
        var deactivateCallbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnDeactivate, EventCallback.Factory.Create(this, () =>
            {
                deactivateCallbackInvoked = true;
            })));

        // Act
        var deactivateButton = cut.FindAll("button").First(b => b.FindAll("i").Any(i => i.GetAttribute("class").Contains("bi-x-circle")));
        deactivateButton.Click();

        // Assert
        Assert.True(deactivateCallbackInvoked);
    }

    [Fact]
    public void Click_ActivateButton_InvokesCallback()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var activateCallbackInvoked = false;

        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnActivate, EventCallback.Factory.Create(this, () =>
            {
                activateCallbackInvoked = true;
            })));

        // Act
        var activateButton = cut.FindAll("button").First(b => b.FindAll("i").Any(i => i.GetAttribute("class").Contains("bi-check-circle")));
        activateButton.Click();

        // Assert
        Assert.True(activateCallbackInvoked);
    }

    [Fact]
    public void Render_DisplaysTableRow()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var row = cut.Find("tr");
        Assert.NotNull(row);

        var cells = cut.FindAll("td");
        Assert.NotEmpty(cells);
    }

    [Fact]
    public void Render_DisplaysUserNameWithIcon()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var cut = RenderComponent<UserListItem>(parameters => parameters
            .Add(p => p.User, user));

        // Assert
        var flexDiv = cut.Find(".d-flex.align-items-center");
        Assert.NotNull(flexDiv);
        Assert.Contains(user.DisplayName, flexDiv.TextContent);
    }

    [Fact]
    public void Render_WithDifferentUsers_DisplaysCorrectInfo()
    {
        // Arrange
        var user1 = CreateTestUser(displayName: "User One", email: "user1@example.com", role: UserRole.Admin);
        var user2 = CreateTestUser(displayName: "User Two", email: "user2@example.com", role: UserRole.Viewer);

        // Act
        var cut1 = RenderComponent<UserListItem>(parameters => parameters.Add(p => p.User, user1));
        var cut2 = RenderComponent<UserListItem>(parameters => parameters.Add(p => p.User, user2));

        // Assert
        Assert.Contains("User One", cut1.Markup);
        Assert.Contains("user1@example.com", cut1.Markup);

        Assert.Contains("User Two", cut2.Markup);
        Assert.Contains("user2@example.com", cut2.Markup);
    }
}
