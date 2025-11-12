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
    private readonly Mock<IAgentPromptService> _mockPromptService;
    private readonly Mock<ILogger<Create>> _mockLogger;

    public CreateTests()
    {
        _mockPromptService = new Mock<IAgentPromptService>();
        _mockLogger = new Mock<ILogger<Create>>();

        Services.AddSingleton(_mockPromptService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public void Render_DisplaysCreateForm()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Create Prompt Template", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSaveButton()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Save", cut.Markup);
    }
}
