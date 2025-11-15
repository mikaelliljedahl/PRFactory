using Bunit;
using Moq;
using Xunit;
using PRFactory.Web.Components.AgentPrompts;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Tests.Components.AgentPrompts;

/// <summary>
/// Tests for the PromptPreview component.
/// Verifies rendering of prompt template and preview with sample data.
/// </summary>
public class PromptPreviewTests : TestContext
{
    private readonly Mock<IAgentPromptService> _mockPromptService;

    public PromptPreviewTests()
    {
        _mockPromptService = new Mock<IAgentPromptService>();
        Services.AddSingleton(_mockPromptService.Object);
    }

    private AgentPromptTemplateDto CreateTestTemplate(string name = "Test Agent", string content = "Test prompt content")
    {
        return new AgentPromptTemplateDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test description",
            PromptContent = content,
            Category = "Implementation",
            IsSystemTemplate = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task PromptPreview_WithTemplate_DisplaysTemplateName()
    {
        // Arrange
        var template = CreateTestTemplate("code-specialist");
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Preview content");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("code-specialist", cut.Markup);
    }

    [Fact]
    public async Task PromptPreview_WithTemplate_DisplaysTemplateContent()
    {
        // Arrange
        var templateContent = "This is the prompt template";
        var template = CreateTestTemplate(content: templateContent);
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Preview content");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains(templateContent, cut.Markup);
    }

    [Fact]
    public async Task PromptPreview_WithoutTemplate_ShowsNoTemplateMessage()
    {
        // Arrange
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(It.IsAny<Guid>(), null, default))
            .ReturnsAsync("No preview");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, (AgentPromptTemplateDto?)null));

        // Assert
        Assert.Contains("No template selected", cut.Markup);
    }

    [Fact]
    public async Task PromptPreview_CallsServiceWithTemplate()
    {
        // Arrange
        var template = CreateTestTemplate();
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Preview content");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        await Task.Delay(100); // Allow async operation to complete

        // Assert
        _mockPromptService.Verify(s => s.PreviewTemplateAsync(template.Id, null, default), Times.Once);
    }

    [Fact]
    public async Task PromptPreview_WithSampleData_PassesDataToService()
    {
        // Arrange
        var template = CreateTestTemplate();
        var sampleData = new Dictionary<string, string>
        {
            { "{{TicketKey}}", "PROJ-123" },
            { "{{TicketTitle}}", "Test Ticket" }
        };

        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, sampleData, default))
            .ReturnsAsync("Preview with replaced data");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.SampleData, sampleData));

        await Task.Delay(100);

        // Assert
        _mockPromptService.Verify(s => s.PreviewTemplateAsync(template.Id, sampleData, default), Times.Once);
    }

    [Fact]
    public async Task PromptPreview_OnError_DisplaysErrorMessage()
    {
        // Arrange
        var template = CreateTestTemplate();
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        await Task.Delay(100);

        // Assert
        Assert.Contains("Error loading preview", cut.Markup);
        Assert.Contains("Service error", cut.Markup);
    }

    [Fact]
    public async Task PromptPreview_DisplaysTemplateLabel()
    {
        // Arrange
        var template = CreateTestTemplate();
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Preview content");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Template", cut.Markup);
    }

    [Fact]
    public async Task PromptPreview_DisplaysPreviewLabel()
    {
        // Arrange
        var template = CreateTestTemplate();
        _mockPromptService.Setup(s => s.PreviewTemplateAsync(template.Id, null, default))
            .ReturnsAsync("Preview content");

        // Act
        var cut = RenderComponent<PromptPreview>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Preview with Sample Data", cut.Markup);
    }
}
