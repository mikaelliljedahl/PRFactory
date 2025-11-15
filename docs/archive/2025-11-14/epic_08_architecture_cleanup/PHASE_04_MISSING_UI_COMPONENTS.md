# Phase 4: Missing UI Components

**Duration**: 1-2 weeks
**Risk Level**: 🟢 Low
**Agent Type**: `code-implementation-specialist` (can parallelize per component)
**Dependencies**: Phase 3 merged to main

---

## Objective

Create 5 missing UI components to eliminate remaining Bootstrap spaghetti code and improve consistency.

**Why this matters**: Raw Bootstrap markup is repeated across pages, making maintenance difficult and styling inconsistent. These components complete the UI library.

---

## Success Criteria

- ✅ All 5 new components created (PageHeader, GridLayout/GridColumn, Section, InfoBox, ProgressBar)
- ✅ Each component has `.razor`, `.razor.cs`, and `.razor.css` files
- ✅ Component usage examples documented
- ✅ At least 3 pages use each new component
- ✅ Zero raw Bootstrap markup for these patterns
- ✅ All tests pass

---

## Components to Create

### 1. PageHeader Component (Priority: High)

**Location**: `src/PRFactory.Web/UI/Layout/PageHeader.razor`

**Purpose**: Consistent page title styling with optional actions/subtitle

**API**:
```csharp
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public string? Icon { get; set; }
[Parameter] public string? Subtitle { get; set; }
[Parameter] public RenderFragment? Actions { get; set; }
```

**Markup** (PageHeader.razor):
```razor
<div class="page-header">
    <div class="page-header-title">
        @if (!string.IsNullOrEmpty(Icon))
        {
            <i class="bi bi-@Icon me-2"></i>
        }
        <h1>@Title</h1>
    </div>
    @if (Actions != null)
    {
        <div class="page-header-actions">
            @Actions
        </div>
    }
</div>
@if (!string.IsNullOrEmpty(Subtitle))
{
    <p class="page-header-subtitle text-muted">@Subtitle</p>
}
```

**CSS** (PageHeader.razor.css):
```css
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
}

.page-header-title {
    display: flex;
    align-items: center;
}

.page-header-title h1 {
    margin: 0;
    font-size: 1.75rem;
    font-weight: 600;
}

.page-header-subtitle {
    margin-top: 0.5rem;
    margin-bottom: 1.5rem;
}
```

**Usage Example**:
```razor
<PageHeader Title="Tickets" Icon="ticket" Subtitle="Manage your tickets">
    <Actions>
        <a href="/tickets/create" class="btn btn-primary">Create Ticket</a>
    </Actions>
</PageHeader>
```

**Pages to Update** (use PageHeader):
- `Pages/Tickets/Index.razor`
- `Pages/Repositories/Index.razor`
- `Pages/Settings/General.razor`

---

### 2. GridLayout & GridColumn Components (Priority: High)

**Location**:
- `src/PRFactory.Web/UI/Layout/GridLayout.razor`
- `src/PRFactory.Web/UI/Layout/GridColumn.razor`

**Purpose**: Replace repetitive `<div class="row">` Bootstrap markup

**GridLayout API**:
```csharp
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string AdditionalClass { get; set; } = string.Empty;
```

**GridColumn API**:
```csharp
[Parameter] public int Width { get; set; } = 12;
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string AdditionalClass { get; set; } = string.Empty;
```

**GridLayout Markup**:
```razor
<div class="row @AdditionalClass">
    @ChildContent
</div>
```

**GridColumn Markup**:
```razor
<div class="col-md-@Width @AdditionalClass">
    @ChildContent
</div>
```

**Usage Example**:
```razor
<GridLayout>
    <GridColumn Width="6">
        <FormTextField Label="Name" @bind-Value="model.Name" />
    </GridColumn>
    <GridColumn Width="6">
        <FormTextField Label="Email" @bind-Value="model.Email" />
    </GridColumn>
</GridLayout>
```

