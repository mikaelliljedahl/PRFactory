# Epic 06 - Phase 2: Repository Management UI

**Status:** 📋 Ready for Implementation
**Estimated Effort:** 1-2 weeks (40-80 hours)
**Priority:** P0 - Critical (Blocks customer onboarding)
**Dependencies:** Phase 1 Complete ✅ (Services implemented)

---

## Table of Contents

- [Overview](#overview)
- [Goals & Success Criteria](#goals--success-criteria)
- [Architecture Overview](#architecture-overview)
- [Page Specifications](#page-specifications)
- [Component Specifications](#component-specifications)
- [Service Layer Integration](#service-layer-integration)
- [State Management](#state-management)
- [Security & Authorization](#security--authorization)
- [Testing Strategy](#testing-strategy)
- [Implementation Checklist](#implementation-checklist)
- [UI Mockups](#ui-mockups)

---

## Overview

### What We're Building

A complete repository management UI that allows tenant administrators to:
- View all repositories configured for their tenant
- Add new repositories with connection testing
- Edit repository settings (access tokens, default branch)
- Test repository connections before saving
- Deactivate (soft delete) repositories
- View repository usage statistics

### Why This Matters

**Business Impact:**
- Enables self-service repository configuration (no database access required)
- Unblocks customer onboarding and trials
- Reduces support burden on engineering team
- Validates product with non-technical stakeholders

**Technical Foundation:**
- Establishes patterns for all subsequent admin UI phases
- Demonstrates Blazor Server architecture best practices
- Sets security/RBAC precedents

---

## Goals & Success Criteria

### Must Have (Phase 2)

- [x] **Service Layer Complete** - `RepositoryService` with CRUD operations ✅
- [ ] **Repository List Page** - Display all repositories for current tenant
- [ ] **Add Repository Page** - Form with platform selection and connection testing
- [ ] **Edit Repository Page** - Update repository settings
- [ ] **Repository Detail Page** - View statistics and usage information
- [ ] **Connection Testing** - Validate git credentials before saving
- [ ] **RBAC Enforcement** - Only Owner/Admin can add/edit repositories
- [ ] **Encrypted Credentials** - All access tokens encrypted at rest (AES-256-GCM)
- [ ] **Responsive UI** - Works on desktop and tablet
- [ ] **Error Handling** - Clear error messages for validation and connection failures
- [ ] **80% Test Coverage** - Unit tests for all components and services

### Nice to Have (Future)

- [ ] Bulk repository import (CSV upload)
- [ ] Repository health monitoring dashboard
- [ ] Automatic token expiration detection
- [ ] Repository connection history log
- [ ] Repository usage analytics (PRs created, success rate)

---

## Architecture Overview

### Component Hierarchy

```
/settings/repositories (Route Group)
│
├── Index.razor (Page)
│   ├── RepositoryListItem.razor (Component) ×N
│   ├── EmptyState.razor (UI Component)
│   ├── LoadingSpinner.razor (UI Component)
│   └── Pagination.razor (UI Component)
│
├── Create.razor (Page)
│   └── RepositoryForm.razor (Component)
│       ├── FormTextField.razor (UI Component)
│       ├── FormSelectField.razor (UI Component)
│       ├── LoadingButton.razor (UI Component)
│       └── RepositoryConnectionTest.razor (Component)
│
├── Edit.razor (Page)
│   └── RepositoryForm.razor (Component) [reused]
│
└── Detail.razor (Page)
    ├── Card.razor (UI Component)
    ├── RepositoryStatistics.razor (Component)
    ├── StatusBadge.razor (UI Component)
    └── IconButton.razor (UI Component)
```

### Data Flow

```
User Action (UI)
  ↓
Blazor Page Component (*.razor.cs)
  ↓
IRepositoryService (Injected)
  ↓
RepositoryService (Infrastructure/Application)
  ↓
IRepositoryRepository (Infrastructure/Persistence)
  ↓
ApplicationDbContext (EF Core)
  ↓
SQLite Database
```

### File Structure

```
PRFactory.Web/
├── Pages/
│   └── Settings/
│       └── Repositories/
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
        ├── RepositoryForm.razor
        ├── RepositoryForm.razor.cs
        ├── RepositoryListItem.razor
        ├── RepositoryListItem.razor.cs
        ├── RepositoryConnectionTest.razor
        ├── RepositoryConnectionTest.razor.cs
        ├── RepositoryStatistics.razor
        └── RepositoryStatistics.razor.cs
```

---

## Page Specifications

### 1. Repository List Page (`/settings/repositories`)

**File:** `/src/PRFactory.Web/Pages/Settings/Repositories/Index.razor`

#### Purpose
Display all repositories configured for the current tenant with search, filter, and action capabilities.

#### Route
```csharp
@page "/settings/repositories"
@attribute [Authorize(Roles = "Owner,Admin,Member")]
```

#### Page State (Index.razor.cs)

```csharp
public partial class Index
{
    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private List<RepositoryDto> repositories = new();
    private List<RepositoryDto> filteredRepositories = new();
    private bool isLoading = true;
    private string searchTerm = string.Empty;
    private GitPlatform? selectedPlatform = null;
    private bool showActiveOnly = true;

    private bool canAddRepository;
    private bool canEditRepository;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadRepositoriesAsync()
    {
        try
        {
            isLoading = true;
            repositories = await RepositoryService.GetRepositoriesForTenantAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load repositories: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canAddRepository = currentUser.Role is UserRole.Owner or UserRole.Admin;
        canEditRepository = currentUser.Role is UserRole.Owner or UserRole.Admin;
    }

    private void ApplyFilters()
    {
        filteredRepositories = repositories
            .Where(r => string.IsNullOrEmpty(searchTerm) ||
                       r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(r => selectedPlatform == null || r.GitPlatform == selectedPlatform)
            .Where(r => !showActiveOnly || r.IsActive)
            .OrderBy(r => r.Name)
            .ToList();
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
    }

    private void OnPlatformFilterChanged(GitPlatform? platform)
    {
        selectedPlatform = platform;
        ApplyFilters();
    }

    private void OnShowActiveOnlyChanged(bool value)
    {
        showActiveOnly = value;
        ApplyFilters();
    }

    private void NavigateToCreate()
    {
        if (canAddRepository)
            Navigation.NavigateTo("/settings/repositories/create");
    }

    private void NavigateToEdit(Guid id)
    {
        if (canEditRepository)
            Navigation.NavigateTo($"/settings/repositories/edit/{id}");
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/settings/repositories/{id}");
    }

    private async Task HandleDeleteAsync(Guid id)
    {
        if (!canEditRepository)
        {
            await ToastService.ShowErrorAsync("You don't have permission to delete repositories");
            return;
        }

        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm",
            "Are you sure you want to deactivate this repository? This cannot be undone.");

        if (!confirmed)
            return;

        try
        {
            await RepositoryService.DeleteRepositoryAsync(id);
            await ToastService.ShowSuccessAsync("Repository deactivated successfully");
            await LoadRepositoriesAsync();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to deactivate repository: {ex.Message}");
        }
    }

    private async Task HandleTestConnectionAsync(Guid id)
    {
        try
        {
            var result = await RepositoryService.TestRepositoryConnectionAsync(id);

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
@page "/settings/repositories"
@attribute [Authorize(Roles = "Owner,Admin,Member")]

<PageContainer>
    <PageHeader Icon="bi-folder2" Title="Repositories">
        @if (canAddRepository)
        {
            <IconButton Icon="bi-plus-circle" Text="Add Repository"
                       OnClick="NavigateToCreate" Class="btn-primary" />
        }
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading repositories..." />
    }
    else if (!filteredRepositories.Any())
    {
        @if (repositories.Any())
        {
            <AlertMessage Type="AlertType.Info"
                         Message="No repositories match your filters." />
        }
        else
        {
            <EmptyState Icon="bi-folder2"
                       Title="No repositories configured"
                       Message="Add your first repository to get started.">
                @if (canAddRepository)
                {
                    <LoadingButton Text="Add Repository"
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
                    <FormSelectField Label="Platform"
                                    Value="selectedPlatform"
                                    OnChange="@((e) => OnPlatformFilterChanged(e.Value))">
                        <option value="">All Platforms</option>
                        <option value="@GitPlatform.GitHub">GitHub</option>
                        <option value="@GitPlatform.Bitbucket">Bitbucket</option>
                        <option value="@GitPlatform.AzureDevOps">Azure DevOps</option>
                        <option value="@GitPlatform.GitLab">GitLab</option>
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
                            <th>Platform</th>
                            <th>Clone URL</th>
                            <th>Default Branch</th>
                            <th>Last Accessed</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var repo in filteredRepositories)
                        {
                            <RepositoryListItem Repository="repo"
                                              CanEdit="canEditRepository"
                                              OnEdit="@(() => NavigateToEdit(repo.Id))"
                                              OnDelete="@(() => HandleDeleteAsync(repo.Id))"
                                              OnTestConnection="@(() => HandleTestConnectionAsync(repo.Id))"
                                              OnViewDetails="@(() => NavigateToDetail(repo.Id))" />
                        }
                    </tbody>
                </table>
            </div>

            <div class="mt-3 text-muted">
                Showing @filteredRepositories.Count of @repositories.Count repositories
            </div>
        </Card>
    }
</PageContainer>
```

#### Validation Rules

- User must have Owner or Admin role to add/edit/delete
- Members can view repositories (read-only)
- Search is case-insensitive
- Filters are applied in real-time (no "Apply" button)

---

### 2. Create Repository Page (`/settings/repositories/create`)

**File:** `/src/PRFactory.Web/Pages/Settings/Repositories/Create.razor`

#### Purpose
Allow administrators to add new repositories with connection testing before saving.

#### Route
```csharp
@page "/settings/repositories/create"
@attribute [Authorize(Roles = "Owner,Admin")]
```

#### Page State (Create.razor.cs)

```csharp
public partial class Create
{
    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private CreateRepositoryDto model = new();
    private bool isSaving = false;

    private async Task HandleValidSubmitAsync()
    {
        try
        {
            isSaving = true;

            var repository = await RepositoryService.CreateRepositoryAsync(model);

            await ToastService.ShowSuccessAsync($"Repository '{repository.Name}' created successfully");

            Navigation.NavigateTo("/settings/repositories");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to create repository: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/repositories");
    }
}
```

#### Markup (Create.razor)

```razor
@page "/settings/repositories/create"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageContainer>
    <PageHeader Icon="bi-plus-circle" Title="Add Repository">
        <Breadcrumbs>
            <li class="breadcrumb-item"><a href="/settings/repositories">Repositories</a></li>
            <li class="breadcrumb-item active">Add</li>
        </Breadcrumbs>
    </PageHeader>

    <Card Title="Repository Details" Icon="bi-folder2">
        <RepositoryForm Model="model"
                       IsEdit="false"
                       IsSaving="isSaving"
                       OnValidSubmit="HandleValidSubmitAsync"
                       OnCancel="HandleCancel" />
    </Card>
</PageContainer>
```

---

### 3. Edit Repository Page (`/settings/repositories/edit/{id}`)

**File:** `/src/PRFactory.Web/Pages/Settings/Repositories/Edit.razor`

#### Purpose
Allow administrators to update repository settings, including access tokens and default branch.

#### Route
```csharp
@page "/settings/repositories/edit/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin")]
```

#### Page State (Edit.razor.cs)

```csharp
public partial class Edit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private UpdateRepositoryDto model = new();
    private RepositoryDto? existingRepository;
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryAsync();
    }

    private async Task LoadRepositoryAsync()
    {
        try
        {
            isLoading = true;
            existingRepository = await RepositoryService.GetRepositoryByIdAsync(Id);

            if (existingRepository == null)
            {
                await ToastService.ShowErrorAsync("Repository not found");
                Navigation.NavigateTo("/settings/repositories");
                return;
            }

            // Map to UpdateDto
            model = new UpdateRepositoryDto
            {
                Name = existingRepository.Name,
                DefaultBranch = existingRepository.DefaultBranch,
                AccessToken = string.Empty, // Don't populate for security
                IsActive = existingRepository.IsActive
            };
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load repository: {ex.Message}");
            Navigation.NavigateTo("/settings/repositories");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleValidSubmitAsync()
    {
        try
        {
            isSaving = true;

            await RepositoryService.UpdateRepositoryAsync(Id, model);

            await ToastService.ShowSuccessAsync("Repository updated successfully");

            Navigation.NavigateTo("/settings/repositories");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to update repository: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/repositories");
    }
}
```

#### Markup (Edit.razor)

```razor
@page "/settings/repositories/edit/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageContainer>
    <PageHeader Icon="bi-pencil" Title="Edit Repository">
        <Breadcrumbs>
            <li class="breadcrumb-item"><a href="/settings/repositories">Repositories</a></li>
            <li class="breadcrumb-item active">Edit</li>
        </Breadcrumbs>
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading repository..." />
    }
    else if (existingRepository != null)
    {
        <Card Title="@existingRepository.Name" Icon="bi-folder2">
            <div class="alert alert-info">
                <i class="bi bi-info-circle me-2"></i>
                Platform: <strong>@existingRepository.GitPlatform</strong> (cannot be changed)
            </div>

            <RepositoryForm Model="model"
                           IsEdit="true"
                           GitPlatform="existingRepository.GitPlatform"
                           CloneUrl="existingRepository.CloneUrl"
                           IsSaving="isSaving"
                           OnValidSubmit="HandleValidSubmitAsync"
                           OnCancel="HandleCancel" />
        </Card>
    }
</PageContainer>
```

---

### 4. Repository Detail Page (`/settings/repositories/{id}`)

**File:** `/src/PRFactory.Web/Pages/Settings/Repositories/Detail.razor`

#### Purpose
Display repository details and usage statistics in a read-only view.

#### Route
```csharp
@page "/settings/repositories/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin,Member")]
```

#### Page State (Detail.razor.cs)

```csharp
public partial class Detail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private RepositoryDto? repository;
    private RepositoryStatisticsDto? statistics;
    private bool isLoading = true;
    private bool canEdit;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadRepositoryAsync()
    {
        try
        {
            isLoading = true;

            repository = await RepositoryService.GetRepositoryByIdAsync(Id);

            if (repository == null)
            {
                await ToastService.ShowErrorAsync("Repository not found");
                Navigation.NavigateTo("/settings/repositories");
                return;
            }

            statistics = await RepositoryService.GetRepositoryStatisticsAsync(Id);
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load repository: {ex.Message}");
            Navigation.NavigateTo("/settings/repositories");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canEdit = currentUser.Role is UserRole.Owner or UserRole.Admin;
    }

    private void HandleEdit()
    {
        if (canEdit)
            Navigation.NavigateTo($"/settings/repositories/edit/{Id}");
    }

    private void HandleBack()
    {
        Navigation.NavigateTo("/settings/repositories");
    }
}
```

#### Markup (Detail.razor)

```razor
@page "/settings/repositories/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin,Member")]

<PageContainer>
    @if (isLoading)
    {
        <LoadingSpinner Message="Loading repository details..." />
    }
    else if (repository != null)
    {
        <PageHeader Icon="bi-folder2" Title="@repository.Name">
            <div class="btn-group">
                @if (canEdit)
                {
                    <IconButton Icon="bi-pencil" Text="Edit"
                               OnClick="HandleEdit" Class="btn-primary" />
                }
                <IconButton Icon="bi-arrow-left" Text="Back"
                           OnClick="HandleBack" Class="btn-secondary" />
            </div>
        </PageHeader>

        <div class="row">
            <div class="col-md-8">
                <Card Title="Repository Information" Icon="bi-info-circle">
                    <dl class="row">
                        <dt class="col-sm-3">Platform</dt>
                        <dd class="col-sm-9">
                            <span class="badge bg-secondary">@repository.GitPlatform</span>
                        </dd>

                        <dt class="col-sm-3">Clone URL</dt>
                        <dd class="col-sm-9">
                            <code>@repository.CloneUrl</code>
                        </dd>

                        <dt class="col-sm-3">Default Branch</dt>
                        <dd class="col-sm-9">
                            <code>@repository.DefaultBranch</code>
                        </dd>

                        <dt class="col-sm-3">Status</dt>
                        <dd class="col-sm-9">
                            <StatusBadge Status="@(repository.IsActive ? "Active" : "Inactive")" />
                        </dd>

                        <dt class="col-sm-3">Created</dt>
                        <dd class="col-sm-9">
                            <RelativeTime Timestamp="repository.CreatedAt" />
                        </dd>

                        @if (repository.LastAccessedAt.HasValue)
                        {
                            <dt class="col-sm-3">Last Accessed</dt>
                            <dd class="col-sm-9">
                                <RelativeTime Timestamp="repository.LastAccessedAt.Value" />
                            </dd>
                        }
                    </dl>
                </Card>
            </div>

            <div class="col-md-4">
                @if (statistics != null)
                {
                    <RepositoryStatistics Statistics="statistics" />
                }
            </div>
        </div>
    }
</PageContainer>
```

---

## Component Specifications

### 1. RepositoryForm Component

**File:** `/src/PRFactory.Web/Components/Settings/RepositoryForm.razor`

#### Purpose
Reusable form component for both Create and Edit pages with connection testing capability.

#### Props (Parameters)

```csharp
public partial class RepositoryForm
{
    [Parameter, EditorRequired]
    public object Model { get; set; } = null!; // CreateRepositoryDto or UpdateRepositoryDto

    [Parameter]
    public bool IsEdit { get; set; }

    [Parameter]
    public GitPlatform? GitPlatform { get; set; } // For edit mode (read-only)

    [Parameter]
    public string? CloneUrl { get; set; } // For edit mode (read-only)

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;
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

            // Create temporary test DTO
            var testDto = Model switch
            {
                CreateRepositoryDto create => new ConnectionTestDto
                {
                    GitPlatform = create.GitPlatform,
                    CloneUrl = create.CloneUrl,
                    AccessToken = create.AccessToken,
                    DefaultBranch = create.DefaultBranch
                },
                UpdateRepositoryDto update => new ConnectionTestDto
                {
                    GitPlatform = GitPlatform!.Value,
                    CloneUrl = CloneUrl!,
                    AccessToken = update.AccessToken,
                    DefaultBranch = update.DefaultBranch
                },
                _ => throw new InvalidOperationException("Invalid model type")
            };

            testResult = await RepositoryService.TestConnectionAsync(testDto);

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

#### Markup (RepositoryForm.razor)

```razor
<EditForm EditContext="editContext" OnValidSubmit="OnValidSubmit">
    <DataAnnotationsValidator />

    @* Repository Name *@
    <FormTextField Label="Repository Name"
                  @bind-Value="Model.Name"
                  Placeholder="my-awesome-app"
                  Required="true"
                  HelpText="A friendly name for this repository" />

    @if (!IsEdit)
    {
        @* Git Platform (Create only) *@
        <FormSelectField Label="Git Platform"
                        @bind-Value="Model.GitPlatform"
                        Required="true"
                        HelpText="Select your git hosting platform">
            <option value="">-- Select Platform --</option>
            <option value="@GitPlatform.GitHub">GitHub</option>
            <option value="@GitPlatform.Bitbucket">Bitbucket</option>
            <option value="@GitPlatform.AzureDevOps">Azure DevOps</option>
            <option value="@GitPlatform.GitLab">GitLab</option>
        </FormSelectField>

        @* Clone URL (Create only) *@
        <FormTextField Label="Clone URL"
                      @bind-Value="Model.CloneUrl"
                      Placeholder="https://github.com/owner/repo.git"
                      Required="true"
                      HelpText="HTTPS clone URL for the repository" />
    }
    else
    {
        @* Show read-only values in edit mode *@
        <div class="mb-3">
            <label class="form-label">Clone URL</label>
            <input type="text" class="form-control" value="@CloneUrl" readonly disabled />
            <small class="form-text text-muted">Clone URL cannot be changed</small>
        </div>
    }

    @* Access Token *@
    <FormTextField Label="Access Token"
                  @bind-Value="Model.AccessToken"
                  Type="password"
                  Placeholder="ghp_xxxxxxxxxxxxxxxxxxxx"
                  Required="@(!IsEdit)"
                  HelpText="@(IsEdit ? "Leave blank to keep existing token" : "Personal access token with repo permissions")" />

    @if (IsEdit && string.IsNullOrEmpty(Model.AccessToken))
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle me-2"></i>
            Current token is encrypted and hidden. Enter a new token to update it.
        </div>
    }

    @* Default Branch *@
    <FormTextField Label="Default Branch"
                  @bind-Value="Model.DefaultBranch"
                  Placeholder="main"
                  Required="true"
                  HelpText="Default branch for pull requests" />

    @if (IsEdit)
    {
        @* Active Status (Edit only) *@
        <div class="form-check mb-3">
            <input class="form-check-input" type="checkbox" id="isActive"
                   @bind="Model.IsActive" />
            <label class="form-check-label" for="isActive">
                Active
            </label>
            <small class="form-text text-muted d-block">
                Inactive repositories cannot be used for new tickets
            </small>
        </div>
    }

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

        <LoadingButton Text="@(IsEdit ? "Update Repository" : "Create Repository")"
                      Icon="@(IsEdit ? "bi-save" : "bi-plus-circle")"
                      Type="submit"
                      IsLoading="IsSaving"
                      LoadingText="Saving..."
                      Class="btn-primary" />

        <button type="button" class="btn btn-secondary" @onclick="OnCancel">
            Cancel
        </button>
    </div>
</EditForm>
```

---

### 2. RepositoryListItem Component

**File:** `/src/PRFactory.Web/Components/Settings/RepositoryListItem.razor`

#### Purpose
Single row in the repository table with action buttons.

#### Props (Parameters)

```csharp
public partial class RepositoryListItem
{
    [Parameter, EditorRequired]
    public RepositoryDto Repository { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    [Parameter]
    public EventCallback OnTestConnection { get; set; }

    [Parameter]
    public EventCallback OnViewDetails { get; set; }

    private string TruncatedCloneUrl =>
        Repository.CloneUrl.Length > 40
            ? Repository.CloneUrl.Substring(0, 37) + "..."
            : Repository.CloneUrl;
}
```

#### Markup (RepositoryListItem.razor)

```razor
<tr>
    <td>
        <a href="#" @onclick="OnViewDetails" @onclick:preventDefault>
            @Repository.Name
        </a>
    </td>
    <td>
        <span class="badge bg-@GetPlatformBadgeColor()">
            @Repository.GitPlatform
        </span>
    </td>
    <td>
        <code class="text-truncate" style="max-width: 300px; display: inline-block;">
            @TruncatedCloneUrl
        </code>
    </td>
    <td>
        <code>@Repository.DefaultBranch</code>
    </td>
    <td>
        @if (Repository.LastAccessedAt.HasValue)
        {
            <RelativeTime Timestamp="Repository.LastAccessedAt.Value" />
        }
        else
        {
            <span class="text-muted">Never</span>
        }
    </td>
    <td>
        <StatusBadge Status="@(Repository.IsActive ? "Active" : "Inactive")" />
    </td>
    <td>
        <div class="btn-group btn-group-sm">
            <button type="button" class="btn btn-outline-primary"
                    title="View Details"
                    @onclick="OnViewDetails">
                <i class="bi bi-eye"></i>
            </button>

            @if (CanEdit)
            {
                <button type="button" class="btn btn-outline-secondary"
                        title="Edit"
                        @onclick="OnEdit">
                    <i class="bi bi-pencil"></i>
                </button>

                <button type="button" class="btn btn-outline-info"
                        title="Test Connection"
                        @onclick="OnTestConnection">
                    <i class="bi bi-plug"></i>
                </button>

                <button type="button" class="btn btn-outline-danger"
                        title="Delete"
                        @onclick="OnDelete">
                    <i class="bi bi-trash"></i>
                </button>
            }
        </div>
    </td>
</tr>

@code {
    private string GetPlatformBadgeColor() => Repository.GitPlatform switch
    {
        GitPlatform.GitHub => "dark",
        GitPlatform.Bitbucket => "primary",
        GitPlatform.AzureDevOps => "info",
        GitPlatform.GitLab => "warning",
        _ => "secondary"
    };
}
```

---

### 3. RepositoryConnectionTest Component

**File:** `/src/PRFactory.Web/Components/Settings/RepositoryConnectionTest.razor`

#### Purpose
Modal dialog for testing repository connection with detailed feedback.

#### Props (Parameters)

```csharp
public partial class RepositoryConnectionTest
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    [Parameter, EditorRequired]
    public Guid RepositoryId { get; set; }

    [Inject] private IRepositoryService RepositoryService { get; set; } = null!;

    private bool isTesting = false;
    private ConnectionTestResult? result;

    private async Task RunTestAsync()
    {
        try
        {
            isTesting = true;
            result = null;

            result = await RepositoryService.TestRepositoryConnectionAsync(RepositoryId);
        }
        finally
        {
            isTesting = false;
        }
    }

    private async Task CloseAsync()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
    }
}
```

#### Markup (RepositoryConnectionTest.razor)

```razor
<Modal IsVisible="IsVisible" Title="Test Repository Connection" Size="ModalSize.Large">
    <ModalBody>
        @if (isTesting)
        {
            <LoadingSpinner Message="Testing connection to repository..." />
        }
        else if (result != null)
        {
            <div class="alert @(result.Success ? "alert-success" : "alert-danger")">
                <h5 class="alert-heading">
                    <i class="bi @(result.Success ? "bi-check-circle" : "bi-x-circle") me-2"></i>
                    @(result.Success ? "Connection Successful" : "Connection Failed")
                </h5>
                <p>@result.Message</p>

                @if (!string.IsNullOrEmpty(result.Details))
                {
                    <hr />
                    <pre class="mb-0"><code>@result.Details</code></pre>
                }
            </div>

            @if (result.Success)
            {
                <div class="row">
                    <div class="col-md-6">
                        <dl>
                            <dt>Repository Found</dt>
                            <dd><i class="bi bi-check text-success"></i> Yes</dd>

                            <dt>Default Branch Exists</dt>
                            <dd><i class="bi bi-check text-success"></i> Yes</dd>
                        </dl>
                    </div>
                    <div class="col-md-6">
                        <dl>
                            <dt>Response Time</dt>
                            <dd>@result.ResponseTimeMs ms</dd>

                            <dt>Test Date</dt>
                            <dd><RelativeTime Timestamp="result.TestedAt" /></dd>
                        </dl>
                    </div>
                </div>
            }
        }
        else
        {
            <p>Click "Run Test" to verify connection to this repository.</p>

            <div class="alert alert-info">
                <strong>What this test checks:</strong>
                <ul class="mb-0">
                    <li>Repository exists and is accessible</li>
                    <li>Access token has correct permissions</li>
                    <li>Default branch exists</li>
                    <li>Clone URL is valid</li>
                </ul>
            </div>
        }
    </ModalBody>

    <ModalFooter>
        @if (!isTesting && result == null)
        {
            <LoadingButton Text="Run Test"
                          Icon="bi-play-circle"
                          OnClick="RunTestAsync"
                          Class="btn-primary" />
        }
        else if (result != null)
        {
            <LoadingButton Text="Run Again"
                          Icon="bi-arrow-clockwise"
                          OnClick="RunTestAsync"
                          Class="btn-secondary" />
        }

        <button type="button" class="btn btn-secondary" @onclick="CloseAsync">
            Close
        </button>
    </ModalFooter>
</Modal>
```

---

### 4. RepositoryStatistics Component

**File:** `/src/PRFactory.Web/Components/Settings/RepositoryStatistics.razor`

#### Purpose
Display repository usage statistics in a card format.

#### Props (Parameters)

```csharp
public partial class RepositoryStatistics
{
    [Parameter, EditorRequired]
    public RepositoryStatisticsDto Statistics { get; set; } = null!;
}
```

#### Markup (RepositoryStatistics.razor)

```razor
<Card Title="Usage Statistics" Icon="bi-bar-chart">
    <div class="row text-center">
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.TotalTickets</div>
            <small class="text-muted">Total Tickets</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.ActiveTickets</div>
            <small class="text-muted">Active Tickets</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.TotalPullRequests</div>
            <small class="text-muted">Pull Requests</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.SuccessRate%</div>
            <small class="text-muted">Success Rate</small>
        </div>
    </div>

    <hr />

    <dl class="mb-0">
        <dt>First Used</dt>
        <dd><RelativeTime Timestamp="Statistics.FirstAccessedAt" /></dd>

        @if (Statistics.LastAccessedAt.HasValue)
        {
            <dt>Last Accessed</dt>
            <dd><RelativeTime Timestamp="Statistics.LastAccessedAt.Value" /></dd>
        }

        <dt>Average Workflow Duration</dt>
        <dd>@Statistics.AverageWorkflowDurationMinutes minutes</dd>
    </dl>
</Card>
```

---

## Service Layer Integration

### IRepositoryService Interface

**File:** `/src/PRFactory.Core/Application/Services/IRepositoryService.cs`

```csharp
public interface IRepositoryService
{
    // List & Retrieve
    Task<List<RepositoryDto>> GetRepositoriesForTenantAsync(CancellationToken cancellationToken = default);
    Task<RepositoryDto?> GetRepositoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Create & Update
    Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto, CancellationToken cancellationToken = default);
    Task UpdateRepositoryAsync(Guid id, UpdateRepositoryDto dto, CancellationToken cancellationToken = default);

    // Delete (soft delete)
    Task DeleteRepositoryAsync(Guid id, CancellationToken cancellationToken = default);

    // Connection Testing
    Task<ConnectionTestResult> TestRepositoryConnectionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestConnectionAsync(ConnectionTestDto dto, CancellationToken cancellationToken = default);

    // Statistics
    Task<RepositoryStatisticsDto> GetRepositoryStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### DTOs

**File:** `/src/PRFactory.Core/Application/DTOs/RepositoryDto.cs`

```csharp
public class RepositoryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GitPlatform GitPlatform { get; set; }
    public string CloneUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}

public class CreateRepositoryDto
{
    [Required(ErrorMessage = "Repository name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Git platform is required")]
    public GitPlatform GitPlatform { get; set; }

    [Required(ErrorMessage = "Clone URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string CloneUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Access token is required")]
    [MinLength(10, ErrorMessage = "Access token must be at least 10 characters")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Default branch is required")]
    [MaxLength(50)]
    public string DefaultBranch { get; set; } = "main";
}

public class UpdateRepositoryDto
{
    [Required(ErrorMessage = "Repository name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AccessToken { get; set; } // Optional - only if updating token

    [Required(ErrorMessage = "Default branch is required")]
    [MaxLength(50)]
    public string DefaultBranch { get; set; } = "main";

    public bool IsActive { get; set; } = true;
}

public class ConnectionTestDto
{
    public GitPlatform GitPlatform { get; set; }
    public string CloneUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int ResponseTimeMs { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

public class RepositoryStatisticsDto
{
    public int TotalTickets { get; set; }
    public int ActiveTickets { get; set; }
    public int TotalPullRequests { get; set; }
    public int SuccessRate { get; set; }
    public DateTime FirstAccessedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AverageWorkflowDurationMinutes { get; set; }
}
```

---

## State Management

### Component State

All state is managed locally in Blazor components using C# properties. No global state management library (Redux, Fluxor) is needed for Phase 2.

**Pattern:**
- Page components manage their own state (loading, errors, data)
- Child components receive state via `[Parameter]` props
- Child components emit events via `EventCallback` props
- Parent components handle events and update state

**Example:**
```csharp
// Parent (Index.razor.cs)
private List<RepositoryDto> repositories = new();
private bool isLoading = true;

// Child component (RepositoryListItem.razor)
[Parameter] public RepositoryDto Repository { get; set; }
[Parameter] public EventCallback OnEdit { get; set; }

// Child emits event
<button @onclick="OnEdit">Edit</button>

// Parent handles event
<RepositoryListItem OnEdit="@(() => HandleEdit(repo.Id))" />

private void HandleEdit(Guid id)
{
    // Update parent state
    Navigation.NavigateTo($"/settings/repositories/edit/{id}");
}
```

---

## Security & Authorization

### RBAC Rules

| Role | List | View Details | Add | Edit | Delete | Test Connection |
|------|------|--------------|-----|------|--------|-----------------|
| **Owner** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Admin** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Member** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Viewer** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

### Implementation

**Page-Level Authorization:**
```csharp
// Read-only access
@attribute [Authorize(Roles = "Owner,Admin,Member")]

// Write access
@attribute [Authorize(Roles = "Owner,Admin")]
```

**Service-Level Authorization:**
```csharp
public async Task CreateRepositoryAsync(CreateRepositoryDto dto)
{
    // Verify user has permission
    var currentUser = await _currentUserService.GetCurrentUserAsync();

    if (currentUser.Role is not (UserRole.Owner or UserRole.Admin))
    {
        throw new UnauthorizedAccessException(
            "Only Owners and Admins can create repositories");
    }

    // Verify tenant isolation
    var tenantId = await _currentUserService.GetCurrentTenantIdAsync();

    // Continue with creation...
}
```

### Credential Encryption

All access tokens are encrypted using AES-256-GCM before storage:

```csharp
// Service layer (RepositoryService.cs)
public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto)
{
    // 1. Encrypt access token
    var encryptedToken = _encryptionService.Encrypt(dto.AccessToken);

    // 2. Create entity with encrypted token
    var repository = Repository.Create(
        tenantId,
        dto.Name,
        dto.GitPlatform,
        dto.CloneUrl,
        encryptedToken,  // ← Encrypted
        dto.DefaultBranch);

    // 3. Save to database
    await _repositoryRepo.AddAsync(repository);

    return MapToDto(repository);
}

// For connection testing, decrypt temporarily
public async Task<ConnectionTestResult> TestConnectionAsync(Guid id)
{
    var repository = await _repositoryRepo.GetByIdAsync(id);

    // Decrypt for testing (never return decrypted token to UI)
    var decryptedToken = _encryptionService.Decrypt(repository.AccessToken);

    // Test connection with decrypted token
    var result = await _gitService.TestConnectionAsync(
        repository.CloneUrl,
        decryptedToken,
        repository.DefaultBranch);

    return result;
}
```

---

## Testing Strategy

### Unit Tests (Target: 80% coverage)

#### Page Tests

**File:** `/tests/PRFactory.Tests/Blazor/Pages/Settings/Repositories/IndexTests.cs`

```csharp
public class RepositoryIndexPageTests : PageTestBase
{
    [Fact]
    public async Task OnInitialized_LoadsRepositories()
    {
        // Arrange
        var repositories = RepositoryDtoBuilder.CreateList(3);
        MockRepositoryService
            .Setup(s => s.GetRepositoriesForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<Repositories.Index>();
        await Task.Delay(100); // Wait for async load

        // Assert
        cut.FindAll("tr").Count.Should().Be(3); // 3 data rows
        MockRepositoryService.Verify(s => s.GetRepositoriesForTenantAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchFilter_FiltersRepositoriesByName()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            RepositoryDtoBuilder.Create().WithName("my-app").Build(),
            RepositoryDtoBuilder.Create().WithName("backend-api").Build(),
            RepositoryDtoBuilder.Create().WithName("frontend-ui").Build()
        };

        MockRepositoryService
            .Setup(s => s.GetRepositoriesForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        var cut = RenderComponent<Repositories.Index>();
        await Task.Delay(100);

        // Act
        var searchInput = cut.Find("input[placeholder='Search by name...']");
        searchInput.Change("backend");

        // Assert
        cut.FindAll("tr").Count.Should().Be(1);
        cut.Find("tr").TextContent.Should().Contain("backend-api");
    }

    [Fact]
    public async Task AddRepositoryButton_NavigatesToCreatePage()
    {
        // Arrange
        MockRepositoryService
            .Setup(s => s.GetRepositoriesForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RepositoryDto>());

        MockCurrentUserService
            .Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync(UserDtoBuilder.Create().WithRole(UserRole.Admin).Build());

        var cut = RenderComponent<Repositories.Index>();
        await Task.Delay(100);

        // Act
        var addButton = cut.Find("button:contains('Add Repository')");
        addButton.Click();

        // Assert
        MockNavigationManager.Uri.Should().EndWith("/settings/repositories/create");
    }

    [Fact]
    public async Task DeleteRepository_ShowsConfirmation_ThenDeletes()
    {
        // Arrange
        var repository = RepositoryDtoBuilder.Create().Build();

        MockRepositoryService
            .Setup(s => s.GetRepositoriesForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RepositoryDto> { repository });

        MockRepositoryService
            .Setup(s => s.DeleteRepositoryAsync(repository.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock window.confirm to return true
        MockJSRuntime.Setup<bool>("confirm", It.IsAny<string[]>())
            .ReturnsAsync(true);

        var cut = RenderComponent<Repositories.Index>();
        await Task.Delay(100);

        // Act
        var deleteButton = cut.Find("button[title='Delete']");
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        MockRepositoryService.Verify(s => s.DeleteRepositoryAsync(
            repository.Id, It.IsAny<CancellationToken>()), Times.Once);

        MockToastService.Verify(s => s.ShowSuccessAsync(
            "Repository deactivated successfully"), Times.Once);
    }
}
```

#### Component Tests

**File:** `/tests/PRFactory.Tests/Blazor/Components/Settings/RepositoryFormTests.cs`

```csharp
public class RepositoryFormTests : ComponentTestBase
{
    [Fact]
    public void Render_CreateMode_ShowsAllFields()
    {
        // Arrange
        var model = new CreateRepositoryDto();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEdit, false));

        // Assert
        cut.Find("input[placeholder='my-awesome-app']").Should().NotBeNull();
        cut.Find("select").Options.Count.Should().Be(5); // 4 platforms + placeholder
        cut.Find("input[placeholder='https://github.com/owner/repo.git']").Should().NotBeNull();
        cut.Find("input[type='password']").Should().NotBeNull();
        cut.Find("input[placeholder='main']").Should().NotBeNull();
        cut.Find("button:contains('Create Repository')").Should().NotBeNull();
    }

    [Fact]
    public void Render_EditMode_HidesImmutableFields()
    {
        // Arrange
        var model = new UpdateRepositoryDto();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEdit, true)
            .Add(p => p.GitPlatform, GitPlatform.GitHub)
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git"));

        // Assert
        cut.FindAll("select").Should().BeEmpty(); // No platform selector
        cut.Find("input[value='https://github.com/test/repo.git'][readonly]").Should().NotBeNull();
        cut.Find("button:contains('Update Repository')").Should().NotBeNull();
    }

    [Fact]
    public async Task TestConnection_WithValidData_ShowsSuccess()
    {
        // Arrange
        var model = new CreateRepositoryDto
        {
            Name = "Test Repo",
            GitPlatform = GitPlatform.GitHub,
            CloneUrl = "https://github.com/test/repo.git",
            AccessToken = "ghp_test123",
            DefaultBranch = "main"
        };

        MockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<ConnectionTestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResult
            {
                Success = true,
                Message = "Connection successful"
            });

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEdit, false));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await cut.InvokeAsync(() => testButton.Click());

        // Assert
        cut.Find(".alert-success").TextContent.Should().Contain("Connection successful");
        MockRepositoryService.Verify(s => s.TestConnectionAsync(
            It.IsAny<ConnectionTestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Submit_WithValidData_CallsOnValidSubmit()
    {
        // Arrange
        var model = new CreateRepositoryDto
        {
            Name = "Test Repo",
            GitPlatform = GitPlatform.GitHub,
            CloneUrl = "https://github.com/test/repo.git",
            AccessToken = "ghp_test123",
            DefaultBranch = "main"
        };

        var onValidSubmitCalled = false;

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEdit, false)
            .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => onValidSubmitCalled = true)));

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        onValidSubmitCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Submit_WithMissingRequiredFields_ShowsValidationErrors()
    {
        // Arrange
        var model = new CreateRepositoryDto(); // Empty model

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEdit, false));

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        cut.FindAll(".validation-message").Count.Should().BeGreaterThan(0);
    }
}
```

#### Service Tests

**File:** `/tests/PRFactory.Tests/Infrastructure/Application/RepositoryServiceTests.cs`

```csharp
public class RepositoryServiceTests
{
    private readonly Mock<IRepositoryRepository> _mockRepo;
    private readonly Mock<IAesEncryptionService> _mockEncryption;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<ILocalGitService> _mockGitService;
    private readonly RepositoryService _service;

    public RepositoryServiceTests()
    {
        _mockRepo = new Mock<IRepositoryRepository>();
        _mockEncryption = new Mock<IAesEncryptionService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockGitService = new Mock<ILocalGitService>();

        _service = new RepositoryService(
            _mockRepo.Object,
            _mockEncryption.Object,
            _mockCurrentUser.Object,
            _mockGitService.Object);
    }

    [Fact]
    public async Task CreateRepositoryAsync_EncryptsAccessToken()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var dto = new CreateRepositoryDto
        {
            Name = "Test Repo",
            GitPlatform = GitPlatform.GitHub,
            CloneUrl = "https://github.com/test/repo.git",
            AccessToken = "plaintext_token",
            DefaultBranch = "main"
        };

        _mockCurrentUser
            .Setup(s => s.GetCurrentTenantIdAsync())
            .ReturnsAsync(tenantId);

        _mockEncryption
            .Setup(s => s.Encrypt("plaintext_token"))
            .Returns("encrypted_token");

        Repository? savedRepository = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Repository>(), It.IsAny<CancellationToken>()))
            .Callback<Repository, CancellationToken>((repo, _) => savedRepository = repo)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateRepositoryAsync(dto);

        // Assert
        Assert.NotNull(savedRepository);
        Assert.Equal("encrypted_token", savedRepository.AccessToken);

        _mockEncryption.Verify(s => s.Encrypt("plaintext_token"), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_DecryptsTokenTemporarily()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new Repository
        {
            Id = repositoryId,
            CloneUrl = "https://github.com/test/repo.git",
            AccessToken = "encrypted_token",
            DefaultBranch = "main",
            GitPlatform = GitPlatform.GitHub
        };

        _mockRepo
            .Setup(r => r.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        _mockEncryption
            .Setup(s => s.Decrypt("encrypted_token"))
            .Returns("plaintext_token");

        _mockGitService
            .Setup(s => s.TestConnectionAsync(
                repository.CloneUrl,
                "plaintext_token",
                repository.DefaultBranch))
            .ReturnsAsync(new ConnectionTestResult { Success = true });

        // Act
        var result = await _service.TestRepositoryConnectionAsync(repositoryId);

        // Assert
        Assert.True(result.Success);

        _mockEncryption.Verify(s => s.Decrypt("encrypted_token"), Times.Once);
        _mockGitService.Verify(s => s.TestConnectionAsync(
            repository.CloneUrl,
            "plaintext_token",
            repository.DefaultBranch), Times.Once);
    }

    [Fact]
    public async Task GetRepositoriesForTenantAsync_ReturnsOnlyTenantRepositories()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var repositories = new List<Repository>
        {
            new Repository { TenantId = tenantId, Name = "Repo 1" },
            new Repository { TenantId = tenantId, Name = "Repo 2" }
        };

        _mockCurrentUser
            .Setup(s => s.GetCurrentTenantIdAsync())
            .ReturnsAsync(tenantId);

        _mockRepo
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var result = await _service.GetRepositoriesForTenantAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(tenantId, r.TenantId));
    }
}
```

### Integration Tests

**File:** `/tests/PRFactory.Tests/Integration/RepositoryManagementIntegrationTests.cs`

```csharp
public class RepositoryManagementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RepositoryManagementIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FullWorkflow_CreateEditDeleteRepository()
    {
        // 1. Navigate to repository list
        var indexResponse = await _client.GetAsync("/settings/repositories");
        indexResponse.EnsureSuccessStatusCode();

        // 2. Navigate to create page
        var createResponse = await _client.GetAsync("/settings/repositories/create");
        createResponse.EnsureSuccessStatusCode();

        // 3. Create repository
        var createDto = new CreateRepositoryDto
        {
            Name = "Integration Test Repo",
            GitPlatform = GitPlatform.GitHub,
            CloneUrl = "https://github.com/test/integration-repo.git",
            AccessToken = "test_token_123",
            DefaultBranch = "main"
        };

        var createFormData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", createDto.Name),
            new KeyValuePair<string, string>("GitPlatform", createDto.GitPlatform.ToString()),
            new KeyValuePair<string, string>("CloneUrl", createDto.CloneUrl),
            new KeyValuePair<string, string>("AccessToken", createDto.AccessToken),
            new KeyValuePair<string, string>("DefaultBranch", createDto.DefaultBranch)
        });

        var createSubmitResponse = await _client.PostAsync("/settings/repositories/create", createFormData);
        createSubmitResponse.EnsureSuccessStatusCode();

        // 4. Verify repository appears in list
        var listResponse = await _client.GetAsync("/settings/repositories");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Integration Test Repo", listContent);

        // 5. Navigate to edit page
        var repositoryId = ExtractRepositoryIdFromContent(listContent);
        var editResponse = await _client.GetAsync($"/settings/repositories/edit/{repositoryId}");
        editResponse.EnsureSuccessStatusCode();

        // 6. Update repository
        var updateDto = new UpdateRepositoryDto
        {
            Name = "Updated Integration Test Repo",
            DefaultBranch = "develop",
            IsActive = true
        };

        var updateFormData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", updateDto.Name),
            new KeyValuePair<string, string>("DefaultBranch", updateDto.DefaultBranch),
            new KeyValuePair<string, string>("IsActive", updateDto.IsActive.ToString())
        });

        var updateSubmitResponse = await _client.PostAsync($"/settings/repositories/edit/{repositoryId}", updateFormData);
        updateSubmitResponse.EnsureSuccessStatusCode();

        // 7. Verify update
        var updatedListResponse = await _client.GetAsync("/settings/repositories");
        var updatedListContent = await updatedListResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Integration Test Repo", updatedListContent);

        // 8. Delete repository
        var deleteResponse = await _client.PostAsync($"/settings/repositories/delete/{repositoryId}", null);
        deleteResponse.EnsureSuccessStatusCode();

        // 9. Verify deletion
        var finalListResponse = await _client.GetAsync("/settings/repositories");
        var finalListContent = await finalListResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Updated Integration Test Repo", finalListContent);
    }

    private Guid ExtractRepositoryIdFromContent(string htmlContent)
    {
        // Parse HTML and extract repository ID from data attribute or href
        // Implementation depends on HTML structure
        throw new NotImplementedException("HTML parsing logic needed");
    }
}
```

### E2E Tests (Playwright/Selenium)

**File:** `/tests/PRFactory.E2ETests/RepositoryManagementE2ETests.cs`

```csharp
public class RepositoryManagementE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
    }

    [Fact]
    public async Task CreateRepository_E2E_WorkflowCompletes()
    {
        // 1. Login
        await _page.GotoAsync("https://localhost:5001/login");
        await _page.FillAsync("input[name='email']", "admin@test.com");
        await _page.FillAsync("input[name='password']", "password123");
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForURLAsync("**/");

        // 2. Navigate to repositories
        await _page.ClickAsync("a[href='/settings/repositories']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 3. Click "Add Repository"
        await _page.ClickAsync("button:text('Add Repository')");
        await _page.WaitForURLAsync("**/settings/repositories/create");

        // 4. Fill form
        await _page.FillAsync("input[placeholder='my-awesome-app']", "E2E Test Repo");
        await _page.SelectOptionAsync("select", "GitHub");
        await _page.FillAsync("input[placeholder*='github.com']", "https://github.com/test/e2e-repo.git");
        await _page.FillAsync("input[type='password']", "ghp_test_token_123");
        await _page.FillAsync("input[placeholder='main']", "main");

        // 5. Test connection
        await _page.ClickAsync("button:text('Test Connection')");
        await _page.WaitForSelectorAsync(".alert-success");

        // 6. Submit form
        await _page.ClickAsync("button:text('Create Repository')");
        await _page.WaitForURLAsync("**/settings/repositories");

        // 7. Verify toast notification
        var toastText = await _page.TextContentAsync(".toast");
        Assert.Contains("created successfully", toastText);

        // 8. Verify repository in list
        var tableContent = await _page.TextContentAsync("table");
        Assert.Contains("E2E Test Repo", tableContent);
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
```

---

## Implementation Checklist

### Week 1: Pages & Core Components

- [ ] **Day 1-2: Repository List Page**
  - [ ] Create `Index.razor` and `Index.razor.cs`
  - [ ] Implement data loading from `IRepositoryService`
  - [ ] Add search and filter functionality
  - [ ] Create `RepositoryListItem` component
  - [ ] Add action handlers (edit, delete, test connection)
  - [ ] Write unit tests for Index page

- [ ] **Day 3-4: Create Repository Page**
  - [ ] Create `Create.razor` and `Create.razor.cs`
  - [ ] Create `RepositoryForm` component with validation
  - [ ] Implement connection testing
  - [ ] Add form submission logic
  - [ ] Write unit tests for Create page and RepositoryForm

- [ ] **Day 5: Edit Repository Page**
  - [ ] Create `Edit.razor` and `Edit.razor.cs`
  - [ ] Reuse `RepositoryForm` component
  - [ ] Handle pre-population of existing data
  - [ ] Implement update logic
  - [ ] Write unit tests for Edit page

### Week 2: Advanced Features & Testing

- [ ] **Day 6: Repository Detail Page**
  - [ ] Create `Detail.razor` and `Detail.razor.cs`
  - [ ] Create `RepositoryStatistics` component
  - [ ] Implement statistics aggregation in service
  - [ ] Write unit tests for Detail page

- [ ] **Day 7: Connection Testing Component**
  - [ ] Create `RepositoryConnectionTest` modal component
  - [ ] Implement detailed connection test results
  - [ ] Add retry functionality
  - [ ] Write unit tests for connection testing

- [ ] **Day 8: Integration Testing**
  - [ ] Write integration tests for service layer
  - [ ] Test RBAC enforcement
  - [ ] Test encryption/decryption flow
  - [ ] Test tenant isolation

- [ ] **Day 9: E2E Testing**
  - [ ] Set up Playwright/Selenium
  - [ ] Write E2E test for create workflow
  - [ ] Write E2E test for edit workflow
  - [ ] Write E2E test for delete workflow

- [ ] **Day 10: Polish & Bug Fixes**
  - [ ] Fix any failing tests
  - [ ] Improve error messages
  - [ ] Add loading states
  - [ ] Verify RBAC enforcement
  - [ ] Code review and refactoring

---

## UI Mockups

### Repository List Page

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠 Home] > [⚙️ Settings] > Repositories                              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  📁 Repositories                               [+ Add Repository]      │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ Search: [_____________] 🔍   Platform: [All ▾]  ☑ Active only   │ │
│  │                                                                   │ │
│  │ ┌─────────────────────────────────────────────────────────────┐ │ │
│  │ │ Name        Platform  Clone URL             Last Access    │ │ │
│  │ ├─────────────────────────────────────────────────────────────┤ │ │
│  │ │ my-app      GitHub    github.com/owner/...  2h ago   [⚙️]  │ │ │
│  │ │ backend-api AzureDevOps dev.azure.com/... 1d ago   [⚙️]  │ │ │
│  │ │ legacy      Bitbucket bitbucket.org/...    3d ago   [⚙️]  │ │ │
│  │ └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                   │ │
│  │ Showing 3 of 5 repositories                                       │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  Actions: [👁 View] [✏️ Edit] [🔌 Test] [🗑️ Delete]                  │
└────────────────────────────────────────────────────────────────────────┘
```

### Create Repository Page

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > [📁 Repositories] > Add                         │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ➕ Add Repository                                                     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ 📁 Repository Details                                            │ │
│  │                                                                   │ │
│  │  Repository Name *                                                │ │
│  │  [my-awesome-app__________________________]                       │ │
│  │  ℹ️ A friendly name for this repository                          │ │
│  │                                                                   │ │
│  │  Git Platform *                                                   │ │
│  │  ( ) GitHub  ( ) Bitbucket  ( ) Azure DevOps  ( ) GitLab        │ │
│  │                                                                   │ │
│  │  Clone URL *                                                      │ │
│  │  [https://github.com/owner/repo.git______]                       │ │
│  │  ℹ️ HTTPS clone URL for the repository                           │ │
│  │                                                                   │ │
│  │  Access Token *                                                   │ │
│  │  [••••••••••••••••••••••••••••••••] 👁                          │ │
│  │  ℹ️ Personal access token with repo permissions                  │ │
│  │                                                                   │ │
│  │  Default Branch *                                                 │ │
│  │  [main___________________________________]                        │ │
│  │  ℹ️ Default branch for pull requests                             │ │
│  │                                                                   │ │
│  │  ┌──────────────────────────────────────────────────────────┐   │ │
│  │  │ ✅ Connection Test Successful                            │   │ │
│  │  │ Repository found and accessible                           │   │ │
│  │  │ Response time: 245ms                                      │   │ │
│  │  └──────────────────────────────────────────────────────────┘   │ │
│  │                                                                   │ │
│  │  [🔌 Test Connection]  [➕ Create Repository]  [Cancel]          │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

### Repository Detail Page

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > [📁 Repositories] > my-app                      │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  📁 my-app                                    [✏️ Edit] [← Back]      │
│                                                                         │
│  ┌────────────────────────────────┐  ┌────────────────────────────┐  │
│  │ ℹ️ Repository Information      │  │ 📊 Usage Statistics        │  │
│  │                                │  │                            │  │
│  │  Platform:      GitHub         │  │      12                    │  │
│  │  Clone URL:     github.com/... │  │  Total Tickets             │  │
│  │  Default Branch: main          │  │                            │  │
│  │  Status:        🟢 Active      │  │       5                    │  │
│  │  Created:       2 weeks ago    │  │  Active Tickets            │  │
│  │  Last Accessed: 2 hours ago    │  │                            │  │
│  │                                │  │      18                    │  │
│  │                                │  │  Pull Requests             │  │
│  │                                │  │                            │  │
│  │                                │  │      94%                   │  │
│  │                                │  │  Success Rate              │  │
│  │                                │  │                            │  │
│  │                                │  │  First Used: 2 weeks ago   │  │
│  │                                │  │  Avg Duration: 24 mins     │  │
│  └────────────────────────────────┘  └────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Acceptance Criteria

### Functional Requirements

- [x] **Service Layer** - All CRUD operations implemented ✅
- [ ] **Repository List** - Display all repositories with search/filter
- [ ] **Create Repository** - Form with all required fields
- [ ] **Edit Repository** - Update existing repository settings
- [ ] **Delete Repository** - Soft delete (deactivate) repositories
- [ ] **Connection Testing** - Test git connection before saving
- [ ] **Repository Details** - View statistics and usage information
- [ ] **Navigation** - Breadcrumbs and back buttons work correctly
- [ ] **Toast Notifications** - Success/error messages display

### Security Requirements

- [ ] **RBAC Enforcement** - Only Owner/Admin can add/edit/delete
- [ ] **Credential Encryption** - All tokens encrypted with AES-256-GCM
- [ ] **Tenant Isolation** - Users only see their tenant's repositories
- [ ] **Authorization Checks** - Service layer validates permissions
- [ ] **Secure Forms** - Access tokens use password input type

### Quality Requirements

- [ ] **Test Coverage** - 80% coverage minimum
- [ ] **No Regressions** - All existing tests pass
- [ ] **Error Handling** - Clear error messages for all failure cases
- [ ] **Loading States** - Spinners shown during async operations
- [ ] **Validation** - Client and server-side validation
- [ ] **Responsive UI** - Works on desktop and tablet
- [ ] **Accessibility** - Keyboard navigation and screen readers work

### Documentation Requirements

- [ ] **Code Comments** - Complex logic documented
- [ ] **Component Props** - Parameters documented
- [ ] **API Documentation** - Service methods documented
- [ ] **Test Documentation** - Test scenarios explained

---

## Next Steps

After Phase 2 completion:

1. **User Acceptance Testing (UAT)**
   - Create test tenant
   - Add sample repositories
   - Test all CRUD operations
   - Verify RBAC enforcement

2. **Documentation Update**
   - Update IMPLEMENTATION_STATUS.md
   - Update ROADMAP.md
   - Create user guide for repository management

3. **Move to Phase 3**
   - Begin LLM Provider Configuration UI
   - Follow similar pattern established in Phase 2

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Author:** AI Planning Assistant
**Status:** Ready for Implementation
