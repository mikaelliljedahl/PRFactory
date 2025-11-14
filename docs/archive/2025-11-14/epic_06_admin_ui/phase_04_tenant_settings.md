# Epic 06 - Phase 4: Tenant Settings UI

**Status:** 📋 Ready for Implementation
**Estimated Effort:** 3-5 days (24-40 hours)
**Priority:** P0 - Critical (Blocks tenant self-configuration)
**Dependencies:** Phase 1 Complete ✅

---

## Table of Contents

- [Overview](#overview)
- [Goals & Success Criteria](#goals--success-criteria)
- [Architecture Overview](#architecture-overview)
- [Settings Categories](#settings-categories)
- [Page Specification](#page-specification)
- [Component Specifications](#component-specifications)
- [Service Layer Integration](#service-layer-integration)
- [Security & Authorization](#security--authorization)
- [Testing Strategy](#testing-strategy)
- [Implementation Checklist](#implementation-checklist)
- [UI Mockups](#ui-mockups)

---

## Overview

### What We're Building

A single-page tabbed interface that allows tenant Owners to configure tenant-wide settings:
- **General Tab**: View read-only tenant information
- **Workflow Settings Tab**: Configure workflow behavior (auto-implementation, retries, timeouts, allowed repositories)
- **Code Review Settings Tab**: Configure code review behavior (enable/disable, max iterations, auto-approve)
- **LLM Provider Assignment Tab**: Assign LLM providers to agent roles (Analysis, Planning, Implementation, CodeReview)

### Why This Matters

**Business Impact:**
- Enables tenant self-service configuration (no database access required)
- Allows customization of workflow behavior per tenant
- Supports tenant-specific LLM strategy
- Validates product flexibility for enterprise customers

**Technical Foundation:**
- Demonstrates tabbed UI pattern in Blazor
- Shows JSON configuration management
- Establishes tenant configuration patterns for future settings

---

## Goals & Success Criteria

### Must Have (Phase 4)

- [x] **Service Layer Complete** - `TenantConfigurationService` with get/update operations ✅
- [ ] **General Tab** - Display read-only tenant information
- [ ] **Workflow Settings Tab** - Configure workflow behavior
- [ ] **Code Review Settings Tab** - Configure code review behavior
- [ ] **LLM Provider Assignment Tab** - Assign providers to agent roles
- [ ] **Form Validation** - Validate settings before saving
- [ ] **RBAC Enforcement** - Only Owner can update settings
- [ ] **Responsive UI** - Works on desktop and tablet
- [ ] **80% Test Coverage** - Unit tests for all components and services

### Nice to Have (Future)

- [ ] Settings history/audit log
- [ ] Settings export/import (JSON)
- [ ] Settings templates for common configurations
- [ ] Settings validation with warnings (e.g., "Low max retries may cause failures")
- [ ] Settings reset to defaults

---

## Architecture Overview

### Component Hierarchy

```
/settings/general (Route)
│
├── General.razor (Page - Single page with tabs)
│   ├── Tab 1: TenantInfoPanel.razor (Component)
│   ├── Tab 2: WorkflowSettingsPanel.razor (Component)
│   ├── Tab 3: CodeReviewSettingsPanel.razor (Component)
│   └── Tab 4: LlmProviderAssignmentPanel.razor (Component)
```

### Data Flow

```
User edits settings (UI)
  ↓
Page component updates local state
  ↓
User clicks "Save Changes"
  ↓
ITenantConfigurationService.UpdateConfigurationAsync()
  ↓
TenantConfigurationService validates and saves
  ↓
Tenant.UpdateConfiguration() domain method
  ↓
TenantConfiguration JSON field updated
  ↓
Database persisted
  ↓
Success toast shown
```

### File Structure

```
PRFactory.Web/
├── Pages/
│   └── Settings/
│       ├── General.razor
│       └── General.razor.cs
│
└── Components/
    └── Settings/
        ├── TenantInfoPanel.razor
        ├── TenantInfoPanel.razor.cs
        ├── WorkflowSettingsPanel.razor
        ├── WorkflowSettingsPanel.razor.cs
        ├── CodeReviewSettingsPanel.razor
        ├── CodeReviewSettingsPanel.razor.cs
        ├── LlmProviderAssignmentPanel.razor
        ├── LlmProviderAssignmentPanel.razor.cs
        └── AllowedRepositoriesEditor.razor
```

---

## Settings Categories

### 1. General Information (Read-Only)

**Purpose**: Display tenant metadata for informational purposes.

**Fields:**
- **Tenant Name** (text, read-only)
- **Identity Provider** (text, read-only) - e.g., "Azure AD (microsoft.com)"
- **Ticket Platform** (text, read-only) - e.g., "Jira Cloud (https://acme.atlassian.net)"
- **Created** (date, read-only)
- **Status** (badge, read-only) - Active/Inactive

**No actions**: This tab is informational only.

---

### 2. Workflow Settings

**Purpose**: Configure workflow execution behavior.

**Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| **Auto-Implementation After Plan Approval** | Checkbox | `false` | Automatically start code implementation after plan is approved |
| **Max Retries for Failed Operations** | Number (1-10) | `3` | Maximum number of retry attempts for failed operations |
| **API Timeout (seconds)** | Number (30-600) | `300` | Timeout for API calls to external systems |
| **Enable Verbose Logging** | Checkbox | `false` | Enable detailed logging for troubleshooting |
| **Allowed Repositories** | Tag Editor | Empty (all allowed) | Whitelist of repository names (empty = all allowed) |

**Validation Rules:**
- Max Retries: 1-10
- API Timeout: 30-600 seconds
- Allowed Repositories: Valid repository names only

---

### 3. Code Review Settings

**Purpose**: Configure AI-powered code review behavior.

**Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| **Enable Auto Code Review** | Checkbox | `true` | Automatically trigger code review after PR creation |
| **Max Code Review Iterations** | Number (1-10) | `3` | Maximum number of code review/fix iterations |
| **Auto-Approve If No Issues** | Checkbox | `false` | Automatically approve PR if no issues found |
| **Require Human Approval After Review** | Checkbox | `true` | Require human approval even if code passes review (read-only for now) |

**Validation Rules:**
- Max Code Review Iterations: 1-10
- If `Enable Auto Code Review` is false, disable other options

**Dependencies:**
- Requires at least one Code Review LLM provider configured
- Warning shown if no Code Review provider assigned

---

### 4. LLM Provider Assignment

**Purpose**: Assign LLM providers to specific agent roles.

**Fields:**

| Agent Role | Dropdown | Default | Description |
|------------|----------|---------|-------------|
| **Analysis LLM Provider** | Provider dropdown | Tenant default | LLM provider for Analysis agent (codebase analysis) |
| **Planning LLM Provider** | Provider dropdown | Tenant default | LLM provider for Planning agent (implementation plans) |
| **Implementation LLM Provider** | Provider dropdown | Tenant default | LLM provider for Implementation agent (code generation) |
| **Code Review LLM Provider** | Provider dropdown | Tenant default | LLM provider for Code Review agent (PR reviews) |

**Dropdown Options:**
- "Use Tenant Default" (null value)
- List of active LLM providers for tenant

**Help Text:**
- Analysis: "Analyzes codebase and generates clarifying questions"
- Planning: "Creates detailed implementation plans for review"
- Implementation: "Generates code based on approved plans (optional)"
- Code Review: "Reviews generated code for issues and improvements"

**Validation:**
- Warning if no providers configured
- Error if provider not active

---

## Page Specification

### Tenant Settings Page (`/settings/general`)

**File:** `/src/PRFactory.Web/Pages/Settings/General.razor`

#### Purpose
Single-page tabbed interface for viewing and updating tenant settings.

#### Route
```csharp
@page "/settings/general"
@attribute [Authorize(Roles = "Owner,Admin,Member,Viewer")]
```

**Note**: All roles can VIEW settings, but only Owner can UPDATE.

#### Page State (General.razor.cs)

```csharp
public partial class General
{
    [Inject] private ITenantConfigurationService ConfigService { get; set; } = null!;
    [Inject] private ITenantService TenantService { get; set; } = null!;
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private TenantDto? tenant;
    private TenantConfigurationDto? configuration;
    private List<TenantLlmProviderDto> llmProviders = new();

    private bool isLoading = true;
    private bool isSaving = false;
    private bool canEdit = false;

    private enum TabType
    {
        General = 1,
        Workflow = 2,
        CodeReview = 3,
        LlmProviders = 4
    }

    private TabType activeTab = TabType.General;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            isLoading = true;

            var tenantId = await CurrentUserService.GetCurrentTenantIdAsync();

            // Load tenant, configuration, and LLM providers in parallel
            var tenantTask = TenantService.GetTenantByIdAsync(tenantId);
            var configTask = ConfigService.GetConfigurationAsync();
            var providersTask = ProviderService.GetProvidersForTenantAsync();

            await Task.WhenAll(tenantTask, configTask, providersTask);

            tenant = await tenantTask;
            configuration = await configTask;
            llmProviders = await providersTask;
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canEdit = currentUser.Role == UserRole.Owner;
    }

    private void OnTabChanged(TabType tab)
    {
        activeTab = tab;
    }

    private async Task HandleSaveAsync()
    {
        if (!canEdit)
        {
            await ToastService.ShowErrorAsync("Only Owners can update tenant settings");
            return;
        }

        if (configuration == null)
            return;

        try
        {
            isSaving = true;

            await ConfigService.UpdateConfigurationAsync(configuration);

            await ToastService.ShowSuccessAsync("Settings updated successfully");

            await LoadDataAsync(); // Reload to reflect changes
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to update settings: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
}
```

#### Markup (General.razor)

```razor
@page "/settings/general"
@attribute [Authorize(Roles = "Owner,Admin,Member,Viewer")]

<PageContainer>
    <PageHeader Icon="bi-gear" Title="Tenant Settings">
        @if (!canEdit)
        {
            <span class="badge bg-warning">
                <i class="bi bi-lock me-1"></i>
                Read-Only
            </span>
        }
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading settings..." />
    }
    else if (tenant != null && configuration != null)
    {
        <Card>
            @* Tab Navigation *@
            <ul class="nav nav-tabs mb-4" role="tablist">
                <li class="nav-item">
                    <a class="nav-link @(activeTab == TabType.General ? "active" : "")"
                       href="#"
                       @onclick="@(() => OnTabChanged(TabType.General))"
                       @onclick:preventDefault>
                        <i class="bi bi-info-circle me-2"></i>
                        General
                    </a>
                </li>
                <li class="nav-item">
                    <a class="nav-link @(activeTab == TabType.Workflow ? "active" : "")"
                       href="#"
                       @onclick="@(() => OnTabChanged(TabType.Workflow))"
                       @onclick:preventDefault>
                        <i class="bi bi-diagram-3 me-2"></i>
                        Workflow
                    </a>
                </li>
                <li class="nav-item">
                    <a class="nav-link @(activeTab == TabType.CodeReview ? "active" : "")"
                       href="#"
                       @onclick="@(() => OnTabChanged(TabType.CodeReview))"
                       @onclick:preventDefault>
                        <i class="bi bi-code-slash me-2"></i>
                        Code Review
                    </a>
                </li>
                <li class="nav-item">
                    <a class="nav-link @(activeTab == TabType.LlmProviders ? "active" : "")"
                       href="#"
                       @onclick="@(() => OnTabChanged(TabType.LlmProviders))"
                       @onclick:preventDefault>
                        <i class="bi bi-robot me-2"></i>
                        LLM Providers
                    </a>
                </li>
            </ul>

            @* Tab Content *@
            <div class="tab-content">
                @if (activeTab == TabType.General)
                {
                    <TenantInfoPanel Tenant="tenant" />
                }
                else if (activeTab == TabType.Workflow)
                {
                    <WorkflowSettingsPanel Configuration="configuration" CanEdit="canEdit" />
                }
                else if (activeTab == TabType.CodeReview)
                {
                    <CodeReviewSettingsPanel Configuration="configuration" CanEdit="canEdit" />
                }
                else if (activeTab == TabType.LlmProviders)
                {
                    <LlmProviderAssignmentPanel Configuration="configuration"
                                               Providers="llmProviders"
                                               CanEdit="canEdit" />
                }
            </div>

            @* Save Button (only for non-General tabs and if user can edit) *@
            @if (activeTab != TabType.General && canEdit)
            {
                <hr />
                <div class="d-flex gap-2 justify-content-end">
                    <LoadingButton Text="Save Changes"
                                  Icon="bi-save"
                                  OnClick="HandleSaveAsync"
                                  IsLoading="isSaving"
                                  LoadingText="Saving..."
                                  Class="btn-primary" />
                </div>
            }
        </Card>
    }
</PageContainer>
```

---

## Component Specifications

### 1. TenantInfoPanel Component

**File:** `/src/PRFactory.Web/Components/Settings/TenantInfoPanel.razor`

#### Purpose
Display read-only tenant information.

#### Props (Parameters)

```csharp
public partial class TenantInfoPanel
{
    [Parameter, EditorRequired]
    public TenantDto Tenant { get; set; } = null!;
}
```

#### Markup (TenantInfoPanel.razor)

```razor
<div class="row">
    <div class="col-md-6">
        <dl>
            <dt>Tenant Name</dt>
            <dd>@Tenant.Name</dd>

            <dt>Identity Provider</dt>
            <dd>
                @if (Tenant.IdentityProvider.HasValue)
                {
                    <span>@Tenant.IdentityProvider (@Tenant.ExternalTenantId)</span>
                }
                else
                {
                    <span class="text-muted">Not configured</span>
                }
            </dd>

            <dt>Ticket Platform</dt>
            <dd>
                @if (!string.IsNullOrEmpty(Tenant.TicketPlatform))
                {
                    <span>@Tenant.TicketPlatform</span>
                    @if (!string.IsNullOrEmpty(Tenant.TicketPlatformUrl))
                    {
                        <br />
                        <small class="text-muted">@Tenant.TicketPlatformUrl</small>
                    }
                }
                else
                {
                    <span class="text-muted">Not configured</span>
                }
            </dd>
        </dl>
    </div>

    <div class="col-md-6">
        <dl>
            <dt>Created</dt>
            <dd><RelativeTime Timestamp="Tenant.CreatedAt" /></dd>

            <dt>Status</dt>
            <dd>
                <StatusBadge Status="@(Tenant.IsActive ? "Active" : "Inactive")" />
            </dd>
        </dl>
    </div>
</div>

<div class="alert alert-info">
    <i class="bi bi-info-circle me-2"></i>
    <strong>Note:</strong> Tenant information is managed by your identity provider and cannot be changed here.
</div>
```

---

### 2. WorkflowSettingsPanel Component

**File:** `/src/PRFactory.Web/Components/Settings/WorkflowSettingsPanel.razor`

#### Purpose
Configure workflow execution behavior.

#### Props (Parameters)

```csharp
public partial class WorkflowSettingsPanel
{
    [Parameter, EditorRequired]
    public TenantConfigurationDto Configuration { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }
}
```

#### Markup (WorkflowSettingsPanel.razor)

```razor
<div class="row">
    <div class="col-md-6">
        @* Auto-Implementation *@
        <div class="form-check mb-3">
            <input class="form-check-input" type="checkbox" id="autoImplement"
                   @bind="Configuration.AutoImplementAfterPlanApproval"
                   disabled="@(!CanEdit)" />
            <label class="form-check-label" for="autoImplement">
                Auto-Implementation After Plan Approval
            </label>
            <ContextualHelp Content="Automatically start code implementation after plan is approved (requires Implementation LLM provider)" />
        </div>

        @* Max Retries *@
        <FormTextField Label="Max Retries for Failed Operations"
                      @bind-Value="Configuration.MaxRetries"
                      Type="number"
                      Min="1"
                      Max="10"
                      Disabled="@(!CanEdit)"
                      HelpText="Maximum number of retry attempts (1-10)" />

        @* API Timeout *@
        <FormTextField Label="API Timeout (seconds)"
                      @bind-Value="Configuration.ApiTimeoutSeconds"
                      Type="number"
                      Min="30"
                      Max="600"
                      Disabled="@(!CanEdit)"
                      HelpText="Timeout for API calls in seconds (30-600)" />
    </div>

    <div class="col-md-6">
        @* Verbose Logging *@
        <div class="form-check mb-3">
            <input class="form-check-input" type="checkbox" id="verboseLogging"
                   @bind="Configuration.EnableVerboseLogging"
                   disabled="@(!CanEdit)" />
            <label class="form-check-label" for="verboseLogging">
                Enable Verbose Logging
            </label>
            <ContextualHelp Content="Enable detailed logging for troubleshooting (may impact performance)" />
        </div>

        @* Allowed Repositories *@
        <AllowedRepositoriesEditor @bind-Value="Configuration.AllowedRepositories"
                                   Disabled="@(!CanEdit)"
                                   HelpText="Whitelist of repository names (leave empty to allow all)" />
    </div>
</div>
```

---

### 3. CodeReviewSettingsPanel Component

**File:** `/src/PRFactory.Web/Components/Settings/CodeReviewSettingsPanel.razor`

#### Purpose
Configure AI-powered code review behavior.

#### Props (Parameters)

```csharp
public partial class CodeReviewSettingsPanel
{
    [Parameter, EditorRequired]
    public TenantConfigurationDto Configuration { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }
}
```

#### Markup (CodeReviewSettingsPanel.razor)

```razor
<div class="row">
    <div class="col-md-6">
        @* Enable Auto Code Review *@
        <div class="form-check mb-3">
            <input class="form-check-input" type="checkbox" id="enableCodeReview"
                   @bind="Configuration.EnableAutoCodeReview"
                   disabled="@(!CanEdit)" />
            <label class="form-check-label" for="enableCodeReview">
                Enable Auto Code Review
            </label>
            <ContextualHelp Content="Automatically trigger code review after PR creation (requires Code Review LLM provider)" />
        </div>

        @if (Configuration.EnableAutoCodeReview)
        {
            @* Max Iterations *@
            <FormTextField Label="Max Code Review Iterations"
                          @bind-Value="Configuration.MaxCodeReviewIterations"
                          Type="number"
                          Min="1"
                          Max="10"
                          Disabled="@(!CanEdit)"
                          HelpText="Maximum number of review/fix iterations (1-10)" />

            @* Auto-Approve *@
            <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" id="autoApprove"
                       @bind="Configuration.AutoApproveIfNoIssues"
                       disabled="@(!CanEdit)" />
                <label class="form-check-label" for="autoApprove">
                    Auto-Approve If No Issues
                </label>
                <ContextualHelp Content="Automatically approve PR if no issues found (still requires human merge)" />
            </div>
        }
        else
        {
            <div class="alert alert-info">
                <i class="bi bi-info-circle me-2"></i>
                Enable auto code review to configure additional settings.
            </div>
        }
    </div>

    <div class="col-md-6">
        @* Warning if no Code Review provider assigned *@
        @if (Configuration.EnableAutoCodeReview && !Configuration.CodeReviewLlmProviderId.HasValue)
        {
            <div class="alert alert-warning">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Warning:</strong> No Code Review LLM provider assigned. Go to the "LLM Providers" tab to assign one.
            </div>
        }

        @* Future: Require Human Approval (read-only for now) *@
        <div class="form-check mb-3">
            <input class="form-check-input" type="checkbox" id="requireHumanApproval"
                   checked="@true"
                   disabled />
            <label class="form-check-label text-muted" for="requireHumanApproval">
                Require Human Approval After Review (always enabled)
            </label>
            <ContextualHelp Content="Human approval is always required before merging, even if code passes review" />
        </div>
    </div>
</div>
```

---

### 4. LlmProviderAssignmentPanel Component

**File:** `/src/PRFactory.Web/Components/Settings/LlmProviderAssignmentPanel.razor`

#### Purpose
Assign LLM providers to agent roles.

#### Props (Parameters)

```csharp
public partial class LlmProviderAssignmentPanel
{
    [Parameter, EditorRequired]
    public TenantConfigurationDto Configuration { get; set; } = null!;

    [Parameter, EditorRequired]
    public List<TenantLlmProviderDto> Providers { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    private List<TenantLlmProviderDto> ActiveProviders =>
        Providers.Where(p => p.IsActive).ToList();
}
```

#### Markup (LlmProviderAssignmentPanel.razor)

```razor
@if (!ActiveProviders.Any())
{
    <div class="alert alert-warning">
        <i class="bi bi-exclamation-triangle me-2"></i>
        <strong>No LLM providers configured.</strong>
        <a href="/settings/llm-providers/create" class="alert-link">Add a provider</a> to assign it to agents.
    </div>
}
else
{
    <div class="row">
        <div class="col-md-6">
            @* Analysis Provider *@
            <FormSelectField Label="Analysis LLM Provider"
                            @bind-Value="Configuration.AnalysisLlmProviderId"
                            Disabled="@(!CanEdit)"
                            HelpText="Analyzes codebase and generates clarifying questions">
                <option value="">Use Tenant Default</option>
                @foreach (var provider in ActiveProviders)
                {
                    <option value="@provider.Id">
                        @provider.Name (@provider.ProviderType)
                        @if (provider.IsDefault) { <text> ⭐</text> }
                    </option>
                }
            </FormSelectField>

            @* Planning Provider *@
            <FormSelectField Label="Planning LLM Provider"
                            @bind-Value="Configuration.PlanningLlmProviderId"
                            Disabled="@(!CanEdit)"
                            HelpText="Creates detailed implementation plans for review">
                <option value="">Use Tenant Default</option>
                @foreach (var provider in ActiveProviders)
                {
                    <option value="@provider.Id">
                        @provider.Name (@provider.ProviderType)
                        @if (provider.IsDefault) { <text> ⭐</text> }
                    </option>
                }
            </FormSelectField>
        </div>

        <div class="col-md-6">
            @* Implementation Provider *@
            <FormSelectField Label="Implementation LLM Provider"
                            @bind-Value="Configuration.ImplementationLlmProviderId"
                            Disabled="@(!CanEdit)"
                            HelpText="Generates code based on approved plans (optional)">
                <option value="">Use Tenant Default</option>
                @foreach (var provider in ActiveProviders)
                {
                    <option value="@provider.Id">
                        @provider.Name (@provider.ProviderType)
                        @if (provider.IsDefault) { <text> ⭐</text> }
                    </option>
                }
            </FormSelectField>

            @* Code Review Provider *@
            <FormSelectField Label="Code Review LLM Provider"
                            @bind-Value="Configuration.CodeReviewLlmProviderId"
                            Disabled="@(!CanEdit)"
                            HelpText="Reviews generated code for issues and improvements">
                <option value="">Use Tenant Default</option>
                @foreach (var provider in ActiveProviders)
                {
                    <option value="@provider.Id">
                        @provider.Name (@provider.ProviderType)
                        @if (provider.IsDefault) { <text> ⭐</text> }
                    </option>
                }
            </FormSelectField>
        </div>
    </div>

    <div class="alert alert-info mt-3">
        <i class="bi bi-info-circle me-2"></i>
        <strong>Tip:</strong> Assign different providers to different roles for optimal results.
        For example, use a fast model for analysis and a powerful model for implementation.
    </div>
}
```

---

## Service Layer Integration

### ITenantConfigurationService Interface

**File:** `/src/PRFactory.Core/Application/Services/ITenantConfigurationService.cs`

```csharp
public interface ITenantConfigurationService
{
    Task<TenantConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigurationAsync(TenantConfigurationDto dto, CancellationToken cancellationToken = default);
}
```

### TenantConfigurationDto

**File:** `/src/PRFactory.Core/Application/DTOs/TenantConfigurationDto.cs`

```csharp
public class TenantConfigurationDto
{
    // Workflow Settings
    public bool AutoImplementAfterPlanApproval { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int ApiTimeoutSeconds { get; set; } = 300;
    public bool EnableVerboseLogging { get; set; }
    public List<string> AllowedRepositories { get; set; } = new();

    // Code Review Settings
    public bool EnableAutoCodeReview { get; set; } = true;
    public int MaxCodeReviewIterations { get; set; } = 3;
    public bool AutoApproveIfNoIssues { get; set; }

    // LLM Provider Assignments
    public Guid? AnalysisLlmProviderId { get; set; }
    public Guid? PlanningLlmProviderId { get; set; }
    public Guid? ImplementationLlmProviderId { get; set; }
    public Guid? CodeReviewLlmProviderId { get; set; }
}
```

---

## Security & Authorization

### RBAC Rules

| Role | View Settings | Update Settings |
|------|---------------|-----------------|
| **Owner** | ✅ | ✅ |
| **Admin** | ✅ | ❌ |
| **Member** | ✅ | ❌ |
| **Viewer** | ❌ | ❌ |

**Note**: Only Owner can update settings. Admins, Members can view but cannot edit.

### Implementation

```csharp
// Page-level authorization (view)
@attribute [Authorize(Roles = "Owner,Admin,Member,Viewer")]

// UI-level authorization (edit)
@if (canEdit)
{
    <LoadingButton Text="Save Changes" ... />
}

// Service-level authorization (update)
public async Task UpdateConfigurationAsync(TenantConfigurationDto dto)
{
    var currentUser = await _currentUserService.GetCurrentUserAsync();

    if (currentUser.Role != UserRole.Owner)
    {
        throw new UnauthorizedAccessException("Only Owners can update tenant settings");
    }

    // Continue with update...
}
```

---

## Testing Strategy

### Unit Tests

**File:** `/tests/PRFactory.Tests/Blazor/Pages/Settings/GeneralTests.cs`

```csharp
public class TenantSettingsPageTests : PageTestBase
{
    [Fact]
    public async Task OnInitialized_LoadsTenantAndConfiguration()
    {
        // Arrange
        var tenant = TenantDtoBuilder.Create().Build();
        var config = new TenantConfigurationDto { MaxRetries = 5 };

        MockTenantService.Setup(s => s.GetTenantByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(tenant);

        MockConfigService.Setup(s => s.GetConfigurationAsync(default))
            .ReturnsAsync(config);

        // Act
        var cut = RenderComponent<General>();
        await Task.Delay(100);

        // Assert
        cut.Find("h1").TextContent.Should().Contain("Tenant Settings");
        cut.Find(".nav-tabs").Should().NotBeNull();
    }

    [Fact]
    public async Task SaveButton_OnlyShownForOwner()
    {
        // Arrange
        MockCurrentUserService.Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync(UserDtoBuilder.Create().WithRole(UserRole.Admin).Build());

        // Act
        var cut = RenderComponent<General>();
        await Task.Delay(100);

        // Assert
        cut.FindAll("button:contains('Save Changes')").Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_CallsServiceAndShowsToast()
    {
        // Arrange
        var config = new TenantConfigurationDto { MaxRetries = 5 };

        MockCurrentUserService.Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync(UserDtoBuilder.Create().WithRole(UserRole.Owner).Build());

        MockConfigService.Setup(s => s.GetConfigurationAsync(default))
            .ReturnsAsync(config);

        MockConfigService.Setup(s => s.UpdateConfigurationAsync(config, default))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<General>();
        await Task.Delay(100);

        // Act
        var saveButton = cut.Find("button:contains('Save Changes')");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        MockConfigService.Verify(s => s.UpdateConfigurationAsync(config, default), Times.Once);
        MockToastService.Verify(s => s.ShowSuccessAsync("Settings updated successfully"), Times.Once);
    }
}
```

---

## Implementation Checklist

### Day 1-2: Page & Panels
- [ ] Create `General.razor` and `General.razor.cs`
- [ ] Implement tab navigation state management
- [ ] Create `TenantInfoPanel` component
- [ ] Create `WorkflowSettingsPanel` component
- [ ] Write unit tests for page and panels

### Day 3: Code Review & LLM Provider Panels
- [ ] Create `CodeReviewSettingsPanel` component
- [ ] Create `LlmProviderAssignmentPanel` component
- [ ] Create `AllowedRepositoriesEditor` component
- [ ] Write unit tests for panels

### Day 4: Integration & Testing
- [ ] Write integration tests for service layer
- [ ] Test RBAC enforcement
- [ ] Test validation rules
- [ ] E2E test for full update workflow

### Day 5: Polish & Bug Fixes
- [ ] Fix any failing tests
- [ ] Improve validation messages
- [ ] Add help text and tooltips
- [ ] Code review and refactoring

---

## UI Mockups

### General Tab

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > General                                         │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ⚙️ Tenant Settings                                                    │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ [General] [Workflow] [Code Review] [LLM Providers]               │ │
│  │ ─────────                                                         │ │
│  │                                                                   │ │
│  │  Tenant Name:         Acme Corporation                            │ │
│  │  Identity Provider:   Azure AD (microsoft.com)                    │ │
│  │  Ticket Platform:     Jira Cloud (https://acme.atlassian.net)   │ │
│  │  Created:             November 10, 2025                           │ │
│  │  Status:              🟢 Active                                   │ │
│  │                                                                   │ │
│  │  ℹ️ Note: Tenant information is managed by your identity         │ │
│  │  provider and cannot be changed here.                            │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

### Workflow Tab

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > General                                         │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ⚙️ Tenant Settings                                                    │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ [General] [Workflow] [Code Review] [LLM Providers]               │ │
│  │           ─────────                                               │ │
│  │                                                                   │ │
│  │  ☑️ Auto-Implementation After Plan Approval                       │ │
│  │  ℹ️ Automatically start code implementation after plan approved  │ │
│  │                                                                   │ │
│  │  Max Retries for Failed Operations                                │ │
│  │  [3_________] (1-10)                                              │ │
│  │                                                                   │ │
│  │  API Timeout (seconds)                                            │ │
│  │  [300_______] (30-600)                                            │ │
│  │                                                                   │ │
│  │  ☐ Enable Verbose Logging                                         │ │
│  │  ℹ️ Detailed logging (may impact performance)                    │ │
│  │                                                                   │ │
│  │  Allowed Repositories (leave empty for all)                       │ │
│  │  [my-app] [backend-api]  [+ Add]                                 │ │
│  │                                                                   │ │
│  │                                              [Save Changes]       │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Author:** AI Planning Assistant
**Status:** Ready for Implementation