---

### 3. Section Component (Priority: Medium)

**Location**: `src/PRFactory.Web/UI/Layout/Section.razor`

**Purpose**: Logical page sections with optional collapse/expand

**API**:
```csharp
[Parameter] public string? Title { get; set; }
[Parameter] public string? Icon { get; set; }
[Parameter] public bool Collapsible { get; set; }
[Parameter] public bool InitiallyCollapsed { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

**Markup**:
```razor
<div class="section @(Collapsible ? "section-collapsible" : "")">
    @if (!string.IsNullOrEmpty(Title))
    {
        <div class="section-header" @onclick="ToggleCollapse">
            <h3 class="section-title">
                @if (!string.IsNullOrEmpty(Icon))
                {
                    <i class="bi bi-@Icon me-2"></i>
                }
                @Title
            </h3>
            @if (Collapsible)
            {
                <i class="bi bi-chevron-@(isCollapsed ? "down" : "up")"></i>
            }
        </div>
    }
    @if (!isCollapsed)
    {
        <div class="section-content">
            @ChildContent
        </div>
    }
</div>

@code {
    private bool isCollapsed;

    protected override void OnInitialized()
    {
        isCollapsed = InitiallyCollapsed;
    }

    private void ToggleCollapse()
    {
        if (Collapsible)
        {
            isCollapsed = !isCollapsed;
        }
    }
}
```

**CSS** (Section.razor.css):
```css
.section {
    margin-bottom: 2rem;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid #dee2e6;
}

.section-collapsible .section-header {
    cursor: pointer;
}

.section-title {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 500;
}

