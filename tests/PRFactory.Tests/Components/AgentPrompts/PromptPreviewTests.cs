using Bunit;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.AgentPrompts;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.AgentPrompts;

public class PromptPreviewTests : ComponentTestBase
{
    [Fact]
    public void Render_WithTemplate_DisplaysPreview()
    {
        // Arrange
        var template = new AgentPromptTemplateDtoBuilder()
            .WithPromptContent("Hello {{name}}!")
            .Build();

        var mockAgentPromptService = new Mock<IAgentPromptService>();
        mockAgentPromptService
            .Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Hello {{name}}!");
        Services.AddSingleton(mockAgentPromptService.Object);

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Hello", cut.Markup);
    }

    [Fact]
    public void Render_WithSampleData_SubstitutesVariables()
    {
        // Arrange
        var template = new AgentPromptTemplateDtoBuilder()
            .WithPromptContent("Hello {{name}}!")
            .Build();
        var sampleData = new Dictionary<string, string> { { "name", "World" } };

        var mockAgentPromptService = new Mock<IAgentPromptService>();
        mockAgentPromptService
            .Setup(s => s.PreviewTemplateAsync(template.Id, sampleData, default))
            .ReturnsAsync("Hello World!");
        Services.AddSingleton(mockAgentPromptService.Object);

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.SampleData, sampleData));

        // Assert
        Assert.Contains("World", cut.Markup);
    }
}
