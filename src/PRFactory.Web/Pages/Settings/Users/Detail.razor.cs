using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.Users;

public partial class Detail
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IUserManagementService UserManagementService { get; set; } = null!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<Detail> Logger { get; set; } = null!;

    private UserManagementDto? user;
    private UserStatisticsDto? statistics;
    private bool isLoading = true;
    private bool canEdit;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();
        await CheckPermissionsAsync();
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

            statistics = await UserManagementService.GetUserStatisticsAsync(Id);
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

    private async Task CheckPermissionsAsync()
    {
        try
        {
            var currentUser = await CurrentUserService.GetCurrentUserAsync();
            canEdit = currentUser?.Role == UserRole.Owner;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to check user permissions");
            canEdit = false;
        }
    }

    private void HandleEdit()
    {
        if (canEdit)
        {
            Navigation.NavigateTo($"/settings/users/edit/{Id}");
        }
    }

    private void HandleBack()
    {
        Navigation.NavigateTo("/settings/users");
    }

    private string GetRoleBadgeColor(UserRole role) => role switch
    {
        UserRole.Owner => "danger",
        UserRole.Admin => "primary",
        UserRole.Member => "success",
        UserRole.Viewer => "secondary",
        _ => "secondary"
    };
}