.section-content {
    padding: 1rem 0;
}
```

---

### 4. InfoBox Component (Priority: Medium)

**Location**: `src/PRFactory.Web/UI/Alerts/InfoBox.razor`

**Purpose**: Display information lists with icon and title

**API**:
```csharp
[Parameter] public string? Title { get; set; }
[Parameter] public string? Icon { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

**Markup**:
```razor
<div class="alert alert-info">
    @if (!string.IsNullOrEmpty(Title))
    {
        <h6 class="alert-heading">
            @if (!string.IsNullOrEmpty(Icon))
            {
                <i class="bi bi-@Icon me-2"></i>
            }
            @Title
        </h6>
    }
    <div class="info-box-content">
        @ChildContent
    </div>
</div>
```

**CSS** (InfoBox.razor.css):
```css
.alert-heading {
    margin-bottom: 0.75rem;
}

.info-box-content ul {
    margin-bottom: 0;
    padding-left: 1.5rem;
}

.info-box-content li {
    margin-bottom: 0.25rem;
}
```

**Usage Example**:
```razor
<InfoBox Title="Prerequisites" Icon="info-circle">
    <ul>
        <li>Valid GitHub token</li>
        <li>Repository access</li>
        <li>Jira API token</li>
    </ul>
</InfoBox>
```

---

### 5. ProgressBar Component (Priority: Low)

**Location**: `src/PRFactory.Web/UI/Display/ProgressBar.razor`

**Purpose**: Display progress for long-running operations

**API**:
```csharp
[Parameter] public int Value { get; set; }
[Parameter] public int Max { get; set; } = 100;
[Parameter] public bool ShowLabel { get; set; } = true;
[Parameter] public ProgressVariant Variant { get; set; } = ProgressVariant.Primary;
```

**Enum** (ProgressVariant.cs):
```csharp
namespace PRFactory.Web.UI.Display;

public enum ProgressVariant
{
    Primary,
    Success,
    Warning,
    Danger,
    Info
}
```

**Markup**:
```razor
<div class="progress">
    <div class="progress-bar @GetVariantClass()"
         role="progressbar"
         style="width: @GetPercentage()%"
         aria-valuenow="@Value"
         aria-valuemin="0"
         aria-valuemax="@Max">
        @if (ShowLabel)
        {
            <span>@GetPercentage()%</span>
        }
    </div>
</div>

@code {
    private double GetPercentage() => Max > 0 ? Math.Round((double)Value / Max * 100, 1) : 0;

    private string GetVariantClass() => Variant switch
    {
        ProgressVariant.Success => "bg-success",
        ProgressVariant.Warning => "bg-warning",
        ProgressVariant.Danger => "bg-danger",
        ProgressVariant.Info => "bg-info",
        _ => "bg-primary"
    };
}
```

**Usage Example**:
```razor
<ProgressBar Value="@currentProgress" Max="100" Variant="ProgressVariant.Success" />
```

---

## Implementation Strategy

### Parallelizable Work

Each component is independent and can be implemented in parallel:

**Sub-task 1**: PageHeader + GridLayout/GridColumn (2-3 days)
- Use `code-implementation-specialist` agent
- Create components
- Update 3 pages to use them

**Sub-task 2**: Section + InfoBox (2-3 days)
- Use another `code-implementation-specialist` agent in parallel
- Create components
- Update 3 pages to use them

**Sub-task 3**: ProgressBar (1-2 days)
- Can be done last or in parallel
- Use `simple-code-implementation` agent
- Create component
- Add to at least 1 page (if applicable)

---

## Testing

### Component Unit Tests

For each component, create tests in `tests/PRFactory.Web.Tests/UI/`:

```csharp
public class PageHeaderTests
{
    [Fact]
    public void PageHeader_RendersTitle()
    {
        // Arrange
        var component = new PageHeader { Title = "Test Title" };

        // Act
        var markup = component.Render();

        // Assert
        Assert.Contains("<h1>Test Title</h1>", markup);
    }

    [Fact]
    public void PageHeader_RendersIcon_WhenProvided()
    {
        // ...
    }
}
```

### Visual Testing

For each component:
1. Create test page showing all variants
2. Verify styling correct
3. Test responsive behavior
4. Test interactivity (if applicable, like Section collapse)

---

## Validation Checklist

- [ ] PageHeader component created (`.razor`, `.razor.cs`, `.razor.css`)
- [ ] GridLayout/GridColumn components created
- [ ] Section component created
- [ ] InfoBox component created
- [ ] ProgressBar component created (+ ProgressVariant enum)
- [ ] Each component has unit tests
- [ ] At least 3 pages use each new component
- [ ] Component usage documented (examples in this doc or separate doc)
- [ ] All tests pass

---

## Deliverables

1. **Components**
   - `UI/Layout/PageHeader.razor` + `.razor.cs` + `.razor.css`
   - `UI/Layout/GridLayout.razor`
   - `UI/Layout/GridColumn.razor`
   - `UI/Layout/Section.razor` + `.razor.cs` + `.razor.css`
   - `UI/Alerts/InfoBox.razor` + `.razor.css`
   - `UI/Display/ProgressBar.razor` + `.razor.cs`
   - `UI/Display/ProgressVariant.cs`

2. **Updated Pages** (at least 3 per component)
   - Use PageHeader in Index pages
   - Use GridLayout in form pages
   - Use Section in settings pages
   - Use InfoBox in documentation/help pages
   - Use ProgressBar where applicable

3. **Tests**
   - Unit tests for each component

4. **Commit**
   ```bash
   git commit -m "feat(epic-08): add missing UI components

   - Create PageHeader component for consistent page titles
   - Create GridLayout/GridColumn for semantic grid layouts
   - Create Section component for collapsible page sections
   - Create InfoBox component for informational content
   - Create ProgressBar component for progress indication
   - Update 10+ pages to use new components
   - Add unit tests for all components

   Closes Phase 4 of EPIC 08"
   ```

---

## Phase 4 Complete!

**Next**: `PHASE_05_DTO_MAPPING.md`
