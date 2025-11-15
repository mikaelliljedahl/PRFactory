# Feature 4: Review Checklist Templates

**Goal**: Provide reviewers with structured, domain-specific checklists to ensure consistent plan evaluation.

**Estimated Effort**: 2 weeks
**Priority**: P2 (Should Have)
**Dependencies**: None (enhances existing PlanReview workflow)

---

## Executive Summary

Currently, reviewers evaluate plans using ad-hoc criteria, leading to inconsistent review quality. Some reviewers catch security issues while others focus only on completeness.

**This feature** provides:
- YAML-based checklist templates (web_ui, rest_api, database, background_jobs)
- ReviewChecklist entity tracks completion per reviewer
- UI panel with collapsible categories
- Progress tracking (% complete)
- Approval validation (required items must be checked)

**Expected Impact**: Consistent, high-quality reviews with 95%+ coverage of important criteria.

---

## Current State Analysis

**Problem**: No structured guidance for reviewers.

Reviewers currently evaluate plans based on:
- Personal experience
- Ad-hoc criteria
- Inconsistent thoroughness

**Result**: Important aspects missed (security, error handling, tests, rollback plans).

---

## Implementation Plan

### Week 1: Domain & Templates

#### Day 1-2: Domain Entities

**File**: `/src/PRFactory.Domain/Entities/ReviewChecklist.cs`

```csharp
namespace PRFactory.Domain.Entities;

public class ReviewChecklist
{
    public Guid Id { get; private set; }
    public Guid PlanReviewId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public List<ChecklistItem> Items { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public PlanReview PlanReview { get; private set; } = null!;

    private ReviewChecklist() { }

    public static ReviewChecklist Create(
        Guid planReviewId,
        string templateName,
        List<ChecklistItem> items)
    {
        return new ReviewChecklist
        {
            Id = Guid.NewGuid(),
            PlanReviewId = planReviewId,
            TemplateName = templateName,
            Items = items,
            CreatedAt = DateTime.UtcNow
        };
    }

    public int CompletionPercentage =>
        Items.Any() ? (Items.Count(i => i.IsChecked) * 100) / Items.Count : 0;

    public bool AllRequiredItemsChecked =>
        Items.Where(i => i.Severity == "required").All(i => i.IsChecked);
}

public class ChecklistItem
{
    public Guid Id { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "recommended";  // "required" or "recommended"
    public bool IsChecked { get; private set; }
    public DateTime? CheckedAt { get; private set; }
    public int SortOrder { get; private set; }

    private ChecklistItem() { }

    public static ChecklistItem Create(
        string category,
        string title,
        string description,
        string severity,
        int sortOrder)
    {
        if (!new[] { "required", "recommended" }.Contains(severity))
            throw new ArgumentException("Severity must be 'required' or 'recommended'");

        return new ChecklistItem
        {
            Id = Guid.NewGuid(),
            Category = category,
            Title = title,
            Description = description,
            Severity = severity,
            SortOrder = sortOrder
        };
    }

    public void Check()
    {
        IsChecked = true;
        CheckedAt = DateTime.UtcNow;
    }

    public void Uncheck()
    {
        IsChecked = false;
        CheckedAt = null;
    }
}
```

---

#### Day 3-4: YAML Templates

**File**: `/config/checklists/web_ui.yaml`

