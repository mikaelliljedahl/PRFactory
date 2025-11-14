# PRFactory.Web Component Architecture Refactoring Plan

**Date**: 2025-11-14
**Version**: 1.0
**Status**: Proposed

---

## Executive Summary

The PRFactory.Web Blazor Server application has a **solid architectural foundation** with a well-structured UI component library containing 33 reusable components. However, there are opportunities to enhance consistency, improve CSS organization, and optimize data fetching patterns.

**The Situation:**

Unlike many Blazor applications that suffer from "Bootstrap Spaghetti Code," PRFactory demonstrates good architectural practices:
- ✅ Three-tier component structure (Pages, Business Components, UI Library)
- ✅ Code-behind pattern consistently applied
- ✅ 33 pure UI components in `/Components/UI/`
- ✅ Clean separation of concerns

**The Opportunities:**

```razor
<!-- ISSUE 1: Inline styles scattered in some components -->
<style>
    .pr-ticket-header {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
</style>

<!-- ISSUE 2: Inconsistent CSS approach -->
<!-- Some components use inline <style>, others use site.css, no CSS isolation -->

<!-- ISSUE 3: In-memory data operations -->
// Loads ALL tickets then filters in memory
var allTickets = await ticketService.GetAllTicketsAsync();
filteredTickets = allTickets.Where(t => t.Status == filter).ToList();
```

**The Solution:**

This plan focuses on **refinement and optimization** rather than wholesale refactoring:

1. **Migrate inline styles to CSS isolation** (`.razor.css` files)
2. **Create CSS isolation for all components** for better encapsulation
3. **Add missing UI components** to cover all patterns
4. **Optimize data fetching** with server-side filtering
5. **Improve consistency** in component usage

---

## Current State Analysis

### Component Organization (Current - Already Good!)

```
Components/
├── Layout/
│   ├── MainLayout.razor
│   └── NavigationMenu.razor
├── Pages/                                  # 203 Razor files (routable components)
│   ├── Tickets/                           # Ticket management pages
│   │   ├── Index.razor                    # Ticket list
│   │   ├── Detail.razor                   # Ticket detail
│   │   └── Create.razor                   # Create ticket
│   ├── Repositories/                      # Repository configuration
│   ├── Settings/                          # LLM, user management
│   ├── Admin/                             # Agent configuration
│   ├── Auth/                              # Authentication pages
│   └── Errors/                            # Error tracking
├── Components/                             # 51 business/domain components
│   ├── AgentPrompts/                      # Prompt template management
│   ├── Auth/                              # User authentication
│   ├── Errors/                            # Error display and filtering
│   ├── Layout/                            # Main layout, navigation
│   ├── Repositories/                      # Repository forms
│   ├── Settings/                          # LLM configuration
│   ├── Tenants/                           # Multi-tenant management
│   ├── Tickets/                           # Core ticket workflow
│   │   ├── PlanReview.razor              # Plan review component
│   │   ├── QuestionAnswer.razor          # Q&A component
│   │   ├── TicketHeader.razor            # Ticket header (has inline styles!)
│   │   └── TicketUpdate.razor            # Ticket updates
│   └── Workflows/                         # Workflow event tracking
└── UI/                                     # 33 pure UI components ✨
    ├── Alerts/
    │   ├── AlertMessage.razor
    │   └── DemoModeBanner.razor
    ├── Buttons/
    │   ├── LoadingButton.razor
    │   └── IconButton.razor
    ├── Cards/
    │   └── Card.razor
    ├── Checklists/
    │   ├── ReviewChecklist.razor
    │   └── ChecklistItem.razor
    ├── Comments/
    │   ├── CommentAnchor.razor
    │   └── CommentPanel.razor
    ├── Dialogs/
    │   ├── Modal.razor
    │   └── ConfirmDialog.razor
    ├── Display/
    │   ├── EmptyState.razor
    │   ├── LoadingSpinner.razor
    │   ├── StatusBadge.razor
    │   ├── RelativeTime.razor
    │   └── EventTimeline.razor
    ├── Editors/
    │   ├── MarkdownEditor.razor
    │   └── MarkdownPreview.razor
    ├── Forms/
    │   ├── FormTextField.razor
    │   ├── FormTextAreaField.razor
    │   ├── FormSelectField.razor
    │   ├── FormCheckboxField.razor
    │   └── FormPasswordField.razor
    ├── Help/
    │   └── ContextualHelp.razor
    ├── Navigation/
    │   ├── Breadcrumbs.razor
    │   └── Pagination.razor
    └── Notifications/
        └── Toast.razor
```

**Architecture Assessment: ✅ GOOD**

This is a **well-architected application** that already follows best practices. The refactoring plan focuses on polish and optimization, not structural changes.

---

## Identified Issues & Opportunities

### Issue 1: Inline Styles in Components ⚠️

**Location**: `Components/Tickets/TicketHeader.razor` (lines 88-106)

