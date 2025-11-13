using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.AgentPrompts;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.AgentPrompts;

public class PromptTemplateFormTests : ComponentTestBase
{
    private readonly Mock<IAgentPromptService> _mockPromptService = new Mock<IAgentPromptService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Register mock BEFORE service provider is locked
        services.AddSingleton(_mockPromptService.Object);
    }

    [Fact]
    public void Render_DisplaysFormFields()
    {
        // Arrange
        var model = new CreatePromptTemplateRequest
        {
            Name = string.Empty,
            Description = string.Empty,
            PromptContent = string.Empty,
            Category = string.Empty
        };

        // Act
        var cut = RenderComponent<PromptTemplateForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Name", cut.Markup);
        Assert.Contains("Description", cut.Markup);
    }

    [Fact]
    public void Render_WithExistingTemplate_PopulatesFields()
    {
        // Arrange
        var model = new UpdatePromptTemplateRequest
        {
            Name = "Test Prompt",
            Description = "Test Description",
            PromptContent = "Test template content",
            Category = "Implementation"
        };

        // Act
        var cut = RenderComponent<PromptTemplateForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Test Prompt", cut.Markup);
    }
}
