using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Pages.Auth;

public partial class Welcome
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private void GoToDashboard()
    {
        Navigation.NavigateTo("/");
    }

    private void GoToSettings()
    {
        Navigation.NavigateTo("/settings/integrations");
    }
}