**Current Code:**
```razor
<style>
    .pr-ticket-header {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 2rem;
        border-radius: 0.5rem;
        margin-bottom: 1.5rem;
    }

    .pr-ticket-title {
        font-size: 1.75rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
    }

    /* ... more styles */
</style>
```

**Impact**:
- Styles are global (can conflict with other components)
- No CSS encapsulation
- Harder to maintain
- Not following Blazor CSS isolation pattern

**Recommended Fix**: Use CSS isolation (`.razor.css` files)

```
TicketHeader.razor
TicketHeader.razor.css     # NEW: Isolated styles
TicketHeader.razor.cs
```

**Files Affected**:
- `TicketHeader.razor` (has inline `<style>` tags)
- Any other components with inline styles (audit needed)

---

### Issue 2: No CSS Isolation Strategy 🎨

**Current Approach**: Mixed CSS strategies
1. Global CSS files (`site.css`, `app.css`, `contextual-help.css`, `markdown-editor.css`)
2. Inline `<style>` tags in some components
3. Bootstrap utility classes
4. Radzen component styles

**Problem**: Inconsistent approach, potential for style conflicts

**Recommendation**: Adopt **CSS Isolation** as standard pattern

**CSS Isolation Benefits:**
- Component styles are scoped automatically (Blazor adds unique attribute)
- No naming conflicts (no need for BEM or other naming conventions)
- Better component encapsulation
- Easier to maintain (styles live next to component)

**Migration Strategy:**
1. Create `.razor.css` files for components with complex styles
2. Move inline `<style>` tags to `.razor.css` files
3. Keep utility classes (Bootstrap) for simple spacing/layout
4. Document when to use CSS isolation vs utility classes

---

### Issue 3: In-Memory Data Filtering & Pagination 🔍

**Location**: `Pages/Tickets/Index.razor.cs` (line 88 comment)

**Current Code:**
```csharp
// TODO: This loads all tickets and filters in memory - should be server-side
var allTickets = await _ticketService.GetAllTicketsAsync(ct);

// Client-side filtering
if (!string.IsNullOrEmpty(searchQuery))
{
    filteredTickets = allTickets
        .Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

// Client-side pagination
paginatedTickets = filteredTickets.Skip((currentPage - 1) * pageSize).Take(pageSize);
```

**Impact**:
- Loads ALL tickets into memory (scalability issue)
- Network overhead for large datasets
- Slow performance with 1000+ tickets
- Memory consumption

**Recommended Fix**: Server-side filtering and pagination

```csharp
// NEW: Server-side filtering and pagination
var result = await _ticketService.GetTicketsPaginatedAsync(
    page: currentPage,
    pageSize: pageSize,
    searchQuery: searchQuery,
    status: statusFilter,
    ct: ct
);

paginatedTickets = result.Items;
totalCount = result.TotalCount;
```

**Service Layer Changes Needed:**
1. Add `GetTicketsPaginatedAsync` method to `ITicketService`
2. Add pagination DTOs (PagedResult<T>)
3. Update repository layer to support filtering/pagination
4. Use Entity Framework query optimization (IQueryable)

**Files Affected**:
- `Pages/Tickets/Index.razor.cs`
- `Services/TicketService.cs` (add pagination method)
- `Infrastructure/Application/TicketApplicationService.cs`
- `Infrastructure/Repositories/TicketRepository.cs`

---

### Issue 4: DTO Mapping in Multiple Places 🗺️

**Current Situation**: Entity-to-DTO conversion happens in:
- Web layer services (`TicketService.cs`)
- Page components (`.razor.cs` files)
- Application layer services

**Problem**: Duplicated mapping logic, inconsistent DTOs

**Recommendation**: Centralize mapping with AutoMapper or Mapperly

**Option A: AutoMapper** (runtime reflection)
```csharp
public class TicketMappingProfile : Profile
{
    public TicketMappingProfile()
    {
        CreateMap<TicketEntity, TicketDto>();
        CreateMap<CreateTicketDto, TicketEntity>();
    }
}
```

**Option B: Mapperly** (compile-time source generation - RECOMMENDED)
```csharp
[Mapper]
public partial class TicketMapper
{
    public partial TicketDto ToDto(TicketEntity entity);
    public partial TicketEntity ToEntity(CreateTicketDto dto);
}
```

**Benefits of Mapperly:**
- Zero runtime overhead (source-generated)
- Compile-time safety
- Better performance than AutoMapper
- Explicit mapping (easier to debug)

---

### Issue 5: Missing UI Components 🧩

**Components that could be added:**

#### 5.1 PageHeader Component
**Current Usage** (duplicated across pages):
```razor
<div class="d-flex justify-content-between align-items-center mb-4">
    <h1><i class="bi bi-ticket me-2"></i>Tickets</h1>
    <button class="btn btn-primary">Create Ticket</button>
</div>
```

