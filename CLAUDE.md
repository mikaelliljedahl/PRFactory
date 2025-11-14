# CLAUDE.md - Architecture Vision for AI Agents

> **Purpose**: This document guides AI agents (like Claude) working on the PRFactory codebase to understand what architectural decisions are INTENTIONAL and should be preserved vs. what can be simplified or removed.

---

## Table of Contents

- [Core Vision & Principles](#core-vision--principles)
  - [Flexible Agent Graph Architecture](#1-flexible-agent-graph-architecture)
  - [Multi-Platform Support](#2-multi-platform-support-is-core)
  - [LibGit2Sharp for Git Flexibility](#3-libgit2sharp-for-git-flexibility)
  - [Blazor UI Component Architecture](#4-blazor-ui-component-architecture)
- [What NOT to Simplify](#what-not-to-simplify)
- [What IS Overengineered](#what-is-overengineered)
- [Architecture Overview](#architecture-overview)
- [Multi-Platform Strategy](#multi-platform-strategy)
- [Development Guidelines](#development-guidelines)

---

## Core Vision & Principles

### 1. Flexible Agent Graph Architecture

**This is INTENTIONAL, NOT overengineering.**

The system uses a **multi-graph architecture** (RefinementGraph, PlanningGraph, ImplementationGraph, CodeReviewGraph + WorkflowOrchestrator) that enables:

- **Flexibility**: Each graph evolves independently with different retry logic and execution patterns
- **Composability**: New graphs can be added for future workflows (TestingGraph, DeploymentGraph)
- **Fault Tolerance**: Graphs suspend/resume independently with checkpoint-based recovery
- **Future Extensibility**: Supports A/B testing implementations, multi-stage approvals, parallel generation

**DO NOT:**
- Collapse multiple graphs into a single monolithic workflow
- Remove the graph abstraction layer
- Simplify the WorkflowOrchestrator to direct agent calls

**DO:**
- Add new specialized graphs for new workflow types
- Extend GraphBuilder with new patterns (loops, fan-out/fan-in)

**For Details:** See [ARCHITECTURE.md - Agent System](/home/user/PRFactory/docs/ARCHITECTURE.md#agent-system) and [IMPLEMENTATION_STATUS.md - Workflow Engine](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md#1-workflow-engine)

---

### 2. Multi-Platform Support is CORE

**This is a CORE PRODUCT FEATURE, NOT premature optimization.**

The system uses the **Strategy Pattern** (`IGitPlatformProvider`) to support multiple Git platforms (GitHub, Bitbucket, Azure DevOps, GitLab-planned) and ticket systems (Jira, Azure DevOps Work Items-planned).

**Why This Matters:**
- Enterprise customers use diverse toolchains (Azure DevOps, GitLab, not just GitHub)
- Market differentiation and vendor independence
- Customer migration support

**DO NOT:**
- Remove provider implementations thinking they're unused
- Simplify to a single GitHub-only implementation
- Remove the strategy pattern abstraction layer
- Hardcode platform-specific logic in core business logic

**DO:**
- Complete GitLab provider implementation
- Extend the provider interface as new platform capabilities are needed

**For Details:** See [IMPLEMENTATION_STATUS.md - Git Platform Providers](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md#2-git-platform-providers)

---

### 3. LibGit2Sharp for Git Flexibility

**Using LibGit2Sharp instead of CLI git commands is INTENTIONAL.**

**Why LibGit2Sharp:**

1. **Cross-Platform Compatibility**: Works identically on Windows, Linux, macOS without shell dependencies
2. **No External Dependencies**: Doesn't require git CLI to be installed
3. **Process Safety**: No shell injection vulnerabilities, no subprocess management complexity
4. **Performance**: In-process operations are faster than spawning processes
5. **Fine-Grained Control**: Programmatic access to all git operations with detailed error handling
6. **Credential Management**: Better integration with secure credential storage
7. **Docker/Container Friendly**: Simpler container images without git installation

**Key Files:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/LocalGitService.cs` (wraps LibGit2Sharp)

**DO NOT:**
- Replace LibGit2Sharp with CLI git commands
- Introduce shell command execution for git operations

**DO:**
- Continue using LibGit2Sharp for all local git operations
- Add helper methods to LocalGitService as needed
- Handle LibGit2Sharp exceptions gracefully

---

### 4. Blazor UI Component Architecture

**This is INTENTIONAL, NOT bootstrap spaghetti code.**

The Blazor UI is organized into a **clean component library** to eliminate repetitive markup and ensure consistent styling.

#### UI Library Structure

```
/PRFactory.Web/
├── Pages/                          # Routable pages (business logic)
│   ├── Tickets/
│   │   ├── Index.razor            # Ticket list page
│   │   ├── Index.razor.cs         # Code-behind for Index page
│   │   ├── Detail.razor           # Ticket detail page
│   │   └── Detail.razor.cs        # Code-behind for Detail page
│
├── Components/                     # Business/domain components
│   ├── Tickets/
│   │   ├── TicketHeader.razor
│   │   ├── TicketHeader.razor.cs  # Always separate code-behind
│   │   ├── QuestionAnswerForm.razor
│   │   └── QuestionAnswerForm.razor.cs
│
└── UI/                             # PURE UI component library
    ├── Alerts/                     # Alert and message components
    │   ├── AlertMessage.razor
    │   └── InfoBox.razor
    ├── Buttons/                    # Button components
    │   ├── LoadingButton.razor
    │   ├── IconButton.razor
    │   └── ConfirmButton.razor
    ├── Cards/                      # Card components
    │   ├── Card.razor
    │   └── CardHeader.razor
    ├── Forms/                      # Form components
    │   ├── FormField.razor
    │   └── FormFieldGroup.razor
    ├── Layout/                     # Layout and structure components
    │   ├── PageContainer.razor
    │   ├── PageHeader.razor
    │   ├── GridLayout.razor
    │   └── Section.razor
    ├── Display/                    # Display and presentation components
    │   ├── StatusBadge.razor
    │   ├── LoadingSpinner.razor
    │   ├── EmptyState.razor
    │   ├── RelativeTime.razor
    │   └── Timeline.razor
    └── Navigation/                 # Navigation components
        └── Pagination.razor
```

#### Why This Matters

**Problem We're Solving:**
```razor
<!-- BAD: Bootstrap spaghetti code repeated 50+ times -->
<div class="mb-3">
    <label class="form-label">Field Name</label>
    <InputText @bind-Value="model.Property" class="form-control" placeholder="..." />
    <ValidationMessage For="@(() => model.Property)" />
</div>

<!-- GOOD: Reusable component -->
<FormField Label="Field Name" @bind-Value="model.Property" Placeholder="..." />
```

**Benefits:**
- **DRY Principle**: UI patterns defined once, reused everywhere
- **Consistency**: Impossible to have inconsistent styling
- **Maintainability**: Change once, update everywhere
- **Type Safety**: Component parameters enforce contracts
- **Testability**: Pure UI components easy to test in isolation
- **Accessibility**: ARIA attributes and semantic HTML in one place

#### Code-Behind Pattern (MANDATORY for Business Components)

**ALWAYS separate .razor and .razor.cs files for business/domain components:**

```csharp
// TicketHeader.razor (markup only)
<div class="card-header bg-primary text-white">
    <h4 class="mb-0">
        <StatusBadge State="@Ticket.State" />
        @Ticket.Title
    </h4>
</div>

// TicketHeader.razor.cs (logic)
namespace PRFactory.Web.Components.Tickets;

public partial class TicketHeader
{
    [Parameter, EditorRequired]
    public TicketDto Ticket { get; set; } = null!;

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    private async Task OnStatusChanged(WorkflowState newState)
    {
        await TicketService.UpdateStateAsync(Ticket.Id, newState);
    }
}
```

**When to use code-behind:**
- ✅ All Pages (Index.razor.cs, Detail.razor.cs, etc.)
- ✅ All business/domain components in `/Components/*`
- ✅ Any component with dependency injection
- ✅ Any component with complex logic (>10 lines)
- ✅ Any component with multiple methods/properties

**When inline code is OK:**
- ✅ Pure UI components in `/UI/*` with minimal logic
- ✅ Simple display components with only `@code { [Parameter] ... }`
- ✅ Layout components with no business logic

#### Approved UI Libraries

**ONLY use these UI libraries:**

1. **Blazor (built-in)** - Standard components
   - `<InputText>`, `<InputNumber>`, `<InputDate>`, etc.
   - `<EditForm>`, `<ValidationSummary>`, `<ValidationMessage>`
   - `<AuthorizeView>`, `<CascadingValue>`, etc.

2. **Radzen Blazor Components** - Rich UI components
   - NuGet: `Radzen.Blazor`
   - Use for: Data grids, charts, advanced inputs, dialogs
   - Documentation: https://blazor.radzen.com/

3. **Bootstrap 5** (CSS framework only)
   - No Bootstrap JS (conflicts with Blazor)
   - Use CSS classes for layout, utilities, colors

**NEVER add these libraries:**
- ❌ **MudBlazor** - Not approved
- ❌ **Ant Design Blazor** - Not approved
- ❌ **Telerik Blazor** - Not approved (expensive, unnecessary)
- ❌ **Syncfusion Blazor** - Not approved
- ❌ **MatBlazor** - Not approved
- ❌ **Any other UI library** - Ask first!

**Why this restriction matters:**
- **Bundle Size**: Multiple UI libraries bloat client downloads
- **Consistency**: Mixing libraries creates inconsistent UX
- **Maintenance**: Fewer dependencies = fewer breaking changes
- **Licensing**: Some libraries have restrictive licenses
- **Performance**: Component libraries can conflict and slow rendering

#### NO JavaScript - Blazor Server Only

**CRITICAL: This is a Blazor Server application. NEVER add custom JavaScript files.**

**❌ FORBIDDEN:**
- Creating custom JavaScript files in `/wwwroot/js/`
- Adding `<script>` tags with custom JavaScript
- Using JavaScript interop (`IJSRuntime`) for functionality that Blazor can handle natively
- Bootstrap JavaScript (conflicts with Blazor event handling)
- jQuery or any JavaScript framework

**✅ ALLOWED (rare exceptions only):**
- Minimal JavaScript interop for browser APIs that Blazor doesn't expose
- Third-party component libraries' bundled JavaScript (Radzen, etc.)
- **Always get approval before adding any JavaScript**

**Why NO JavaScript:**
- **Blazor Server renders on the server** - JavaScript defeats the purpose
- **SignalR handles all interactivity** - No need for client-side code
- **Complexity**: Maintaining separate JavaScript and C# code paths
- **Debugging**: JavaScript errors harder to catch than C# compilation errors
- **Type Safety**: JavaScript breaks C#'s type safety guarantees
- **Performance**: SignalR roundtrips are optimized, JavaScript interop adds overhead

**Examples of what to use instead:**

| ❌ DON'T use JavaScript for: | ✅ DO use Blazor for: |
|------------------------------|----------------------|
| File downloads | `NavigationManager` with data URIs or stream downloads |
| Form validation | Blazor `<EditForm>` with `DataAnnotations` |
| DOM manipulation | Blazor component re-rendering |
| AJAX calls | `HttpClient` in C# with `@inject` |
| Event handling | Blazor `@onclick`, `@onchange`, etc. |
| Animations | CSS transitions/animations |
| Modals/dialogs | Radzen `DialogService` or Blazor component state |
| Clipboard | Blazor `IJSRuntime` only if no C# alternative exists |

**File Download Pattern (NO JavaScript):**
```csharp
// CORRECT: Pure Blazor approach for downloads
public async Task DownloadCsvAsync()
{
    var csvData = GenerateCsvData();
    var fileName = $"export-{DateTime.Now:yyyyMMdd}.csv";

    // Use NavigationManager for simple downloads
    NavigationManager.NavigateTo($"data:text/csv;charset=utf-8,{Uri.EscapeDataString(csvData)}", true);

    // OR use a file stream endpoint for larger files
    NavigationManager.NavigateTo($"/api/export/csv/{fileId}", true);
}
```

**If you absolutely need JavaScript (get approval first):**
1. Document why Blazor cannot handle it
2. Keep it minimal (< 10 lines)
3. Isolate in component-specific interop
4. Never create separate `.js` files

#### Component Design Principles

**Pure UI Components (`/UI/*`):**
1. **No business logic** - Only presentation
2. **No service injection** - Accept data via parameters
3. **Emit events** - Don't mutate parent state
4. **Fully parameterized** - All styling/behavior configurable
5. **Self-contained** - No external dependencies

**Example Pure UI Component:**
```razor
<!-- /UI/Alerts/AlertMessage.razor -->
@if (!string.IsNullOrEmpty(Message))
{
    <div class="alert alert-@AlertClass alert-dismissible fade show" role="alert">
        @if (!string.IsNullOrEmpty(Icon))
        {
            <i class="bi bi-@Icon me-2"></i>
        }
        @Message
        @if (Dismissible)
        {
            <button type="button" class="btn-close" @onclick="OnDismiss"></button>
        }
    </div>
}

@code {
    [Parameter] public string? Message { get; set; }
    [Parameter] public AlertType Type { get; set; } = AlertType.Info;
    [Parameter] public string? Icon { get; set; }
    [Parameter] public bool Dismissible { get; set; } = true;
    [Parameter] public EventCallback OnDismiss { get; set; }

    private string AlertClass => Type switch
    {
        AlertType.Success => "success",
        AlertType.Warning => "warning",
        AlertType.Danger => "danger",
        AlertType.Info => "info",
        _ => "info"
    };
}

public enum AlertType { Success, Warning, Danger, Info }
```

**Business Components (`/Components/*`):**
1. **Encapsulate domain logic** - Ticket workflows, validation, etc.
2. **Inject services** - Use DI for data access
3. **Compose UI components** - Build from pure UI components
4. **Code-behind required** - Separate .razor.cs file
5. **Stateful** - Manage component state, handle events

**Example Business Component:**
```razor
<!-- /Components/Tickets/TicketStatusSelector.razor -->
<Card Title="Change Status" Icon="arrow-repeat">
    <FormField Label="New Status">
        <InputSelect @bind-Value="selectedState" class="form-control">
            @foreach (var state in availableStates)
            {
                <option value="@state">@state</option>
            }
        </InputSelect>
    </FormField>

    <LoadingButton OnClick="HandleSubmit" IsLoading="@isSubmitting" Icon="check-circle">
        Update Status
    </LoadingButton>
</Card>

<!-- TicketStatusSelector.razor.cs -->
public partial class TicketStatusSelector
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private WorkflowState selectedState;
    private List<WorkflowState> availableStates = new();
    private bool isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        availableStates = await TicketService.GetAvailableStatesAsync(TicketId);
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            await TicketService.UpdateStateAsync(TicketId, selectedState);
            Navigation.NavigateTo($"/tickets/{TicketId}");
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

#### Blazor Server Service Architecture (CRITICAL)

**This is BLAZOR SERVER, NOT Blazor WebAssembly.**

PRFactory uses **Blazor Server**, which means the application runs on the server and maintains a SignalR connection to the browser. This has **critical architectural implications**:

##### NEVER Use HTTP Calls Within Blazor Server

**❌ WRONG PATTERN** (makes unnecessary HTTP calls within the same process):
```csharp
// TicketService.cs - BAD!
public class TicketService
{
    private readonly HttpClient _httpClient;

    public async Task<TicketUpdate> GetLatestUpdateAsync(Guid ticketId)
    {
        // WRONG: Making HTTP call to API controller in the same process!
        return await _httpClient.GetFromJsonAsync<TicketUpdate>($"/api/tickets/{ticketId}/updates/latest");
    }
}

// Component.razor.cs
public partial class MyComponent
{
    [Inject] private ITicketService TicketService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // This actually makes HTTP call -> serialization -> API controller -> deserialization
        // All within the same process! Massive overhead.
        var update = await TicketService.GetLatestUpdateAsync(ticketId);
    }
}
```

**✅ CORRECT PATTERN** (uses dependency injection directly):
```csharp
// Core/Application/Services/ITicketUpdateService.cs
public interface ITicketUpdateService
{
    Task<TicketUpdate?> GetLatestUpdateAsync(Guid ticketId);
    Task ApproveUpdateAsync(Guid ticketUpdateId, string? approvedBy);
    Task RejectUpdateAsync(Guid ticketUpdateId, string reason, bool regenerate);
}

// Infrastructure/Application/TicketUpdateService.cs
public class TicketUpdateService : ITicketUpdateService
{
    private readonly ITicketUpdateRepository _ticketUpdateRepo;
    private readonly IWorkflowOrchestrator _orchestrator;

    public TicketUpdateService(
        ITicketUpdateRepository ticketUpdateRepo,
        IWorkflowOrchestrator orchestrator)
    {
        _ticketUpdateRepo = ticketUpdateRepo;
        _orchestrator = orchestrator;
    }

    public async Task<TicketUpdate?> GetLatestUpdateAsync(Guid ticketId)
    {
        // Direct repository access - no HTTP overhead!
        return await _ticketUpdateRepo.GetLatestDraftByTicketIdAsync(ticketId);
    }

    public async Task ApproveUpdateAsync(Guid ticketUpdateId, string? approvedBy)
    {
        var update = await _ticketUpdateRepo.GetByIdAsync(ticketUpdateId);
        update.Approve();
        await _ticketUpdateRepo.UpdateAsync(update);

        // Trigger workflow
        await _orchestrator.ResumeAsync(
            update.TicketId,
            new TicketUpdateApprovedMessage(ticketUpdateId, DateTime.UtcNow, approvedBy));
    }
}

// Web/Services/TicketService.cs (Facade for Blazor components)
public class TicketService : ITicketService
{
    private readonly ITicketUpdateService _ticketUpdateService;

    public async Task<TicketUpdateDto?> GetLatestTicketUpdateAsync(Guid ticketId)
    {
        var entity = await _ticketUpdateService.GetLatestUpdateAsync(ticketId);
        return entity != null ? MapToDto(entity) : null;
    }
}

// Component.razor.cs
public partial class MyComponent
{
    [Inject] private ITicketService TicketService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Direct service call - no HTTP serialization!
        var update = await TicketService.GetLatestTicketUpdateAsync(ticketId);
    }
}
```

##### Service Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 Blazor Server Components                     │
│           (TicketUpdatePreview.razor.cs)                     │
└────────────────────────┬────────────────────────────────────┘
                         │ @inject ITicketService
                         ▼
┌─────────────────────────────────────────────────────────────┐
│          Web Layer Service (Facade Pattern)                  │
│              PRFactory.Web/Services/                         │
│               TicketService.cs                               │
│                                                               │
│   - Converts between DTOs and domain entities                │
│   - Facade for multiple application services                 │
│   - Injected into Blazor components                          │
└────────────────────────┬────────────────────────────────────┘
                         │ Injects application services
                         ▼
┌─────────────────────────────────────────────────────────────┐
│        Application Service Layer (Business Logic)            │
│         PRFactory.Infrastructure/Application/                │
│            TicketUpdateService.cs                            │
│                                                               │
│   - Encapsulates business logic                              │
│   - Coordinates multiple repositories                        │
│   - Triggers workflow orchestration                          │
│   - Shared by Blazor AND API controllers                     │
└────────────────────────┬────────────────────────────────────┘
                         │ Injects repositories & orchestrator
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                            │
│       TicketUpdateRepository, WorkflowOrchestrator          │
└─────────────────────────────────────────────────────────────┘
```

##### When to Use API Controllers

**API Controllers (`PRFactory.Api/Controllers/*`) are ONLY for external clients:**

```csharp
/// <summary>
/// API Controller for ticket updates.
///
/// ⚠️ IMPORTANT: This controller is for EXTERNAL clients only:
///   - Jira webhooks (@claude mentions)
///   - Mobile apps
///   - Third-party integrations
///   - Future public API
///
/// ❌ DO NOT use from Blazor components - inject ITicketUpdateService directly!
/// </summary>
[ApiController]
[Route("api/ticket-updates")]
public class TicketUpdatesController : ControllerBase
{
    private readonly ITicketUpdateService _ticketUpdateService;

    // Controller delegates to same service Blazor uses
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _ticketUpdateService.ApproveUpdateAsync(id, User.Identity?.Name);
        return Ok();
    }
}
```

**External clients flow:**
```
External Client (Jira webhook)
  → HTTP POST
    → API Controller
      → ITicketUpdateService (same service Blazor uses)
        → Repository
```

**Blazor Server flow:**
```
Blazor Component
  → ITicketService (Web facade)
    → ITicketUpdateService (same service API uses)
      → Repository
```

##### Benefits of This Architecture

1. **Performance**: No HTTP serialization/deserialization overhead within Blazor Server
2. **Shared Logic**: Both Blazor and API use same `ITicketUpdateService` implementation
3. **Testability**: Application services can be unit tested in isolation
4. **Consistency**: Business logic in one place, consumed by multiple clients
5. **Type Safety**: No need to convert to/from JSON within the same process
6. **Debugging**: Easier to debug direct method calls vs HTTP requests
7. **Transactions**: Can use database transactions across repository calls

##### Rules for PRFactory

**✅ DO:**
- Inject application services (e.g., `ITicketUpdateService`) from `PRFactory.Infrastructure/Application/`
- Use Web services (`PRFactory.Web/Services/`) as facades for Blazor components
- Have API controllers delegate to application services
- Keep business logic in application service layer
- Convert entities to DTOs in Web layer service facades

**❌ DO NOT:**
- Make HTTP calls from Blazor components to API controllers in same process
- Use `HttpClient` to call `/api/*` endpoints from Blazor Server
- Put business logic in API controllers (they should delegate to services)
- Put business logic in Web service facades (they should delegate to application services)
- Skip the application service layer and inject repositories directly into Web services

**File Locations:**
- Application Services: `/PRFactory.Infrastructure/Application/` (e.g., `TicketUpdateService.cs`)
- Application Service Interfaces: `/PRFactory.Core/Application/Services/` (e.g., `ITicketUpdateService.cs`)
- Web Service Facades: `/PRFactory.Web/Services/` (e.g., `TicketService.cs`)
- API Controllers: `/PRFactory.Api/Controllers/` (for external clients only)

---

#### Key Files

**UI Component Library:**
- `/home/user/PRFactory/src/PRFactory.Web/UI/` - Pure UI components

**Business Components:**
- `/home/user/PRFactory/src/PRFactory.Web/Components/` - Domain components
- `/home/user/PRFactory/src/PRFactory.Web/Pages/` - Routable pages

**Configuration:**
- `/home/user/PRFactory/src/PRFactory.Web/PRFactory.Web.csproj` - Package references

**DO NOT:**
- Create inline `<div class="card">` spaghetti code
- Add new UI component libraries without approval
- Mix business logic into pure UI components
- Skip code-behind for business components
- Duplicate UI patterns (alerts, cards, forms, etc.)

**DO:**
- Extract repetitive markup into `/UI/*` components
- Use Radzen for complex controls (grids, charts, dialogs)
- Keep pure UI components in `/UI/*`, business components in `/Components/*`
- Always use code-behind (.razor.cs) for Pages and business Components
- Compose complex UIs from simple, reusable components

---

## What NOT to Simplify

These architectural decisions should be **PRESERVED**:

### 1. Agent Graph Architecture

- **Keep all 4 graph types**: RefinementGraph, PlanningGraph, ImplementationGraph, WorkflowOrchestrator
- **Keep graph abstraction layer**: AgentGraphBase, IAgentGraph, GraphBuilder
- **Keep checkpoint-based resumption**: Critical for fault tolerance
- **Keep parallel execution support**: GitPlan + JiraPost in parallel is intentional

**Rationale**: This architecture enables future complex workflows and provides flexibility for enterprise requirements.

### 2. Multi-Platform Provider Support

- **Keep all Git platform providers**: GitHub, Bitbucket, Azure DevOps (and future GitLab)
- **Keep strategy pattern**: IGitPlatformProvider interface and implementations
- **Keep platform auto-detection**: Repository.GitPlatform-based routing
- **Keep provider-specific retry policies**: Different platforms have different rate limits

**Rationale**: Enterprise customers require multi-platform support. This is a core product feature.

### 3. Configuration Flexibility

- **Keep multi-tenant architecture**: Isolated tenant environments are critical
- **Keep tenant-level configuration**: Different tenants need different settings
- **Keep repository-level configuration**: Per-repo settings (branch names, approval rules)
- **Keep encrypted credential storage**: Security requirement

**Rationale**: SaaS/multi-tenant deployment is a core business model.

### 4. State Machine Pattern

- **Keep WorkflowState enum with 12 states**: Provides clear workflow visibility
- **Keep state transition validation**: Prevents invalid state changes
- **Keep checkpoint system**: Enables resume after failures

**Rationale**: Workflow complexity requires explicit state management.

### 5. Clean Architecture Layers

- **Keep Domain/Infrastructure/Application separation**: Enables testing and maintainability
- **Keep domain entities**: Ticket, Tenant, Repository, Checkpoint
- **Keep repository pattern**: Abstracts data access

**Rationale**: Standard architectural pattern for maintainable systems.

### 6. UI Component Library Architecture

- **Keep /UI/* component library**: Pure, reusable UI components
- **Keep code-behind pattern**: Separate .razor and .razor.cs for business components
- **Keep component organization**: /UI/ for pure components, /Components/ for business logic
- **Keep approved libraries only**: Blazor + Radzen only (NO MudBlazor, Telerik, etc.)

**Rationale**: Eliminates bootstrap spaghetti code, ensures UI consistency, maintains clean separation of concerns.

---

## What IS Overengineered

These items CAN be simplified or removed:

### 1. Redundant Middleware Layers 🔄

**Review:** Check if multiple middleware layers duplicate logging or error handling. Consolidate into fewer layers as needed. Keep essential cross-cutting concerns (authentication, authorization, tenant context).

### 2. Duplicate Configuration Keys 📋

**Review:** Audit appsettings.json files for unused keys. Remove deprecated sections and document required vs optional configuration.

### 3. Placeholder Agent Types 🤖

**Review:** Agent placeholder classes should be replaced with actual implementations. Ensure each agent has proper error handling and logging.

---

## Architecture Overview

### 3-Phase Workflow Model

PRFactory orchestrates work through three distinct phases. For complete workflow details including sequence diagrams, state transitions, and example walkthroughs, see [WORKFLOW.md](docs/WORKFLOW.md).

**Brief Overview:**
1. **Phase 1: Requirements Refinement (RefinementGraph)** - Understand what needs to be built
2. **Phase 2: Implementation Planning (PlanningGraph)** - Create reviewable implementation plan
3. **Phase 3: Code Implementation (ImplementationGraph)** - Execute approved plan (optional)

Each phase suspends the workflow for human approval before proceeding to the next phase. This ensures AI assists but humans decide.

For architectural details on how graphs execute and coordinate, see [ARCHITECTURE.md](docs/ARCHITECTURE.md#agent-system).

---

### Why Flexibility Matters for Future Enhancements

The multi-graph architecture enables future capabilities without major refactoring. For detailed future plans and timelines, see [ROADMAP.md](docs/ROADMAP.md).

**Examples of Future Workflows:**
- **CodeReviewGraph**: Automated review with iteration before PR
- **TestingGraph**: Automated test generation and execution
- **ParallelImplementationGraph**: A/B testing multiple approaches
- **ApprovalGraph**: Multi-stage approval workflows
- **ContinuousRefinementLoops**: Quality-driven improvement iterations
- **DeploymentGraph**: Automated deployment orchestration

This is why we preserve the graph abstraction - it's designed for extensibility, not just current features.

---

### Multi-Tenant Architecture

PRFactory is designed as a multi-tenant SaaS application with strict tenant isolation. For technical details on tenant isolation, security, and configuration, see [ARCHITECTURE.md - Security Architecture](docs/ARCHITECTURE.md#security-architecture).

**Why This Matters:**
- Different customers have different approval processes
- Token limits vary by customer subscription tier
- Feature flags enable gradual rollout
- Per-tenant customization without code changes

**DO NOT** remove or simplify multi-tenant isolation - this is a core business model requirement.

---

## Development Guidelines

### For AI Agents Working on This Codebase

#### .NET Development Environment Setup (CRITICAL - Claude Code Web Only)

**IMPORTANT: This section applies ONLY to Claude Code on the web, not local Claude Code.**

**Before running any `dotnet restore` or `dotnet build` commands in Claude Code web sessions, you MUST configure the NuGet proxy.**

PRFactory uses .NET 10, which is automatically installed by the SessionStart hook. However, .NET HttpClient cannot handle Claude Code web's JWT-based proxy authentication directly. A local NuGet proxy is started automatically to handle this.

**Note:** Local Claude Code environments don't have this proxy requirement and can run `dotnet` commands directly.

**Required Setup Before Any `dotnet` Commands:**

```bash
# Source the helper script created by SessionStart hook
source /tmp/dotnet-proxy-setup.sh

# Now you can run dotnet commands
dotnet restore
dotnet build
```

**What the helper script does:**
- Unsets Claude Code's proxy environment variables that .NET can't handle
- Sets HTTP_PROXY and HTTPS_PROXY to the local NuGet proxy (http://127.0.0.1:8888)

**One-liner for convenience:**
```bash
source /tmp/dotnet-proxy-setup.sh && dotnet restore && dotnet build
```

**Why This Is Required:**
- .NET HttpClient doesn't support the JWT authentication format used by Claude Code's container proxy
- Without proper proxy configuration, `dotnet restore` fails with HTTP 401 errors
- The NuGet proxy (running on port 8888) handles the outer proxy authentication transparently
- This proxy is automatically started by the SessionStart hook

**For New Claude Sessions:**
- The SessionStart hook automatically:
  1. Installs .NET SDK 10 to `/root/.dotnet`
  2. Starts the NuGet proxy on port 8888
  3. Creates `/tmp/dotnet-proxy-setup.sh` helper script
- You still need to source the helper script before each `dotnet` command

**Files Involved:**
- `/home/user/PRFactory/.claude/scripts/session-start.sh` - SessionStart hook
- `/home/user/PRFactory/.claude/scripts/nuget-proxy.py` - NuGet proxy implementation
- `/tmp/dotnet-proxy-setup.sh` - Helper script to configure environment (created at session start)

**DO:**
- Always source `/tmp/dotnet-proxy-setup.sh` before running `dotnet` commands
- Check that the NuGet proxy is running: `pgrep -f nuget-proxy.py`
- Check proxy logs if issues occur: `tail -f /tmp/nuget-proxy.log`

**DON'T:**
- Run `dotnet restore` or `dotnet build` without sourcing the helper script (in Claude Code web)
- Modify proxy environment variables manually (use the helper script)
- Try to use .NET without the NuGet proxy in Claude Code web sessions

#### File Encoding (CRITICAL)

**IMPORTANT: All source files MUST be UTF-8 without BOM (Byte Order Mark).**

The `.editorconfig` enforces `charset = utf-8` (without BOM), and CI will fail if files contain a BOM.

**Problem:**
- UTF-8 BOM is an invisible character (`﻿`) at the start of a file
- Some editors add it automatically when saving files
- The `dotnet format` tool will fail CI checks if BOM is present
- Error message: `Fix file encoding` from the CHARSET formatter

**How to Detect BOM Issues:**

```bash
# Run dotnet format to check for encoding issues
export PATH="/root/.dotnet:$PATH" && source /tmp/dotnet-proxy-setup.sh
dotnet format PRFactory.sln --verify-no-changes --verbosity diagnostic
```

If you see an error like:
```
/path/to/file.cs(1,1): error CHARSET: Fix file encoding.
```

This means the file has a UTF-8 BOM that needs to be removed.

**How to Fix BOM Issues:**

When using the Edit tool, if you see an invisible character before the first line (like `﻿using System;`), remove it:

```csharp
// BEFORE (has BOM - note the invisible character):
﻿using System;

// AFTER (correct - no BOM):
using System;
```

**Prevention:**

1. **Always use the Edit tool correctly**: When editing the first line of a file, ensure you don't preserve any BOM character
2. **Check after editing**: If you modify migration files or any .cs files, verify encoding:
   ```bash
   dotnet format PRFactory.sln --verify-no-changes
   ```
3. **Files most at risk**: Entity Framework migrations, auto-generated files, files created by external tools

**DO:**
- Remove BOM characters when detected
- Verify encoding with `dotnet format --verify-no-changes` before pushing
- Use the Edit tool to replace the first line without the BOM character

**DON'T:**
- Preserve invisible BOM characters when editing files
- Skip the `dotnet format` verification step
- Assume files are correctly encoded just because they compile

**Quick Fix Pattern:**

If `dotnet format` reports a CHARSET error:

1. Read the file and identify the BOM (invisible `﻿` character at start)
2. Use the Edit tool to replace the first line:
   ```
   old_string: ﻿using System;
   new_string: using System;
   ```
3. Verify: `dotnet format --verify-no-changes`
4. Commit: "fix: Remove UTF-8 BOM from [filename] to satisfy dotnet format"

#### When Reviewing Code

**ASK YOURSELF:**
1. Is this architectural pattern (graphs, providers, etc.) part of the core design?
2. If I simplify this, will it limit future extensibility?
3. Does this support multi-platform or multi-tenant requirements?
4. Is this overengineering or intentional flexibility?

**RED FLAGS** (Don't simplify these):
- ❌ "This graph system is complex, let's use simple if/else"
- ❌ "We only use GitHub, let's remove provider abstractions"
- ❌ "LibGit2Sharp is overkill, let's use git CLI"
- ❌ "We don't need checkpointing, workflows will finish fast"
- ❌ "Let's merge all agents into one big workflow class"
- ❌ "Let's add MudBlazor/Telerik for this one feature"
- ❌ "This inline Bootstrap markup is fine, no need for components"

**GREEN FLAGS** (Safe to simplify):
- ✅ "This middleware duplicates logging from another layer"
- ✅ "These configuration keys are unused"
- ✅ "This Bootstrap markup is repeated, let's extract a component"
- ✅ "This page needs code-behind separation"

#### When Adding Features

**DO**:
- Add new graphs for new workflow types
- Implement missing platform providers
- Add configuration options for tenant customization
- Extend existing patterns (don't reinvent)
- Extract repetitive UI markup into /UI/* components
- Use code-behind (.razor.cs) for Pages and business Components
- Use Radzen for complex UI controls (grids, charts, dialogs)

**DON'T**:
- Bypass the graph system for "quick" workflows
- Hardcode platform-specific logic in core code
- Use shell commands for git operations
- Store credentials unencrypted
- Skip state machine transitions
- Add new UI component libraries (MudBlazor, Telerik, etc.)
- Mix business logic into pure UI components in /UI/*
- Skip code-behind separation for business components

#### When Refactoring

**SAFE TO REFACTOR**:
- Extract duplicate code into shared utilities
- Improve error messages and logging
- Add type safety and validation
- Improve test coverage
- Optimize performance (without changing architecture)

**RISKY TO REFACTOR** (Verify with product owner):
- Changing graph architecture
- Removing platform providers
- Modifying state machine transitions
- Changing tenant isolation model
- Altering credential encryption

#### When Writing Tests

**Testing Framework Standards:**

PRFactory uses **xUnit** as the primary testing framework. All test assertions MUST use xUnit's native `Assert` class.

**CRITICAL: DO NOT use FluentAssertions**

FluentAssertions is **NOT ALLOWED** in this codebase. All assertions must use xUnit's standard `Assert` class.

**Approved Testing Libraries:**
- ✅ **xUnit** - Primary testing framework
- ✅ **Moq** - Mocking framework
- ✅ **Microsoft.AspNetCore.Mvc.Testing** - Integration testing
- ✅ **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for tests

**Forbidden Testing Libraries:**
- ❌ **FluentAssertions** - NOT ALLOWED (use xUnit Assert instead)
- ❌ **NUnit** - Not used in this project
- ❌ **MSTest** - Not used in this project
- ❌ **Shouldly** - Not allowed
- ❌ **Any other assertion library** - Use xUnit Assert only

**Standard xUnit Assertion Patterns:**

```csharp
// Equality
Assert.Equal(expected, actual);
Assert.NotEqual(expected, actual);

// Boolean
Assert.True(condition);
Assert.False(condition);

// Null checks
Assert.Null(obj);
Assert.NotNull(obj);

// Collections
Assert.Equal(expectedCount, collection.Count);
Assert.Contains(item, collection);
Assert.DoesNotContain(item, collection);
Assert.Empty(collection);
Assert.NotEmpty(collection);

// Exceptions
Assert.Throws<ExceptionType>(() => methodCall());
var ex = Assert.Throws<ExceptionType>(() => methodCall());
Assert.Equal("Expected message", ex.Message);

// Ranges (for approximate values)
Assert.InRange(actual, low, high);
```

**DO:**
- Use xUnit's `Assert` class for all test assertions
- Write clear, descriptive test names (e.g., `CreateTicket_WithValidData_ReturnsTicket`)
- Follow Arrange-Act-Assert pattern
- Test both success and failure paths
- Mock external dependencies

**DON'T:**
- Add FluentAssertions or any other assertion library
- Use magic strings or numbers in tests (use constants)
- Test multiple concerns in a single test
- Ignore test warnings or failures

#### Before Committing and Pushing Code

**CRITICAL: NEVER push code that doesn't compile, has failing tests, or fails formatting checks.**

Before committing and pushing any code changes, you **MUST** verify:

1. **Code Compiles Successfully**
   ```bash
   source /tmp/dotnet-proxy-setup.sh && dotnet build
   ```
   - All projects must compile without errors
   - Warnings should be investigated and fixed when possible
   - Build must succeed across all projects in the solution

2. **All Tests Pass**
   ```bash
   source /tmp/dotnet-proxy-setup.sh && dotnet test
   ```
   - All unit tests must pass
   - All integration tests must pass
   - No skipped tests without explicit reason documented in code
   - Test output must show 0 failures
   - **NEW CODE MUST HAVE 80% TEST COVERAGE MINIMUM**
   - Write tests for new classes, methods, and critical paths before pushing

3. **Code Formatting is Correct**
   ```bash
   source /tmp/dotnet-proxy-setup.sh && dotnet format PRFactory.sln --verify-no-changes
   ```
   - No formatting issues (indentation, spacing, etc.)
   - No encoding issues (UTF-8 BOM will cause failures)
   - CI will fail if this check fails locally
   - **This is mandatory** - format checks are enforced in CI/CD

**Pre-Push Checklist:**

✅ **DO** verify before every push:
- [ ] Run `dotnet build` - confirms code compiles
- [ ] Run `dotnet test` - confirms all tests pass
- [ ] Run `dotnet format --verify-no-changes` - confirms formatting is correct
- [ ] Check for compilation warnings and address critical ones
- [ ] Verify no new test failures introduced by changes
- [ ] **Ensure new code has 80% test coverage minimum** - Write unit tests for new classes, methods, and logic
- [ ] **Update existing documentation** to reflect code changes
- [ ] **Remove irrelevant or temporary documents** created during the session
- [ ] **Verify implementation matches documentation** - no deviation between what's documented and what's built

❌ **NEVER** push:
- Code that doesn't compile
- Code that causes existing tests to fail
- Code that fails `dotnet format --verify-no-changes`
- Code that breaks the build
- Files with UTF-8 BOM encoding issues
- **New code without 80% test coverage** (write tests first!)
- Untested code to production branches (without documented reason)
- **Documentation that's out of sync with implementation**
- **Temporary or work-in-progress documents** without cleaning them up
- **Features that deviate from their documentation** without updating docs first

**If Tests Fail After Your Changes:**
1. **Fix the tests or the code** - Don't commit broken code
2. **Investigate root cause** - Did your change break existing functionality?
3. **Update tests if needed** - If behavior intentionally changed, update test expectations
4. **Ask for help** - If unable to resolve, document the issue and ask for guidance

**Documentation Maintenance (CRITICAL):**

Before pushing, ensure documentation is synchronized:

1. **Update Existing Documentation** - Update `/docs/IMPLEMENTATION_STATUS.md`, `/docs/ARCHITECTURE.md`, `/docs/WORKFLOW.md`, `README.md`, `CLAUDE.md`, and inline code comments as needed
2. **Remove Temporary Documents** - Delete session-specific notes, work-in-progress documents, `.tmp`/`.draft` files. Archive valuable insights to `/docs/archive/`
3. **Verify Implementation Matches Documentation** - Code reflects what's documented, docs reflect what's coded, no feature drift

**DON'T**: Push code without updating docs, leave session-specific notes, let documentation describe old behavior after changing implementation
**DO**: Update docs in same commit, remove draft/planning documents once feature complete, ensure newcomers can understand system from docs alone

**Quick Verification Command:**
```bash
# Run this before every git push
source /tmp/dotnet-proxy-setup.sh && \
  dotnet build && \
  dotnet test && \
  dotnet format PRFactory.sln --verify-no-changes

# Only push if build, test, and format checks ALL succeed
git push
```

**Remember:** The quality bar for committed code is that it **always**:
1. Compiles successfully
2. Passes all tests
3. Passes format checks (including UTF-8 encoding without BOM)

This is non-negotiable for professional software development.

#### When Writing Documentation

Write documentation for **newcomers and future developers**, not for tracking work sessions.

**DO**: UPDATE existing documents (IMPLEMENTATION_STATUS.md, ROADMAP.md, ARCHITECTURE.md), write in present tense, focus on what exists today and why, include code examples and file paths
**DON'T**: Create new documents like "audit summaries" or "proposals", reference Claude sessions/branch names, use past tense ("we built"), write documentation as work logs

**Documentation Categories**:
- **Core Documentation** (`/docs/*.md`) - Timeless reference, architecture, current status, no session-specific information
- **Archive** (`/docs/archive/*.md`) - Session-specific summaries, proposals, historical decisions (can include dates/branches)

**When in doubt**: Ask "Will a developer in 6 months care about which session this was built in?" If no, don't include session-specific details.

#### Updating Documentation After Merges

**CRITICAL: When a feature branch is merged, update both IMPLEMENTATION_STATUS.md and ROADMAP.md.**

**Required Updates:**
1. **IMPLEMENTATION_STATUS.md** - Update "Quick Status" and "What Works Today" with new features (include PR number), add detailed sections for new components, update "What's Missing", update test coverage numbers
2. **ROADMAP.md** - Add to "Recently Completed" section (max 2-3 most recent), mark completed items with ✅ in their original sections, keep entries brief

**DON'T**: Update only one doc and forget the other, leave planned items in ROADMAP without marking them completed, add detailed implementation summaries to ROADMAP (keep brief)

---

## Quick Reference

### Core Architectural Patterns

| Pattern | Purpose | Keep? |
|---------|---------|-------|
| **Multi-Graph Architecture** | Flexible workflow orchestration | ✅ YES |
| **Strategy Pattern (Providers)** | Multi-platform support | ✅ YES |
| **State Machine** | Workflow state management | ✅ YES |
| **Clean Architecture** | Separation of concerns | ✅ YES |
| **Multi-Tenancy** | SaaS isolation | ✅ YES |
| **Checkpoint-Based Resume** | Fault tolerance | ✅ YES |
| **LibGit2Sharp** | Cross-platform git | ✅ YES |
| **UI Component Library** | Reusable, consistent UI | ✅ YES |

### File Locations

**Core Architecture**:
- Graphs: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/`
- Providers: `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/Providers/`
- Domain: `/home/user/PRFactory/src/PRFactory.Domain/`
- UI Components: `/home/user/PRFactory/src/PRFactory.Web/UI/`
- Business Components: `/home/user/PRFactory/src/PRFactory.Web/Components/`

**Documentation**:
- Implementation Status: `/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md` ⭐
- Roadmap: `/home/user/PRFactory/docs/ROADMAP.md`
- Architecture: `/home/user/PRFactory/docs/ARCHITECTURE.md`
- Workflow: `/home/user/PRFactory/docs/WORKFLOW.md`
- Setup: `/home/user/PRFactory/docs/SETUP.md`
- Documentation Index: `/home/user/PRFactory/docs/README.md`

**Configuration**:
- API: `/home/user/PRFactory/src/PRFactory.Api/appsettings.json`
- Worker: `/home/user/PRFactory/src/PRFactory.Worker/appsettings.json`

---

## Summary

**PRESERVE These Core Decisions:**
1. ✅ Multi-graph architecture (4 graph types + orchestrator)
2. ✅ Multi-platform provider support (GitHub, Bitbucket, Azure DevOps; GitLab planned)
3. ✅ LibGit2Sharp for git operations
4. ✅ Multi-tenant architecture with configuration flexibility
5. ✅ State machine pattern with checkpointing
6. ✅ Clean Architecture separation
7. ✅ UI Component Library structure (/UI/* for pure components, code-behind pattern)

**SIMPLIFY OR REMOVE:**
1. ❌ Redundant middleware layers
2. ❌ Unused configuration keys

**The Bottom Line:**

This system is designed for **enterprise flexibility** and **future extensibility**. What may look like over-engineering is actually **intentional architecture** to support:
- Multiple platforms (market requirement)
- Complex workflows (future capability)
- Multi-tenant SaaS (business model)
- Fault tolerance (production reliability)

When in doubt, **preserve flexibility** over simplicity. The architecture is optimized for long-term maintainability and feature evolution, not short-term code minimalism.

---

**Questions? Check:**
- [README.md](/home/user/PRFactory/README.md) - Project overview
- [IMPLEMENTATION_STATUS.md](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md) - ⭐ What's built vs. planned
- [ARCHITECTURE.md](/home/user/PRFactory/docs/ARCHITECTURE.md) - Detailed architecture
- [WORKFLOW.md](/home/user/PRFactory/docs/WORKFLOW.md) - Workflow details
- [ROADMAP.md](/home/user/PRFactory/docs/ROADMAP.md) - Future enhancements
- [docs/README.md](/home/user/PRFactory/docs/README.md) - Documentation index

**Still unsure?** Ask the human developer before making structural changes to core architecture.
