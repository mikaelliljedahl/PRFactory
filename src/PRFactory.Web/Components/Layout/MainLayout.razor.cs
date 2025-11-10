using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    // Demo tenant ID (hardcoded for demo purposes)
    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // For demo purposes - in real app, get from auth/session
    private Guid CurrentTenantId { get; set; } = DemoTenantId;

    private bool IsDemoMode => CurrentTenantId == DemoTenantId || IsDevelopmentEnvironment;

    private bool IsDevelopmentEnvironment
    {
        get
        {
            var environment = Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
            return environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}
