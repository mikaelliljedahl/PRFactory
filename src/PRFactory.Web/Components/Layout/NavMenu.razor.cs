using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject]
    private IErrorService ErrorService { get; set; } = null!;

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private ILogger<NavMenu> Logger { get; set; } = null!;

    [Parameter]
    public bool IsDemoMode { get; set; }

    private bool collapseNavMenu = true;
    private int UnresolvedErrorCount { get; set; }
    private int TicketCount { get; set; }
    private System.Threading.Timer? _refreshTimer;

    // For demo purposes - in real app, get from auth/session
    private Guid TenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    /// <summary>
    /// Show Getting Started link if in demo mode or user has fewer than 3 tickets (new user)
    /// </summary>
    private bool ShouldShowGettingStarted => IsDemoMode || TicketCount < 3;

    protected override async Task OnInitializedAsync()
    {
        await LoadUnresolvedCount();
        await LoadTicketCount();

        // Refresh the counts every 30 seconds
        _refreshTimer = new System.Threading.Timer(async _ =>
        {
            await LoadUnresolvedCount();
            await LoadTicketCount();
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
            Logger.LogError(ex, "Error loading unresolved error count for tenant {TenantId}", TenantId);
        }
    }

    private async Task LoadTicketCount()
    {
        try
        {
            var tickets = await TicketService.GetAllTicketsAsync();
            TicketCount = tickets.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading ticket count for tenant {TenantId}", TenantId);
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