```yaml
name: "Web UI Implementation Review"
domain: "web_ui"
version: "1.0"
categories:
  - name: "Completeness"
    items:
      - title: "All UI components identified"
        description: "Plan lists all Blazor components to create/modify with file paths"
        severity: "required"
        sort_order: 1

      - title: "Page routing defined"
        description: "All @page directives and navigation paths specified"
        severity: "required"
        sort_order: 2

      - title: "State management specified"
        description: "Component state, cascading parameters, and service injection documented"
        severity: "required"
        sort_order: 3

      - title: "UI component reuse"
        description: "Plan uses existing components from /UI/ directory where applicable"
        severity: "recommended"
        sort_order: 4

      - title: "Code-behind pattern"
        description: "All Pages and business Components use code-behind (.razor.cs) separation"
        severity: "required"
        sort_order: 5

  - name: "Security"
    items:
      - title: "Authorization checks present"
        description: "Protected pages have [Authorize] attribute or authorization logic"
        severity: "required"
        sort_order: 1

      - title: "Input validation"
        description: "Form validation with DataAnnotations or FluentValidation specified"
        severity: "required"
        sort_order: 2

      - title: "XSS protection"
        description: "User input sanitized or escaped before rendering"
        severity: "required"
        sort_order: 3

  - name: "Architecture"
    items:
      - title: "Service injection used"
        description: "Components inject services directly (NO HTTP calls within process)"
        severity: "required"
        sort_order: 1

      - title: "Clean Architecture layers"
        description: "Changes follow Domain → Application → Infrastructure → Web pattern"
        severity: "required"
        sort_order: 2

      - title: "No custom JavaScript"
        description: "Pure Blazor Server patterns used (NO custom .js files)"
        severity: "required"
        sort_order: 3

  - name: "Testing"
    items:
      - title: "Unit tests specified"
        description: "Plan includes unit tests for component logic and services"
        severity: "required"
        sort_order: 1

      - title: "80% coverage target"
        description: "Tests cover critical paths and edge cases (80% minimum)"
        severity: "required"
        sort_order: 2

      - title: "Integration tests"
        description: "Component rendering and interaction tests specified"
        severity: "recommended"
        sort_order: 3

  - name: "Code Quality"
    items:
      - title: "UTF-8 without BOM"
        description: "Files will be saved as UTF-8 without BOM (dotnet format check)"
        severity: "required"
        sort_order: 1

      - title: "File-scoped namespaces"
        description: "Uses file-scoped namespace syntax (namespace Foo.Bar;)"
        severity: "recommended"
        sort_order: 2

      - title: "Error handling"
        description: "Try-catch blocks and user-friendly error messages specified"
        severity: "required"
        sort_order: 3
```

**Create similar templates**:
- `/config/checklists/rest_api.yaml` - API validation, DTOs, versioning
- `/config/checklists/database.yaml` - Migrations, indexes, constraints
- `/config/checklists/background_jobs.yaml` - Idempotency, retry logic

---

#### Day 5: Template Loader Service

**File**: `/src/PRFactory.Core/Application/Services/IChecklistTemplateService.cs`

```csharp
namespace PRFactory.Core.Application.Services;

public interface IChecklistTemplateService
{
    /// <summary>
    /// Load checklist template from YAML file
    /// </summary>
    Task<ChecklistTemplate> LoadTemplateAsync(string domain);

    /// <summary>
    /// Get all available templates
    /// </summary>
    Task<List<ChecklistTemplateMetadata>> GetAvailableTemplatesAsync();

    /// <summary>
    /// Create ReviewChecklist from template
    /// </summary>
    ReviewChecklist CreateChecklistFromTemplate(
        Guid planReviewId,
        ChecklistTemplate template);
}

public class ChecklistTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<ChecklistCategory> Categories { get; set; } = new();
}

public class ChecklistCategory
{
    public string Name { get; set; } = string.Empty;
    public List<ChecklistTemplateItem> Items { get; set; } = new();
}

public class ChecklistTemplateItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "recommended";
    public int SortOrder { get; set; }
}
```

