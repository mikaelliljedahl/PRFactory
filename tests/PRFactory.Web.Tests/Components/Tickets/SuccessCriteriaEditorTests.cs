using Bunit;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for SuccessCriteriaEditor component
/// </summary>
public class SuccessCriteriaEditorTests : TestContext
{
    [Fact]
    public void SuccessCriteriaEditor_WithEmptyList_ShowsEmptyStateMessage()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("No success criteria defined yet", cut.Markup);
        Assert.Contains("Add Criterion", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_WithExistingCriteria_DisplaysCriteria()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test criterion",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Test criterion", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ClickAddCriterion_InvokesCallback()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();
        var callbackInvoked = false;
        var updatedCriteria = new List<SuccessCriterionDto>();

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria)
            .Add(p => p.SuccessCriteriaChanged, newCriteria =>
            {
                callbackInvoked = true;
                updatedCriteria = newCriteria;
            }));

        var addButton = cut.Find("button:contains('Add Criterion')");
        addButton.Click();

        // Assert
        Assert.True(callbackInvoked);
        Assert.Single(updatedCriteria);
    }

    [Fact]
    public void SuccessCriteriaEditor_AddCriterion_CreatesNewCriterionWithDefaults()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();
        var updatedCriteria = new List<SuccessCriterionDto>();

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria)
            .Add(p => p.SuccessCriteriaChanged, newCriteria =>
            {
                updatedCriteria = newCriteria;
            }));

        var addButton = cut.Find("button:contains('Add Criterion')");
        addButton.Click();

        // Assert
        var newCriterion = updatedCriteria.First();
        Assert.Equal(SuccessCriterionCategory.Functional, newCriterion.Category);
        Assert.Equal(0, newCriterion.Priority);
        Assert.True(newCriterion.IsTestable);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsCategoryDropdown()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Category", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsPriorityDropdown()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Priority", cut.Markup);
        Assert.Contains("Must-Have", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsTestableCheckbox()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Testable", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsDescriptionTextArea()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test description",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Description", cut.Markup);
        Assert.Contains("Test description", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsRemoveButton()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Test",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        var removeButton = cut.Find("button[title='Remove criterion']");
        Assert.NotNull(removeButton);
    }

    [Fact]
    public void SuccessCriteriaEditor_WithMultipleCriteria_DisplaysAll()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "First criterion",
                Priority = 0,
                IsTestable = true
            },
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Performance,
                Description = "Second criterion",
                Priority = 1,
                IsTestable = false
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("First criterion", cut.Markup);
        Assert.Contains("Second criterion", cut.Markup);
    }

    [Fact]
    public void SuccessCriteriaEditor_ShowsSuccessCriteriaHeader()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Success Criteria", cut.Markup);
    }
}
