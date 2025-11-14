using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for ChecklistTemplateService
/// Covers YAML loading, template parsing, and checklist creation
/// </summary>
public class ChecklistTemplateServiceTests : IDisposable
{
    private readonly string _testTemplatesPath;
    private readonly Mock<ILogger<ChecklistTemplateService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public ChecklistTemplateServiceTests()
    {
        _mockLogger = new Mock<ILogger<ChecklistTemplateService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Create temporary directory for test templates
        _testTemplatesPath = Path.Combine(Path.GetTempPath(), $"checklist_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testTemplatesPath);

        _mockConfiguration.Setup(c => c["ChecklistTemplatesPath"]).Returns(_testTemplatesPath);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testTemplatesPath))
        {
            Directory.Delete(_testTemplatesPath, true);
        }
    }

    #region LoadTemplateAsync Tests

    [Fact]
    public async Task LoadTemplateAsync_WithValidTemplate_ReturnsChecklistTemplate()
    {
        // Arrange
        var service = CreateService();
        CreateTestTemplate("test_domain.yaml", @"
name: ""Test Template""
domain: ""test_domain""
version: ""1.0""
categories:
  - name: ""Category 1""
    items:
      - title: ""Item 1""
        description: ""Description 1""
        severity: ""required""
        sort_order: 1
");

        // Act
        var result = await service.LoadTemplateAsync("test_domain");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Template", result.Name);
        Assert.Equal("test_domain", result.Domain);
        Assert.Equal("1.0", result.Version);
        Assert.Single(result.Categories);
        Assert.Equal("Category 1", result.Categories[0].Name);
        Assert.Single(result.Categories[0].Items);
        Assert.Equal("Item 1", result.Categories[0].Items[0].Title);
    }

    [Fact]
    public async Task LoadTemplateAsync_WithNonExistentTemplate_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.LoadTemplateAsync("nonexistent"));
    }

    [Fact]
    public async Task LoadTemplateAsync_WithEmptyDomain_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.LoadTemplateAsync(""));
    }

    [Fact]
    public async Task LoadTemplateAsync_WithInvalidYaml_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        CreateTestTemplate("invalid.yaml", "this is not valid yaml: [unclosed");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoadTemplateAsync("invalid"));
    }

    [Fact]
    public async Task LoadTemplateAsync_WithMultipleCategories_ParsesAllCorrectly()
    {
        // Arrange
        var service = CreateService();
        CreateTestTemplate("multi_category.yaml", @"
name: ""Multi Category Template""
domain: ""multi_category""
version: ""1.0""
categories:
  - name: ""Security""
    items:
      - title: ""Auth checks""
        description: ""Check authorization""
        severity: ""required""
        sort_order: 1
      - title: ""Input validation""
        description: ""Validate inputs""
        severity: ""required""
        sort_order: 2
  - name: ""Performance""
    items:
      - title: ""Load testing""
        description: ""Performance tests""
        severity: ""recommended""
        sort_order: 1
");

        // Act
        var result = await service.LoadTemplateAsync("multi_category");

        // Assert
        Assert.Equal(2, result.Categories.Count);
        Assert.Equal("Security", result.Categories[0].Name);
        Assert.Equal(2, result.Categories[0].Items.Count);
        Assert.Equal("Performance", result.Categories[1].Name);
        Assert.Single(result.Categories[1].Items);
    }

    #endregion

    #region GetAvailableTemplatesAsync Tests

    [Fact]
    public async Task GetAvailableTemplatesAsync_WithMultipleTemplates_ReturnsAllMetadata()
    {
        // Arrange
        var service = CreateService();
        CreateTestTemplate("template1.yaml", @"
name: ""Template 1""
domain: ""domain1""
version: ""1.0""
categories: []
");
        CreateTestTemplate("template2.yaml", @"
name: ""Template 2""
domain: ""domain2""
version: ""2.0""
categories: []
");

        // Act
        var result = await service.GetAvailableTemplatesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Name == "Template 1" && t.Domain == "domain1" && t.Version == "1.0");
        Assert.Contains(result, t => t.Name == "Template 2" && t.Domain == "domain2" && t.Version == "2.0");
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetAvailableTemplatesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["ChecklistTemplatesPath"])
            .Returns(Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid()));
        var service = CreateService();

        // Act
        var result = await service.GetAvailableTemplatesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_WithInvalidTemplate_SkipsAndContinues()
    {
        // Arrange
        var service = CreateService();
        CreateTestTemplate("valid.yaml", @"
name: ""Valid Template""
domain: ""valid""
version: ""1.0""
categories: []
");
        CreateTestTemplate("invalid.yaml", "invalid yaml content [");

        // Act
        var result = await service.GetAvailableTemplatesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid Template", result[0].Name);
    }

    #endregion

    #region CreateChecklistFromTemplate Tests

    [Fact]
    public void CreateChecklistFromTemplate_WithValidTemplate_CreatesChecklist()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();
        var template = new ChecklistTemplate
        {
            Name = "Test Template",
            Domain = "test",
            Version = "1.0",
            Categories = new List<ChecklistCategory>
            {
                new ChecklistCategory
                {
                    Name = "Category 1",
                    Items = new List<ChecklistTemplateItem>
                    {
                        new ChecklistTemplateItem
                        {
                            Title = "Item 1",
                            Description = "Description 1",
                            Severity = "required",
                            SortOrder = 1
                        },
                        new ChecklistTemplateItem
                        {
                            Title = "Item 2",
                            Description = "Description 2",
                            Severity = "recommended",
                            SortOrder = 2
                        }
                    }
                }
            }
        };

        // Act
        var result = service.CreateChecklistFromTemplate(planReviewId, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(planReviewId, result.PlanReviewId);
        Assert.Equal("Test Template", result.TemplateName);
        Assert.Equal(2, result.Items.Count);
        var items = result.Items.ToList();
        Assert.Equal("Category 1", items[0].Category);
        Assert.Equal("Item 1", items[0].Title);
        Assert.Equal("required", items[0].Severity);
        Assert.False(items[0].IsChecked);
    }

    [Fact]
    public void CreateChecklistFromTemplate_WithNullTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => service.CreateChecklistFromTemplate(planReviewId, null!));
    }

    [Fact]
    public void CreateChecklistFromTemplate_WithEmptyCategories_CreatesEmptyChecklist()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();
        var template = new ChecklistTemplate
        {
            Name = "Empty Template",
            Domain = "empty",
            Version = "1.0",
            Categories = new List<ChecklistCategory>()
        };

        // Act
        var result = service.CreateChecklistFromTemplate(planReviewId, template);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.CompletionPercentage());
    }

    [Fact]
    public void CreateChecklistFromTemplate_WithMultipleCategories_CreatesAllItems()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();
        var template = new ChecklistTemplate
        {
            Name = "Multi Category",
            Domain = "multi",
            Version = "1.0",
            Categories = new List<ChecklistCategory>
            {
                new ChecklistCategory
                {
                    Name = "Category 1",
                    Items = new List<ChecklistTemplateItem>
                    {
                        new ChecklistTemplateItem { Title = "C1-Item1", Description = "Desc", Severity = "required", SortOrder = 0 }
                    }
                },
                new ChecklistCategory
                {
                    Name = "Category 2",
                    Items = new List<ChecklistTemplateItem>
                    {
                        new ChecklistTemplateItem { Title = "C2-Item1", Description = "Desc", Severity = "recommended", SortOrder = 0 },
                        new ChecklistTemplateItem { Title = "C2-Item2", Description = "Desc", Severity = "required", SortOrder = 0 }
                    }
                }
            }
        };

        // Act
        var result = service.CreateChecklistFromTemplate(planReviewId, template);

        // Assert
        Assert.Equal(3, result.Items.Count);
        var items = result.Items.ToList();
        Assert.Equal("Category 1", items[0].Category);
        Assert.Equal("Category 2", items[1].Category);
        Assert.Equal("Category 2", items[2].Category);
    }

    [Fact]
    public void CreateChecklistFromTemplate_PreservesSortOrder()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();
        var template = new ChecklistTemplate
        {
            Name = "Sort Order Test",
            Domain = "sort",
            Version = "1.0",
            Categories = new List<ChecklistCategory>
            {
                new ChecklistCategory
                {
                    Name = "Category",
                    Items = new List<ChecklistTemplateItem>
                    {
                        new ChecklistTemplateItem { Title = "Third", Description = "Desc", Severity = "required", SortOrder = 3 },
                        new ChecklistTemplateItem { Title = "First", Description = "Desc", Severity = "required", SortOrder = 1 },
                        new ChecklistTemplateItem { Title = "Second", Description = "Desc", Severity = "required", SortOrder = 2 }
                    }
                }
            }
        };

        // Act
        var result = service.CreateChecklistFromTemplate(planReviewId, template);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(3, items[0].SortOrder);
        Assert.Equal(1, items[1].SortOrder);
        Assert.Equal(2, items[2].SortOrder);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EndToEnd_LoadAndCreateChecklist_WorksCorrectly()
    {
        // Arrange
        var service = CreateService();
        var planReviewId = Guid.NewGuid();
        CreateTestTemplate("integration.yaml", @"
name: ""Integration Test Template""
domain: ""integration""
version: ""1.0""
categories:
  - name: ""Completeness""
    items:
      - title: ""All features specified""
        description: ""Check that all features are documented""
        severity: ""required""
        sort_order: 1
      - title: ""Acceptance criteria defined""
        description: ""Verify acceptance criteria exist""
        severity: ""required""
        sort_order: 2
  - name: ""Quality""
    items:
      - title: ""Code quality standards""
        description: ""Follows project standards""
        severity: ""recommended""
        sort_order: 1
");

        // Act
        var template = await service.LoadTemplateAsync("integration");
        var checklist = service.CreateChecklistFromTemplate(planReviewId, template);

        // Assert - Verify end-to-end flow
        Assert.Equal("Integration Test Template", checklist.TemplateName);
        Assert.Equal(3, checklist.Items.Count);
        Assert.Equal(2, checklist.Items.Count(i => i.Severity == "required"));
        Assert.Single(checklist.Items.Where(i => i.Severity == "recommended"));
        Assert.False(checklist.AllRequiredItemsChecked());
        Assert.Equal(0, checklist.CompletionPercentage());

        // Check an item and verify
        var items = checklist.Items.ToList();
        items[0].Check();
        Assert.True(items[0].IsChecked);
        Assert.Equal(33, checklist.CompletionPercentage()); // 1 of 3 = 33%
        Assert.False(checklist.AllRequiredItemsChecked()); // Still have 1 required unchecked

        // Check all required items
        items[1].Check(); // Second required item
        Assert.True(checklist.AllRequiredItemsChecked());
    }

    #endregion

    #region Helper Methods

    private ChecklistTemplateService CreateService()
    {
        return new ChecklistTemplateService(_mockConfiguration.Object, _mockLogger.Object);
    }

    private void CreateTestTemplate(string filename, string content)
    {
        var filePath = Path.Combine(_testTemplatesPath, filename);
        File.WriteAllText(filePath, content);
    }

    #endregion
}