**Proposed Component:**
```razor
<PageHeader Title="Tickets" Icon="ticket">
    <Actions>
        <button class="btn btn-primary">Create Ticket</button>
    </Actions>
</PageHeader>
```

**Files to Create:**
- `UI/Layout/PageHeader.razor`
- `UI/Layout/PageHeader.razor.css`
- `UI/Layout/PageHeader.razor.cs`

---

#### 5.2 GridLayout / GridColumn Components
**Current Usage**:
```razor
<div class="row">
    <div class="col-md-6"><!-- Content --></div>
    <div class="col-md-6"><!-- Content --></div>
</div>
```

**Proposed Components:**
```razor
<GridLayout>
    <GridColumn Width="6"><!-- Content --></GridColumn>
    <GridColumn Width="6"><!-- Content --></GridColumn>
</GridLayout>
```

**Files to Create:**
- `UI/Layout/GridLayout.razor`
- `UI/Layout/GridColumn.razor`

---

#### 5.3 Section Component
**Purpose**: Logical page sections with consistent spacing

```razor
<Section Title="Configuration" Collapsible="true">
    <!-- Section content -->
</Section>
```

**Files to Create:**
- `UI/Layout/Section.razor`
- `UI/Layout/Section.razor.css`

---

#### 5.4 InfoBox Component
**Purpose**: Display information lists (like the Portal project)

```razor
<InfoBox Title="Prerequisites" Icon="info-circle">
    <ul>
        <li>Valid GitHub token</li>
        <li>Repository access</li>
    </ul>
</InfoBox>
```

**Files to Create:**
- `UI/Alerts/InfoBox.razor`
- `UI/Alerts/InfoBox.razor.css`

---

#### 5.5 ProgressBar Component
**Purpose**: Display progress for long-running operations

```razor
<ProgressBar Value="@currentProgress" Max="100" Variant="ProgressVariant.Success" />
```

**Files to Create:**
- `UI/Display/ProgressBar.razor`
- `UI/Display/ProgressBarVariant.cs`

---

### Issue 6: Inconsistent Error Handling Display 🚨

**Current Approach**: Mix of:
- `AlertMessage` component (good!)
- Inline `<div class="alert">` tags
- Custom error display markup

**Recommendation**: Standardize on `AlertMessage` component everywhere

**Audit Needed**: Search for inline alert divs and replace with component

---

## Proposed Refactoring Plan

### Phase 1: CSS Organization & Isolation (Week 1) - **Priority P0**

**Goal**: Migrate inline styles to CSS isolation and establish CSS standards

**Tasks:**

1. **Migrate TicketHeader inline styles to CSS isolation**
   - Create `TicketHeader.razor.css`
   - Move all `<style>` content to `.razor.css`
   - Remove `<style>` tags from `.razor` file
   - Test for any style regressions

2. **Audit all components for inline styles**
   - Run grep/search for `<style>` tags in `.razor` files
   - Create list of components with inline styles
   - Migrate each to CSS isolation

3. **Create CSS isolation for existing UI components**
   - Add `.razor.css` files for components with complex styling
   - Examples: `Card.razor.css`, `Modal.razor.css`, `EventTimeline.razor.css`
   - Keep simple components using Bootstrap utilities

4. **Document CSS strategy**
   - Create `docs/CSS-Strategy.md`
   - Define when to use CSS isolation vs Bootstrap utilities
   - Provide examples

**Success Metrics:**
- Zero `<style>` tags in `.razor` files
- All complex components have `.razor.css` files
- CSS strategy documented

**Files to Create/Modify:**
- `Components/Tickets/TicketHeader.razor.css` (NEW)
- `Components/Tickets/TicketHeader.razor` (MODIFY - remove `<style>`)
- `UI/Cards/Card.razor.css` (NEW)
- `UI/Dialogs/Modal.razor.css` (NEW)
- `UI/Display/EventTimeline.razor.css` (NEW)
- `docs/CSS-Strategy.md` (NEW)

---

### Phase 2: Data Fetching Optimization (Week 2) - **Priority P1**

**Goal**: Implement server-side filtering and pagination for scalability

**Tasks:**

1. **Create pagination infrastructure**
   ```csharp
   // Shared/DTOs/PagedResult.cs
   public class PagedResult<T>
   {
       public List<T> Items { get; set; } = new();
       public int TotalCount { get; set; }
       public int Page { get; set; }
       public int PageSize { get; set; }
       public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
   }

   // Shared/DTOs/PaginationParams.cs
   public class PaginationParams
   {
       public int Page { get; set; } = 1;
       public int PageSize { get; set; } = 20;
       public string? SearchQuery { get; set; }
       public string? SortBy { get; set; }
       public bool Descending { get; set; }
   }
   ```

