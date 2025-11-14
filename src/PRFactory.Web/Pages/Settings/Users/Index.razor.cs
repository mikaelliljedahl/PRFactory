using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Dialogs;
using Radzen;

namespace PRFactory.Web.Pages.Settings.Users;

public partial class Index
{
    [Inject]
    private IUserManagementService UserManagementService { get; set; } = null!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = null!;

    private List<UserManagementDto> users = new();
    private List<UserManagementDto> filteredUsers = new();
    private bool isLoading = true;
    private string searchTerm = string.Empty;
    private UserRole? selectedRole = null;
    private bool showActiveOnly = true;
    private bool canEditUsers;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            isLoading = true;
            users = await UserManagementService.GetUsersForTenantAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load users");
            ToastService.ShowError($"Failed to load users: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        try
        {
            var currentUser = await CurrentUserService.GetCurrentUserAsync();
            canEditUsers = currentUser?.Role == UserRole.Owner;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to check user permissions");
            canEditUsers = false;
        }
    }

    private void ApplyFilters()
    {
        filteredUsers = users
            .Where(u => string.IsNullOrEmpty(searchTerm) ||
                       u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(u => selectedRole == null || u.Role == selectedRole)
            .Where(u => !showActiveOnly || u.IsActive)
            .OrderByDescending(u => u.Role == UserRole.Owner)
            .ThenByDescending(u => u.Role == UserRole.Admin)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void OnRoleFilterChanged(UserRole? role)
    {
        selectedRole = role;
        ApplyFilters();
    }

    private void OnShowActiveOnlyChanged(bool value)
    {
        showActiveOnly = value;
        ApplyFilters();
    }

    private void NavigateToEdit(Guid id)
    {
        if (canEditUsers)
        {
            Navigation.NavigateTo($"/settings/users/edit/{id}");
        }
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/settings/users/{id}");
    }

    private async Task HandleActivateAsync(Guid id)
    {
        if (!canEditUsers)
        {
            ToastService.ShowError("You don't have permission to activate users");
            return;
        }

        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return;
        }

        var confirmed = await ConfirmDialogHelper.ShowActivateAsync(DialogService, user.DisplayName);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await UserManagementService.ActivateUserAsync(id);
            ToastService.ShowSuccess($"User '{user.DisplayName}' activated successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to activate user {UserId}", id);
            ToastService.ShowError($"Failed to activate user: {ex.Message}");
        }
    }

    private async Task HandleDeactivateAsync(Guid id)
    {
        if (!canEditUsers)
        {
            ToastService.ShowError("You don't have permission to deactivate users");
            return;
        }

        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return;
        }

        var confirmed = await ConfirmDialogHelper.ShowDeactivateAsync(DialogService, user.DisplayName);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await UserManagementService.DeactivateUserAsync(id);
            ToastService.ShowSuccess($"User '{user.DisplayName}' deactivated successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deactivate user {UserId}", id);
            ToastService.ShowError($"Failed to deactivate user: {ex.Message}");
        }
    }
}
