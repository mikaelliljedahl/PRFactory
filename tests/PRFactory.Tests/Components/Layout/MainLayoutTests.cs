using Bunit;
using Bunit.TestDoubles;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Layout;
using Xunit;

namespace PRFactory.Tests.Components.Layout;

public class MainLayoutTests : ComponentTestBase
{
    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services before any components are rendered
        this.AddTestAuthorization();
    }

    [Fact(Skip = "MainLayout requires IConfiguration dependency and ChildContent parameter handling - needs test environment redesign")]
    public void Render_DisplaysLayoutStructureWithNavMenuAndContentArea()
    {
        // Test implementation pending test environment redesign
        throw new NotSupportedException("Test requires environment redesign - see Skip attribute for details");
    }
}
