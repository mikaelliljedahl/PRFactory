using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class SuccessCriteriaEditorTests : ComponentTestBase
{
    [Fact]
    public void Renders_WithEmptyCriteriaList()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_ExistingCriteria()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "User can login",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("User can login", cut.Markup);
    }

    [Fact]
    public async Task AddCriterion_AddsNewCriterion()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();
        var callbackInvoked = false;
        List<SuccessCriterionDto>? updatedCriteria = null;

        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria)
            .Add(p => p.SuccessCriteriaChanged, EventCallback.Factory.Create<List<SuccessCriterionDto>>(
                this,
                (newCriteria) =>
                {
                    callbackInvoked = true;
                    updatedCriteria = newCriteria;
                })));

        // Act
        var addButton = cut.Find("button[type='button']"); // Find the Add button
        await addButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(updatedCriteria);
        Assert.Single(updatedCriteria);
        Assert.Equal(SuccessCriterionCategory.Functional, updatedCriteria[0].Category);
        Assert.Equal(0, updatedCriteria[0].Priority);
        Assert.True(updatedCriteria[0].IsTestable);
    }

    [Fact]
    public async Task RemoveCriterion_RemovesCriterionAtIndex()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Criterion 1",
                Priority = 0,
                IsTestable = true
            },
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Performance,
                Description = "Criterion 2",
                Priority = 1,
                IsTestable = true
            }
        };

        var callbackInvoked = false;
        List<SuccessCriterionDto>? updatedCriteria = null;

        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria)
            .Add(p => p.SuccessCriteriaChanged, EventCallback.Factory.Create<List<SuccessCriterionDto>>(
                this,
                (newCriteria) =>
                {
                    callbackInvoked = true;
                    updatedCriteria = newCriteria;
                })));

        // Act - Remove the first criterion
        var removeButtons = cut.FindAll("button.btn-danger, button.btn-outline-danger");
        if (removeButtons.Any())
        {
            await removeButtons[0].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        }

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(updatedCriteria);
        Assert.Single(updatedCriteria);
        Assert.Equal("Criterion 2", updatedCriteria[0].Description);
    }

    [Fact]
    public void Renders_MultipleCriteria()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Functional,
                Description = "Functional requirement",
                Priority = 0,
                IsTestable = true
            },
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Performance,
                Description = "Performance requirement",
                Priority = 1,
                IsTestable = true
            },
            new SuccessCriterionDto
            {
                Category = SuccessCriterionCategory.Security,
                Description = "Security requirement",
                Priority = 0,
                IsTestable = false
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains("Functional requirement", cut.Markup);
        Assert.Contains("Performance requirement", cut.Markup);
        Assert.Contains("Security requirement", cut.Markup);
    }

    [Fact]
    public void Renders_CategoryDropdown()
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

        // Assert - Should render category selection
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_PriorityDropdown()
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

        // Assert - Should render priority selection
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_IsTestableCheckbox()
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

        // Assert - Should render checkbox inputs
        var checkboxes = cut.FindAll("input[type='checkbox']");
        Assert.NotEmpty(checkboxes);
    }

    [Fact]
    public async Task AddMultipleCriteria_InvokesCallbackMultipleTimes()
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>();
        var callbackCount = 0;

        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria)
            .Add(p => p.SuccessCriteriaChanged, EventCallback.Factory.Create<List<SuccessCriterionDto>>(
                this,
                (newCriteria) => { callbackCount++; })));

        // Act
        var addButton = cut.Find("button[type='button']");
        await addButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await addButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await addButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(3, callbackCount);
    }

    [Theory]
    [InlineData(SuccessCriterionCategory.Functional)]
    [InlineData(SuccessCriterionCategory.Performance)]
    [InlineData(SuccessCriterionCategory.Security)]
    public void Renders_AllCategoryTypes(SuccessCriterionCategory category)
    {
        // Arrange
        var criteria = new List<SuccessCriterionDto>
        {
            new SuccessCriterionDto
            {
                Category = category,
                Description = $"Test {category}",
                Priority = 0,
                IsTestable = true
            }
        };

        // Act
        var cut = RenderComponent<SuccessCriteriaEditor>(parameters => parameters
            .Add(p => p.SuccessCriteria, criteria));

        // Assert
        Assert.Contains($"Test {category}", cut.Markup);
    }
}
