using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Components.Settings;

public partial class UserRoleEditor
{
    [Parameter, EditorRequired]
    public UserRole SelectedRole { get; set; }

    [Parameter]
    public EventCallback<UserRole> OnRoleChanged { get; set; }

    [Parameter]
    public bool IsActive { get; set; }

    [Parameter]
    public EventCallback<bool> OnActiveChanged { get; set; }

    [Parameter, EditorRequired]
    public UserManagementDto CurrentUser { get; set; } = null!;

    [Inject]
    private IUserManagementService UserManagementService { get; set; } = null!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = null!;

    [Inject]
    private ILogger<UserRoleEditor> Logger { get; set; } = null!;

    private bool showOwnerWarning = false;
    private int ownerCount = 0;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await CheckOwnerCountAsync();
    }

    private async Task CheckOwnerCountAsync()
    {
        try
        {
            var users = await UserManagementService.GetUsersForTenantAsync();
            ownerCount = users.Count(u => u.Role == UserRole.Owner && u.IsActive);

            showOwnerWarning = CurrentUser.Role == UserRole.Owner &&
                              SelectedRole != UserRole.Owner &&
                              ownerCount <= 1;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to check owner count for warning");
        }
    }

    private async Task HandleRoleChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<UserRole>(e.Value?.ToString(), out var newRole))
        {
            SelectedRole = newRole;
            await OnRoleChanged.InvokeAsync(newRole);
            await CheckOwnerCountAsync();
        }
    }

    private async Task HandleActiveChanged(ChangeEventArgs e)
    {
        if (bool.TryParse(e.Value?.ToString(), out var newActive))
        {
            IsActive = newActive;
            await OnActiveChanged.InvokeAsync(newActive);
        }
    }

    private string GetRoleDescription(UserRole role) => role switch
    {
        UserRole.Owner => "Full access to all settings and users",
        UserRole.Admin => "Can manage repositories, LLM providers, and tickets",
        UserRole.Member => "Can create and manage tickets",
        UserRole.Viewer => "Read-only access to tickets",
        _ => ""
    };
}
