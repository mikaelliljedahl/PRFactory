using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Pages.AgentPrompts;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Pages.AgentPrompts;

public class CreateTests : PageTestBase
{
    private readonly Mock<IAgentPromptService> _mockPromptService = new();
    private readonly Mock<ILogger<Create>> _mockLogger = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockPromptService.Object);
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<Radzen.DialogService>();
    }

    [Fact]
    public void Render_DisplaysCreateForm()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Create Agent Prompt Template", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSaveButton()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Create Template", cut.Markup);
    }
}
