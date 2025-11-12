using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Services;
using Xunit;
using AgentPromptsIndex = PRFactory.Web.Pages.AgentPrompts.Index;

namespace PRFactory.Tests.Pages.AgentPrompts;

public class IndexTests : PageTestBase
{
    private readonly Mock<IAgentPromptService> _mockPromptService;
    private readonly Mock<ILogger<AgentPromptsIndex>> _mockLogger;

    public IndexTests()
    {
        _mockPromptService = new Mock<IAgentPromptService>();
        _mockLogger = new Mock<ILogger<AgentPromptsIndex>>();

        Services.AddSingleton(_mockPromptService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public async Task OnInitialized_LoadsPromptTemplates()
    {
        // Arrange
        var templates = new List<PRFactory.Web.Models.AgentPromptTemplateDto>
        {
            new AgentPromptTemplateDtoBuilder().WithName("Prompt 1").Build(),
            new AgentPromptTemplateDtoBuilder().WithName("Prompt 2").Build()
        };

        _mockPromptService.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var cut = RenderComponent<AgentPromptsIndex>();
        await Task.Delay(100);

        // Assert
        _mockPromptService.Verify(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Prompt 1", cut.Markup);
        Assert.Contains("Prompt 2", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithNoTemplates_DisplaysEmptyState()
    {
        // Arrange
        _mockPromptService.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PRFactory.Web.Models.AgentPromptTemplateDto>());

        // Act
        var cut = RenderComponent<AgentPromptsIndex>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("No prompt templates", cut.Markup);
    }
}
