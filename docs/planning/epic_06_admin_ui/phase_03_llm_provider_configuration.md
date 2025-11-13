# Epic 06 - Phase 3: LLM Provider Configuration UI

**Status:** 📋 Ready for Implementation
**Estimated Effort:** 1-2 weeks (40-80 hours)
**Priority:** P0 - Critical (Blocks multi-LLM usage)
**Dependencies:** Phase 1 Complete ✅, Phase 2 Recommended

---

## Table of Contents

- [Overview](#overview)
- [Goals & Success Criteria](#goals--success-criteria)
- [Architecture Overview](#architecture-overview)
- [Provider Types](#provider-types)
- [Page Specifications](#page-specifications)
- [Component Specifications](#component-specifications)
- [OAuth Flow](#oauth-flow)
- [Service Layer Integration](#service-layer-integration)
- [State Management](#state-management)
- [Security & Authorization](#security--authorization)
- [Testing Strategy](#testing-strategy)
- [Implementation Checklist](#implementation-checklist)
- [UI Mockups](#ui-mockups)

---

## Overview

### What We're Building

A multi-step wizard UI that allows tenant administrators to:
- View all configured LLM providers for their tenant
- Add new LLM providers with type-specific configuration
- Configure OAuth-based providers (Anthropic Native)
- Configure API key-based providers (Z.ai, Minimax M2, OpenRouter, Together AI, Custom)
- Test provider connections before saving
- Set a default LLM provider for the tenant
- Configure model overrides (map generic model names to provider-specific names)
- Deactivate (soft delete) providers

### Why This Matters

**Business Impact:**
- Enables multi-LLM strategy (no vendor lock-in to Claude)
- Allows customers to choose their preferred LLM provider
- Supports per-agent LLM configuration (different models for different tasks)
- Validates product's flexibility and enterprise readiness

**Technical Foundation:**
- Demonstrates complex wizard UI patterns in Blazor
- Establishes OAuth integration patterns
- Shows dynamic form rendering based on user selection

---

## Goals & Success Criteria

### Must Have (Phase 3)

- [x] **Service Layer Complete** - `TenantLlmProviderService` with CRUD operations ✅
- [ ] **LLM Provider List Page** - Display all providers for current tenant
- [ ] **Add LLM Provider Wizard** - Multi-step wizard with type selection
- [ ] **OAuth Flow** - Anthropic Native OAuth integration
- [ ] **API Key Configuration** - Forms for 5 API key-based providers
- [ ] **Connection Testing** - Validate LLM credentials before saving
- [ ] **Default Provider Management** - Set/unset default provider
- [ ] **Model Override Configuration** - JSON editor for model mappings
- [ ] **Edit Provider** - Update provider settings
- [ ] **Provider Detail Page** - View statistics and usage information
- [ ] **RBAC Enforcement** - Only Owner/Admin can add/edit providers
- [ ] **Encrypted Credentials** - All tokens encrypted at rest (AES-256-GCM)
- [ ] **Responsive UI** - Works on desktop and tablet
- [ ] **80% Test Coverage** - Unit tests for all components and services

### Nice to Have (Future)

- [ ] Provider health monitoring dashboard
- [ ] Automatic token refresh for OAuth providers
- [ ] Cost tracking per provider
- [ ] Provider performance analytics
- [ ] Bulk provider configuration import

---

## Architecture Overview

### Component Hierarchy

```
/settings/llm-providers (Route Group)
│
├── Index.razor (Page)
│   ├── LlmProviderListItem.razor (Component) ×N
│   ├── EmptyState.razor (UI Component)
│   ├── LoadingSpinner.razor (UI Component)
│   └── Pagination.razor (UI Component)
│
├── Create.razor (Page - Wizard)
│   ├── Step1: ProviderTypeSelector.razor (Component)
│   └── Step2: LlmProviderForm.razor (Component)
│       ├── OAuthProviderForm.razor (Component)
│       ├── ApiKeyProviderForm.razor (Component)
│       └── LlmProviderConnectionTest.razor (Component)
│
├── Edit.razor (Page)
│   └── LlmProviderForm.razor (Component) [reused]
│
└── Detail.razor (Page)
    ├── Card.razor (UI Component)
    ├── LlmProviderStatistics.razor (Component)
    ├── StatusBadge.razor (UI Component)
    └── IconButton.razor (UI Component)
```

### Wizard Flow

```
Step 1: Provider Type Selection
  ↓
User selects: Anthropic Native (OAuth)
  ↓
Step 2: OAuth Configuration Form
  - Provider Name
  - Default Model (dropdown)
  - [Authorize with Anthropic] button
  ↓
OAuth Flow
  - Redirect to Anthropic
  - User authorizes
  - Callback with tokens
  - Encrypt and save
  ↓
Success
```

OR

```
Step 1: Provider Type Selection
  ↓
User selects: Z.ai / Minimax / OpenRouter / Together / Custom
  ↓
Step 2: API Key Configuration Form
  - Provider Name
  - API Key (password field)
  - API Base URL (optional for non-custom)
  - Default Model
  - Timeout (ms)
  - Model Overrides (JSON)
  - Disable Non-Essential Traffic (checkbox)
  ↓
Connection Test (optional but recommended)
  ↓
Save
  ↓
Success
```

### File Structure

```
PRFactory.Web/
├── Pages/
│   └── Settings/
│       └── LlmProviders/
│           ├── Index.razor
│           ├── Index.razor.cs
│           ├── Create.razor
│           ├── Create.razor.cs
│           ├── Edit.razor
│           ├── Edit.razor.cs
│           ├── Detail.razor
│           └── Detail.razor.cs
│
└── Components/
    └── Settings/
        ├── ProviderTypeSelector.razor
        ├── ProviderTypeSelector.razor.cs
        ├── LlmProviderForm.razor
        ├── LlmProviderForm.razor.cs
        ├── OAuthProviderForm.razor
        ├── OAuthProviderForm.razor.cs
        ├── ApiKeyProviderForm.razor
        ├── ApiKeyProviderForm.razor.cs
        ├── LlmProviderListItem.razor
        ├── LlmProviderListItem.razor.cs
        ├── LlmProviderConnectionTest.razor
        ├── LlmProviderConnectionTest.razor.cs
        ├── LlmProviderStatistics.razor
        ├── LlmProviderStatistics.razor.cs
        ├── ModelOverridesEditor.razor
        └── ModelOverridesEditor.razor.cs
```

---

## Provider Types

### 1. Anthropic Native (OAuth)

**Authentication:** OAuth 2.0
**API Base URL:** `https://api.anthropic.com`
**Supported Models:**
- `claude-sonnet-4-5` (Claude Sonnet 4.5)
- `claude-opus-4-5` (Claude Opus 4.5)
- `claude-3-5-sonnet` (Claude 3.5 Sonnet)

**Configuration Fields:**
- Provider Name (text)
- Default Model (dropdown)
- OAuth Authorization (button)

**Special Features:**
- Automatic token refresh
- Uses OAuth tokens (not API keys)

---

### 2. Z.ai Unified API (API Key)

**Authentication:** API Key
**API Base URL:** `https://api.z.ai` (default)
**Supported Models:** All major models from multiple providers
- `gpt-4o` (OpenAI GPT-4o)
- `claude-sonnet-4-5` (Anthropic Claude Sonnet 4.5)
- `gemini-2.0-flash` (Google Gemini 2.0 Flash)
- And 100+ more models

**Configuration Fields:**
- Provider Name (text)
- API Key (password)
- API Base URL (text, default: `https://api.z.ai`)
- Default Model (text)
- Timeout (ms, default: 300000)
- Model Overrides (JSON, optional)
- Disable Non-Essential Traffic (checkbox)

**Special Features:**
- Unified API for multiple LLM providers
- Consistent interface across different models

---

### 3. Minimax M2 (API Key)

**Authentication:** API Key
**API Base URL:** `https://api.minimax.chat`
**Supported Models:**
- `minimax-m2` (Minimax M2)

**Configuration Fields:**
- Provider Name (text)
- API Key (password)
- API Base URL (text, default: `https://api.minimax.chat`)
- Default Model (text, default: `minimax-m2`)
- Timeout (ms, default: 300000)
- Model Overrides (JSON, optional)
- Disable Non-Essential Traffic (checkbox)

**Special Features:**
- Chinese LLM provider
- Competitive pricing

---

### 4. OpenRouter (API Key)

**Authentication:** API Key
**API Base URL:** `https://openrouter.ai/api/v1`
**Supported Models:** 100+ models from multiple providers
- `anthropic/claude-sonnet-4-5`
- `openai/gpt-4o`
- `google/gemini-2.0-flash`
- And many more

**Configuration Fields:**
- Provider Name (text)
- API Key (password)
- API Base URL (text, default: `https://openrouter.ai/api/v1`)
- Default Model (text)
- Timeout (ms, default: 300000)
- Model Overrides (JSON, optional)
- Disable Non-Essential Traffic (checkbox)

**Special Features:**
- Model routing across multiple providers
- Competitive pricing with credits system

---

### 5. Together AI (API Key)

**Authentication:** API Key
**API Base URL:** `https://api.together.xyz`
**Supported Models:**
- `meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo`
- `mistralai/Mixtral-8x7B-Instruct-v0.1`
- And many open-source models

**Configuration Fields:**
- Provider Name (text)
- API Key (password)
- API Base URL (text, default: `https://api.together.xyz`)
- Default Model (text)
- Timeout (ms, default: 300000)
- Model Overrides (JSON, optional)
- Disable Non-Essential Traffic (checkbox)

**Special Features:**
- Focus on open-source models
- Fast inference

---

### 6. Custom (API Key)

**Authentication:** API Key
**API Base URL:** User-specified
**Supported Models:** User-specified

**Configuration Fields:**
- Provider Name (text)
- API Key (password)
- API Base URL (text, required)
- Default Model (text, required)
- Timeout (ms, default: 300000)
- Model Overrides (JSON, optional)
- Disable Non-Essential Traffic (checkbox)

**Special Features:**
- Supports any OpenAI-compatible API
- Self-hosted LLMs (Ollama, vLLM, etc.)

---

## Page Specifications

### 1. LLM Provider List Page (`/settings/llm-providers`)

**File:** `/src/PRFactory.Web/Pages/Settings/LlmProviders/Index.razor`

#### Purpose
Display all LLM providers configured for the current tenant with filter and action capabilities.

#### Route
```csharp
@page "/settings/llm-providers"
@attribute [Authorize(Roles = "Owner,Admin,Member")]
```

#### Page State (Index.razor.cs)

```csharp
public partial class Index
{
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private List<TenantLlmProviderDto> providers = new();
    private List<TenantLlmProviderDto> filteredProviders = new();
    private bool isLoading = true;
    private string searchTerm = string.Empty;
    private LlmProviderType? selectedType = null;
    private bool showActiveOnly = true;

    private bool canAddProvider;
    private bool canEditProvider;

    protected override async Task OnInitializedAsync()
    {
        await LoadProvidersAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            isLoading = true;
            providers = await ProviderService.GetProvidersForTenantAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load LLM providers: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canAddProvider = currentUser.Role is UserRole.Owner or UserRole.Admin;
        canEditProvider = currentUser.Role is UserRole.Owner or UserRole.Admin;
    }

    private void ApplyFilters()
    {
        filteredProviders = providers
            .Where(p => string.IsNullOrEmpty(searchTerm) ||
                       p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(p => selectedType == null || p.ProviderType == selectedType)
            .Where(p => !showActiveOnly || p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
    }

    private void OnTypeFilterChanged(LlmProviderType? type)
    {
        selectedType = type;
        ApplyFilters();
    }

    private void OnShowActiveOnlyChanged(bool value)
    {
        showActiveOnly = value;
        ApplyFilters();
    }

    private void NavigateToCreate()
    {
        if (canAddProvider)
            Navigation.NavigateTo("/settings/llm-providers/create");
    }

    private void NavigateToEdit(Guid id)
    {
        if (canEditProvider)
            Navigation.NavigateTo($"/settings/llm-providers/edit/{id}");
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/settings/llm-providers/{id}");
    }

    private async Task HandleSetDefaultAsync(Guid id)
    {
        if (!canEditProvider)
        {
            await ToastService.ShowErrorAsync("You don't have permission to set default provider");
            return;
        }

        try
        {
            await ProviderService.SetDefaultProviderAsync(id);
            await ToastService.ShowSuccessAsync("Default provider updated successfully");
            await LoadProvidersAsync();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to set default provider: {ex.Message}");
        }
    }

    private async Task HandleDeleteAsync(Guid id)
    {
        if (!canEditProvider)
        {
            await ToastService.ShowErrorAsync("You don't have permission to delete providers");
            return;
        }

        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm",
            "Are you sure you want to deactivate this LLM provider? This cannot be undone.");

        if (!confirmed)
            return;

        try
        {
            await ProviderService.DeleteProviderAsync(id);
            await ToastService.ShowSuccessAsync("LLM provider deactivated successfully");
            await LoadProvidersAsync();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to deactivate provider: {ex.Message}");
        }
    }

    private async Task HandleTestConnectionAsync(Guid id)
    {
        try
        {
            var result = await ProviderService.TestProviderConnectionAsync(id);

            if (result.Success)
            {
                await ToastService.ShowSuccessAsync("Connection test successful!");
            }
            else
            {
                await ToastService.ShowErrorAsync($"Connection test failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Connection test error: {ex.Message}");
        }
    }
}
```

#### Markup (Index.razor)

```razor
@page "/settings/llm-providers"
@attribute [Authorize(Roles = "Owner,Admin,Member")]

<PageContainer>
    <PageHeader Icon="bi-robot" Title="LLM Providers">
        @if (canAddProvider)
        {
            <IconButton Icon="bi-plus-circle" Text="Add Provider"
                       OnClick="NavigateToCreate" Class="btn-primary" />
        }
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading LLM providers..." />
    }
    else if (!filteredProviders.Any())
    {
        @if (providers.Any())
        {
            <AlertMessage Type="AlertType.Info"
                         Message="No providers match your filters." />
        }
        else
        {
            <EmptyState Icon="bi-robot"
                       Title="No LLM providers configured"
                       Message="Add your first LLM provider to start using AI agents.">
                @if (canAddProvider)
                {
                    <LoadingButton Text="Add Provider"
                                  Icon="bi-plus-circle"
                                  OnClick="NavigateToCreate" />
                }
            </EmptyState>
        }
    }
    else
    {
        <Card>
            <div class="row mb-3">
                <div class="col-md-4">
                    <FormTextField Label="Search"
                                  @bind-Value="searchTerm"
                                  OnChange="@((e) => OnSearchChanged(e.Value.ToString()))"
                                  Placeholder="Search by name..." />
                </div>
                <div class="col-md-3">
                    <FormSelectField Label="Provider Type"
                                    Value="selectedType"
                                    OnChange="@((e) => OnTypeFilterChanged(e.Value))">
                        <option value="">All Types</option>
                        <option value="@LlmProviderType.AnthropicNative">Anthropic Native</option>
                        <option value="@LlmProviderType.ZAi">Z.ai</option>
                        <option value="@LlmProviderType.MinimaxM2">Minimax M2</option>
                        <option value="@LlmProviderType.OpenRouter">OpenRouter</option>
                        <option value="@LlmProviderType.TogetherAI">Together AI</option>
                        <option value="@LlmProviderType.Custom">Custom</option>
                    </FormSelectField>
                </div>
                <div class="col-md-3">
                    <div class="form-check mt-4">
                        <input class="form-check-input" type="checkbox"
                               id="showActiveOnly"
                               @bind="showActiveOnly"
                               @onchange="@((e) => OnShowActiveOnlyChanged((bool)e.Value))" />
                        <label class="form-check-label" for="showActiveOnly">
                            Active only
                        </label>
                    </div>
                </div>
            </div>

            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Provider Type</th>
                            <th>Auth Method</th>
                            <th>Default Model</th>
                            <th>Default</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var provider in filteredProviders)
                        {
                            <LlmProviderListItem Provider="provider"
                                               CanEdit="canEditProvider"
                                               OnEdit="@(() => NavigateToEdit(provider.Id))"
                                               OnDelete="@(() => HandleDeleteAsync(provider.Id))"
                                               OnSetDefault="@(() => HandleSetDefaultAsync(provider.Id))"
                                               OnTestConnection="@(() => HandleTestConnectionAsync(provider.Id))"
                                               OnViewDetails="@(() => NavigateToDetail(provider.Id))" />
                        }
                    </tbody>
                </table>
            </div>

            <div class="mt-3 text-muted">
                Showing @filteredProviders.Count of @providers.Count providers
            </div>
        </Card>
    }
</PageContainer>
```

---

### 2. Create LLM Provider Page (Wizard) (`/settings/llm-providers/create`)

**File:** `/src/PRFactory.Web/Pages/Settings/LlmProviders/Create.razor`

#### Purpose
Multi-step wizard for adding new LLM providers with type-specific configuration.

#### Route
```csharp
@page "/settings/llm-providers/create"
@attribute [Authorize(Roles = "Owner,Admin")]
```

#### Page State (Create.razor.cs)

```csharp
public partial class Create
{
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private enum WizardStep
    {
        SelectType = 1,
        Configure = 2
    }

    private WizardStep currentStep = WizardStep.SelectType;
    private LlmProviderType? selectedType = null;

    // Models for different provider types
    private CreateOAuthProviderDto? oauthModel = null;
    private CreateApiKeyProviderDto? apiKeyModel = null;

    private bool isSaving = false;

    private void OnProviderTypeSelected(LlmProviderType type)
    {
        selectedType = type;

        // Initialize appropriate model
        if (type == LlmProviderType.AnthropicNative)
        {
            oauthModel = new CreateOAuthProviderDto
            {
                ProviderType = type,
                DefaultModel = "claude-sonnet-4-5"
            };
        }
        else
        {
            apiKeyModel = new CreateApiKeyProviderDto
            {
                ProviderType = type,
                DefaultModel = GetDefaultModelForType(type),
                ApiBaseUrl = GetDefaultApiBaseUrlForType(type),
                TimeoutMs = 300000,
                DisableNonEssentialTraffic = false
            };
        }

        currentStep = WizardStep.Configure;
    }

    private void OnBack()
    {
        currentStep = WizardStep.SelectType;
        selectedType = null;
        oauthModel = null;
        apiKeyModel = null;
    }

    private async Task HandleOAuthSubmitAsync()
    {
        if (oauthModel == null)
            return;

        try
        {
            isSaving = true;

            var provider = await ProviderService.CreateOAuthProviderAsync(oauthModel);

            await ToastService.ShowSuccessAsync($"Provider '{provider.Name}' created successfully");

            Navigation.NavigateTo("/settings/llm-providers");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to create provider: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandleApiKeySubmitAsync()
    {
        if (apiKeyModel == null)
            return;

        try
        {
            isSaving = true;

            var provider = await ProviderService.CreateApiKeyProviderAsync(apiKeyModel);

            await ToastService.ShowSuccessAsync($"Provider '{provider.Name}' created successfully");

            Navigation.NavigateTo("/settings/llm-providers");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to create provider: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/llm-providers");
    }

    private string GetDefaultModelForType(LlmProviderType type) => type switch
    {
        LlmProviderType.ZAi => "gpt-4o",
        LlmProviderType.MinimaxM2 => "minimax-m2",
        LlmProviderType.OpenRouter => "anthropic/claude-sonnet-4-5",
        LlmProviderType.TogetherAI => "meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo",
        LlmProviderType.Custom => "",
        _ => ""
    };

    private string GetDefaultApiBaseUrlForType(LlmProviderType type) => type switch
    {
        LlmProviderType.ZAi => "https://api.z.ai",
        LlmProviderType.MinimaxM2 => "https://api.minimax.chat",
        LlmProviderType.OpenRouter => "https://openrouter.ai/api/v1",
        LlmProviderType.TogetherAI => "https://api.together.xyz",
        LlmProviderType.Custom => "",
        _ => ""
    };
}
```

#### Markup (Create.razor)

```razor
@page "/settings/llm-providers/create"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageContainer>
    <PageHeader Icon="bi-plus-circle" Title="Add LLM Provider">
        <Breadcrumbs>
            <li class="breadcrumb-item"><a href="/settings/llm-providers">LLM Providers</a></li>
            <li class="breadcrumb-item active">Add</li>
        </Breadcrumbs>
    </PageHeader>

    <Card Title="@GetWizardTitle()" Icon="bi-robot">
        <div class="wizard-steps mb-4">
            <div class="step @(currentStep == WizardStep.SelectType ? "active" : "")">
                <span class="step-number">1</span>
                <span class="step-title">Select Type</span>
            </div>
            <div class="step-divider"></div>
            <div class="step @(currentStep == WizardStep.Configure ? "active" : "")">
                <span class="step-number">2</span>
                <span class="step-title">Configure</span>
            </div>
        </div>

        @if (currentStep == WizardStep.SelectType)
        {
            <ProviderTypeSelector OnProviderTypeSelected="OnProviderTypeSelected"
                                 OnCancel="HandleCancel" />
        }
        else if (currentStep == WizardStep.Configure)
        {
            @if (selectedType == LlmProviderType.AnthropicNative && oauthModel != null)
            {
                <OAuthProviderForm Model="oauthModel"
                                  IsSaving="isSaving"
                                  OnValidSubmit="HandleOAuthSubmitAsync"
                                  OnBack="OnBack"
                                  OnCancel="HandleCancel" />
            }
            else if (apiKeyModel != null)
            {
                <ApiKeyProviderForm Model="apiKeyModel"
                                   IsSaving="isSaving"
                                   OnValidSubmit="HandleApiKeySubmitAsync"
                                   OnBack="OnBack"
                                   OnCancel="HandleCancel" />
            }
        }
    </Card>
</PageContainer>

@code {
    private string GetWizardTitle() => currentStep switch
    {
        WizardStep.SelectType => "Step 1: Choose Provider Type",
        WizardStep.Configure => $"Step 2: Configure {selectedType}",
        _ => "Add LLM Provider"
    };
}
```

---

## Component Specifications

### 1. ProviderTypeSelector Component

**File:** `/src/PRFactory.Web/Components/Settings/ProviderTypeSelector.razor`

#### Purpose
Display provider type selection cards in a grid layout.

#### Props (Parameters)

```csharp
public partial class ProviderTypeSelector
{
    [Parameter]
    public EventCallback<LlmProviderType> OnProviderTypeSelected { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleSelectAsync(LlmProviderType type)
    {
        await OnProviderTypeSelected.InvokeAsync(type);
    }
}
```

#### Markup (ProviderTypeSelector.razor)

```razor
<div class="provider-type-grid">
    <div class="row g-3">
        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.AnthropicNative))">
                <div class="provider-icon">
                    <i class="bi bi-lock-fill text-primary"></i>
                </div>
                <h5 class="provider-name">Anthropic Native</h5>
                <p class="provider-description">Official Anthropic API with OAuth</p>
                <span class="badge bg-primary">OAuth</span>
            </div>
        </div>

        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.ZAi))">
                <div class="provider-icon">
                    <i class="bi bi-globe text-info"></i>
                </div>
                <h5 class="provider-name">Z.ai Unified API</h5>
                <p class="provider-description">Access 100+ models through one API</p>
                <span class="badge bg-info">API Key</span>
            </div>
        </div>

        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.MinimaxM2))">
                <div class="provider-icon">
                    <i class="bi bi-stars text-warning"></i>
                </div>
                <h5 class="provider-name">Minimax M2</h5>
                <p class="provider-description">Chinese LLM provider</p>
                <span class="badge bg-warning">API Key</span>
            </div>
        </div>

        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.OpenRouter))">
                <div class="provider-icon">
                    <i class="bi bi-signpost-split text-success"></i>
                </div>
                <h5 class="provider-name">OpenRouter</h5>
                <p class="provider-description">Route to 100+ models</p>
                <span class="badge bg-success">API Key</span>
            </div>
        </div>

        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.TogetherAI))">
                <div class="provider-icon">
                    <i class="bi bi-lightning-charge text-danger"></i>
                </div>
                <h5 class="provider-name">Together AI</h5>
                <p class="provider-description">Fast open-source models</p>
                <span class="badge bg-danger">API Key</span>
            </div>
        </div>

        <div class="col-md-4">
            <div class="provider-card" @onclick="@(() => HandleSelectAsync(LlmProviderType.Custom))">
                <div class="provider-icon">
                    <i class="bi bi-gear-fill text-secondary"></i>
                </div>
                <h5 class="provider-name">Custom Provider</h5>
                <p class="provider-description">Self-hosted or custom API</p>
                <span class="badge bg-secondary">API Key</span>
            </div>
        </div>
    </div>
</div>

<div class="mt-4 d-flex gap-2 justify-content-end">
    <button type="button" class="btn btn-secondary" @onclick="OnCancel">
        Cancel
    </button>
</div>

<style>
    .provider-card {
        border: 2px solid #e0e0e0;
        border-radius: 8px;
        padding: 20px;
        text-align: center;
        cursor: pointer;
        transition: all 0.2s;
        height: 100%;
    }

    .provider-card:hover {
        border-color: var(--bs-primary);
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        transform: translateY(-2px);
    }

    .provider-icon {
        font-size: 48px;
        margin-bottom: 15px;
    }

    .provider-name {
        font-size: 18px;
        font-weight: bold;
        margin-bottom: 10px;
    }

    .provider-description {
        font-size: 14px;
        color: #666;
        margin-bottom: 15px;
    }
</style>
```

---

### 2. ApiKeyProviderForm Component

**File:** `/src/PRFactory.Web/Components/Settings/ApiKeyProviderForm.razor`

#### Purpose
Form for configuring API key-based LLM providers (Z.ai, Minimax, OpenRouter, Together, Custom).

#### Props (Parameters)

```csharp
public partial class ApiKeyProviderForm
{
    [Parameter, EditorRequired]
    public CreateApiKeyProviderDto Model { get; set; } = null!;

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private EditContext? editContext;
    private ValidationMessageStore? messageStore;
    private bool isTesting = false;
    private ConnectionTestResult? testResult;

    protected override void OnInitialized()
    {
        editContext = new EditContext(Model);
        messageStore = new ValidationMessageStore(editContext);
    }

    private async Task HandleTestConnectionAsync()
    {
        if (editContext == null || !editContext.Validate())
        {
            await ToastService.ShowErrorAsync("Please fill in all required fields before testing connection");
            return;
        }

        try
        {
            isTesting = true;
            testResult = null;

            testResult = await ProviderService.TestConnectionAsync(new ConnectionTestDto
            {
                ProviderType = Model.ProviderType,
                ApiKey = Model.ApiKey,
                ApiBaseUrl = Model.ApiBaseUrl,
                DefaultModel = Model.DefaultModel
            });

            if (testResult.Success)
            {
                await ToastService.ShowSuccessAsync("Connection test successful!");
            }
            else
            {
                await ToastService.ShowErrorAsync($"Connection test failed: {testResult.Message}");
            }
        }
        catch (Exception ex)
        {
            testResult = new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message
            };
            await ToastService.ShowErrorAsync($"Connection test error: {ex.Message}");
        }
        finally
        {
            isTesting = false;
        }
    }
}
```

#### Markup (ApiKeyProviderForm.razor)

```razor
<EditForm EditContext="editContext" OnValidSubmit="OnValidSubmit">
    <DataAnnotationsValidator />

    @* Provider Name *@
    <FormTextField Label="Provider Name"
                  @bind-Value="Model.Name"
                  Placeholder="Production Z.ai"
                  Required="true"
                  HelpText="A friendly name for this provider" />

    @* API Key *@
    <FormTextField Label="API Key"
                  @bind-Value="Model.ApiKey"
                  Type="password"
                  Placeholder="sk-xxxxxxxxxxxxxxxxxxxx"
                  Required="true"
                  HelpText="Your API key from the provider" />

    @* API Base URL *@
    @if (Model.ProviderType == LlmProviderType.Custom)
    {
        <FormTextField Label="API Base URL"
                      @bind-Value="Model.ApiBaseUrl"
                      Placeholder="https://api.example.com/v1"
                      Required="true"
                      HelpText="Base URL for the API endpoint" />
    }
    else
    {
        <div class="mb-3">
            <label class="form-label">API Base URL</label>
            <input type="text" class="form-control" value="@Model.ApiBaseUrl" readonly disabled />
            <small class="form-text text-muted">Default URL for @Model.ProviderType</small>
        </div>
    }

    @* Default Model *@
    <FormTextField Label="Default Model"
                  @bind-Value="Model.DefaultModel"
                  Placeholder="gpt-4o"
                  Required="true"
                  HelpText="Default model to use for requests" />

    @* Timeout *@
    <FormTextField Label="Timeout (milliseconds)"
                  @bind-Value="Model.TimeoutMs"
                  Type="number"
                  Placeholder="300000"
                  Required="true"
                  HelpText="Request timeout in milliseconds (default: 300000 = 5 minutes)" />

    @* Model Overrides *@
    <ModelOverridesEditor @bind-Value="Model.ModelOverrides"
                         HelpText="Map generic model names to provider-specific names (optional)" />

    @* Disable Non-Essential Traffic *@
    <div class="form-check mb-3">
        <input class="form-check-input" type="checkbox" id="disableNonEssentialTraffic"
               @bind="Model.DisableNonEssentialTraffic" />
        <label class="form-check-label" for="disableNonEssentialTraffic">
            Disable Non-Essential Traffic
        </label>
        <small class="form-text text-muted d-block">
            Skip non-essential API calls to reduce costs
        </small>
    </div>

    @* Test Result Display *@
    @if (testResult != null)
    {
        <div class="alert @(testResult.Success ? "alert-success" : "alert-danger")">
            <i class="bi @(testResult.Success ? "bi-check-circle" : "bi-x-circle") me-2"></i>
            @testResult.Message

            @if (!string.IsNullOrEmpty(testResult.Details))
            {
                <hr />
                <small>@testResult.Details</small>
            }
        </div>
    }

    @* Action Buttons *@
    <div class="d-flex gap-2">
        <LoadingButton Text="Test Connection"
                      Icon="bi-plug"
                      OnClick="HandleTestConnectionAsync"
                      IsLoading="isTesting"
                      LoadingText="Testing..."
                      Class="btn-secondary" />

        <LoadingButton Text="Create Provider"
                      Icon="bi-plus-circle"
                      Type="submit"
                      IsLoading="IsSaving"
                      LoadingText="Creating..."
                      Class="btn-primary" />

        <button type="button" class="btn btn-secondary" @onclick="OnBack">
            Back
        </button>

        <button type="button" class="btn btn-secondary" @onclick="OnCancel">
            Cancel
        </button>
    </div>
</EditForm>
```

---

### 3. ModelOverridesEditor Component

**File:** `/src/PRFactory.Web/Components/Settings/ModelOverridesEditor.razor`

#### Purpose
JSON editor for configuring model name mappings.

#### Props (Parameters)

```csharp
public partial class ModelOverridesEditor
{
    [Parameter]
    public Dictionary<string, string>? Value { get; set; }

    [Parameter]
    public EventCallback<Dictionary<string, string>?> ValueChanged { get; set; }

    [Parameter]
    public string? HelpText { get; set; }

    private string jsonText = string.Empty;
    private string? validationError = null;

    protected override void OnInitialized()
    {
        if (Value != null && Value.Any())
        {
            jsonText = JsonSerializer.Serialize(Value, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        else
        {
            jsonText = "{\n  \"claude-3-5-sonnet\": \"claude-sonnet-3-5-20250101\"\n}";
        }
    }

    private async Task OnJsonTextChanged(ChangeEventArgs e)
    {
        jsonText = e.Value?.ToString() ?? string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                await ValueChanged.InvokeAsync(null);
                validationError = null;
                return;
            }

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);

            await ValueChanged.InvokeAsync(parsed);

            validationError = null;
        }
        catch (JsonException ex)
        {
            validationError = $"Invalid JSON: {ex.Message}";
        }
    }
}
```

#### Markup (ModelOverridesEditor.razor)

```razor
<div class="mb-3">
    <label class="form-label">
        Model Overrides (JSON)
        <ContextualHelp Content="Map generic model names to provider-specific names. Example: { 'gpt-4': 'gpt-4-turbo-2024-04-09' }" />
    </label>

    <textarea class="form-control font-monospace"
              rows="8"
              @bind="jsonText"
              @bind:event="oninput"
              @onchange="OnJsonTextChanged"
              placeholder='{ "model-name": "provider-specific-name" }'></textarea>

    @if (!string.IsNullOrEmpty(HelpText))
    {
        <small class="form-text text-muted">@HelpText</small>
    }

    @if (!string.IsNullOrEmpty(validationError))
    {
        <div class="invalid-feedback d-block">
            @validationError
        </div>
    }

    <div class="mt-2">
        <small class="text-muted">
            <strong>Example:</strong>
            <pre class="bg-light p-2 rounded"><code>{
  "claude-3-5-sonnet": "claude-sonnet-3-5-20250101",
  "gpt-4": "gpt-4-turbo-2024-04-09"
}</code></pre>
        </small>
    </div>
</div>
```

---

(Continued in next response due to length constraints...)

## Implementation Checklist

### Week 1: Pages & Core Components

- [ ] **Day 1-2: LLM Provider List Page**
  - [ ] Create `Index.razor` and `Index.razor.cs`
  - [ ] Implement data loading from `ITenantLlmProviderService`
  - [ ] Add search and filter functionality
  - [ ] Create `LlmProviderListItem` component
  - [ ] Add action handlers (edit, delete, set default, test connection)
  - [ ] Write unit tests for Index page

- [ ] **Day 3-4: Create Provider Wizard (Step 1)**
  - [ ] Create `Create.razor` and `Create.razor.cs`
  - [ ] Create `ProviderTypeSelector` component
  - [ ] Implement wizard state management
  - [ ] Write unit tests for wizard flow

- [ ] **Day 5: Create Provider Wizard (Step 2)**
  - [ ] Create `ApiKeyProviderForm` component
  - [ ] Create `ModelOverridesEditor` component
  - [ ] Implement connection testing
  - [ ] Add form submission logic
  - [ ] Write unit tests for forms

### Week 2: OAuth, Testing & Polish

- [ ] **Day 6: OAuth Provider Form**
  - [ ] Create `OAuthProviderForm` component
  - [ ] Implement OAuth flow
  - [ ] Handle OAuth callback
  - [ ] Write unit tests for OAuth flow

- [ ] **Day 7: Edit Provider Page**
  - [ ] Create `Edit.razor` and `Edit.razor.cs`
  - [ ] Reuse form components
  - [ ] Handle pre-population
  - [ ] Write unit tests for Edit page

- [ ] **Day 8: Provider Detail Page**
  - [ ] Create `Detail.razor` and `Detail.razor.cs`
  - [ ] Create `LlmProviderStatistics` component
  - [ ] Write unit tests for Detail page

- [ ] **Day 9: Integration Testing**
  - [ ] Write integration tests for service layer
  - [ ] Test RBAC enforcement
  - [ ] Test encryption/decryption flow

- [ ] **Day 10: Polish & Bug Fixes**
  - [ ] Fix any failing tests
  - [ ] Improve error messages
  - [ ] Add loading states
  - [ ] Code review and refactoring

---

## UI Mockups

### LLM Provider List Page

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > LLM Providers                                  │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🤖 LLM Providers                              [+ Add Provider]        │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ Search: [_____________] 🔍   Type: [All ▾]  ☑ Active only       │ │
│  │                                                                   │ │
│  │ ┌─────────────────────────────────────────────────────────────┐ │ │
│  │ │ Name        Type     Auth    Model      Default  Status    │ │ │
│  │ ├─────────────────────────────────────────────────────────────┤ │ │
│  │ │ Production  Claude   OAuth   Sonnet4.5  ⭐      Active [⚙️]│ │ │
│  │ │ Z.ai API    Z.ai     APIKey  GPT-4o              Active [⚙️]│ │ │
│  │ │ Dev Test    Minimax  APIKey  M2                  Active [⚙️]│ │ │
│  │ └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                   │ │
│  │ Showing 3 of 3 providers                                          │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  Actions: [👁 View] [✏️ Edit] [⭐ Set Default] [🔌 Test] [🗑️ Delete] │
└────────────────────────────────────────────────────────────────────────┘
```

### Create Provider Wizard - Step 1

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️] > [🤖 LLM Providers] > Add                                 │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ➕ Add LLM Provider                                                   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ 🤖 Step 1: Choose Provider Type                                  │ │
│  │                                                                   │ │
│  │  Steps: [1 Select Type] ──── [2 Configure]                       │ │
│  │                                                                   │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐                       │ │
│  │  │ 🔒       │  │ 🌐       │  │ ⭐       │                       │ │
│  │  │ Anthropic│  │ Z.ai     │  │ Minimax  │                       │ │
│  │  │ Native   │  │ Unified  │  │ M2       │                       │ │
│  │  │ OAuth    │  │ API Key  │  │ API Key  │                       │ │
│  │  │ [Select] │  │ [Select] │  │ [Select] │                       │ │
│  │  └──────────┘  └──────────┘  └──────────┘                       │ │
│  │                                                                   │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐                       │ │
│  │  │ 🚀       │  │ ⚡       │  │ ⚙️       │                       │ │
│  │  │ OpenRouter│  │ Together │  │ Custom   │                       │ │
│  │  │ 100+ Mdls│  │ AI       │  │ Provider │                       │ │
│  │  │ API Key  │  │ API Key  │  │ API Key  │                       │ │
│  │  │ [Select] │  │ [Select] │  │ [Select] │                       │ │
│  │  └──────────┘  └──────────┘  └──────────┘                       │ │
│  │                                                                   │ │
│  │                                              [Cancel]             │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

### Create Provider Wizard - Step 2 (API Key)

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️] > [🤖 LLM Providers] > Add                                 │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ➕ Add LLM Provider                                                   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ 🤖 Step 2: Configure Z.ai Unified API                            │ │
│  │                                                                   │ │
│  │  Steps: [1 Select Type] ──── [2 Configure]                       │ │
│  │                                                                   │ │
│  │  Provider Name *                                                  │ │
│  │  [Z.ai Production_________________________]                       │ │
│  │                                                                   │ │
│  │  API Key *                                                        │ │
│  │  [••••••••••••••••••••••••••••••••] 👁                          │ │
│  │  ℹ️ Get your API key from https://z.ai/dashboard                │ │
│  │                                                                   │ │
│  │  API Base URL                                                     │ │
│  │  [https://api.z.ai___________________] (disabled)                │ │
│  │  ℹ️ Default URL for Z.ai                                         │ │
│  │                                                                   │ │
│  │  Default Model *                                                  │ │
│  │  [gpt-4o______________________________]                           │ │
│  │  ℹ️ Available: gpt-4o, claude-sonnet-4-5, gemini-2.0-flash      │ │
│  │                                                                   │ │
│  │  Timeout (ms)                                                     │ │
│  │  [300000______________________________]                           │ │
│  │                                                                   │ │
│  │  Model Overrides (JSON)                                           │ │
│  │  ┌──────────────────────────────────────────────────────────┐   │ │
│  │  │ {                                                         │   │ │
│  │  │   "claude-3-5-sonnet": "claude-sonnet-3-5-20250101"      │   │ │
│  │  │ }                                                         │   │ │
│  │  └──────────────────────────────────────────────────────────┘   │ │
│  │  ℹ️ Map generic model names to provider-specific names          │ │
│  │                                                                   │ │
│  │  ☐ Disable Non-Essential Traffic                                 │ │
│  │                                                                   │ │
│  │  [🔌 Test Connection] [➕ Create Provider] [← Back] [Cancel]    │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Author:** AI Planning Assistant
**Status:** Ready for Implementation
