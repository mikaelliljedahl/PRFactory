using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Auth;

public partial class UserProfileDropdown
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private void Logout()
    {
        Navigation.NavigateTo("/api/auth/logout", forceLoad: true);
    }
}