2. **Update Repository Layer**
   ```csharp
   // Infrastructure/Repositories/TicketRepository.cs
   public async Task<PagedResult<Ticket>> GetTicketsPagedAsync(
       PaginationParams paginationParams,
       TicketStatus? statusFilter = null,
       CancellationToken ct = default)
   {
       var query = _context.Tickets.AsQueryable();

       // Apply filters
       if (!string.IsNullOrEmpty(paginationParams.SearchQuery))
       {
           query = query.Where(t => t.Title.Contains(paginationParams.SearchQuery));
       }

       if (statusFilter.HasValue)
       {
           query = query.Where(t => t.Status == statusFilter.Value);
       }

       // Get total count
       var totalCount = await query.CountAsync(ct);

       // Apply sorting
       query = paginationParams.SortBy switch
       {
           "title" => paginationParams.Descending
               ? query.OrderByDescending(t => t.Title)
               : query.OrderBy(t => t.Title),
           "created" => paginationParams.Descending
               ? query.OrderByDescending(t => t.CreatedAt)
               : query.OrderBy(t => t.CreatedAt),
           _ => query.OrderByDescending(t => t.CreatedAt)
       };

       // Apply pagination
       var items = await query
           .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
           .Take(paginationParams.PageSize)
           .ToListAsync(ct);

       return new PagedResult<Ticket>
       {
           Items = items,
           TotalCount = totalCount,
           Page = paginationParams.Page,
           PageSize = paginationParams.PageSize
       };
   }
   ```

3. **Update Service Layer**
   ```csharp
   // Services/ITicketService.cs
   Task<PagedResult<TicketDto>> GetTicketsPagedAsync(
       PaginationParams paginationParams,
       TicketStatus? statusFilter = null,
       CancellationToken ct = default);
   ```

4. **Update Pages to use pagination**
   ```csharp
   // Pages/Tickets/Index.razor.cs
   private PagedResult<TicketDto>? pagedTickets;

   protected override async Task OnInitializedAsync()
   {
       var paginationParams = new PaginationParams
       {
           Page = currentPage,
           PageSize = pageSize,
           SearchQuery = searchQuery
       };

       pagedTickets = await _ticketService.GetTicketsPagedAsync(
           paginationParams,
           statusFilter,
           ct
       );
   }
   ```

**Files to Create/Modify:**
- `Shared/DTOs/PagedResult.cs` (NEW)
- `Shared/DTOs/PaginationParams.cs` (NEW)
- `Infrastructure/Repositories/TicketRepository.cs` (MODIFY)
- `Infrastructure/Application/TicketApplicationService.cs` (MODIFY)
- `Services/ITicketService.cs` (MODIFY)
- `Services/TicketService.cs` (MODIFY)
- `Pages/Tickets/Index.razor.cs` (MODIFY)

**Success Metrics:**
- No more `GetAllTicketsAsync` calls in pages
- Page load time improved for large datasets
- Memory usage reduced

---

### Phase 3: Missing UI Components (Week 3) - **Priority P2**

**Goal**: Add missing UI components to cover all common patterns

**Tasks:**

1. **Create PageHeader component**
   ```razor
   <!-- UI/Layout/PageHeader.razor -->
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
       <p class="page-header-subtitle">@Subtitle</p>
   }

   @code {
       [Parameter] public string Title { get; set; } = string.Empty;
       [Parameter] public string? Icon { get; set; }
       [Parameter] public string? Subtitle { get; set; }
       [Parameter] public RenderFragment? Actions { get; set; }
   }
   ```

2. **Create GridLayout components**
   ```razor
   <!-- UI/Layout/GridLayout.razor -->
   <div class="row @AdditionalClass">
       @ChildContent
   </div>

   @code {
       [Parameter] public RenderFragment? ChildContent { get; set; }
       [Parameter] public string AdditionalClass { get; set; } = string.Empty;
   }

   <!-- UI/Layout/GridColumn.razor -->
   <div class="col-md-@Width @AdditionalClass">
       @ChildContent
   </div>

   @code {
       [Parameter] public int Width { get; set; } = 12;
       [Parameter] public RenderFragment? ChildContent { get; set; }
       [Parameter] public string AdditionalClass { get; set; } = string.Empty;
   }
   ```

3. **Create Section component**
   ```razor
   <!-- UI/Layout/Section.razor -->
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
       [Parameter] public string? Title { get; set; }
       [Parameter] public string? Icon { get; set; }
       [Parameter] public bool Collapsible { get; set; }
       [Parameter] public RenderFragment? ChildContent { get; set; }

       private bool isCollapsed = false;

       private void ToggleCollapse()
       {
           if (Collapsible)
           {
               isCollapsed = !isCollapsed;
           }
       }
   }
   ```

4. **Create InfoBox component**
   ```razor
   <!-- UI/Alerts/InfoBox.razor -->
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
       @ChildContent
   </div>

   @code {
       [Parameter] public string? Title { get; set; }
       [Parameter] public string? Icon { get; set; }
       [Parameter] public RenderFragment? ChildContent { get; set; }
   }
   ```