**File**: `/src/PRFactory.Infrastructure/Application/ChecklistTemplateService.cs`

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ChecklistTemplateService : IChecklistTemplateService
{
    private readonly string _templateBasePath;
    private readonly ILogger<ChecklistTemplateService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public ChecklistTemplateService(
        IConfiguration configuration,
        ILogger<ChecklistTemplateService> logger)
    {
        _templateBasePath = configuration["ChecklistTemplatesPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "config", "checklists");
        _logger = logger;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    public async Task<ChecklistTemplate> LoadTemplateAsync(string domain)
    {
        var filePath = Path.Combine(_templateBasePath, $"{domain}.yaml");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Checklist template not found: {Domain}", domain);
            throw new FileNotFoundException($"Template not found: {domain}");
        }

        var yaml = await File.ReadAllTextAsync(filePath);
        var template = _yamlDeserializer.Deserialize<ChecklistTemplate>(yaml);

        return template;
    }

    public async Task<List<ChecklistTemplateMetadata>> GetAvailableTemplatesAsync()
    {
        var templates = new List<ChecklistTemplateMetadata>();
        var files = Directory.GetFiles(_templateBasePath, "*.yaml");

        foreach (var file in files)
        {
            var yaml = await File.ReadAllTextAsync(file);
            var template = _yamlDeserializer.Deserialize<ChecklistTemplate>(yaml);

            templates.Add(new ChecklistTemplateMetadata
            {
                Domain = template.Domain,
                Name = template.Name,
                Version = template.Version
            });
        }

        return templates;
    }

    public ReviewChecklist CreateChecklistFromTemplate(
        Guid planReviewId,
        ChecklistTemplate template)
    {
        var items = new List<ChecklistItem>();
        int sortOrder = 0;

        foreach (var category in template.Categories)
        {
            foreach (var item in category.Items)
            {
                items.Add(ChecklistItem.Create(
                    category.Name,
                    item.Title,
                    item.Description,
                    item.Severity,
                    sortOrder++));
            }
        }

        return ReviewChecklist.Create(
            planReviewId,
            template.Name,
            items);
    }
}
```

---

### Week 2: UI Components

#### Day 6-8: ReviewChecklistPanel Component

**File**: `/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor`

```razor
@namespace PRFactory.Web.UI.Checklists

<div class="review-checklist-panel card">
    <div class="card-header bg-info text-white">
        <h6 class="mb-0">
            <i class="bi bi-check2-square me-2"></i>
            Review Checklist
        </h6>
    </div>
    <div class="card-body">
        @if (checklist != null)
        {
            <div class="progress mb-3">
                <div class="progress-bar @GetProgressBarClass()"
                     style="width: @checklist.CompletionPercentage%">
                    @checklist.CompletionPercentage% Complete
                </div>
            </div>

            @foreach (var category in GetCategories())
            {
                <div class="checklist-category mb-3">
                    <h6 class="category-header"
                        @onclick="() => ToggleCategory(category)">
                        <i class="bi bi-@(IsCategoryExpanded(category) ? "chevron-down" : "chevron-right") me-2"></i>
                        @category
                        <span class="badge bg-secondary ms-2">
                            @GetCategoryProgress(category)
                        </span>
                    </h6>

                    @if (IsCategoryExpanded(category))
                    {
                        <div class="category-items">
                            @foreach (var item in GetItemsByCategory(category))
                            {
                                <ChecklistItemRow Item="@item"
                                                  OnCheckedChanged="HandleItemChecked" />
                            }
                        </div>
                    }
                </div>
            }

            @if (!checklist.AllRequiredItemsChecked)
            {
                <div class="alert alert-warning mt-3">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    <strong>Required items must be checked before approval.</strong>
                </div>
            }
        }
        else
        {
            <EmptyState Icon="list-check"
                        Message="No checklist template loaded"
                        Description="Checklist will appear when review is assigned" />
        }
    </div>
</div>

@code {
    [Parameter]
    public ReviewChecklistDto? Checklist { get; set; }

    [Parameter]
    public EventCallback<ChecklistItemDto> OnItemCheckedChanged { get; set; }

    [Inject]
    private IPlanReviewService PlanReviewService { get; set; } = null!;

    private ReviewChecklistDto? checklist;
    private HashSet<string> expandedCategories = new();

    protected override void OnParametersSet()
    {
        checklist = Checklist;

        if (checklist != null && !expandedCategories.Any())
        {
            // Expand all categories by default
            expandedCategories = GetCategories().ToHashSet();
        }
    }

    private IEnumerable<string> GetCategories()
    {
        return checklist?.Items
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            ?? Enumerable.Empty<string>();
    }

    private IEnumerable<ChecklistItemDto> GetItemsByCategory(string category)
    {
        return checklist?.Items
            .Where(i => i.Category == category)
            .OrderBy(i => i.SortOrder)
            ?? Enumerable.Empty<ChecklistItemDto>();
    }

    private void ToggleCategory(string category)
    {
        if (expandedCategories.Contains(category))
            expandedCategories.Remove(category);
        else
            expandedCategories.Add(category);
    }

    private bool IsCategoryExpanded(string category)
    {
        return expandedCategories.Contains(category);
    }

    private string GetCategoryProgress(string category)
    {
        var items = GetItemsByCategory(category).ToList();
        if (!items.Any()) return "0/0";

        var checkedCount = items.Count(i => i.IsChecked);
        return $"{checkedCount}/{items.Count}";
    }

    private string GetProgressBarClass()
    {
        if (checklist == null) return "bg-secondary";

        return checklist.CompletionPercentage switch
        {
            100 => "bg-success",
            >= 70 => "bg-info",
            >= 40 => "bg-warning",
            _ => "bg-danger"
        };
    }

    private async Task HandleItemChecked(ChecklistItemDto item)
    {
        if (OnItemCheckedChanged.HasDelegate)
        {
            await OnItemCheckedChanged.InvokeAsync(item);
        }
    }
}
```

**File**: `/src/PRFactory.Web/UI/Checklists/ChecklistItemRow.razor`

```razor
<div class="checklist-item form-check">
    <input type="checkbox"
           class="form-check-input"
           id="item-@Item.Id"
           checked="@Item.IsChecked"
           @onchange="HandleCheckedChange" />
    <label class="form-check-label" for="item-@Item.Id">
        <strong>@Item.Title</strong>
        @if (Item.Severity == "required")
        {
            <span class="badge bg-danger ms-2">Required</span>
        }
        <div class="small text-muted">@Item.Description</div>
    </label>
</div>

@code {
    [Parameter, EditorRequired]
    public ChecklistItemDto Item { get; set; } = null!;

    [Parameter]
    public EventCallback<ChecklistItemDto> OnCheckedChanged { get; set; }

    private async Task HandleCheckedChange(ChangeEventArgs e)
    {
        Item.IsChecked = (bool)(e.Value ?? false);

        if (OnCheckedChanged.HasDelegate)
        {
            await OnCheckedChanged.InvokeAsync(Item);
        }
    }
}
```

---

#### Day 9-10: Integration & Validation

**Update PlanReviewService**:

```csharp
public async Task<bool> ValidateChecklistAsync(Guid planReviewId)
{
    var review = await _planReviewRepo.GetByIdAsync(planReviewId);
    if (review?.Checklist == null)
        return true;  // No checklist = no validation

    return review.Checklist.AllRequiredItemsChecked;
}

public async Task ApproveWithChecklistValidationAsync(
    Guid ticketId,
    Guid reviewerId,
    string? note = null)
{
    var review = await GetReviewByReviewerAsync(ticketId, reviewerId);

    // Validate checklist if present
    if (!await ValidateChecklistAsync(review.Id))
    {
        throw new InvalidOperationException(
            "All required checklist items must be checked before approval.");
    }

    // Proceed with approval
    await ApproveAsync(ticketId, reviewerId, note);
}
```

**Update PlanReviewSection.razor**:

```razor
<div class="row">
    <div class="col-lg-8">
        <!-- Plan content -->
    </div>
    <div class="col-lg-4">
        <ReviewChecklistPanel Checklist="@currentReviewChecklist"
                              OnItemCheckedChanged="HandleChecklistItemChanged" />
    </div>
</div>
```

---

## Acceptance Criteria

- [ ] ReviewChecklist and ChecklistItem entities created
- [ ] YAML templates for web/API/database/jobs created
- [ ] ChecklistTemplateService loads templates from YAML
- [ ] ReviewChecklistPanel component displays categories
- [ ] ChecklistItemRow component with required/recommended badges
- [ ] Progress tracking (% complete)
- [ ] Approval validation blocks if required items unchecked
- [ ] Collapsible categories
- [ ] Database migration applied
- [ ] Unit tests (80%+ coverage)

---

## Files Created/Modified

### New Files (11 files)

- `/src/PRFactory.Domain/Entities/ReviewChecklist.cs`
- `/src/PRFactory.Domain/Entities/ChecklistItem.cs`
- `/src/PRFactory.Core/Application/Services/IChecklistTemplateService.cs`
- `/src/PRFactory.Infrastructure/Application/ChecklistTemplateService.cs`
- `/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor`
- `/src/PRFactory.Web/UI/Checklists/ChecklistItemRow.razor`
- `/config/checklists/web_ui.yaml`
- `/config/checklists/rest_api.yaml`
- `/config/checklists/database.yaml`
- `/config/checklists/background_jobs.yaml`
- `/tests/PRFactory.Infrastructure.Tests/Application/ChecklistTemplateServiceTests.cs`

### Modified Files (2 files)

- `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs` - Add checklist validation
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add checklist panel

---

## Success Metrics

- 95%+ of reviews use checklists
- Required items checked 98%+ of the time before approval
- Review quality consistency improves (measured by post-implementation issues)

---

**End of Feature 4: Review Checklist Templates**
