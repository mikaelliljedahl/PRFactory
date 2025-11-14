using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Components.Settings;

public partial class UserListItem
{
    [Parameter, EditorRequired]
    public UserManagementDto User { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnActivate { get; set; }

    [Parameter]
    public EventCallback OnDeactivate { get; set; }

    [Parameter]
    public EventCallback OnViewDetails { get; set; }

    private string GetRoleBadgeColor() => User.Role switch
    {
        UserRole.Owner => "danger",
        UserRole.Admin => "primary",
        UserRole.Member => "success",
        UserRole.Viewer => "secondary",
        _ => "secondary"
    };
}
