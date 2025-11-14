using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.Users;

public partial class Edit
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IUserManagementService UserManagementService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<Edit> Logger { get; set; } = null!;

    private UserManagementDto? user;
    private UserRole selectedRole;
    private bool isActive;
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        try
        {
            isLoading = true;
            user = await UserManagementService.GetUserByIdAsync(Id);

            if (user == null)
            {
                ToastService.ShowError("User not found");
                Navigation.NavigateTo("/settings/users");
                return;
            }

            selectedRole = user.Role;
            isActive = user.IsActive;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load user {UserId}", Id);
            ToastService.ShowError($"Failed to load user: {ex.Message}");
            Navigation.NavigateTo("/settings/users");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleSaveAsync()
    {
        if (user == null)
        {
            return;
        }

        try
        {
            isSaving = true;

            if (selectedRole != user.Role)
            {
                await UserManagementService.UpdateUserRoleAsync(Id, selectedRole);
            }

            if (isActive != user.IsActive)
            {
                if (isActive)
                {
                    await UserManagementService.ActivateUserAsync(Id);
                }
                else
                {
                    await UserManagementService.DeactivateUserAsync(Id);
                }
            }

            ToastService.ShowSuccess("User updated successfully");
            Navigation.NavigateTo("/settings/users");
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Business rule violation when updating user {UserId}", Id);
            ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update user {UserId}", Id);
            ToastService.ShowError($"Failed to update user: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/users");
    }
}
