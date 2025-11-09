using Microsoft.AspNetCore.Components;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject]
    private IErrorService ErrorService { get; set; } = null!;

    private bool collapseNavMenu = true;
    private int UnresolvedErrorCount { get; set; }
    private System.Threading.Timer? _refreshTimer;

    // For demo purposes - in real app, get from auth/session
    private Guid TenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    protected override async Task OnInitializedAsync()
    {
        await LoadUnresolvedCount();

        // Refresh the count every 30 seconds
        _refreshTimer = new System.Threading.Timer(async _ =>
        {
            await LoadUnresolvedCount();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadUnresolvedCount()
    {
        try
        {
            UnresolvedErrorCount = await ErrorService.GetUnresolvedCountAsync(TenantId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading unresolved count: {ex.Message}");
        }
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