5. **Create ProgressBar component**
   ```razor
   <!-- UI/Display/ProgressBar.razor -->
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
       [Parameter] public int Value { get; set; }
       [Parameter] public int Max { get; set; } = 100;
       [Parameter] public bool ShowLabel { get; set; } = true;
       [Parameter] public ProgressVariant Variant { get; set; } = ProgressVariant.Primary;

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

**Files to Create:**
- `UI/Layout/PageHeader.razor` + `.razor.cs` + `.razor.css`
- `UI/Layout/GridLayout.razor`
- `UI/Layout/GridColumn.razor`
- `UI/Layout/Section.razor` + `.razor.cs` + `.razor.css`
- `UI/Alerts/InfoBox.razor` + `.razor.css`
- `UI/Display/ProgressBar.razor` + `.razor.cs`
- `UI/Display/ProgressVariant.cs` (enum)

**Success Metrics:**
- All common UI patterns have corresponding components
- Pages use components instead of raw Bootstrap markup
- Consistent look and feel

---

### Phase 4: DTO Mapping Centralization (Week 4) - **Priority P2**

**Goal**: Centralize entity-to-DTO mapping with Mapperly

**Tasks:**

1. **Install Mapperly NuGet package**
   ```bash
   dotnet add package Riok.Mapperly
   ```

2. **Create mapping interfaces**
   ```csharp
   // Infrastructure/Mapping/ITicketMapper.cs
   [Mapper]
   public partial class TicketMapper
   {
       public partial TicketDto ToDto(Ticket entity);
       public partial List<TicketDto> ToDtoList(List<Ticket> entities);
       public partial Ticket ToEntity(CreateTicketDto dto);
       public partial void UpdateEntity(UpdateTicketDto dto, Ticket entity);
   }
   ```

3. **Register mappers in DI**
   ```csharp
   // Program.cs
   builder.Services.AddSingleton<TicketMapper>();
   builder.Services.AddSingleton<RepositoryMapper>();
   builder.Services.AddSingleton<WorkflowEventMapper>();
   ```

4. **Update services to use mappers**
   ```csharp
   // Services/TicketService.cs
   public class TicketService(
       ITicketApplicationService ticketApplicationService,
       TicketMapper mapper) : ITicketService
   {
       public async Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default)
       {
           var ticket = await ticketApplicationService.GetTicketByIdAsync(id, ct);
           return mapper.ToDto(ticket);
       }
   }
   ```

**Files to Create/Modify:**
- `Infrastructure/Mapping/TicketMapper.cs` (NEW)
- `Infrastructure/Mapping/RepositoryMapper.cs` (NEW)
- `Infrastructure/Mapping/WorkflowEventMapper.cs` (NEW)
- `Services/TicketService.cs` (MODIFY - use mapper)
- `Program.cs` (MODIFY - register mappers)

**Success Metrics:**
- All entity-to-DTO conversions use Mapperly
- No manual mapping code in services
- Compile-time mapping validation

---

### Phase 5: Page Refactoring & Polish (Week 5) - **Priority P3**

**Goal**: Refactor pages to use new components and standardize patterns

**Tasks:**

1. **Audit pages for inline Bootstrap markup**
   - Find pages with raw `<div class="row">` patterns
   - Find pages with raw `<div class="alert">` patterns
   - Find pages with duplicated page headers

2. **Refactor high-traffic pages first**
   - `Pages/Tickets/Index.razor` - Use PageHeader, GridLayout
   - `Pages/Tickets/Detail.razor` - Use Section components
   - `Pages/Repositories/Index.razor` - Use standardized patterns

3. **Standardize error display**
   - Replace all inline `<div class="alert">` with `<AlertMessage>`
   - Ensure consistent error handling across all pages

4. **Document component usage**
   - Update CLAUDE.md with component guidelines
   - Add component usage examples
   - Create component showcase page (optional)

**Files to Modify:**
- Multiple page files (`.razor`, `.razor.cs`)
- `CLAUDE.md` (add component usage guidelines)

**Success Metrics:**
- All pages use component library consistently
- No raw Bootstrap markup for common patterns
- Documented component usage guidelines

---

## Component Naming Conventions

### Existing Conventions (Already Good!) ✅

1. **Component Names**: PascalCase, descriptive (e.g., `FormTextField`, `LoadingSpinner`, `StatusBadge`)
2. **Parameter Names**: PascalCase (e.g., `Title`, `Icon`, `IsLoading`)
3. **Event Callbacks**: Prefix with `On` (e.g., `OnClick`, `OnDismiss`, `OnSubmit`)
4. **Folder Names**: PascalCase, categorized by purpose (e.g., `Alerts/`, `Buttons/`, `Forms/`)
5. **Code-behind**: Consistent `.razor` + `.razor.cs` separation

**Continue following these conventions!**

### New Convention: CSS Isolation

**Pattern:**
```
ComponentName.razor
ComponentName.razor.cs
ComponentName.razor.css        # NEW: Scoped styles
```

**When to use CSS isolation vs Bootstrap utilities:**

**Use CSS Isolation for:**
- Complex component-specific styles
- Custom animations/transitions
- Component variants with multiple styles
- Any styles with more than 3-4 CSS properties

**Use Bootstrap Utilities for:**
- Simple spacing (mb-3, p-2)
- Simple layout (d-flex, justify-content-between)
- Simple colors (text-muted, bg-light)
- Simple sizing (w-100, h-100)

---

## Implementation Strategy Summary

| Phase | Duration | Priority | Focus | Impact |
|-------|----------|----------|-------|--------|
| **Phase 1** | Week 1 | P0 | CSS Organization | High - Improves maintainability |
| **Phase 2** | Week 2 | P1 | Data Fetching | High - Improves scalability |
| **Phase 3** | Week 3 | P2 | Missing Components | Medium - Improves consistency |
| **Phase 4** | Week 4 | P2 | DTO Mapping | Medium - Reduces duplication |
| **Phase 5** | Week 5 | P3 | Page Refactoring | Low - Polish |

**Total Duration**: 5 weeks
**Total Effort**: ~2-3 developer weeks (assuming 1-2 developers)

---

## Example: Before/After Page Refactoring

### Tickets/Index.razor - Before

```razor
@page "/tickets"
@using PRFactory.Web.Services
@inject ITicketService TicketService

