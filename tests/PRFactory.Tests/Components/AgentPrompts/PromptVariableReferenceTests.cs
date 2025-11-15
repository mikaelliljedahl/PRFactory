using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.AgentPrompts;
using Xunit;

namespace PRFactory.Tests.Components.AgentPrompts;

public class PromptVariableReferenceTests : ComponentTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Register IJSRuntime mock
        var mockJSRuntime = new Mock<IJSRuntime>();
        services.AddSingleton(mockJSRuntime.Object);
    }

    [Fact]
    public void Render_DisplaysTicketVariables()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert - Component shows predefined ticket variables
        Assert.Contains("TicketKey", cut.Markup);
        Assert.Contains("TicketTitle", cut.Markup);
        Assert.Contains("TicketDescription", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysRepositoryVariables()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert - Component shows predefined repository variables
        Assert.Contains("RepositoryName", cut.Markup);
        Assert.Contains("RepositoryUrl", cut.Markup);
        Assert.Contains("BranchName", cut.Markup);
    }
}
