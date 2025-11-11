using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Pages.Auth;

public partial class Login
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool isLoading;

    private void SignInWithProvider(string provider)
    {
        isLoading = true;
        var returnUrl = "/";
        Navigation.NavigateTo(
            $"/api/auth/login?provider={provider}&returnUrl={Uri.EscapeDataString(returnUrl)}",
            forceLoad: true);
    }
}