<div class="container-fluid">
    <div class="row">
        <div class="col-md-12">
            <div class="d-flex justify-content-between align-items-center mb-4">
                <h1><i class="bi bi-ticket me-2"></i>Tickets</h1>
                <a href="/tickets/create" class="btn btn-primary">
                    <i class="bi bi-plus-lg me-2"></i>Create Ticket
                </a>
            </div>

            @if (!string.IsNullOrEmpty(errorMessage))
            {
                <div class="alert alert-danger alert-dismissible fade show" role="alert">
                    @errorMessage
                    <button type="button" class="btn-close" @onclick="() => errorMessage = string.Empty"></button>
                </div>
            }

            @if (isLoading)
            {
                <div class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            }
            else if (tickets == null || !tickets.Any())
            {
                <div class="text-center p-5">
                    <i class="bi bi-inbox" style="font-size: 4rem; color: #ccc;"></i>
                    <h3 class="text-muted mt-3">No tickets found</h3>
                    <p class="text-muted">Create your first ticket to get started</p>
                </div>
            }
            else
            {
                <div class="row">
                    <div class="col-md-12">
                        <div class="card">
                            <div class="card-body">
                                <!-- Ticket list -->
                            </div>
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
</div>

@code {
    private List<TicketDto>? tickets;
    private string? errorMessage;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        // Loads ALL tickets (scalability issue!)
        tickets = await TicketService.GetAllTicketsAsync();
        isLoading = false;
    }
}
```

---

### Tickets/Index.razor - After

```razor
@page "/tickets"
@using PRFactory.Web.Services
@inject ITicketService TicketService

<PageHeader Title="Tickets" Icon="ticket" Subtitle="Manage your tickets">
    <Actions>
        <a href="/tickets/create" class="btn btn-primary">
            <i class="bi bi-plus-lg me-2"></i>Create Ticket
        </a>
    </Actions>
</PageHeader>

<AlertMessage Type="AlertType.Error" Message="@errorMessage"
              OnDismiss="() => errorMessage = string.Empty" />

<LoadingSpinner Show="@isLoading" Centered="true" Message="Loading tickets..." />

@if (!isLoading)
{
    @if (pagedTickets?.Items == null || !pagedTickets.Items.Any())
    {
        <EmptyState Icon="inbox"
                    Title="No tickets found"
                    Message="Create your first ticket to get started" />
    }
    else
    {
        <Card>
            <Body>
                <!-- Ticket list -->

                <Pagination CurrentPage="@pagedTickets.Page"
                           TotalPages="@pagedTickets.TotalPages"
                           OnPageChanged="HandlePageChanged" />
            </Body>
        </Card>
    }
}

