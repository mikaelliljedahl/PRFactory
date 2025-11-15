using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.UI.Checklists;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.UI.Checklists;

/// <summary>
/// Tests for ChecklistItemRow component
/// </summary>
public class ChecklistItemRowTests : TestContext
{
    [Fact]
    public void ChecklistItemRow_RendersUncheckedItem()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Item",
            Description = "Test Description",
            Severity = "required",
            IsChecked = false
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("Test Item", cut.Markup);
        Assert.Contains("Test Description", cut.Markup);
        Assert.Contains("Required", cut.Markup);
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.False(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void ChecklistItemRow_RendersCheckedItemWithCheckmark()
    {
        // Arrange
        var checkedAt = DateTime.UtcNow;
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Checked Item",
            Description = "Description",
            Severity = "recommended",
            IsChecked = true,
            CheckedAt = checkedAt
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("Checked Item", cut.Markup);
        Assert.Contains("Recommended", cut.Markup);
        Assert.Contains("bi-check-circle-fill", cut.Markup);
        Assert.Contains("Checked", cut.Markup);
    }

    [Fact]
    public void ChecklistItemRow_OnToggleCallbackInvokedWhenClicked()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Item",
            Description = "Description",
            Severity = "required",
            IsChecked = false
        };

        ChecklistItemDto? changedItem = null;
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.OnCheckedChanged, EventCallback.Factory.Create<ChecklistItemDto>(
                this, (ChecklistItemDto dto) => changedItem = dto)));

        // Act
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Change(true);

        // Assert
        Assert.NotNull(changedItem);
        Assert.True(changedItem.IsChecked);
        Assert.NotNull(changedItem.CheckedAt);
    }

    [Fact]
    public void ChecklistItemRow_ShowsItemTextCorrectly()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "My Custom Title",
            Description = "My Custom Description",
            Severity = "required",
            IsChecked = false
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("My Custom Title", cut.Markup);
        Assert.Contains("My Custom Description", cut.Markup);
    }

    [Fact]
    public void ChecklistItemRow_AppliesCorrectCssForCheckedState()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = "Description",
            Severity = "required",
            IsChecked = true,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("checklist-item", cut.Markup);
        Assert.Contains("form-check", cut.Markup);
    }

    [Fact]
    public void ChecklistItemRow_ShowsRequiredBadge()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Required Item",
            Description = "Description",
            Severity = "required",
            IsChecked = false
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("badge bg-danger", cut.Markup);
        Assert.Contains("Required", cut.Markup);
    }

    [Fact]
    public void ChecklistItemRow_ShowsRecommendedBadge()
    {
        // Arrange
        var item = new ChecklistItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Recommended Item",
            Description = "Description",
            Severity = "recommended",
            IsChecked = false
        };

        // Act
        var cut = RenderComponent<ChecklistItemRow>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        Assert.Contains("badge bg-secondary", cut.Markup);
        Assert.Contains("Recommended", cut.Markup);
    }
}
