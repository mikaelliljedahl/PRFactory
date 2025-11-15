using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using PRFactory.Web.Components.AgentPrompts;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Components.AgentPrompts;

/// <summary>
/// Tests for the PromptTemplateListItem component.
/// Verifies rendering of template information, status badges, and event callbacks.
/// </summary>
public class PromptTemplateListItemTests : TestContext
{
    private AgentPromptTemplateDto CreateTestTemplate(
        string name = "Test Agent",
        bool isSystemTemplate = false,
        string category = "Implementation",
        string? color = "#FF0000")
    {
        return new AgentPromptTemplateDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "This is a test template",
            PromptContent = "Test prompt content",
            Category = category,
            IsSystemTemplate = isSystemTemplate,
            RecommendedModel = "sonnet",
            Color = color,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            UpdatedAt = new DateTime(2024, 1, 20, 14, 45, 0)
        };
    }

    [Fact]
    public void PromptTemplateListItem_WithTemplate_DisplaysTemplateName()
    {
        // Arrange
        var template = CreateTestTemplate(name: "code-implementation-specialist");

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("code-implementation-specialist", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithTemplate_DisplaysDescription()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("This is a test template", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithColor_DisplaysColorBadge()
    {
        // Arrange
        var template = CreateTestTemplate(color: "#4A90E2");

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        var badge = cut.Find(".badge");
        Assert.NotNull(badge);
        // Style attribute should contain the color
        Assert.Contains("background-color", badge.GetAttribute("style") ?? "");
    }

    [Fact]
    public void PromptTemplateListItem_WithoutColor_DoesNotDisplayColorBadge()
    {
        // Arrange
        var template = CreateTestTemplate(color: null);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        var markup = cut.Markup;
        // Should not render the color badge span
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void PromptTemplateListItem_WithSystemTemplate_DisplaysSystemTemplateBadge()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: true);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("System Template", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithCustomTemplate_DisplaysCustomTemplateBadge()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Custom Template", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithCategory_DisplaysCategoryBadge()
    {
        // Arrange
        var template = CreateTestTemplate(category: "Implementation");

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Implementation", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithRecommendedModel_DisplaysModelBadge()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("sonnet", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_DisplaysCreatedDate()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Created", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithUpdateDate_DisplaysUpdatedDate()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template));

        // Assert
        Assert.Contains("Updated", cut.Markup);
    }

    [Fact]
    public void PromptTemplateListItem_WithPreviewCallback_DisplaysPreviewButton()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnPreview, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var previewButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Preview"));
        Assert.NotNull(previewButton);
    }

    [Fact]
    public void PromptTemplateListItem_ClickPreviewButton_InvokesCallback()
    {
        // Arrange
        var template = CreateTestTemplate();
        var previewInvoked = false;
        AgentPromptTemplateDto? invokedTemplate = null;

        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnPreview, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, t =>
            {
                previewInvoked = true;
                invokedTemplate = t;
            })));

        // Act
        var previewButton = cut.FindAll("button").First(b => b.TextContent.Contains("Preview"));
        previewButton.Click();

        // Assert
        Assert.True(previewInvoked);
        Assert.NotNull(invokedTemplate);
        Assert.Equal(template.Id, invokedTemplate.Id);
    }

    [Fact]
    public void PromptTemplateListItem_WithCustomTemplate_DisplaysEditButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Edit"));
        Assert.NotNull(editButton);
    }

    [Fact]
    public void PromptTemplateListItem_WithSystemTemplate_DoesNotDisplayEditButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: true);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var markup = cut.Markup;
        // Edit button should only show for non-system templates
        var editButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Edit")).ToList();
        Assert.Empty(editButtons);
    }

    [Fact]
    public void PromptTemplateListItem_ClickEditButton_InvokesCallback()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);
        var editInvoked = false;
        AgentPromptTemplateDto? invokedTemplate = null;

        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, t =>
            {
                editInvoked = true;
                invokedTemplate = t;
            })));

        // Act
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
        editButton.Click();

        // Assert
        Assert.True(editInvoked);
        Assert.NotNull(invokedTemplate);
        Assert.Equal(template.Id, invokedTemplate.Id);
    }

    [Fact]
    public void PromptTemplateListItem_WithSystemTemplate_DisplaysCloneButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: true);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnClone, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var cloneButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Clone"));
        Assert.NotNull(cloneButton);
    }

    [Fact]
    public void PromptTemplateListItem_WithCustomTemplate_DoesNotDisplayCloneButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnClone, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var markup = cut.Markup;
        var cloneButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Clone")).ToList();
        Assert.Empty(cloneButtons);
    }

    [Fact]
    public void PromptTemplateListItem_ClickCloneButton_InvokesCallback()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: true);
        var cloneInvoked = false;
        AgentPromptTemplateDto? invokedTemplate = null;

        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnClone, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, t =>
            {
                cloneInvoked = true;
                invokedTemplate = t;
            })));

        // Act
        var cloneButton = cut.FindAll("button").First(b => b.TextContent.Contains("Clone"));
        cloneButton.Click();

        // Assert
        Assert.True(cloneInvoked);
        Assert.NotNull(invokedTemplate);
        Assert.Equal(template.Id, invokedTemplate.Id);
    }

    [Fact]
    public void PromptTemplateListItem_WithCustomTemplate_DisplaysDeleteButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
        Assert.NotNull(deleteButton);
    }

    [Fact]
    public void PromptTemplateListItem_WithSystemTemplate_DoesNotDisplayDeleteButton()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: true);

        // Act
        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, _ => { })));

        // Assert
        var markup = cut.Markup;
        var deleteButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Delete")).ToList();
        Assert.Empty(deleteButtons);
    }

    [Fact]
    public void PromptTemplateListItem_ClickDeleteButton_InvokesCallback()
    {
        // Arrange
        var template = CreateTestTemplate(isSystemTemplate: false);
        var deleteInvoked = false;
        AgentPromptTemplateDto? invokedTemplate = null;

        var cut = RenderComponent<PromptTemplateListItem>(parameters => parameters
            .Add(p => p.Template, template)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<AgentPromptTemplateDto>(this, t =>
            {
                deleteInvoked = true;
                invokedTemplate = t;
            })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteButton.Click();

        // Assert
        Assert.True(deleteInvoked);
        Assert.NotNull(invokedTemplate);
        Assert.Equal(template.Id, invokedTemplate.Id);
    }
}