@code {
    private PagedResult<TicketDto>? pagedTickets;
    private string? errorMessage;
    private bool isLoading = true;
    private int currentPage = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadTicketsAsync();
    }

    private async Task LoadTicketsAsync()
    {
        isLoading = true;

        var paginationParams = new PaginationParams
        {
            Page = currentPage,
            PageSize = 20
        };

        // Server-side pagination!
        pagedTickets = await TicketService.GetTicketsPagedAsync(paginationParams);

        isLoading = false;
    }

    private async Task HandlePageChanged(int page)
    {
        currentPage = page;
        await LoadTicketsAsync();
    }
}
```

**Benefits:**
- **Cleaner markup** - Semantic component names instead of Bootstrap divs
- **Better performance** - Server-side pagination instead of loading all tickets
- **Consistent UI** - Using component library throughout
- **Easier maintenance** - Component changes affect all pages
- **Better readability** - Clear intent with component names

---

## Testing Strategy

### Component Testing

**For new components created in Phase 3:**

1. **Manual Testing**:
   - Create test page showcasing all component variants
   - Test all parameter combinations
   - Verify responsive behavior (mobile, tablet, desktop)
   - Test keyboard navigation
   - Test screen reader compatibility

2. **Integration Testing**:
   - Test components in real page contexts
   - Verify event callbacks work correctly
   - Test RenderFragment parameters
   - Verify CSS isolation works (no style leaks)

### CSS Migration Testing (Phase 1)

**Critical: Visual regression testing**

1. **Before migration**:
   - Take screenshots of all pages
   - Document current styling

2. **After migration**:
   - Compare screenshots (should be identical)
   - Verify no visual changes for users
   - Test in multiple browsers (Chrome, Firefox, Edge)

### Data Fetching Testing (Phase 2)

**Performance testing:**

1. **Load Testing**:
   - Test with 10 tickets
   - Test with 100 tickets
   - Test with 1,000 tickets
   - Test with 10,000 tickets

2. **Metrics to Track**:
   - Page load time
   - API response time
   - Memory usage
   - Database query performance

**Expected Results:**
- No degradation with large datasets
- Consistent page load time regardless of total ticket count
- Lower memory usage on client

---

## Migration Guidelines

### For Developers

**Daily Workflow:**

1. **Before starting work**:
   - Pull latest changes
   - Review this document for guidelines

2. **When creating new components**:
   - Check if component already exists in `/UI/` folder
   - If creating new component, follow naming conventions
   - Add `.razor.css` file for complex styling
   - Use Bootstrap utilities for simple spacing/layout

3. **When creating new pages**:
   - Use `PageHeader` component for page title
   - Use `AlertMessage` for all alerts
   - Use `LoadingSpinner` for loading states
   - Use `EmptyState` for empty data
   - Use `Card` for content sections
   - Use pagination services (not in-memory filtering)

4. **Commit Guidelines**:
   ```
   refactor(ui): migrate TicketHeader to CSS isolation

   - Move inline styles to TicketHeader.razor.css
   - Remove <style> tags from TicketHeader.razor
   - No visual changes
   ```

### Component Usage Quick Reference

```razor
@* Page Structure *@
<PageHeader Title="Page Title" Icon="icon" Subtitle="Description">
    <Actions>
        <button class="btn btn-primary">Action</button>
    </Actions>
</PageHeader>

@* Alerts *@
<AlertMessage Type="AlertType.Success" Message="@successMessage" />
<AlertMessage Type="AlertType.Error" Message="@errorMessage" Dismissible="true" />

@* Cards *@
<Card Title="Card Title" Icon="gear">
    <Body>Content here</Body>
</Card>

@* Form Fields (Already exists!) *@
<FormTextField Label="Name" @bind-Value="model.Name" Required="true" />
<FormTextAreaField Label="Description" @bind-Value="model.Description" Rows="5" />
<FormSelectField Label="Status" @bind-Value="model.Status" />

@* Layout *@
<GridLayout>
    <GridColumn Width="6">Content</GridColumn>
    <GridColumn Width="6">Content</GridColumn>
</GridLayout>

<Section Title="Configuration" Icon="gear" Collapsible="true">
    <!-- Section content -->
</Section>

@* Display *@
<LoadingSpinner Show="@isLoading" Centered="true" Message="Loading..." />
<EmptyState Icon="inbox" Title="No data" Message="Create something to get started" />
<StatusBadge Status="@ticket.Status" />
<RelativeTime DateTime="@ticket.CreatedAt" />
<ProgressBar Value="@progress" Max="100" Variant="ProgressVariant.Success" />

@* Buttons (Already exists!) *@
<LoadingButton Text="Submit" Icon="save" IsLoading="@isLoading" OnClick="HandleSubmit" />
<IconButton Icon="trash" Variant="ButtonVariant.Danger" OnClick="HandleDelete" />

@* Dialogs (Already exists!) *@
<Modal Title="Confirm" IsOpen="@showModal" OnClose="() => showModal = false">
    <Body>Are you sure?</Body>
</Modal>

<ConfirmDialog Title="Delete Ticket"
               Message="Are you sure you want to delete this ticket?"
               OnConfirm="HandleDelete"
               OnCancel="() => showDialog = false" />
