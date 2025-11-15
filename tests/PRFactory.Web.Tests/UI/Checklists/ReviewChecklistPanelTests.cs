using Bunit;
using Xunit;
using PRFactory.Web.UI.Checklists;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.UI.Checklists;

/// <summary>
/// Tests for ReviewChecklistPanel component
/// </summary>
public class ReviewChecklistPanelTests : TestContext
{
    [Fact]
    public void ReviewChecklistPanel_RendersPanelWithTitle()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Code Review Template",
            Items = new List<ChecklistItemDto>()
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert
        Assert.Contains("Review Checklist", cut.Markup);
        Assert.Contains("Code Review Template", cut.Markup);
        Assert.Contains("bi-check2-square", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_RendersListOfChecklistItems()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Security Check 1",
                    Description = "Check security",
                    Severity = "required",
                    SortOrder = 1
                },
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Security Check 2",
                    Description = "Check security again",
                    Severity = "recommended",
                    SortOrder = 2
                }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert
        Assert.Contains("Security Check 1", cut.Markup);
        Assert.Contains("Security Check 2", cut.Markup);
        Assert.Contains("Security", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_ShowsProgressBarWithPercentageCompletion()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto { Id = Guid.NewGuid(), Category = "Test", Title = "Item 1", IsChecked = true, SortOrder = 1 },
                new ChecklistItemDto { Id = Guid.NewGuid(), Category = "Test", Title = "Item 2", IsChecked = false, SortOrder = 2 }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert - 1 out of 2 = 50%
        Assert.Contains("50% Complete", cut.Markup);
        Assert.Contains("progress-bar", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_AllItemsCheckedShows100PercentCompletion()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Test",
                    Title = "Item 1",
                    IsChecked = true,
                    Severity = "required",
                    SortOrder = 1
                },
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Test",
                    Title = "Item 2",
                    IsChecked = true,
                    Severity = "required",
                    SortOrder = 2
                }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert
        Assert.Contains("100% Complete", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
        Assert.Contains("Ready for Approval", cut.Markup);
        Assert.Contains("All required checklist items are complete", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_EmptyChecklistShowsAppropriateState()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, null));

        // Assert
        Assert.Contains("No checklist template loaded", cut.Markup);
        Assert.Contains("Checklist will appear when review is assigned", cut.Markup);
        Assert.Contains("bi-list-check", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_ShowsWarningWhenRequiredItemsNotChecked()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Test",
                    Title = "Required Item",
                    IsChecked = false,
                    Severity = "required",
                    SortOrder = 1
                },
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Test",
                    Title = "Recommended Item",
                    IsChecked = true,
                    Severity = "recommended",
                    SortOrder = 2
                }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert
        Assert.Contains("Required Items", cut.Markup);
        Assert.Contains("All required items must be checked before approval", cut.Markup);
        Assert.Contains("bi-exclamation-triangle", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_CategoryCanBeCollapsed()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Security Check",
                    Description = "Check security",
                    Severity = "required",
                    SortOrder = 1
                }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert - Category should be expanded by default
        Assert.Contains("bi-chevron-down", cut.Markup);
        Assert.Contains("Security Check", cut.Markup);

        // Act - Click to collapse
        var categoryHeader = cut.Find(".category-header");
        categoryHeader.Click();

        // Assert - Category should now be collapsed
        Assert.Contains("bi-chevron-right", cut.Markup);
    }

    [Fact]
    public void ReviewChecklistPanel_ShowsCategoryProgress()
    {
        // Arrange
        var checklist = new ReviewChecklistDto
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Items = new List<ChecklistItemDto>
            {
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Check 1",
                    IsChecked = true,
                    SortOrder = 1
                },
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Check 2",
                    IsChecked = false,
                    SortOrder = 2
                },
                new ChecklistItemDto
                {
                    Id = Guid.NewGuid(),
                    Category = "Security",
                    Title = "Check 3",
                    IsChecked = false,
                    SortOrder = 3
                }
            }
        };

        // Act
        var cut = RenderComponent<ReviewChecklistPanel>(parameters => parameters
            .Add(p => p.Checklist, checklist));

        // Assert - Should show 1/3 for Security category
        Assert.Contains("1/3", cut.Markup);
    }
}