```

---

## Success Metrics & KPIs

### Code Quality Metrics

**Target Goals:**

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Inline `<style>` tags | 3-5 | 0 | Grep count |
| CSS isolation files | 0 | 20+ | File count |
| In-memory filtering | Yes | No | Code review |
| Manual DTO mapping | Yes | No | Code review |
| Missing components | 5 | 0 | Component audit |
| Page consistency | 70% | 95% | Visual audit |

### Performance Metrics

**Target Goals:**

| Metric | Current | Target | Tool |
|--------|---------|--------|------|
| Ticket list load time (1000 items) | ~2-3s | <500ms | Browser DevTools |
| Memory usage (ticket list) | High | Low | Browser DevTools |
| CSS file size | Medium | Small | Build output |
| Component render time | <50ms | <50ms | Blazor profiler |

### Component Library Metrics

**Target Goals:**

| Metric | Current | Target |
|--------|---------|--------|
| UI components | 33 | 38 |
| Pages using components | 60% | 90% |
| Component reusability | High | Very High |
| CSS isolation adoption | 0% | 80% |

---

## Risks & Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| **CSS migration breaks styling** | High | Low | Screenshot comparison, thorough testing, incremental rollout |
| **Performance regression** | High | Low | Performance testing, benchmark before/after |
| **Developer learning curve** | Medium | Medium | Documentation, examples, code review |
| **Mapperly compile errors** | Medium | Low | Incremental adoption, test each mapper |
| **Breaking changes to existing components** | High | Low | Semantic versioning, backwards compatibility |
| **CSS specificity conflicts** | Low | Low | Use CSS isolation (automatic scoping) |

---

## Maintenance & Governance

### Component Library Ownership

**Component Review Process:**
1. All new UI components require 2+ developer reviews
2. Components must include XML documentation comments
3. Components must follow naming conventions
4. Components must have usage examples in CLAUDE.md

### Adding New Components

**Guidelines:**
1. Check if existing component can be extended first
2. Ensure pattern appears 3+ times in codebase (DRY principle)
3. Design API with team (parameter names, RenderFragments)
4. Implement with `.razor` + `.razor.cs` + `.razor.css` (if needed)
5. Add XML documentation
6. Update CLAUDE.md with usage examples
7. Update this document

### CSS Strategy

**When to use CSS isolation:**
- Component has 5+ CSS rules
- Component needs custom animations
- Component has multiple visual variants
- Styles are specific to component (not general Bootstrap)

**When to use Bootstrap utilities:**
- Simple spacing (mb-3, p-2, mt-4)
- Simple layout (d-flex, justify-content-between)
- Simple colors (text-muted, bg-light)
- Simple sizing (w-100, h-auto)

---

## Appendix: Additional Component APIs

### PageHeader (New)

```csharp
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public string? Icon { get; set; }
[Parameter] public string? Subtitle { get; set; }
[Parameter] public RenderFragment? Actions { get; set; }
```

### GridLayout / GridColumn (New)

```csharp
// GridLayout
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string AdditionalClass { get; set; } = string.Empty;

// GridColumn
[Parameter] public int Width { get; set; } = 12;
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string AdditionalClass { get; set; } = string.Empty;
```

### Section (New)

```csharp
[Parameter] public string? Title { get; set; }
[Parameter] public string? Icon { get; set; }
[Parameter] public bool Collapsible { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

### InfoBox (New)

```csharp
[Parameter] public string? Title { get; set; }
[Parameter] public string? Icon { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

### ProgressBar (New)

```csharp
[Parameter] public int Value { get; set; }
[Parameter] public int Max { get; set; } = 100;
[Parameter] public bool ShowLabel { get; set; } = true;
[Parameter] public ProgressVariant Variant { get; set; } = ProgressVariant.Primary;
```

---

## Conclusion

PRFactory.Web already has a **strong architectural foundation** with excellent component organization and a well-designed UI library. This refactoring plan focuses on **polish and optimization** rather than wholesale changes.

**Key Improvements:**
- ✅ **CSS Isolation** - Move from inline styles to scoped `.razor.css` files
- ✅ **Server-side Pagination** - Eliminate in-memory filtering for scalability
- ✅ **Missing Components** - Add 5 new components (PageHeader, GridLayout, Section, InfoBox, ProgressBar)
- ✅ **DTO Mapping** - Centralize with Mapperly for consistency
- ✅ **Consistency** - Standardize component usage across all pages

**Long-term Benefits:**
- Easier maintenance (scoped CSS, centralized mapping)
- Better performance (server-side filtering)
- More consistent UI (component library usage)
- Faster development (reusable components)
- Better scalability (pagination infrastructure)

**Next Steps:**
1. Review and approve this plan
2. Prioritize phases based on business needs
3. Start Phase 1: CSS Organization & Isolation
4. Track progress and iterate
5. Update CLAUDE.md with new guidelines

---

**Document Version**: 1.0
**Date**: 2025-11-14
**Status**: Proposed - Awaiting Review

**Note**: This plan assumes PRFactory continues to grow. If the application remains small (<1000 tickets total), Phase 2 (pagination) may be deferred. Phases 1, 3, and 5 provide the most immediate value for code quality and maintainability.
